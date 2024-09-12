namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
     Task CreatePermit(int licenceId);
    }
}