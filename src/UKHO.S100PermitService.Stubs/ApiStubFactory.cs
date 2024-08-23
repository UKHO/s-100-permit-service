using UKHO.S100PermitService.Stubs.Configuration;
using UKHO.S100PermitService.Stubs.Stubs;

namespace UKHO.S100PermitService.Stubs
{
    public class ApiStubFactory
    {
        private readonly ShopFacadeApi _shopFacadeApi;
        private readonly ProductKeyServiceApi _productKeyServiceApi;

        public ApiStubFactory(ShopFacadeApi shopFacadeApi, ProductKeyServiceApi productKeyServiceApi)
        {
            _shopFacadeApi = shopFacadeApi;
            _productKeyServiceApi = productKeyServiceApi;
        }

        public IApiStub CreateShopFacadeApiStub()
        {
            return new ShopFacadeApiStub(_shopFacadeApi);
        }

        public IApiStub CreateProductKeyServiceApiStub()
        {
            return new ProductKeyServiceApiStub(_productKeyServiceApi);
        }
    }

}
