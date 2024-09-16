using UKHO.S100PermitService.Common.Models.PermitService;

namespace UKHO.S100PermitService.Common.IO
{
    public interface IPermitReaderWriter
    {
        public string ReadPermit(Permit permit);
        public void WritePermit(string fileContent);
    }
}
