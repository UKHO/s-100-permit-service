using System.IO.Compression;

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

        public static async Task<string> DownloadZipFile(HttpResponseMessage response)
        {
            var zipFileName = "Permits.zip";
            var tempFilePath = Path.Combine(Path.GetTempPath(), "temp");
            if(!Directory.Exists(tempFilePath))
            {
                Directory.CreateDirectory(tempFilePath);
            }

            if(response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                
                await using(FileStream outputFileStream = new(Path.Combine(tempFilePath, zipFileName), FileMode.Create))
                {
                    await stream.CopyToAsync(outputFileStream);
                }
            }
            else
            {
                Console.WriteLine($"Failed to save response as zip due to: {response.StatusCode}");
            }

            var zipPath = Path.Combine(tempFilePath, zipFileName);
            var extractPath = Path.Combine(tempFilePath, RenameFolder(zipPath));
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return extractPath;
        }

        public static string RenameFolder(string pathInput)
        {
            string fileName = Path.GetFileName(pathInput);
            if(fileName.Contains(".zip"))
            {
                fileName = fileName.Replace(".zip", "");
            }

            return fileName;
        }
    }
}
