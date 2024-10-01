terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.112.0"
      configuration_aliases = [
        azurerm.hub,
        azurerm.erp,
      ]
    }
      }

  required_version = "=1.9.2"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "terraform.deployment.tfplan"
  }
}

provider "azurerm" {
  features {} 
  alias = "hub"
  subscription_id = var.hub_subscription_id
}

provider "azurerm" {
  features {} 
  alias = "erp"
  subscription_id = var.subscription_id
}
