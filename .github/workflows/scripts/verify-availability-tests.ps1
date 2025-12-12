<#
.SYNOPSIS
Verifies that availability tests have published at least one result to Application Insights using metrics.

.DESCRIPTION
Queries the Application Insights metric 'availabilityResults/availabilityPercentage' for a fixed set of
availability tests. Uses a retry mechanism to account for ingestion latency. Prints a final summary table.

.PARAMETER ResourceGroupName
The resource group name containing the Application Insights component.

.PARAMETER AppInsightsName
The name of the Application Insights component.

.EXAMPLE
PS> .\verify-availability-tests.ps1 -ResourceGroupName "rg-track-availability-sdc-cliqc" -AppInsightsName "appi-track-availability-sdc-cliqc" 

Runs the verification with default retry settings and prints a summary.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$AppInsightsName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Constants and configuration
$metricName   = 'availabilityResults/availabilityPercentage'
$resourceType = 'Microsoft.Insights/components'

$testNames = @(
    'Azure Function - API Management SSL Certificate Check',
    'Azure Function - Backend API Status',
    'Logic App Workflow - Backend API Status',
    'Standard Test - API Management SSL Certificate Check',
    'Standard Test - Backend API Status'
)

$maxRetries = 1
$retryIntervalSeconds = 1
$lookbackMinutes = 2

# Tracking: use script start as offset for the lookback window
$startTime = (Get-Date).AddMinutes(-$lookbackMinutes).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

Write-Output 'Starting availability tests verification (metrics method)...'
Write-Output "Checking for results published after $startTime"
Write-Output "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Output ''

function Invoke-WithRetry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Operation,

        [Parameter()]
        [int]$MaxAttempts = 15,

        [Parameter()]
        [int]$DelayInSeconds = 2
    )

    $attempt = 1
    $lastResult = $null

    while ($attempt -le $MaxAttempts) {
        try {
            $lastResult = & $Operation

            if ($lastResult -and $lastResult.Success) {
                Write-Output "Operation succeeded on attempt $attempt"
                return $lastResult
            }

            if ($attempt -eq $MaxAttempts) {
                Write-Output "Operation did not meet success criteria after $MaxAttempts attempts"
                return $lastResult
            }

            Write-Output "Operation not successful (attempt $attempt/$MaxAttempts). Retrying in $DelayInSeconds seconds..."
            Start-Sleep -Seconds $DelayInSeconds
            $attempt++
        }
        catch {
            if ($attempt -eq $MaxAttempts) {
                Write-Error "Operation failed on final attempt: $($_.Exception.Message)"
                throw
            }
            Write-Output "Operation threw an error (attempt $attempt/$MaxAttempts): $($_.Exception.Message). Retrying in $DelayInSeconds seconds..."
            Start-Sleep -Seconds $DelayInSeconds
            $attempt++
        }
    }
}

function Test-AvailabilityMetricForTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$ResourceGroupName,
        [Parameter(Mandatory = $true)] [string]$AppInsightsName,
        [Parameter(Mandatory = $true)] [string]$TestName,
        [Parameter(Mandatory = $true)] [string]$StartTime,
        [Parameter(Mandatory = $true)] [string]$EndTime
    )

    try {
        $result = az monitor metrics list `
            --resource "$AppInsightsName" `
            --resource-group "$ResourceGroupName" `
            --resource-type $resourceType `
            --metric $metricName `
            --start-time $StartTime `
            --end-time $EndTime `
            --interval PT5M `
            --filter "availabilityResult/name eq '$TestName'" `
            --output json 2>$null | ConvertFrom-Json

        $latestAverage = $null

        if ($result -and $result.value) {
            $allTimeSeries = @()
            foreach ($metric in $result.value) {
                if ($metric.timeseries) { $allTimeSeries += $metric.timeseries }
            }

            $tsWithData = $allTimeSeries | Where-Object { $_.data -and $_.data.Count -gt 0 } | Select-Object -First 1
            if ($tsWithData) {
                $latestPoint = $tsWithData.data[-1]
                if ($latestPoint -and $null -ne $latestPoint.average) {
                    $latestAverage = [double]::Parse($latestPoint.average.ToString())
                }
            }
        }

        if ($null -ne $latestAverage) {
            Write-Output "  ✓ $TestName - metric data found"
            return [pscustomobject]@{
                Name    = $TestName
                Status  = 'Found'
                Average = $latestAverage
                Success = $true
            }
        }
        else {
            Write-Output "  ✗ $TestName - no metric data found yet"
            return [pscustomobject]@{
                Name    = $TestName
                Status  = 'NotFound'
                Average = $null
                Success = $false
            }
        }
    }
    catch {
        Write-Error "  Metrics query error for '$TestName' in '$AppInsightsName' (RG: '$ResourceGroupName'): $($_.Exception.Message)"
        return [pscustomobject]@{
            Name        = $TestName
            Status      = 'Error'
            Average     = $null
            Success     = $false
            ErrorMessage = $_.Exception.Message
        }
    }
}

$failedTests = @()
$summaryRows = @()

foreach ($testName in $testNames) {
    $resultRow = Invoke-WithRetry -Operation {
        $endTime = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        Test-AvailabilityMetricForTest -ResourceGroupName $ResourceGroupName -AppInsightsName $AppInsightsName -TestName $testName -StartTime $startTime -EndTime $endTime
    } -MaxAttempts $maxRetries -DelayInSeconds $retryIntervalSeconds

    if (-not $resultRow.Success) {
        $failedTests += $testName
    }
    $summaryRows += $resultRow
}

# Final result and summary
if ($failedTests.Count -eq 0) {
    Write-Output ''
    Write-Output '✓ All tests verified successfully (metrics method)!'
    Write-Output ''
    Write-Output 'Summary (last 5-min averages):'
    $summaryRows | Sort-Object Name | Format-Table -AutoSize Name, Status, Average
    exit 0
}

Write-Output ''
Write-Output "✗ Verification failed (metrics method) - The following tests did not publish results within $([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes:"
foreach ($test in $failedTests) {
    Write-Output "  - $test"
}

Write-Output ''
Write-Output 'Summary (last 5-min averages):'
$summaryRows | Sort-Object Name | Format-Table -AutoSize Name, Status, Average

exit 1
