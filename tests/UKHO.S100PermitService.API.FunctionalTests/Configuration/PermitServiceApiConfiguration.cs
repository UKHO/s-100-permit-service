namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public class PermitServiceApiConfiguration
    {
        public string? BaseUrl { get; set; }
        public string? InvalidToken { get; set; }
        public IEnumerable<string>? InvalidChars { get; set; }
        public List<string>? PermitHeaders { get; set; }
        public IReadOnlyDictionary<string, string>? UserPermitNumbers { get; set; }
        public string? TempFolderName { get; set; }
    }
}
