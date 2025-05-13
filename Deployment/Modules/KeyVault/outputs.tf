output keyvault_uri {
	value = azurerm_key_vault.kv.vault_uri
}

output keyvault_securedatakv_uri {
	value = azurerm_key_vault.securedatakv.vault_uri
	sensitive = true
}