using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TestData.Models;
using UKHO.S100PermitService.Common.Encryption;

namespace TestData.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestDataController : ControllerBase
    {
        private readonly ILogger<TestDataController> _logger;
        private readonly IS100Crypt _s100Crypt;
        private readonly IAesEncryption _aesEncryption;

        private const int EncryptedHardwareIdLength = 32;
        private const string WellknownHardwareId = "7D3A583E6CB6F32FD0B0328AF006A2BD";

        public TestDataController(ILogger<TestDataController> logger, IS100Crypt s100Crypt, IAesEncryption aesEncryption)
        {
            _logger = logger;
            _s100Crypt = s100Crypt;
            _aesEncryption = aesEncryption;
        }

        [HttpPost]
        [Route("/testdata/GenerateUserPermit")]
        public virtual async Task<IActionResult> GenerateUserPermit()
        {
            var upn = CreateUserPermit();

            return HttpStatusCode.Created;
        }

        [HttpPost]
        [Route("/testdata/GenerateProductKey")]
        public virtual async Task<IActionResult> GenerateProductKey()
        {
            var dataKey = GenerateRandomDataKeyString(32);
            var encryptedProductKey = _aesEncryption.Encrypt(dataKey, WellknownHardwareId);

            var productKey = new ProductKey
            {
                Key = encryptedProductKey,
                DecryptedKey = dataKey,
                HardwareId = WellknownHardwareId
            };

            return HttpStatusCode.Created;
        }

        [HttpGet]
        [Route("/testdata/ExtractHwIdFromUPN")]
        public virtual async Task<IActionResult> ExtractHwIdFromUPN(string upn, string mKey)
        {
            var decryptedHardwareId = _aesEncryption.Decrypt(upn[..EncryptedHardwareIdLength], mKey);


            return HttpStatusCode.Created;
        }

        [HttpGet]
        [Route("/testdata/DecryptProductKey")]
        public virtual async Task<IActionResult> DecryptProductKey(string productKey, string hwId)
        {
            var decryptedProductKey = _aesEncryption.Encrypt(productKey, WellknownHardwareId);

            return HttpStatusCode.Created;
        }

        [HttpGet]
        [Route("/testdata/CreateEncryptedKey")]
        public virtual async Task<IActionResult> CreateEncryptedKey(string hwId, string decryptedProductKey)
        {
            var encryptedKey = _aesEncryption.Decrypt(decryptedProductKey, hwId);

            return HttpStatusCode.Created;
        }

        public Upn CreateUserPermit()
        {
            var mId = GenerateRandomMIDString();
            var mKey = GenerateRandomMKEYString(32);
            var hwId = CreateRandomHwId();

            var hwIdEncrypted = _aesEncryption.Encrypt(mKey, hwId);
            var checksum = GetEncryptedHwIdCRC(hwIdEncrypted);

            var upn = new Upn
            {
                MId = mId,
                MKey = mKey,
                HwId = hwId,
                HwIdEncrypted = hwIdEncrypted,
                Crc32 = checksum,
                CompleteUserPermit = hwIdEncrypted + checksum + mId
            };

            return upn;
        }

        public string GetEncryptedHwIdCRC(string hwIdEncrypted)
        {
            var crc = new Crc32();
            crc.Update(Encoding.UTF8.GetBytes(hwIdEncrypted));
            var calculatedChecksum = crc.Value.ToString("X8");
            return calculatedChecksum;
        }

        public string GenerateRandomMIDString()
        {
            Random random = new Random();
            string numericData = string.Empty;

            for(int i = 0 ; i < 6 ; i++)
            {
                numericData += random.Next(0, 10).ToString();
            }

            return new string(numericData);
        }

        public string GenerateRandomMKEYString(int length)
        {
            const string hexChars = "0123456789ABCD";
            Random random = new Random();
            char[] buffer = new char[length];

            for(int i = 0 ; i < length ; i++)
            {
                buffer[i] = hexChars[random.Next(hexChars.Length)];
            }

            return new string(buffer);
        }

        public string GenerateRandomDataKeyString(int length)
        {
            const string hexChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Random random = new Random();
            char[] buffer = new char[length];

            for(int i = 0 ; i < length ; i++)
            {
                buffer[i] = hexChars[random.Next(hexChars.Length)];
            }

            return new string(buffer);
        }

        public string CreateRandomHwId()
        {
            byte[] bytes = new byte[16];
            RandomNumberGenerator.Create().GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
