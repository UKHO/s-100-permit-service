locals {
    pks_subnet_id = {
        dev  = "/subscriptions/a66d7b5f-b6e5-4eca-9002-0550129921d3/resourceGroups/m-spokeconfig-rg/providers/Microsoft.Network/virtualNetworks/PKSDev-vnet/subnets/main-subnet"
        qa   = ""
        live = ""
    }

    pks_subnet_restriction = {
        ip_address                = null,
        name                      = "PKS-${var.env_name}-Subnet"
        action                    = "Allow",
        priority                  = 64000,
        service_tag               = null,
        subnet_id                 = local.pks_subnet_id[lower(var.env_name)],
        virtual_network_subnet_id = local.pks_subnet_id[lower(var.env_name)],
        headers                   = []
    }

    ip_restrictions = {
        dev = [
        local.pks_subnet_restriction
        ]
        qa   = []
        live = []
    }
}