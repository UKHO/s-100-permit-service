using FakeItEasy;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.UnitTests.Providers
{
    [TestFixture]
    public class DigitalSignatureProviderTests
    {
        private ILogger<DigitalSignatureProvider> _fakeLogger;
        private DigitalSignatureProvider _digitalSignatureProvider;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<DigitalSignatureProvider>>();
            _digitalSignatureProvider = new DigitalSignatureProvider(_fakeLogger);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullLogger_ThenShouldThrowArgumentNullException()
        {
            Assert.That(() => new DigitalSignatureProvider(null),
                Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'logger')"));
        }

        [Test]
        public void WhenGeneratePermitXmlHashHasDifferentValidContent_ThenShouldReturnHashesOfSameLengthAndLogMessages()
        {
            var content1 = "TestContents";
            var content2 = "TestContents_123";

            var result1 = _digitalSignatureProvider.GeneratePermitXmlHash(content1);
            var result2 = _digitalSignatureProvider.GeneratePermitXmlHash(content2);

            Assert.That(result1.Length, Is.EqualTo(result2.Length), "The hash lengths for both inputs should be the same.");
            Assert.That(result1.Length, Is.EqualTo(SHA384.HashSizeInBytes), "The hash length should be 48 bytes.");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash generation started."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash successfully generated."
            ).MustHaveHappenedTwiceExactly();
        }

        [Test]
        public void WhenGeneratePermitXmlHashHasNullContent_ThenShouldThrowPermitServiceException()
        {
            var exception = Assert.Throws<PermitServiceException>(() => _digitalSignatureProvider.GeneratePermitXmlHash(null));
            Assert.That(exception.Message, Does.Contain("Permit hash generation failed with Exception"));
            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationStarted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash generation started."
             ).MustHaveHappenedOnceExactly();

        }
        [Test]
        public void WhenSignHashWithPrivateKeyIsCalledWithValidInputs_ThenShouldReturnBase64EncodedSignature()
        {
           
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            var privateKeyData = ecdsa.ExportECPrivateKey();
            var validBase64PrivateKey = Convert.ToBase64String(privateKeyData);
            var hashContent = new byte[] { 1, 2, 3, 4, 5 };

            var signature = _digitalSignatureProvider.SignHashWithPrivateKey(validBase64PrivateKey, hashContent);

            Assert.That(signature, Is.Not.Null, "The returned signature should not be null.");
            Assert.That(signature, Is.Not.Empty, "The returned signature should not be empty.");
            Assert.DoesNotThrow(() => Convert.FromBase64String(signature), "The signature should be a valid Base64-encoded string.");
        }

        [Test]
        [TestCase("InvalidBase64Key")]
        [TestCase("")]
        public void WhenSignHashWithPrivateKeyIsCalledWithInvalidPrivateKey_ThenShouldThrowPermitServiceException(string privateKey)
        {
            var hashContent = new byte[] { 1, 2, 3, 4, 5 };

            var exception = Assert.Throws<PermitServiceException>(() =>
                _digitalSignatureProvider.SignHashWithPrivateKey(privateKey, hashContent));
            Assert.That(exception.Message, Does.Contain("An error occurred while signing the hash with the private key with exception: {Message}"));
        }

        [Test]
        public void WhenSignHashWithPrivateKeyIsCalledWithNullHashContent_ThenShouldThrowPermitServiceException()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            var privateKeyData = ecdsa.ExportECPrivateKey();
            var validBase64PrivateKey = Convert.ToBase64String(privateKeyData);
            byte[]? nullHashContent = null;

            var exception = Assert.Throws<PermitServiceException>(() =>
                _digitalSignatureProvider.SignHashWithPrivateKey(validBase64PrivateKey, nullHashContent));
            Assert.That(exception.Message, Does.Contain("An error occurred while signing the hash with the private key with exception: {Message}"));
        }
    }
}