using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;
        private readonly ILogger<HoldingsServiceStub> _holdingsServiceStubLogger;
        private readonly ILogger<ProductKeyServiceStub> _productKeyServiceStubLogger;

        public StubFactory(HoldingsServiceConfiguration holdingsServiceConfiguration, ProductKeyServiceConfiguration productKeyServiceConfiguration, ILogger<HoldingsServiceStub> holdingsServiceStubLogger, ILogger<ProductKeyServiceStub> productKeyServiceStubLogger)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration ?? throw new ArgumentNullException(nameof(holdingsServiceConfiguration));
            _productKeyServiceConfiguration = productKeyServiceConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceConfiguration));
            _holdingsServiceStubLogger = holdingsServiceStubLogger ?? throw new ArgumentNullException(nameof(holdingsServiceStubLogger));
            _productKeyServiceStubLogger = productKeyServiceStubLogger ?? throw new ArgumentNullException(nameof(productKeyServiceStubLogger));
        }

        public IStub CreateHoldingsServiceStub()
        {
            return new HoldingsServiceStub(_holdingsServiceConfiguration, _holdingsServiceStubLogger);
        }

        public IStub CreateProductKeyServiceStub()
        {
            return new ProductKeyServiceStub(_productKeyServiceConfiguration, _productKeyServiceStubLogger);
        }
    }
}