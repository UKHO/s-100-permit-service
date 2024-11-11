namespace UKHO.S100PermitService.StubService.Configuration
{
    public class HoldingsServiceConfiguration
    {
        public required string Url { get; set; }
        public required List<int> ValidLicenceIds { get; set; }
    }
}