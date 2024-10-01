using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private const int KeySizeEncoded = 32;
        private const int MIdLength = 6;
        private const int EncryptedHardwareIdLength = 32;

        private readonly IAesEncryption _aesEncryption;
        private readonly IManufacturerKeyService _manufacturerKeyService;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, IManufacturerKeyService manufacturerKeyService, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _manufacturerKeyService =
                manufacturerKeyService ?? throw new ArgumentNullException(nameof(manufacturerKeyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetDecryptedHardwareIdFromUserPermit(string upn)
        {
            _logger.LogInformation(EventIds.GetHwIdFromUserPermitStarted.ToEventId(), "Get decrypted hardware id from user permit started");

            var encryptedHardwareId = upn[..EncryptedHardwareIdLength];

            var mId = upn[^MIdLength..];

            var mKey = _manufacturerKeyService.GetManufacturerKeys(mId);

            if(mKey?.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.InvalidMKey.ToEventId(), "Invalid mKey found from Cache/KeyVault, Expected length is {0}, but mKey length is {1}", KeySizeEncoded, mKey.Length);
            }

            var hardwareId = _aesEncryption.Decrypt(encryptedHardwareId, mKey);

            _logger.LogInformation(EventIds.GetHwIdFromUserPermitCompleted.ToEventId(), "Get decrypted hardware id from user permit completed");

            return hardwareId;
        }
    }
}