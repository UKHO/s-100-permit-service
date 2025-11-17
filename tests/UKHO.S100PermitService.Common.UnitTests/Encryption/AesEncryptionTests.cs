using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.UnitTests.Encryption
{
    [TestFixture]
    public class AesEncryptionTests
    {
        private const string FakeText = "00112233445566778899AABBCCDDEEFF";
        private const string FakeKey = "000102030405060708090A0B0C0D0E0F";
        private IAesEncryption _aesEncryption;

        [SetUp]
        public void SetUp()
        {
            _aesEncryption = new AesEncryption();
        }

        [Test]
        public async Task WhenProvidedValidData_ThenSuccessfullyReturnsDecryptedData()
        {
            var result = await _aesEncryption.DecryptAsync(FakeText, FakeKey);

            Assert.That(result, Is.Not.Null.Or.Empty);
            Assert.That(result, Is.Not.EqualTo(FakeText));
        }

        [Test]
        public async Task WhenInvalidHexStringIsPassed_ThenThrowException()
        {
            var ex = Assert.ThrowsAsync<AesEncryptionException>(() => _aesEncryption.DecryptAsync("123456", FakeKey));
            Assert.That(ex.Message, Is.EqualTo("Expected hex string length {HexSize}, but found {HexString Length}."));
        }

        [Test]
        public async Task WhenInvalidHexKeyIsPassed_ThenThrowException()
        {
            var ex = Assert.ThrowsAsync<AesEncryptionException>(() => _aesEncryption.DecryptAsync(FakeText, "123456"));
            Assert.That(ex.Message, Is.EqualTo("Expected hex key length {HexSize}, but found {HexKey Length}."));
        }

        [Test]
        public async Task WhenValidDataProvided_ThenSuccessfullyReturnsEncryptedData()
        {
            var result = await _aesEncryption.EncryptAsync(FakeText, FakeKey);

            Assert.That(result, Is.Not.Null.Or.Empty);
            Assert.That(result, Is.Not.EqualTo(FakeText));
        }

        [Test]
        public async Task WhenInvalidHexStringIsPassedToEncryption_ThenThrowException()
        {
            var ex = Assert.ThrowsAsync<AesEncryptionException>(() => _aesEncryption.EncryptAsync("123456", FakeKey));
            Assert.That(ex.Message, Is.EqualTo("Expected hex string length {HexSize}, but found {HexString Length}."));
        }

        [Test]
        public async Task WhenInvalidHexKeyIsPassedToEncryption_ThenThrowException()
        {
            var ex = Assert.ThrowsAsync<AesEncryptionException>(() => _aesEncryption.EncryptAsync(FakeText, "123456"));
            Assert.That(ex.Message, Is.EqualTo("Expected hex key length {HexSize}, but found {HexKey Length}."));
        }
    }
}