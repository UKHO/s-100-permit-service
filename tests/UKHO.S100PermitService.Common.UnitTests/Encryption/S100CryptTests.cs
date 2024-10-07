using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Encryption
{
    [TestFixture]
    public class S100CryptTests
    {
        private IAesEncryption _fakeAesEncryption;
        private IManufacturerKeyService _fakeManufacturerKeyService;
        private ILogger<S100Crypt> _fakeLogger;

        private S100Crypt _s100Crypt;

        [SetUp]
        public void Setup()
        {
            _fakeAesEncryption = A.Fake<IAesEncryption>();
            _fakeManufacturerKeyService = A.Fake<IManufacturerKeyService>();
            _fakeLogger = A.Fake<ILogger<S100Crypt>>();

            _s100Crypt = new S100Crypt(_fakeAesEncryption, _fakeManufacturerKeyService, _fakeLogger);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAesEncryption = () => new S100Crypt(null, _fakeManufacturerKeyService, _fakeLogger);
            nullAesEncryption.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("aesEncryption");

            Action nullManufacturerKeyService = () => new S100Crypt(_fakeAesEncryption, null, _fakeLogger);
            nullManufacturerKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("manufacturerKeyService");

            Action nullLogger = () => new S100Crypt(_fakeAesEncryption, _fakeManufacturerKeyService, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [Test]
        public void WhenMKeyExpectedLengthDoesNotMatch_ThenThrowPermitServiceException()
        {
            const string FakeMKey = "invalidMKey";

            A.CallTo(() => _fakeManufacturerKeyService.GetManufacturerKeys(A<string>.Ignored)).Returns(FakeMKey);

            FluentActions.Invoking(() => _s100Crypt.GetDecryptedHardwareIdFromUserPermit(GetUpnInfoWithDecryptedHardwareId())).Should().Throw<PermitServiceException>().WithMessage("Invalid mKey found from Cache/KeyVault, Expected length is {KeySizeEncoded}, but mKey length is {mKeyLength}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permits started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permits completed"
            ).MustNotHaveHappened();
        }

        [Test]
        public void WhenValidMKeyAndUpnInfo_ThenListOfDecryptedHardwareIdIsReturned()
        {
            const string FakeDecryptedHardwareId = "86C520323CEA3056B5ED7000F98814CB";

            const string FakeMKey = "validMKey12345678901234567890123";

            A.CallTo(() => _fakeManufacturerKeyService.GetManufacturerKeys(A<string>.Ignored)).Returns(FakeMKey);

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored)).Returns(FakeDecryptedHardwareId);

            var result = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(GetUpnInfoWithDecryptedHardwareId());

            result.Equals(GetUpnInfo());

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permits started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>

                call.Method.Name == "Log"
                    && call.GetArgument<LogLevel>(0) == LogLevel.Information
                    && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitCompleted.ToEventId()
                    && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permits completed"
                ).MustHaveHappenedOnceExactly();
        }

        private static List<UpnInfo> GetUpnInfo()
        {
            return
            [
                new UpnInfo()
                {
                    EncryptedHardwareId = "FE5A853DEF9E83C9FFEF5AA001478103",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    MId = "A1B2C3",
                    Crc32 = "DB74C038"
                },
                new UpnInfo()
                {
                    EncryptedHardwareId = "869D4E0E902FA2E1B934A3685E5D0E85",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6",
                    MId = "D4E5F6",
                    Crc32 = "C1FDEC8B"
                }
            ];
        }

        private static List<UpnInfo> GetUpnInfoWithDecryptedHardwareId()
        {
            return
            [
                new UpnInfo()
                {
                    HardwareId = "86C520323CEA3056B5ED7000F98814CB",
                    EncryptedHardwareId = "FE5A853DEF9E83C9FFEF5AA001478103",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    MId = "A1B2C3",
                    Crc32 = "DB74C038"
                },
                new UpnInfo()
                {
                    HardwareId = "B2C0F91ADAAEA51CC5FCCA05C47499E4",
                    EncryptedHardwareId = "869D4E0E902FA2E1B934A3685E5D0E85",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6",
                    MId = "D4E5F6",
                    Crc32 = "C1FDEC8B"
                }
            ];
        }
    }
}
