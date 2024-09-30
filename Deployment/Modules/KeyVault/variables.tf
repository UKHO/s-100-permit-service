variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "env_name" {
  type = string
}

variable "read_access_objects" {
  type = map(string)
}

variable "secrets" {
  type = map(string)
}

variable "tags" {

}
variable "name_midkv" {
  
}
#############

variable "resource_group_name" {
  type    = string
 }

variable "subscription_id" {
  type = string
}

variable "hub_subscription_id" {
  type = string
}

variable "sku_name" {
  type = map(any)
  default = {
            "dev"     =  "P1v2"            
            "vni"     =  "P1v3"
            "iat"     =  "P1v3"            
            "e2e"     =  "P1v3"
            "qa"      =  "P1v3"
            live      =  "P1v3"
            }
}

variable "spoke_rg" {
  type = string
}

variable "pe_rg" {
  type = string
}

variable "spoke_vnet_name" {
  type = string
}

variable "spoke_subnet_name" {
  type = string
}

variable "pe_vnet_name" {
  type = string
}

variable "pe_subnet_name" {
  type = string
}

