using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.UnitTests.Encryption
{
    [TestFixture]
    public class S100CryptTests
    {
        private const string FakeHardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD";

        private IAesEncryption _fakeAesEncryption;
        private ILogger<S100Crypt> _fakeLogger;
        private IS100Crypt _s100Crypt;

        [SetUp]
        public void SetUp()
        {
            _fakeAesEncryption = A.Fake<IAesEncryption>();
            _fakeLogger = A.Fake<ILogger<S100Crypt>>();

            _s100Crypt = new S100Crypt(_fakeAesEncryption, _fakeLogger);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAesEncryption = () => new S100Crypt(null, _fakeLogger);
            nullAesEncryption.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("aesEncryption");

            Action nullLogger = () => new S100Crypt(_fakeAesEncryption, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [Test]
        public void WhenProductKeysDecryptedSuccessfully_ThenReturnsEncKeys()
        {
            var test101EncKey = "20191817161514131211109876543210";
            var test102EncKey = "36353433323130292827262524232221";

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored))
                                             .Returns(test101EncKey).Once().Then.Returns(test102EncKey);

            var result = _s100Crypt.GetDecryptedKeysFromProductKeys(GetProductKeyServiceResponse(), FakeHardwareId);

            result.Should().NotBeNull();
            result.FirstOrDefault().DecryptedKey.Should().Be(test101EncKey);
            result.LastOrDefault().DecryptedKey.Should().Be(test102EncKey);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetDecryptedKeysFromProductKeysStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted keys from product keys started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted keys from product keys completed."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidHardwareIdPassed_ThenThrowException()
        {
            FluentActions.Invoking(() => _s100Crypt.GetDecryptedKeysFromProductKeys(GetProductKeyServiceResponse(), "123456")).Should().
                                            ThrowExactly<PermitServiceException>().WithMessage("Expected hardware id length {KeySizeEncoded}, but found {HardwareId Length}.");

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted keys from product keys completed."
            ).MustNotHaveHappened();
        }

        [Test]
        public void WhenInvalidProductKeyPassed_ThenThrowException()
        {
            FluentActions.Invoking(() => _s100Crypt.GetDecryptedKeysFromProductKeys(GetInvalidProductKeyServiceResponse(), FakeHardwareId)).Should().
                                            ThrowExactly<PermitServiceException>().WithMessage("Expected product key length {KeySizeEncoded}, but found {ProductKeyServiceResponse Key Length}.");

            A.CallTo(() => _fakeAesEncryption.Decrypt(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get decrypted keys from product keys completed."
            ).MustNotHaveHappened();
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
        private List<ProductKeyServiceResponse> GetInvalidProductKeyServiceResponse()
        {
            return
            [
                new()
                {
                    Edition = "1",
                    Key = "0123456",
                    ProductName = "test101"
                },
                new()
                {
                    Edition = "1",
                    Key = "67891011",
                    ProductName = "test102"
                }
            ];
        }
    }
}