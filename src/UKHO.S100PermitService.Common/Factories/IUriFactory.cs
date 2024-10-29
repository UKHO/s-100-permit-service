namespace UKHO.S100PermitService.Common.Factories
{
    public interface IUriFactory
    {
        Uri CreateUri(string baseUrl, string endpointFormat, params object[] args);
    }
}
