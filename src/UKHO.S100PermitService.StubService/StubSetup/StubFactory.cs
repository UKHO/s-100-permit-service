using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;

        public StubFactory(HoldingsServiceConfiguration holdingsServiceConfiguration, ProductKeyServiceConfiguration productKeyServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration;
            _productKeyServiceConfiguration = productKeyServiceConfiguration;
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