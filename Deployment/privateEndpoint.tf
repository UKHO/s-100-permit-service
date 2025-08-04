data "azurerm_resource_group" "rg" {
    provider = azurerm.ps
    name = var.pe_rg
} 

data "azurerm_resource_group" "perg" {
    provider = azurerm.ps
    name = var.spoke_rg
}

data "azurerm_virtual_network" "pevn" {
    provider = azurerm.ps
    name = var.pe_vnet_name
    resource_group_name = var.spoke_rg
}

data "azurerm_subnet" "pesn" {
    provider = azurerm.ps
    name = var.pe_subnet_name
    virtual_network_name = var.pe_vnet_name
    resource_group_name = var.spoke_rg
}
