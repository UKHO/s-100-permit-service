using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.API.Filters
{
    [ExcludeFromCodeCoverage]
    public class AddHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Responses ??= [];

            foreach(var response in operation.Responses)
            {
                response.Value.Headers.Add("X-Correlation-ID", new OpenApiHeader
                {
                    Description = "GUID for the request for logging/tracing",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
            }
        }
    }
}