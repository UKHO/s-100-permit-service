using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;
        private readonly UserPermitsServiceConfiguration _userPermitsServiceConfiguration;

        public StubFactory(HoldingsServiceConfiguration holdingsServiceConfiguration, ProductKeyServiceConfiguration productKeyServiceConfiguration, UserPermitsServiceConfiguration userPermitsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration;
            _productKeyServiceConfiguration = productKeyServiceConfiguration;
            _userPermitsServiceConfiguration = userPermitsServiceConfiguration;
        }

        public IStub CreateHoldingsServiceStub()
        {
            return new HoldingsServiceStub(_holdingsServiceConfiguration);
        }

        public IStub CreateProductKeyServiceStub()
        {
            return new ProductKeyServiceStub(_productKeyServiceConfiguration);
        }

        public IStub CreateUserPermitsServiceStub()
        {
            return new UserPermitsServiceStub(_userPermitsServiceConfiguration);
        }
    }
}