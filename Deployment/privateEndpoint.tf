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

module "private_endpoint_link" {
  source              = "github.com/UKHO/tfmodule-azure-private-endpoint-private-link?ref=0.6.0"
  providers = {
    azurerm.hub   = azurerm.hub
    azurerm.spoke   = azurerm.ps
  }
  vnet_link           = local.vnet_link
  private_connection  = [local.private_connection]
  zone_group          = local.zone_group 
  pe_identity         = [local.pe_identity]
  pe_environment      = local.env_name 
  pe_vnet_rg          = var.spoke_rg 
  pe_vnet_name        = var.pe_vnet_name
  pe_subnet_name      = var.pe_subnet_name
  pe_resource_group   = data.azurerm_resource_group.rg
  dns_resource_group  = local.dns_resource_group
}