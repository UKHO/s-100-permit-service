using WireMock.Server;
using WireMock.Settings;
namespace UKHO.S100PermitService.Stubs
{
    public class WireMockFactory
    {

        public static WireMockServer CreateServer()
        {
            var settings = new WireMockServerSettings
            {
                Port = 8080,
                StartAdminInterface = true,
                AllowPartialMapping = true
            };
            var server = WireMockServer.Start(settings);

            ConfigurePKSStub(server);

            ConfigureShopFacadeStub(server);

            return server;
        }

        private static void ConfigurePKSStub(WireMockServer server)
        {
            IStubService pksStub = new PKSStub();
            pksStub.ConfigureStub(server);
        }

        private static void ConfigureShopFacadeStub(WireMockServer server)
        {
            IStubService shopFacadeStub = new ShopFacadeStub();
            shopFacadeStub.ConfigureStub(server);

        }
    }
}