variable "name" {
  type = string
}

variable "service_name"{
   type = string

 }

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "app_settings" {
  type = map(string)
}

variable "tags" {

}

variable "sku_name" {

}

variable "env_name" {
  type = string
}

variable "stub_webapp_name" {
  type = string
}

variable "stub_app_settings" {
  type = map(string)
}


