output keyvault_uri {
	value = azurerm_key_vault.kv.vault_uri
}

output keyvault_datakv_uri {
	value = azurerm_key_vault.datakv.vault_uri
	sensitive = true
}