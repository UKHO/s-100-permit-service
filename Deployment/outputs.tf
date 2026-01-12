output "webapp_name" {
  value = module.webapp_service.webapp_name
}

output "stub_webapp_name" {
  value = local.stub_web_app_name
}

output "resource_group" {
  value = azurerm_resource_group.rg.name
}

output keyvault_uri {
  value = module.key_vault.keyvault_uri
}

output keyvault_datakv_uri {
  value = module.key_vault.keyvault_datakv_uri
  sensitive = true
}

output "webapp_default_site_hostname" {
  value = module.webapp_service.default_site_hostname
}

output "webapp_slot_name" {
  value = module.webapp_service.slot_name
}

output "webapp_slot_default_site_hostname" {
  value = module.webapp_service.slot_default_site_hostname
}