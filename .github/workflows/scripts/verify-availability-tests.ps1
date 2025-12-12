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

$maxRetries = 30
$retryIntervalSeconds = 10

# Tracking: use script start as offset for the lookback window
$startTime = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

Write-Host 'Starting availability tests verification (metrics method)...'
Write-Host "Checking for results published after $startTime"
Write-Host "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Host ''

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
                Write-Host "  Operation succeeded on attempt $attempt"
                return $lastResult
            }

            if ($attempt -eq $MaxAttempts) {
                Write-Host "  Operation did not meet success criteria after $MaxAttempts attempts"
                return $lastResult
            }

            Write-Host "  Operation not successful (attempt $attempt/$MaxAttempts). Retrying in $DelayInSeconds seconds..."
            Start-Sleep -Seconds $DelayInSeconds
            $attempt++
        }
        catch {
            if ($attempt -eq $MaxAttempts) {
                Write-Error "Operation failed on final attempt: $($_.Exception.Message)"
                throw
            }
            Write-Host "  Operation threw an error (attempt $attempt/$MaxAttempts): $($_.Exception.Message). Retrying in $DelayInSeconds seconds..."
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
            return [pscustomobject]@{
                Name                   = $TestName
                Status                 = 'Found'
                AvailabilityPercentage = $latestAverage
                Success                = $true
            }
        }
        else {
            return [pscustomobject]@{
                Name                   = $TestName
                Status                 = 'Not Found'
                AvailabilityPercentage = $null
                Success                = $false
            }
        }
    }
    catch {
        Write-Error "  Metrics query error for '$TestName' in '$AppInsightsName' (RG: '$ResourceGroupName'): $($_.Exception.Message)"
        return [pscustomobject]@{
            Name                   = $TestName
            Status                 = 'Error'
            AvailabilityPercentage = $null
            Success                = $false
            ErrorMessage           = $_.Exception.Message
        }
    }
}

$summaryRows = @()

foreach ($testName in $testNames) {
    Write-Host "Verifying availability test: $testName"

    $resultRow = Invoke-WithRetry -Operation {
        $endTime = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        Test-AvailabilityMetricForTest -ResourceGroupName $ResourceGroupName -AppInsightsName $AppInsightsName -TestName $testName -StartTime $startTime -EndTime $endTime
    } -MaxAttempts $maxRetries -DelayInSeconds $retryIntervalSeconds

    $summaryRows += $resultRow
}

Write-Host ''
Write-Host 'Summary (last 5-min averages):'
$summaryRows | Sort-Object Name | Format-Table -AutoSize Name, Status, AvailabilityPercentage
Write-Host ''

$anyFailures = ($summaryRows | Where-Object { $_.Status -ne 'Found' }).Count -gt 0

if (-not $anyFailures) {
    Write-Host '✓ All tests verified successfully (metrics method)!'
    exit 0
}
else {
    Write-Host '✗ Verification failed (metrics method). Some tests did not publish results.'
    exit 1
}
