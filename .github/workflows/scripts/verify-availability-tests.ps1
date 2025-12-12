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

.PARAMETER MaxRetries
The maximum number of retry attempts to wait for metrics ingestion.

.PARAMETER RetryIntervalSeconds
The delay in seconds between retry attempts.

.PARAMETER StartTime
The start of the time window to query availability metrics for. Defaults to current time.

.PARAMETER TestNames
The list of availability test names to verify. Defaults to the five built-in tests.

.EXAMPLE
PS> .\verify-availability-tests.ps1 -ResourceGroupName "rg-track-availability-sdc-cliqc" -AppInsightsName "appi-track-availability-sdc-cliqc" 

Runs the verification with default retry settings and tests.

.EXAMPLE
PS> .\verify-availability-tests.ps1 -ResourceGroupName "rg-track-availability-sdc-cliqc" -AppInsightsName "appi-track-availability-sdc-cliqc" -MaxRetries 45 -RetryIntervalSeconds 5 -StartTime (Get-Date).AddMinutes(-15) -TestNames @('Test A','Test B')

Runs verification with custom retry settings, a custom start time, and a custom list of test names.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$AppInsightsName,

    # Optional parameters for retry strategy and time window
    [Parameter()]
    [int]$MaxRetries = 30,

    [Parameter()]
    [int]$RetryIntervalSeconds = 10,

    [Parameter()]
    $StartTime = (Get-Date),

    # Optional list of test names to verify
    [Parameter()]
    [string[]]$TestNames = @(
        'Azure Function - API Management SSL Certificate Check',
        'Azure Function - Backend API Status',
        'Logic App Workflow - Backend API Status',
        'Unknow Test Name',
        'Standard Test - API Management SSL Certificate Check',
        'Standard Test - Backend API Status'
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Load functions from separate file
$functionsPath = Join-Path -Path $PSScriptRoot -ChildPath 'verify-availability-tests.functions.ps1'
. $functionsPath


Write-Host 'Starting availability tests verification...'
Write-Host "Checking for results published after $($StartTime.ToString("o"))"
Write-Host "Retry strategy: $MaxRetries retries every $RetryIntervalSeconds seconds (total ~$([math]::Round($MaxRetries * $RetryIntervalSeconds / 60, 1)) minutes)"
Write-Host ''

$summaryRows = @()

foreach ($testName in $TestNames) {
    Write-Host "Verifying availability test: $testName"

    $resultRow = Invoke-WithRetry -Operation {
        Get-AverageAvailabilityPercentageForTest -ResourceGroupName $ResourceGroupName -AppInsightsName $AppInsightsName -TestName $testName -StartTime $StartTime
    } -MaxAttempts $MaxRetries -DelayInSeconds $RetryIntervalSeconds

    $summaryRows += $resultRow
}

Write-Host ''
Write-Host 'Summary (last 5-min average availability percentage per test):'
$summaryRows | Sort-Object Name | Format-Table -AutoSize Name, Status, AvailabilityPercentage
Write-Host ''

# We consider it a success if all tests have availability results (status 'Found'). It's OK if the availability percentage is not 100%.
$failedVerifications = @($summaryRows | Where-Object { $_.Status -ne 'Found' })
if ($failedVerifications.Count -eq 0) {
    Write-Host '✓ All tests have availability results!'
    exit 0
}
else {
    Write-Host '✗ Verification failed. Some tests did not publish results.'
    exit 1
}