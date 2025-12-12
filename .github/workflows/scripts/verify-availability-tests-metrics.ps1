<#
Verifies that all availability tests have published at least one result to Application Insights using metrics.
Uses a retry mechanism to account for data ingestion latency.

Usage:
    .\verify-availability-tests-metrics.ps1 -ResourceGroupName "my-rg" -AppInsightsName "appi-prod-001" -SubscriptionId "xxx-xxx-xxx"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$AppInsightsName,
    
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId
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

# Get the Application Insights resource ID
$appInsightsResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/microsoft.insights/components/$AppInsightsName"

# Tracking
$startTime = (Get-Date).AddMinutes(-$lookbackMinutes).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$allTestsPassed = $true
$failedTests = @()

Write-Host "Starting availability tests verification (Metrics method)..."
Write-Host "Checking for results published after $startTime"
Write-Host "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Host ""

# Retry loop
for ($retry = 1; $retry -le $maxRetries; $retry++) {
    Write-Host "Attempt $retry/$maxRetries..."
    
    $retryTestsFailed = @()
    
    # Calculate time range for metrics query
    $endTime = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    
    # Check each test
    foreach ($testName in $testNames) {
        try {
            # Query availability metrics
            # Note: Metrics may aggregate data, so we check if any data exists for the test
            $result = az monitor metrics list `
                --resource $appInsightsResourceId `
                --metric "availabilityResults/count" `
                --start-time $startTime `
                --end-time $endTime `
                --interval PT1M `
                --aggregation Total `
                --filter "name.value eq '$testName'" `
                --output json 2>$null | ConvertFrom-Json

            # Check if we have any timeseries data with values
            $hasResults = $false
            if ($result.value -and $result.value.Count -gt 0) {
                foreach ($metric in $result.value) {
                    if ($metric.timeseries -and $metric.timeseries.Count -gt 0) {
                        foreach ($ts in $metric.timeseries) {
                            if ($ts.data -and $ts.data.Count -gt 0) {
                                # Check if any data point has a value
                                $totalValue = ($ts.data | Measure-Object -Property total -Sum).Sum
                                if ($totalValue -gt 0) {
                                    $hasResults = $true
                                    break
                                }
                            }
                        }
                    }
                }
            }
            
            if ($hasResults) {
                Write-Host "  ✓ $testName - Metric data found"
            }
            else {
                Write-Host "  ✗ $testName - No metric data found yet"
                $retryTestsFailed += $testName
            }
        }
        catch {
            Write-Host "  ⚠ $testName - Metrics query error: $_"
            $retryTestsFailed += $testName
        }
    }
    
    # Check if all tests passed
    if ($retryTestsFailed.Count -eq 0) {
        Write-Host ""
        Write-Host "✓ All tests verified successfully (Metrics method)!"
        exit 0
    }
    
    # If not the last retry, wait and continue
    if ($retry -lt $maxRetries) {
        Write-Host "  Waiting $retryIntervalSeconds seconds before retry..."
        Start-Sleep -Seconds $retryIntervalSeconds
        Write-Host ""
    }
    
    $failedTests = $retryTestsFailed
}

# All retries exhausted
Write-Host ""
Write-Host "✗ Verification failed (Metrics method) - The following tests did not publish results within $([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes:"
foreach ($test in $failedTests) {
    Write-Host "  - $test"
}

exit 1
