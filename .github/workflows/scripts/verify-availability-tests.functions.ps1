<#
.SYNOPSIS
Helper functions for verifying availability tests in Application Insights.

.DESCRIPTION
Contains reusable functions for retrying operations and querying Application Insights metrics
to verify that availability tests have published results.
#>

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
            --resource-type 'Microsoft.Insights/components' `
            --metric 'availabilityResults/availabilityPercentage' `
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
