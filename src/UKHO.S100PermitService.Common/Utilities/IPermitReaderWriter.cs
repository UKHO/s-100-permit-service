using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Utilities
{
    public interface IPermitReaderWriter
    {
        public string ReadPermit(Permit permit);
        public void WritePermit(string fileContent);
    }
}
