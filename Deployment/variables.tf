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
