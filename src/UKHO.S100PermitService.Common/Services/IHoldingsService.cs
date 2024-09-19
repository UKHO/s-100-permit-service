﻿using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IHoldingsService
    {
        Task<List<HoldingsServiceResponse>> GetHoldingsAsync(int licenceId, string correlationId);
    }
}