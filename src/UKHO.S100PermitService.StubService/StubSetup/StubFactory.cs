using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;
        private readonly UserPermitsServiceConfiguration _userPermitsServiceConfiguration;

        public StubFactory(IOptions<HoldingsServiceConfiguration> holdingsServiceConfiguration, IOptions<ProductKeyServiceConfiguration> productKeyServiceConfiguration, IOptions<UserPermitsServiceConfiguration> userPermitsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration?.Value ?? throw new ArgumentNullException(nameof(holdingsServiceConfiguration));           
            _productKeyServiceConfiguration = productKeyServiceConfiguration?.Value ?? throw new ArgumentNullException(nameof(productKeyServiceConfiguration)); ;
            _userPermitsServiceConfiguration = userPermitsServiceConfiguration?.Value ?? throw new ArgumentNullException(nameof(userPermitsServiceConfiguration)); ;
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