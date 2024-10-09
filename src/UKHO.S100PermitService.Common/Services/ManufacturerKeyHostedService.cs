using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Services
{
    [ExcludeFromCodeCoverage]
    public class ManufacturerKeyHostedService : IHostedService
    {
        private readonly IManufacturerKeyService _manufacturerKeyService;

        public ManufacturerKeyHostedService(IManufacturerKeyService manufacturerKeyService)
        {
            _manufacturerKeyService = manufacturerKeyService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _manufacturerKeyService.CacheManufacturerKeys();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
