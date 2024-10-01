using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Encryption
{
    [TestFixture]
    public class S100CryptTests
    {
        private IAesEncryption _fakeAesEncryption;
        private IManufacturerKeyService _manufacturerKeyService;
        private ILogger<S100Crypt> _fakeLogger;

        private S100Crypt _s100Crypt;

        const string Upn = "encryptedHardwareId1234567890123ChecksummId123";

        [SetUp]
        public void Setup()
        {
            _fakeAesEncryption = A.Fake<IAesEncryption>();
            _manufacturerKeyService = A.Fake<IManufacturerKeyService>();
            _fakeLogger = A.Fake<ILogger<S100Crypt>>();

            _s100Crypt = new S100Crypt(_fakeAesEncryption, _manufacturerKeyService, _fakeLogger);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAesEncryption = () => new S100Crypt(null, _manufacturerKeyService, _fakeLogger);
            nullAesEncryption.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("aesEncryption");

            Action nullManufacturerKeyService = () => new S100Crypt(_fakeAesEncryption, null, _fakeLogger);
            nullManufacturerKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("manufacturerKeyService");

            Action nullLogger = () => new S100Crypt(_fakeAesEncryption, _manufacturerKeyService, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [Test]
        public async Task WhenMKeyExpectedLengthDoesNotMatch_ThenThrowPermitServiceException()
        {
            const string MKey = "invalidMKey";

            A.CallTo(() => _manufacturerKeyService.GetManufacturerKeys(A<string>.Ignored)).Returns(MKey);

            await FluentActions.Invoking(async () => _s100Crypt.GetDecryptedHardwareIdFromUserPermit(Upn)).Should().ThrowAsync<PermitServiceException>().WithMessage("Invalid mKey found from Cache/KeyVault, Expected length is {0}, but mKey length is {1}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permit completed"
            ).MustNotHaveHappened();
        }

        [Test]
        public void WhenValidmKeyAndUpn_ThenDecryptedHardwareIdIsReturned()
        {
            
            const string ExpectedHardwareId = "decryptedHardwareId1234567890mId";
            const string MKey = "validMKey12345678901234567890123";

            A.CallTo(() => _manufacturerKeyService.GetManufacturerKeys(A<string>.Ignored)).Returns(MKey);

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored)).Returns(ExpectedHardwareId);

            var result = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(Upn);

            result.Equals(ExpectedHardwareId);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetHwIdFromUserPermitCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted hardware id from user permit completed"
            ).MustHaveHappenedOnceExactly();
        }
    }
}
