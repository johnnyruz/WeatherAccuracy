terraform {
  backend "azurerm" {}
}


provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "function_storage" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_table" "data_table" {
  name                 = var.storage_table_name
  storage_account_name = azurerm_storage_account.function_storage.name
}

resource "azurerm_service_plan" "sp" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_application_insights" "app_insights" {
  name                = var.app_insights_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_linux_function_app" "function" {
  name                = var.app_service_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  storage_account_name       = azurerm_storage_account.function_storage.name
  storage_account_access_key = azurerm_storage_account.function_storage.primary_access_key
  service_plan_id            = azurerm_service_plan.sp.id

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.app_insights.instrumentation_key
    TABLE_STORAGE_CONN_STRING      = azurerm_storage_account.function_storage.primary_connection_string
    TABLE_STORAGE_TABLE_NAME       = azurerm_storage_table.data_table.name
    TARGET_TIME_ZONE               = var.target_time_zone
    WEATHER_API_ENDPOINT           = var.weather_api_endpoint
    WEATHER_API_KEY                = var.weather_api_key
    ZIP_CODE                       = var.zip_code
  }

  site_config {}

  depends_on = [
    azurerm_resource_group.rg,
    azurerm_storage_account.function_storage,
    azurerm_storage_table.data_table,
    azurerm_service_plan.sp,
    azurerm_application_insights.app_insights
  ]
}