output keyvault_uri {
	value = azurerm_key_vault.kv.vault_uri
	sensitive = true
}

output keyvault_mid_uri {
	value = azurerm_key_vault.midkv.vault_uri
    sensitive = true
}