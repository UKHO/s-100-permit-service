using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;

        public StubFactory(IOptions<HoldingsServiceConfiguration> holdingsServiceConfiguration, IOptions<ProductKeyServiceConfiguration> productKeyServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration.Value;
            _productKeyServiceConfiguration = productKeyServiceConfiguration.Value;
        }

        public IStub CreateHoldingsServiceStub()
        {
            return new HoldingsServiceStub(_holdingsServiceConfiguration);
        }

        public IStub CreateProductKeyServiceStub()
        {
            return new ProductKeyServiceStub(_productKeyServiceConfiguration);
        }
    }
}