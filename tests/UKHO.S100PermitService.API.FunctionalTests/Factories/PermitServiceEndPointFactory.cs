using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using static UKHO.S100PermitService.API.FunctionalTests.Models.S100PermitServiceRequestModel;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public static class PermitServiceEndPointFactory
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly string _zipFileName = "Permits.zip";
        private static ILogger _logger = NullLogger.Instance;

        public static void InitializeHttpClient(string baseUrl)
        {
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        /// <summary>
        /// This method is used to interact with permits endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the baseUrl</param>
        /// <param name="accessToken">Sets the access Token</param>
        /// <param name="payload">Provides the payload</param>
        /// <param name="isUrlValid">Sets the validity of url</param>
        /// <returns>Response of S-100 Permit Service Endpoint</returns>
        public static async Task<HttpResponseMessage> PermitServiceEndPointAsync(string? accessToken, RequestBodyModel payload, bool isUrlValid = true)
        {
            var uri = isUrlValid ? "/v1/permits/s100" : "/permits/s100";

            _logger.LogInformation("Calling S-100 Permit Service Endpoint: {uri}", uri);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            var payloadJson = JsonConvert.SerializeObject(payload);
            _logger.LogInformation("Request Payload: {payloadJson}", payloadJson);
            httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is used to download the Permits.Zip File
        /// </summary>
        /// <param name="response"></param>
        /// <returns>The path of the location where PERMIT.ZIP is downloaded and extracted</returns>
        public static async Task<string> DownloadZipFileAsync(HttpResponseMessage response)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "temp");
            Directory.CreateDirectory(tempRoot);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to save response as zip due to: {response.StatusCode}");
                // Still return a deterministic path for callers; no extraction.
                return tempRoot;
            }

            // Save response stream to a zip file
            var zipPath = Path.Combine(tempRoot, _zipFileName);
            await using (var outputFileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await stream.CopyToAsync(outputFileStream).ConfigureAwait(false);
            }

            // Extract to a folder named after the zip (without extension)
            var extractPath = Path.Combine(tempRoot, Path.GetFileNameWithoutExtension(zipPath));

            // Ensure clean extraction directory
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, recursive: true);
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            return extractPath;
        }

        /// <summary>
        /// This method is used to rename the .zip folder.
        /// </summary>
        /// <param name="pathInput"></param>
        /// <returns>The filename after renaming</returns>
        public static string RenameFolder(string pathInput)
        {
            return Path.GetFileNameWithoutExtension(pathInput);
        }

        /// <summary>
        /// This method is used to load the payload from the file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The payload after reading as Request Body</returns>
        public static async Task<RequestBodyModel> LoadPayloadAsync(string filePath)
        {
            var payload = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RequestBodyModel>(payload)!;
        }

        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }
    }
}