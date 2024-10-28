using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Encryption
{
    [TestFixture]
    public class S100CryptTests
    {
        private const string FakeHardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD";

        private IAesEncryption _fakeAesEncryption;
        private IManufacturerKeyService _fakeManufacturerKeyService;
        private ILogger<S100Crypt> _fakeLogger;
        private IS100Crypt _s100Crypt;

        [SetUp]
        public void SetUp()
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
        public void WhenProductKeysDecryptedSuccessfully_ThenReturnsDecryptedKeys()
        {
            var test101ProductKey = "20191817161514131211109876543210";
            var test102ProductKey = "36353433323130292827262524232221";

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored))
                                             .Returns(test101ProductKey).Once().Then.Returns(test102ProductKey);

            var result = _s100Crypt.GetDecryptedKeysFromProductKeys(GetProductKeyServiceResponse(), FakeHardwareId);

            result.Should().NotBeNull();
            result.FirstOrDefault().DecryptedKey.Should().Be(test101ProductKey);
            result.LastOrDefault().DecryptedKey.Should().Be(test102ProductKey);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.DecryptProductKeysStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Decryption of product keys started."
            ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>

                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.DecryptProductKeysCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Decryption of product keys completed."
            ).MustHaveHappened();
        }

        [Test]
        public void WhenValidMKeyAndUpnInfo_ThenListOfDecryptedHardwareIdIsReturned()
        {
            const string FakeDecryptedHardwareId = "86C520323CEA3056B5ED7000F98814CB";
            const string FakeMKey = "validMKey12345678901234567890123";

            A.CallTo(() => _fakeManufacturerKeyService.GetManufacturerKeys(A<string>.Ignored)).Returns(FakeMKey);

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored)).Returns(FakeDecryptedHardwareId);

            var result = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(GetUserPermitServiceResponse());

            result.Equals(GetUpnInfo());

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ExtractDecryptedHardwareIdFromUserPermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extraction of decrypted HW_ID from user permits started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>

                call.Method.Name == "Log"
                    && call.GetArgument<LogLevel>(0) == LogLevel.Information
                    && call.GetArgument<EventId>(1) == EventIds.ExtractDecryptedHardwareIdFromUserPermitCompleted.ToEventId()
                    && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extraction of decrypted HW_ID from user permits completed."
                ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenValidHardwareIdAndPKSKeyPassed_ThenEncryptedKeyIsReturned()
        {
            const string FakeEncryptedKey = "86C520323CEA3056B5ED7000F98814CB";
            const string FakeKey = "2F72DDDD2144B24939KBKPS76FH52FDD1";
            const string FakeHardwareId = "H5P2P62BDDBHS32PM6PSSA256P2000A1";

            A.CallTo(() => _fakeAesEncryption.Encrypt(A<string>.Ignored, A<string>.Ignored)).Returns(FakeEncryptedKey);

            var result = _s100Crypt.CreateEncryptedKey(FakeKey, FakeHardwareId);
            
            result.Equals(FakeEncryptedKey);            
        }

        private List<ProductKeyServiceResponse> GetProductKeyServiceResponse()
        {
            return
            [
                new()
                {
                    Edition = "1",
                    Key = "01234567891011121314151617181920",
                    ProductName = "test101"
                },
                new()
                {
                    Edition = "1",
                    Key = "21222324252627282930313233343536",
                    ProductName = "test102"
                }
            ];
        }

        private static List<UpnInfo> GetUpnInfo()
        {
            return
            [
                new UpnInfo()
                {
                    DecryptedHardwareId = "86C520323CEA3056B5ED7000F98814CB",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                },
                new UpnInfo()
                {
                    DecryptedHardwareId = "B2C0F91ADAAEA51CC5FCCA05C47499E4",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                }
            ];
        }

        private static UserPermitServiceResponse GetUserPermitServiceResponse()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6" },
                    new UserPermit{ Title = "Navi Radar", Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9" }
                ]
            };
        }
    }
}