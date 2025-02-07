﻿using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IUserPermitService
    {
        void ValidateUpnsAndChecksum(UserPermitServiceResponse userPermitServiceResponse);
    }
}