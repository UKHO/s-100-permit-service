output "web_app_object_id" {
  value = azurerm_windows_web_app.webapp_service.identity.0.principal_id
}

output "web_app_tenant_id" {
  value = azurerm_windows_web_app.webapp_service.identity.0.tenant_id
}

output "default_site_hostname" {
  value = azurerm_windows_web_app.webapp_service.default_hostname
}

output "webapp_name" {
  value = azurerm_windows_web_app.webapp_service.name
}

output "mock_web_app_object_id" {
  value = azurerm_windows_web_app.mock_webapp_service.identity.0.principal_id
}

output "default_site_hostname_mock" {
  value = azurerm_windows_web_app.mock_webapp_service.default_hostname
}