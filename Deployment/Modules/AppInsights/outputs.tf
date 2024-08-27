output "instrumentation_key" {
  value = azurerm_application_insights.app_insights.instrumentation_key
  sensitive = true
}

output "connection_string" {
  value = azurerm_application_insights.app_insights.connection_string
  sensitive = true
}