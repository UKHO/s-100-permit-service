namespace UKHO.S100PermitService.Common.Factories
{
    public class UriFactory : IUriFactory
    {
        public Uri CreateUri(string baseUrl, string endpointFormat, params object[] args)
        {
            var formattedEndpoint = string.Format(endpointFormat, args);
            return new Uri(new Uri(baseUrl), formattedEndpoint);
        }
    }
}
