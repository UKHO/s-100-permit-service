using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
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

        public List<UpnInfo> GetDecryptedHardwareIdFromUserPermit(UserPermitServiceResponse userPermitServiceResponse)
        {
            _logger.LogInformation(EventIds.GetHwIdFromUserPermitStarted.ToEventId(), "Get decrypted hardware id from user permits started");

            List<UpnInfo> listOfUpnInfo = [];
            UpnInfo upnInfo = new();

            foreach(var userPermit in userPermitServiceResponse.UserPermits)
            {
                var encryptedHardwareId = userPermit.Upn[..EncryptedHardwareIdLength];

                var mId = userPermit.Upn[^MIdLength..];

                var mKey = _manufacturerKeyService.GetManufacturerKeys(mId);

                if(mKey?.Length != KeySizeEncoded)
                {
                    throw new PermitServiceException(EventIds.InvalidMKey.ToEventId(),
                        "Invalid mKey found from Cache/KeyVault, Expected length is {KeySizeEncoded}, but mKey length is {mKeyKength}",
                        KeySizeEncoded, mKey.Length);
                }

                var hardwareId = _aesEncryption.Decrypt(encryptedHardwareId, mKey);

                upnInfo.Upn = userPermit.Upn;
                upnInfo.DecryptedHardwareId = hardwareId;

                listOfUpnInfo.Add(upnInfo);
            }

            _logger.LogInformation(EventIds.GetHwIdFromUserPermitCompleted.ToEventId(), "Get decrypted hardware id from user permits completed");

            return listOfUpnInfo;
        }
    }
}