namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public static class PermitServiceEndPointFactory
    {
        private static readonly HttpClient _httpClient = new();
        private static string? _uri;

        /// <summary>
        /// This method is used to interact with permits endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the baseUrl</param>
        /// <param name="accessToken">Sets the access Token</param>
        /// <param name="licenceId">Sets the licence ID</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PermitServiceEndPoint(string? baseUrl, string? accessToken, string licenceId)
        {
            _uri = $"{baseUrl}/permits/{licenceId}";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _uri);
            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add("Authorization", "Bearer " + accessToken);
            }
            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}