resource "azurerm_service_plan" "app_service_plan" {
  name                = "${var.service_name}-${var.env_name}-asp"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku_name            = var.sku_name
  os_type             = "Windows"
  tags                = var.tags
}

resource "azurerm_windows_web_app" "webapp_service" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.app_service_plan.id
  tags                = var.tags

  site_config {
    application_stack {    
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on  = true
    ftps_state = "Disabled"
  }
     
  app_settings = var.app_settings

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    ignore_changes = [ virtual_network_subnet_id ]
  }

  https_only = true
}

resource "azurerm_windows_web_app" "stub_webapp_service" {  
  name                = var.stub_webapp_name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.app_service_plan.id
  tags                = var.tags

  site_config {
    application_stack {    
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on  = true
    ftps_state = "Disabled"
  }
     
  app_settings = var.stub_app_settings

  identity {
    type = "SystemAssigned"
  }

  https_only = true
}

resource "azurerm_app_service_virtual_network_swift_connection" "webapp_vnet_integration" {
  app_service_id = azurerm_windows_web_app.webapp_service.id
  subnet_id      = var.subnet_id
}