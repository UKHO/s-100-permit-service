﻿namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public class PermitServiceApiConfiguration
    {
        public string? BaseUrl { get; set; }
        public string? InvalidToken { get; set; }
        public int? ValidLicenceId { get; set; }
        public List<string>? NonIntegerLicenceIds { get; set; }
        public List<int>? InvalidHoldingsLicenceId { get; set; }
        public List<int>? InvalidUPNLicenceId { get; set; }
        public List<int>? InvalidPKSLicenceId { get; set; }
        public List<int>? NoDataLicenceId { get; set; }
        public List<string>? InvalidChars { get; set; }
    }
}
