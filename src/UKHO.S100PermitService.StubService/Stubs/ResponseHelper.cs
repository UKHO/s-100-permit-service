using System.Net;
using System.Text.Json;

namespace UKHO.S100PermitService.StubService.Stubs
{
    public static class ResponseHelper
    {
        public static string UpdateCorrelationIdInResponse(string filePath, string correlationId, HttpStatusCode statusCode)
        {
            var responseBody = File.ReadAllText(filePath);
            if(statusCode == HttpStatusCode.BadRequest || statusCode == HttpStatusCode.NotFound)
            {
                var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
                jsonResponse!["correlationId"] = correlationId;
                responseBody = JsonSerializer.Serialize(jsonResponse);
            }
            return responseBody;
        }
    }

}
