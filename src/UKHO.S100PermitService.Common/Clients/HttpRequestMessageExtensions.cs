using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace UKHO.S100PermitService.Common.Clients
{
    [ExcludeFromCodeCoverage]
    public static class HttpRequestMessageExtensions
    {
        public static void SetBearerToken(this HttpRequestMessage requestMessage, string accessToken)
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public static void AddHeader(this HttpRequestMessage requestMessage, string name, string value)
        {
            requestMessage.Headers.Add(name, value);
        }
    }
}
