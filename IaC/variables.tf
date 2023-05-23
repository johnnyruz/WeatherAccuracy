variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "storage_account_name" {
  type = string
}

variable "storage_table_name" {
  type = string
}

variable "app_service_plan_name" {
  type = string
}

variable "app_service_name" {
  type = string
}

variable "app_insights_name" {
  type    = string
  default = "tf-weather-appinsights"
}

variable "target_time_zone" {
  type    = string
  default = "Eastern Standard Time"
}

variable "weather_api_endpoint" {
  type    = string
  default = "http://api.weatherapi.com/v1/"
}

variable "weather_api_key" {
  type      = string
  sensitive = true
}

variable "zip_code" {
  type = string
}