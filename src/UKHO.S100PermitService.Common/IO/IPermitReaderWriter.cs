﻿using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.IO
{
    public interface IPermitReaderWriter
    {
        Stream CreatePermits(IReadOnlyDictionary<string, Permit> permits);

        string ReadXsdVersion();
    }
}