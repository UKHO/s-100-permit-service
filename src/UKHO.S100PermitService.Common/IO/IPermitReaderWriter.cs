﻿using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.IO
{
    public interface IPermitReaderWriter
    {
        MemoryStream CreatePermits(List<Permit> permits);
    }
}