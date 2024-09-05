using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitXmlService : IPermitXmlService
    {
        private const string DateFormat = "yyyy-MM-ddzzz";

        public Permit MapDataToPermit(DateTimeOffset issueDate, string dataServerIdentifier, string dataServerName, string userPermit,decimal version, List<products> products)
        {
            var productsList = new List<products>();
            productsList.AddRange(products);
            return new Permit()
            {
                header = new header()
                {
                    issueDate = issueDate.ToString(DateFormat),
                    dataServerIdentifier = dataServerIdentifier,
                    dataServerName = dataServerName,
                    userpermit = userPermit,
                    version = version
                },
                products = productsList.ToArray()
            };
        }
    }
}