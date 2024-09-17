﻿using System.Text;

namespace UKHO.S100PermitService.Common.Helpers
{
    public class UserPermitApiClient : IUserPermitApiClient
    {
        private readonly HttpClient _httpClient;

        public UserPermitApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> GetUserPermitsAsync(string uri, int licenceId, string accessToken, string correlationId)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Content = new StringContent(licenceId.ToString(), Encoding.UTF8, "application/json");

            if(string.IsNullOrEmpty(accessToken))
            {
                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }

            httpRequestMessage.SetBearerToken(accessToken);
            httpRequestMessage.AddHeader("X-Correlation-ID", correlationId);

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
