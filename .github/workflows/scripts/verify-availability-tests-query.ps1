<#
Verifies that all availability tests have published at least one result to Application Insights using the availabilityResults table query.
Uses a retry mechanism to account for data ingestion latency.

Usage:
    .\verify-availability-tests-query.ps1 -ResourceGroupName "my-rg" -AppInsightsName "appi-prod-001"
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

# Tracking
$startTime = Get-Date
$allTestsPassed = $true
$failedTests = @()

Write-Host "Starting availability tests verification (Query method)..."
Write-Host "Checking for results in the last $lookbackMinutes minutes"
Write-Host "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Host ""

# Retry loop
for ($retry = 1; $retry -le $maxRetries; $retry++) {
    Write-Host "Attempt $retry/$maxRetries..."
    
    $retryTestsFailed = @()
    
    # Check each test
    foreach ($testName in $testNames) {
        # Build KQL query to count results from the last 2 minutes
        $query = @"
availabilityResults
| where name == "$testName"
| where timestamp > ago($lookbackMinutes`m)
| summarize count()
"@

        try {
            $result = az monitor app-insights query `
                --app "$AppInsightsName" `
                --analytics-query $query `
                --resource-group "$ResourceGroupName" `
                --output json 2>$null | ConvertFrom-Json

            $count = $result.tables[0].rows[0][0]
            
            if ($count -gt 0) {
                Write-Host "  ✓ $testName - Found $count result(s)"
            }
            else {
                Write-Host "  ✗ $testName - No results found yet"
                $retryTestsFailed += $testName
            }
        }
        catch {
            Write-Host "  ⚠ $testName - Query error: $_"
            $retryTestsFailed += $testName
        }
    }
    
    # Check if all tests passed
    if ($retryTestsFailed.Count -eq 0) {
        Write-Host ""
        Write-Host "✓ All tests verified successfully (Query method)!"
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
Write-Host "✗ Verification failed (Query method) - The following tests did not publish results within $([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes:"
foreach ($test in $failedTests) {
    Write-Host "  - $test"
}

exit 1
