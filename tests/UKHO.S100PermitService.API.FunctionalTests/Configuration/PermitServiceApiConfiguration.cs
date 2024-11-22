namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public class PermitServiceApiConfiguration
    {
        public string? BaseUrl { get; set; }
        public string? InvalidToken { get; set; }
        public int? ValidLicenceId { get; set; }
        public IEnumerable<string>? NonIntegerLicenceIds { get; set; }
        public IEnumerable<int>? MissingDataLicenceId { get; set; }
        public IEnumerable<int>? InvalidLicenceId { get; set; }
        public int? InvalidPksLicenceId { get; set; }
        public IEnumerable<int>? NoDataLicenceId { get; set; }
        public int? InvalidExpiryDateLicenceId { get; set; }
        public IEnumerable<string>? InvalidChars { get; set; }
        public IEnumerable<string>? PermitHeaders { get; set; }
        public IReadOnlyDictionary<string, string>? UserPermitNumbers { get; set; }
        public string? TempFolderName { get; set; }
    }
}
