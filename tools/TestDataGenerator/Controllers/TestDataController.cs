using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using TestDataGenerator.Models;
using UKHO.S100PermitService.Common.Encryption;

namespace TestDataGenerator.Controllers
{
    [ApiController]
    public class TestDataController : ControllerBase
    {
        private const int EncryptedHardwareIdLength = 32;

        private readonly IAesEncryption _aesEncryption;
        private readonly IConfiguration _configuration;

        public TestDataController(IAesEncryption aesEncryption, IConfiguration configuration)
        {
            _aesEncryption = aesEncryption;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/GenerateUserPermit")]
        public virtual async Task<IActionResult> GenerateUserPermit()
        {
            var upn = CreateUserPermit();

            await Task.CompletedTask;

            return new JsonResult(upn);
        }

        [HttpGet]
        [Route("/GenerateProductKey")]
        public virtual async Task<IActionResult> GenerateProductKey()
        {
            var dataKey = CreateRandomHex32String();
            var encryptedProductKey = await _aesEncryption.EncryptAsync(dataKey, _configuration["HardwareId"]);

            var productKey = new ProductKey
            {
                Key = encryptedProductKey,
                DecryptedKey = dataKey,
                HardwareId = _configuration["HardwareId"]
            };

            await Task.CompletedTask;

            return new JsonResult(productKey);
        }

        [HttpGet]
        [Route("/ExtractHwIdFromUPN")]
        public virtual async Task<IActionResult> ExtractHwIdFromUPN(string upn, string mKey)
        {
            var decryptedHardwareId = await _aesEncryption.DecryptAsync(upn[..EncryptedHardwareIdLength], mKey);

            await Task.CompletedTask;

            return new JsonResult(decryptedHardwareId);
        }

        [HttpGet]
        [Route("/DecryptProductKey")]
        public virtual async Task<IActionResult> DecryptProductKey(string productKey)
        {
            var decryptedProductKey = await _aesEncryption.DecryptAsync(productKey, _configuration["HardwareId"]);

            await Task.CompletedTask;

            return new JsonResult(decryptedProductKey);
        }

        [HttpGet]
        [Route("/CreateEncryptedKey")]
        public virtual async Task<IActionResult> CreateEncryptedKey(string decryptedProductKey, string hwId)
        {
            var encryptedKey = await _aesEncryption.EncryptAsync(decryptedProductKey, hwId);

            await Task.CompletedTask;

            return new JsonResult(encryptedKey);
        }

        [NonAction]
        public async Task<Upn> CreateUserPermit()
        {
            var mId = GenerateRandomMIDString();
            var mKey = CreateRandomHex32String();
            var hwId = CreateRandomHex32String();

            var hwIdEncrypted = await _aesEncryption.EncryptAsync(hwId, mKey);
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