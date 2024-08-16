namespace UKHO.S100PermitService.Stubs
{
    public class Program
    {
        static void Main(string[] args)
        {
            var server = WireMockFactory.CreateServer();
            Console.WriteLine("WireMockServer running at {0}", string.Join(",", server.Ports));
            Console.WriteLine("Press any key to stop the server");
            Console.ReadKey();            
            server.Stop();
        }
    }
}
