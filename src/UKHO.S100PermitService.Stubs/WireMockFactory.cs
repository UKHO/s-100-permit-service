using UKHO.S100PermitService.Stubs.Service;
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
            IStubConfiguration pksService = new PKSService();
            pksService.ConfigureStub(server);
        }

        private static void ConfigureShopFacadeStub(WireMockServer server)
        {
            IStubConfiguration shopFacadeService = new ShopFacadeService();
            shopFacadeService.ConfigureStub(server);

        }
    }
}