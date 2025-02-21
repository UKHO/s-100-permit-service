using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using static UKHO.S100PermitService.API.FunctionalTests.Models.S100PermitServiceRequestModel;

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
        /// <param name="payload">Provides the payload</param>
        /// <param name="isUrlValid">Sets the validity of url</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> AsyncPermitServiceEndPoint(string? baseUrl, string? accessToken, RequestBodyModel payload, bool isUrlValid = true)
        {
            _uri = $"{baseUrl}/v1/permits/s100";
            if(!isUrlValid)
            {
                _uri = $"{baseUrl}/permits/s100";
            }
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _uri);
            var payloadJson = JsonConvert.SerializeObject(payload);
            Console.WriteLine($"Payload: {payload}");
            httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
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

        /// <summary>
        /// This method is used to load the payload from the file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<RequestBodyModel> LoadPayload(string filePath)
        {
            using(var reader = new StreamReader(filePath))
            {
                var payload = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<RequestBodyModel>(payload)!;
            }
        }
    }
}