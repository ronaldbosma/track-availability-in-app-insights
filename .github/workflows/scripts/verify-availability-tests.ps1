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

# Load functions from separate file
$functionsPath = Join-Path -Path $PSScriptRoot -ChildPath 'verify-availability-tests.functions.ps1'
. $functionsPath


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

Write-Host 'Starting availability tests verification...'
Write-Host "Checking for results published after $startTime"
Write-Host "Retry strategy: $maxRetries retries every $retryIntervalSeconds seconds (total ~$([math]::Round($maxRetries * $retryIntervalSeconds / 60, 1)) minutes)"
Write-Host ''

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

$failedTests = @($summaryRows | Where-Object { $_.Status -ne 'Found' })

if ($failedTests.Count -eq 0) {
    Write-Host '✓ All tests verified successfully!'
    exit 0
}
else {
    Write-Host '✗ Verification failed. Some tests did not publish results.'
    exit 1
}