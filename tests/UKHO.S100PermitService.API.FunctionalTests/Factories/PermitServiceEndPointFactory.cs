using System.IO.Compression;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public static class PermitServiceEndPointFactory
    {
        private static readonly HttpClient _httpClient = new();
        private static string? _uri;
        private static readonly string _zipFileName = "Permits.zip";

        /// <summary>
        /// This method is used to interact with permits endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the baseUrl</param>
        /// <param name="accessToken">Sets the access Token</param>
        /// <param name="licenceId">Sets the licence ID</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> AsyncPermitServiceEndPoint(string? baseUrl, string? accessToken, string licenceId)
        {
            _uri = $"{baseUrl}/permits/{licenceId}";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _uri);
            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add("Authorization", "Bearer " + accessToken);
            }
            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This method is used to download the Permits.Zip File
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<string> AsyncDownloadZipFile(HttpResponseMessage response)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), "temp");
            if(!Directory.Exists(tempFilePath))
            {
                Directory.CreateDirectory(tempFilePath);
            }

            if(response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                await using FileStream outputFileStream = new(Path.Combine(tempFilePath, _zipFileName), FileMode.Create);
                await stream.CopyToAsync(outputFileStream);
            }
            else
            {
                Console.WriteLine($"Failed to save response as zip due to: {response.StatusCode}");
            }

            var zipPath = Path.Combine(tempFilePath, _zipFileName);
            var extractPath = Path.Combine(tempFilePath, RenameFolder(zipPath));
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            return extractPath;
        }

        /// <summary>
        /// This method is used to rename the .zip folder.
        /// </summary>
        /// <param name="pathInput"></param>
        /// <returns></returns>
        public static string RenameFolder(string pathInput)
        {
            var fileName = Path.GetFileName(pathInput);
            if(fileName.Contains(".zip"))
            {
                fileName = fileName.Replace(".zip", "");
            }
            return fileName;
        }
    }
}