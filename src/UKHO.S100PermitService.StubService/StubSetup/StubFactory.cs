using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubFactory
    {
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;

        public StubFactory(IOptions<ProductKeyServiceConfiguration> productKeyServiceConfiguration)
        {
            _productKeyServiceConfiguration = productKeyServiceConfiguration?.Value ?? throw new ArgumentNullException(nameof(productKeyServiceConfiguration));
        }

        public IStub CreateProductKeyServiceStub()
        {
            return new ProductKeyServiceStub(_productKeyServiceConfiguration);
        }
    }
}