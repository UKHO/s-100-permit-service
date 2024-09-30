data "azurerm_resource_group" "rg" {
    provider = azurerm.erp
    name = var.pe_rg
} 

module "private_endpoint_link" {
  source              = "github.com/UKHO/tfmodule-azure-private-endpoint-private-link?ref=0.6.0"
  vnet_link           = local.vnet_link
  private_connection  = [local.private_connection]
  zone_group          = local.zone_group 
  pe_identity         = [local.pe_identity]
  pe_environment      = local.env_name 
  pe_vnet_rg          = var.spoke_rg 
  pe_vnet_name        = var.spoke_vnet_name
  pe_subnet_name      = var.spoke_subnet_name
  pe_resource_group   = data.azurerm_resource_group.rg
  dns_resource_group  = local.dns_resource_group
}