# Track Availability in Application Insights

An `azd` template (Bicep) that implements three different ways to track availability in Application Insights using: 
- a standard test (webtest)
- an Azure Function
- a Logic App workflow

## Overview

This template deploys the following resources:

![Track Availability App](/images/track-availability-diagrams-app.png)

The following availability tests are deployed:
- Two standard tests (webtest):
  1. Checks the availability of an API every 5 minutes from 5 locations
  1. Checks the validity of the SSL certificate of the API
- Two Azure Functions:
  1. Checks the availability of an API every minute
  1. Checks the validity of the SSL certificate of the API
- A Logic App workflow:
  1. Checks the availability of an API every minute

This sample uses Azure Functions to perform availability tests from code because it provides an easy way to trigger the tests on a schedule, but you can use other services that can host .NET code as well.

For the backend, an API in API Management is used that randomly returns a `200 OK` or `503 Service Unavailable` response based on a [configurable approximate failure percentage](#configure-approximate-failure-percentage). Note that you can use any backend to check for availability, not just an API in API Management.

After deployment, availability test results should appear in Application Insights. See the following image for an example:  

![Availability Test Results](/images/availability-test-results.png)

See the [Demo Guide](demos/demo-availability-tests.md) for a more detailed overview of what's included in this template and how it works.


## Getting Started

### Prerequisites  

Before you can deploy this template, make sure you have the following tools installed and the necessary permissions:  

- [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)  
  - Installing `azd` also installs the following tools:  
    - [GitHub CLI](https://cli.github.com)  
    - [Bicep CLI](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install)  
- [.NET Core 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)  
- [npm CLI](https://nodejs.org/) 
  _(This template uses a workaround to deploy the Logic App workflow, which requires the npm CLI.)_
- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) 
  _(This template uses a workaround to build the custom .NET code project for the Logic App using a PowerShell prepackage hook.)_
- You need Owner or Contributor permissions on an Azure Subscription to deploy this template.  

### Deployment

Once `azd` is installed on your machine, you can deploy this template using the following steps:

1. Run the `azd init` command in an empty directory with the `--template` parameter to clone this template into the current directory.  

    ```cmd
    azd init --template ronaldbosma/track-availability-in-app-insights
    ```

    When prompted, specify the name of the environment, for example, `track-availability`. The maximum length is 32 characters.

1. Run the `azd auth login` command to authenticate to your Azure subscription _(if you haven't already)_.

    ```cmd
    azd auth login
    ```

1. Run the `azd up` command to provision the resources in your Azure subscription. This will deploy both the infrastructure and the sample application, and typically takes around 7 minutes to complete. _(Use `azd provision` to only deploy the infrastructure.)_

    ```cmd
    azd up
    ```

    See [Troubleshooting](#troubleshooting) if you encounter any issues during deployment.

1. Once the deployment is complete, you can locally modify the application or infrastructure and run `azd up` again to update the resources in Azure.

### Demo

See the [Demo Guide](demos/demo-availability-tests.md) for a step-by-step walkthrough on how to check and demonstrate the deployed availability tests.

### Clean up

Once you're done and want to clean up, run the `azd down` command. By including the `--purge` parameter, you ensure that the API Management service doesn't remain in a soft-deleted state, which could block future deployments of the same environment.

```cmd
azd down --purge
```

## Configure approximate failure percentage

The backend API will randomly return errors for an approximate percentage based on the `approximateFailurePercentage` parameter that you can configure in [main.parameters.json](/infra/main.parameters.json). 
In the following example, the approximate failure percentage is set to 10%:

```
"approximateFailurePercentage": {
  "value": ${APPROXIMATE_FAILURE_PERCENTAGE=10}
}
```

As you can see, the value can also be set using the `APPROXIMATE_FAILURE_PERCENTAGE` environment variable.

The value is used to create a named value in API Management called `approximate-failure-percentage`. 
The backend API has a policy that uses the named value to implement the logic to return either a `200 OK` or `503 Service Unavailable` response. 
See [backend-api.get-status.xml](/infra/modules/application/backend-api.get-status.xml) for the details.

## Contents

The repository consists of the following files and directories:

```
├── demos                      [ Demo guide(s) ]
├── images                     [ Images used in the README ]
├── infra                      [ Infrastructure As Code files ]
│   |── functions              [ Bicep user-defined functions ]
│   ├── modules                
│   │   ├── application        [ Modules for application infrastructure resources ]
│   │   ├── services           [ Modules for all Azure services ]
│   │   └── shared             [ Reusable modules ]
│   ├── types                  [ Bicep user-defined types ]
│   ├── main.bicep             [ Main infrastructure file ]
│   └── main.parameters.json   [ Parameters file ]
├── src                        [ Application code ]
│   ├── functionApp            [ Azure Functions ]
│   └── logicApp               [ Logic App workflow]
├── azure.yaml                 [ Describes the apps and types of Azure resources ]
└── bicepconfig.json           [ Bicep configuration file ]
```


## Troubleshooting

### API Management deployment failed because the service already exists in soft-deleted state

If you've previously deployed this template and deleted the resources, you may encounter the following error when redeploying the template. This error occurs because the API Management service is in a soft-deleted state and needs to be purged before you can create a new service with the same name.

```json
{
    "code": "DeploymentFailed",
    "target": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-track-availability-sdc-cliqc/providers/Microsoft.Resources/deployments/apiManagement",
    "message": "At least one resource deployment operation failed. Please list deployment operations for details. Please see https://aka.ms/arm-deployment-operations for usage details.",
    "details": [
        {
            "code": "ServiceAlreadyExistsInSoftDeletedState",
            "message": "Api service apim-track-availability-sdc-cliqc was soft-deleted. In order to create the new service with the same name, you have to either undelete the service or purge it. See https://aka.ms/apimsoftdelete."
        }
    ]
}
```

Use the [az apim deletedservice list](https://learn.microsoft.com/en-us/cli/azure/apim/deletedservice?view=azure-cli-latest#az-apim-deletedservice-list) Azure CLI command to list all deleted API Management services in your subscription. Locate the service that is in a soft-deleted state and purge it using the [purge](https://learn.microsoft.com/en-us/cli/azure/apim/deletedservice?view=azure-cli-latest#az-apim-deletedservice-purge) command. See the following example:

```cmd
az apim deletedservice purge --location "swedencentral" --service-name "apim-track-availability-sdc-cliqc"
```

### Function App deployment failed because of quota limitations

If you already have a Consumption tier (`SKU=Y1`) Function App deployed in the same region, you may encounter the following error when deploying the template. This error occurs because you have reached the region's quota for your subscription.

```json
{
  "code": "InvalidTemplateDeployment",
  "message": "The template deployment 'functionApp' is not valid according to the validation procedure. The tracking id is '00000000-0000-0000-0000-000000000000'. See inner errors for details.",
  "details": [
    {
      "code": "ValidationForResourceFailed",
      "message": "Validation failed for a resource. Check 'Error.Details[0]' for more information.",
      "details": [
        {
          "code": "SubscriptionIsOverQuotaForSku",
          "message": "This region has quota of 1 instances for your subscription. Try selecting different region or SKU."
        }
      ]
    }
  ]
}
```

Use the `azd down --purge` command to delete the resources, then deploy the template in a different region.

### Logic App deployment failed because of quota limitations

If you already have a Workflow Standard WS1 tier (`SKU=WS1`) Logic App deployed in the same region, you may encounter the following error when deploying the template. This error occurs because you have reached the region's quota for your subscription.

```json
{
  "code": "InvalidTemplateDeployment",
  "message": "The template deployment 'logicApp' is not valid according to the validation procedure. The tracking id is '00000000-0000-0000-0000-000000000000'. See inner errors for details.",
  "details": [
    {
      "code": "ValidationForResourceFailed",
      "message": "Validation failed for a resource. Check 'Error.Details[0]' for more information.",
      "details": [
        {
          "code": "SubscriptionIsOverQuotaForSku",
          "message": "This region has quota of 1 instances for your subscription. Try selecting different region or SKU."
        }
      ]
    }
  ]
}
```

Use the `azd down --purge` command to delete the resources, then deploy the template in a different region.

### Logging doesn't show in App Insights

Sometimes the requests and traces don't show up in Application Insights & Log Analytics. I've had this happen when I'd taken down an environment and redeployed it with the same name. 

To resolve this, first remove the environment using `azd down --purge`. Then, permanently delete the Log Analytics workspace using the [`az monitor log-analytics workspace delete`](https://learn.microsoft.com/en-us/cli/azure/monitor/log-analytics/workspace?view=azure-cli-latest#az-monitor-log-analytics-workspace-delete) command. Here's an example:

```cmd
az monitor log-analytics workspace delete --resource-group rg-track-availability-sdc-cliqc --workspace-name log-track-availability-sdc-cliqc --force
```

After that, redeploy the template. If logging still doesn't appear, deploy the template in a different region or using a different environment name.

I've registered https://github.com/Azure/azure-dev/issues/5080 in the Azure Developer CLI repository to track this issue.