# WeatherAccuracy

## Overview

This respository is designed to demonstrate GitHub Actions using a cool project that I've wanted to put together for some time - a Weather Forecast Accuracy report. I want to design this repo to be able to be forked and published to your own Azure instance to track your specific location.

## Project Architecture

This project uses 2 Azure Functions to track weather forecast data for a specific Zip Code using WeatherApi data. The goal is to visualize how preditions change and adjust as the date gets closer, and then compare the historical predctions to the actual weather for that day.

Currently using the FREE tier of WeatherApi only gets 3 days of forecast data, but ideally this would be more valuable for tracking preditions futher out, like 10-day or 14-day forecasts. This application will automatically handle if the API is configured to return more days in the Forecast.

### Functions

1. **Load Forecast** - Runs every 15 minutes and loads the predicted forecast for N number of days into an Azure Storage Table. We track min/max temperatures and predicted rainfall.
2. **Load Actual** - Runs every day at 6AM UTC and records the actual weather results for the previous day for the selected Zip Code

# Application Setup - Azure

## Prerequisites

1. Azure Account - With the default settings/configuration of this application, the limits should be almost FREE or cost a few cents on the Azure Storage Account and Azure Function App Consumption plan. Depending on how long you run this, you may have a small charge for data storage in Azure Tables

    - Function Free Limit: 1M Executions
      - This App Executions: 3000/month (every 15 minutes + 1 per day actual)

    - Storage Account Pricing: $0.045/GB stored, $0.025/10k Writes, $0.005/10k reads
      - This App: Depending on forecast days, expect about 9,000 - 30,000 writes    per month

## Azure Service Principal Creation

In order to run the Terraform code to create the resources, as well as run the Github Actions to deploy the Function App, you will need to create a Service Principal with Contributor rights on your Azure Subscription. In addition, you will need to assign a Federated Identity that allows GitHub Actions to authenticate for that Service Principal using OIDC.

I've included steps below using the Azure CLI - but this can be accomplished using Azure PowerShell or the Azure Portal. More details can be found here: https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux

1. Login to Azure using the Azure CLI (This will also display your Tenant and Subscription Id)

```az login```

2. Create Azure App Registration

```az ad app create --display-name myApp```

3. Create Service Principal for Application

```az ad sp create --id $appId```

4. Assing Contributor Role to your Azure Subscription

```az role assignment create --role contributor --subscription $subscriptionId --assignee-object-id  $assigneeObjectId --assignee-principal-type ServicePrincipal --scope /subscriptions/$subscriptionId```

1. Create Federated Identities for Service Principal. Note, a sample credentials file is in the repository at credentials-template.json. You will need to create 3 identities in order for this project to fully function. Each identity configuration defines what GitHub Action is occuring, whether that is a pull request, a push to a specific branch, or a publish for an environment.

```az ad app federated-credential create --id <APPLICATION-ID> --parameters [credential-file].json```

    1. Federated Identity for Pull Requests

        subject="repo:[Your Repo Name]/WeatherAccuracy:pull_request"

    2. Federated Identity for Production Environment

        subject="repo:[Your Repo Name]/WeatherAccuracy:environment:Production"

    3. Federated Identity for merges to the Main branch

        subject="repo:[Your Repo Name]/WeatherAccuracy:ref:refs/heads/main"

## Azure Storage Account - Terraform State Storage

In order to successfully deploy and manage resources using Terraform, we need a place to store a Terraform State file. For this project we'll be using Azure Storage Account (separate from the Function App and Weather Data Storage) to manage this. This is usually created via a separate pipeline, but in this case we'll just create it via the Portal. You can also use the Azure CLI if desired.

1. In the Azure Portal - search for Storage Account and create a new one
2. Put this storage account in a different resource group than you intend to use for the function app.
3. Choose your options, the defaults are fine but you may want to switch to LRS for lower costs. You will need public access open and you'll want to allow Storage Account Key Access
4. Once your Storage Account Resource is created - select "Containers" and create a new Container.

You'll need to record your:
- Resource Group Name
- Storage Account Name
- Container Name
- Storage Account Key (available under the "Access Keys" menu)
  
These will need to be populated in variables and secrets for your GitHub Actions in a following section.


# Application Setup - WeatherAPI

1. You can register for a free account and get your WeatherAPI key at https://www.weatherapi.com/. You will need the API Key for the following step.

# Application Setup - GitHub Actions

In order to successfully run the GitHub Actions in this repository - you will have to create an environment and update various Secrets and Variables that are specific to your environment.

1. Create an environment named "Production". Ideally, also create a Protection rule so that any deployments to the environment need to be approved.
2. Create the following GitHub Action Secrets based on your Azure and Weather API information

| Secret Name                | Value                                                     |
|----------------------------|-----------------------------------------------------------|
| AZURE_CLIENT_ID            | Client ID for your Azure App Registration                 |
| AZURE_SUBSCRIPTION_ID      | Subscription ID for your Azure Subscription               |
| AZURE_TENANT_ID            | Tenant ID for your Azure Tenant                           |
| TFSTATE_STORAGE_ACCESS_KEY | Access Key for your Terraform State Azure Storage Account |
| WEATHER_API_KEY            | Your API get from weatherapi.com                          |

3. Create the following GitHub Action Variables based on your desired settings

| Variable Name               | Value                                                                                   |
|-----------------------------|-----------------------------------------------------------------------------------------|
| APP_INSIGHTS_NAME           | Name of App Insights Resource that will be created by Terraform                         |
| APP_SERVICE_NAME            | Name of your Function App that will be created by Terraform                             |
| APP_SERVICE_PLAN_NAME       | Name of the App Service Plan that will be created by Terraform                          |
| LOCATION                    | The desired Azure region in which to deploy your resources                              |
| RESOURCE_GROUP_NAME         | The desired resource group name in which to deploy your resources                       |
| STORAGE_ACCOUNT_NAME        | Name of the Storage Account to deploy (NOTE - 23 chars no spaces or special characters) |
| STORAGE_TABLE_NAME          | Name of the Azure Table to create to store Weather Data                                 |
| TARGET_TIME_ZONE            | Target Time Zone for your table data (i.e. "Eastern Standard Time")                     |
| TFSTATE_CONTAINER_NAME      | Azure Storage Container that has your Terraform State File                              |
| TFSTATE_RESOURCE_GROUP_NAME | Resource Group of the Azure Storage Resource that has your Terraform State File         |
| TFSTATE_STORAGE_ACCOUNT     | Azure Storage Account that has your Terraform State File                                |
| WEATHER_API_ENDPOINT        | Weather API Endpoint (should be http://api.weatherapi.com/v1/)                          |
| ZIP_CODE                    | Desired Zip Code to monitor forecast accuracy                                           |


# Deployment

After this configuration and variable setup - you should be able to successfully run the Deployment GitHub Action. Terraform should successfully PLAN the deployment, and you'll be able to approve the Terraform APPLY to Production, after which the Function App will build and be deployed to Azure.