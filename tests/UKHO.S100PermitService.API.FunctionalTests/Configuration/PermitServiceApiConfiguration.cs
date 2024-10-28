namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public class PermitServiceApiConfiguration
    {
        public string? BaseUrl { get; set; }
        public string? InvalidToken { get; set; }
        public int? ValidLicenceId { get; set; }
        public List<string>? NonIntegerLicenceIds { get; set; }
        public List<int>? MissingDataLicenceId { get; set; }
        public int? InvalidLicenceId { get; set; }
        public int? InvalidPksLicenceId { get; set; }
        public List<int>? NoDataLicenceId { get; set; }
        public int? InvalidExpiryDateLicenceId { get; set; }
        public List<string>? InvalidChars { get; set; }
        public List<string>? PermitHeaders { get; set; }
        public IReadOnlyDictionary<string, string>? UserPermitNumbers { get; set; }
        public string? TempFolderName { get; set; }
    }
}
