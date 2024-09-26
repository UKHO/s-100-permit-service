using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private readonly IAesEncryption _aesEncryption;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string DecryptData(string hexString, string keyHexEncoded)
        {
            _logger.LogInformation(EventIds.DecryptionStarted.ToEventId(), "Decryption started");

            var decryptedData = _aesEncryption.Decrypt(hexString, keyHexEncoded);

            _logger.LogInformation(EventIds.DecryptionCompleted.ToEventId(), "Decryption completed");

            return decryptedData;
        }
    }
}