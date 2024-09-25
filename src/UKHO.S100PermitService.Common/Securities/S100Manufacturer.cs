using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Securities
{
    public class S100Manufacturer : IS100Manufacturer
    {
        private readonly IS100Crypt _s100Crypt;
        private readonly ILogger<S100Manufacturer> _logger;

        public S100Manufacturer(IS100Crypt s100Crypt, ILogger<S100Manufacturer> logger)
        {
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string DecryptData(string hexString, string keyHexEncoded)
        {
            _logger.LogInformation(EventIds.DecryptionStarted.ToEventId(), "Decryption started");

            var decryptedData = _s100Crypt.Decrypt(hexString, keyHexEncoded);

            _logger.LogInformation(EventIds.DecryptionCompleted.ToEventId(), "Decryption completed");

            return decryptedData;
        }
    }
}