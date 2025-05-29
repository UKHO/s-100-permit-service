variable "location" {
  type    = string
  default = "uksouth"
}

variable "resource_group_name" {
  type    = string
  default = "ps"
}

locals {
  env_name           = lower(terraform.workspace)
  service_name       = "ps"  
  web_app_name       = "${local.service_name}-${local.env_name}-api"
  stub_web_app_name  = "${local.service_name}-${local.env_name}-stub"
  key_vault_name     = "${local.service_name}-ukho-${local.env_name}-kv"
  key_vault_data_kv  = "${local.service_name}-ukho-${local.env_name}-data-kv"
  pe_identity        = "${local.service_name}${local.env_name}"
  vnet_link          = "${local.service_name}${local.env_name}"
  private_connection = "/subscriptions/${var.subscription_id}/resourceGroups/ps-${local.env_name}-rg/providers/Microsoft.Web/sites/ps-${local.env_name}-api"
  dns_resource_group = "engineering-rg"
  zone_group         = "${local.service_name}${local.env_name}zone"
  dns_zones          = "privatelink.azurewebsites.net"
  tags = {
    SERVICE                   = "S100 Permit Service"
    ENVIRONMENT               = local.env_name
    SERVICE_OWNER             = "UKHO"
    RESPONSIBLE_TEAM          = "Mastek"
    CALLOUT_TEAM              = "On-Call_N/A"
    COST_CENTRE               = "A.011.15.5"
    }
  }

variable "sku_name" {
  type = map(any)
  default = {
            "dev"       =  "P1v2"            
            "vni"       =  "P1v3"
            "vne"       =  "P1v3"
            "iat"       =  "P1v3"
            }
}

variable "spoke_rg" {
  type = string
}

variable "spoke_vnet_name" {
  type = string
}

variable "spoke_subnet_name" {
  type = string
}

variable "hardwareid" {
  type = string
}

variable "subscription_id" {
  type = string
}

variable "hub_subscription_id" {
  type = string
}

variable "pe_vnet_name" {
  type = string
}

variable "pe_subnet_name" {
  type = string
}

variable "pe_rg" {
  type = string
}