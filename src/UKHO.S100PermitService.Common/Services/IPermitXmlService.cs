using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitXmlService
    {
        public Permit MapDataToPermit(DateTimeOffset issueDate, string dataServerIdentifier, string dataServerName,
            string userPermit, decimal version, List<products> products);

    }
}