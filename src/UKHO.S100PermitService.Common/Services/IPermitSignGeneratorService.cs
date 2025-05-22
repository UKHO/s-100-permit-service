namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitSignGeneratorService
    {
        public Task<string> GeneratePermitSignXmlAsync(string permitXmlContent);
    }
}