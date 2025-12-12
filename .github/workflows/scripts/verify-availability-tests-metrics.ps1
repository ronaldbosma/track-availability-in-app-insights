<#
Verifies that all availability tests have published at least one result to Application Insights using metrics.
Uses a retry mechanism to account for data ingestion latency.

Usage:
    .\verify-availability-tests-metrics.ps1 -ResourceGroupName "my-rg" -AppInsightsName "appi-prod-001"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$AppInsightsName
)

# Configuration
$testNames = @(
    "Azure Function - API Management SSL Certificate Check",
    "Azure Function - Backend API Status",
    "Logic App Workflow - Backend API Status",
    "Standard Test - API Management SSL Certificate Check",
    "Standard Test - Backend API Status"
)

$maxRetries = 30
$retryIntervalSeconds = 10
$lookbackMinutes = 2

# Tracking: use script start as offset for the 2-minute lookback window
$startTime = (Get-Date).AddMinutes(-$lookbackMinutes).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$failedTests = @()

Write-Host "Starting availability tests verification (Metrics method)..."
Write-Host "Checking for results published after $startTime"
Write-Host "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Host ""

# Local retry function (generic)
function Invoke-WithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock,
        
        [Parameter(Mandatory = $false)]
        [int]$MaxAttempts = 15,
        
        [Parameter(Mandatory = $false)]
        [int]$DelayInSeconds = 2
    )
    
    $attempt = 1
    
    while ($attempt -le $MaxAttempts) {
        & $ScriptBlock
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Operation succeeded on attempt $attempt"
            return
        }
        
        if ($attempt -eq $MaxAttempts) {
            Write-Host "Operation failed after $MaxAttempts attempts (exit code: $LASTEXITCODE)"
            return
        }
        
        Write-Host "Operation failed (attempt $attempt/$MaxAttempts, exit code: $LASTEXITCODE). Retrying in $DelayInSeconds seconds..."
        Start-Sleep -Seconds $DelayInSeconds
        $attempt++
    }
}

# Function: checks if availability metric exists for a single test within the time window
function Test-AvailabilityMetricForTest {
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
            --resource-type "Microsoft.Insights/components" `
            --metric "availabilityResults/availabilityPercentage" `
            --start-time $StartTime `
            --end-time $EndTime `
            --interval PT1M `
            --filter "availabilityResult/name eq '$TestName'" `
            --output json 2>$null | ConvertFrom-Json

        $hasMetric = $false
        if ($result.value -and $result.value.Count -gt 0) {
            foreach ($metric in $result.value) {
                if ($metric.timeseries -and $metric.timeseries.Count -gt 0) {
                    foreach ($ts in $metric.timeseries) {
                        if ($ts.data -and $ts.data.Count -gt 0) {
                            # Consider metric present if any datapoint has an 'average' value (percentage)
                            foreach ($dp in $ts.data) {
                                if ($dp.PSObject.Properties.Name -contains 'average' -and $null -ne $dp.average) {
                                    $hasMetric = $true
                                    break
                                }
                            }
                        }
                        if ($hasMetric) { break }
                    }
                }
                if ($hasMetric) { break }
            }
        }

        if ($hasMetric) {
            Write-Host "  ✓ $TestName - Metric data found"
            $global:LASTEXITCODE = 0
        }
        else {
            Write-Host "  ✗ $TestName - No metric data found yet"
            $global:LASTEXITCODE = 1
        }
    }
    catch {
        Write-Host "  ⚠ $TestName - Metrics query error: $_"
        $global:LASTEXITCODE = 1
    }
}

# Execute for each test with retry
foreach ($testName in $testNames) {
    Invoke-WithRetry -ScriptBlock {
        $endTime = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        Test-AvailabilityMetricForTest -ResourceGroupName $ResourceGroupName -AppInsightsName $AppInsightsName -TestName $testName -StartTime $startTime -EndTime $endTime
    } -MaxAttempts $maxRetries -DelayInSeconds $retryIntervalSeconds

    if ($LASTEXITCODE -ne 0) {
        $failedTests += $testName
    }
}

# Final result
if ($failedTests.Count -eq 0) {
    Write-Host ""
    Write-Host "✓ All tests verified successfully (Metrics method)!"
    exit 0
}

Write-Host ""
Write-Host "✗ Verification failed (Metrics method) - The following tests did not publish results within $([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes:"
foreach ($test in $failedTests) {
    Write-Host "  - $test"
}

exit 1
