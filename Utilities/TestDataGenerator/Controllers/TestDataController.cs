using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using TestData.Models;
using UKHO.S100PermitService.Common.Encryption;

namespace TestDataGenerator1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestDataController : ControllerBase
    {
        private readonly IAesEncryption _aesEncryption;

        private const int EncryptedHardwareIdLength = 32;
        private const string WellknownHardwareId = "7D3A583E6CB6F32FD0B0328AF006A2BD";

        public TestDataController(IAesEncryption aesEncryption)
        {
            _aesEncryption = aesEncryption;
        }

        [HttpGet]
        [Route("/testdata/GenerateUserPermit")]
        public virtual async Task<IActionResult> GenerateUserPermit()
        {
            var upn = CreateUserPermit();

            await Task.CompletedTask;

            return new JsonResult(upn);
        }

        [HttpPost]
        [Route("/testdata/GenerateProductKey")]
        public virtual async Task<IActionResult> GenerateProductKey()
        {
            var dataKey = CreateRandomHex32String();
            var encryptedProductKey = _aesEncryption.Encrypt(dataKey, WellknownHardwareId);

            var productKey = new ProductKey
            {
                Key = encryptedProductKey,
                DecryptedKey = dataKey,
                HardwareId = WellknownHardwareId
            };

            await Task.CompletedTask;

            return new JsonResult(productKey);
        }

        [HttpGet]
        [Route("/testdata/ExtractHwIdFromUPN")]
        public virtual async Task<IActionResult> ExtractHwIdFromUPN(string upn, string mKey)
        {
            var decryptedHardwareId = _aesEncryption.Decrypt(upn[..EncryptedHardwareIdLength], mKey);

            await Task.CompletedTask;

            return new JsonResult(decryptedHardwareId);
        }

        [HttpGet]
        [Route("/testdata/DecryptProductKey")]
        public virtual async Task<IActionResult> DecryptProductKey(string productKey)
        {
            var decryptedProductKey = _aesEncryption.Decrypt(productKey, WellknownHardwareId);

            await Task.CompletedTask;

            return new JsonResult(decryptedProductKey);
        }

        [HttpGet]
        [Route("/testdata/CreateEncryptedKey")]
        public virtual async Task<IActionResult> CreateEncryptedKey(string decryptedProductKey, string hwId)
        {
            var encryptedKey = _aesEncryption.Decrypt(decryptedProductKey, hwId);

            await Task.CompletedTask;

            return new JsonResult(encryptedKey);
        }

        [NonAction]
        public Upn CreateUserPermit()
        {
            var mId = GenerateRandomMIDString();
            var mKey = CreateRandomHex32String();
            var hwId = CreateRandomHex32String();

            var hwIdEncrypted = _aesEncryption.Encrypt(hwId, mKey);
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

        [NonAction]
        public string GetEncryptedHwIdCRC(string hwIdEncrypted)
        {
            var crc = new Crc32();
            crc.Update(Encoding.UTF8.GetBytes(hwIdEncrypted));
            var calculatedChecksum = crc.Value.ToString("X8");
            return calculatedChecksum;
        }

        /// <summary>
        /// 6 character random digits
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public string GenerateRandomMIDString()
        {
            var random = new Random();
            return new string(Enumerable.Range(0, 6).Select(_ => random.Next(0, 10).ToString()[0]).ToArray());
        }

        /// <summary>
        /// 32 character hexadecimal random digits.
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public string CreateRandomHex32String()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Create().GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}