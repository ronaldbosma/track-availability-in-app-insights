<#
.SYNOPSIS
    Restarts the Azure Logic App.

.DESCRIPTION
    Restarts the Logic App using the Azure CLI.

    The logged in user needs permission to restart the Logic App.

.PARAMETER SubscriptionId
    The id of the Azure subscription. Defaults to the AZURE_SUBSCRIPTION_ID environment variable.

.PARAMETER ResourceGroupName
    The name of the resource group. Defaults to the AZURE_RESOURCE_GROUP environment variable.

.PARAMETER LogicAppName
    The name of the Logic App. Defaults to the AZURE_LOGIC_APP_NAME environment variable.

.EXAMPLE
    azd exec ./restart-logic-app.ps1

    Restarts the Logic App using the environment variables of the current azd environment.

.EXAMPLE
    ./restart-logic-app.ps1 -SubscriptionId '<subscription-id>' `
        -ResourceGroupName 'rg-monitorais-frc-u7eei' -LogicAppName 'logic-monitorais-frc-u7eei'

    Runs the script directly with explicitly specified values.
#>
[CmdletBinding()]
param(
    [string]$SubscriptionId = $env:AZURE_SUBSCRIPTION_ID,
    [string]$ResourceGroupName = $env:AZURE_RESOURCE_GROUP,
    [string]$LogicAppName = $env:AZURE_LOGIC_APP_NAME
)

$ErrorActionPreference = 'Stop'

# Validate required parameters
$requiredParameters = @{
    SubscriptionId    = $SubscriptionId
    ResourceGroupName = $ResourceGroupName
    LogicAppName      = $LogicAppName
}
$missingParameters = $requiredParameters.GetEnumerator() | Where-Object { [string]::IsNullOrWhiteSpace($_.Value) } | ForEach-Object { $_.Key }
if ($missingParameters) {
    throw "The following parameters are not specified and have no environment variable value: $($missingParameters -join ', '). Specify them explicitly or run the script using 'azd exec'."
}

# First, ensure the Azure CLI is logged in and set to the correct subscription
az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) {
    throw "Unable to set the Azure subscription. Please make sure that you're logged into the Azure CLI with the same credentials as the Azure Developer CLI."
}

# Restart the Logic App
Write-Host "Restarting Logic App '$LogicAppName' in resource group '$ResourceGroupName'..."
az webapp restart `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroupName `
    --name $LogicAppName

if ($LASTEXITCODE -ne 0) {
    throw "Failed to restart Logic App '$LogicAppName'."
}
