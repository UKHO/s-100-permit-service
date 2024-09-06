using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        public void CreatePermit(DateTimeOffset issueDate, string dataServerIdentifier, string dataServerName,
            string userPermit, decimal version, List<Products> products);

    }
}