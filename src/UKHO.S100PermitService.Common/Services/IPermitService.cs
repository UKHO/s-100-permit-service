namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task CreatePermitAsync(int licenceId, string correlationId);
    }
}