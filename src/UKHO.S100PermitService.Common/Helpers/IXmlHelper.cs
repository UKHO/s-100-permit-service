using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IXmlHelper
    {
        public string GetPermitXmlString(Permit permit);

    }
}