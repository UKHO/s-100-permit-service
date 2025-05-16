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
        private DigitalSignatureProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<DigitalSignatureProvider>>();
            _provider = new DigitalSignatureProvider(_fakeLogger);
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
            var content2 = "TestContents12345";

            var result1 = _provider.GeneratePermitXmlHash(content1);
            var result2 = _provider.GeneratePermitXmlHash(content2);

            Assert.That(result1.Length, Is.EqualTo(result2.Length), "The hash lengths for both inputs should be the same.");

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
            var exception = Assert.Throws<PermitServiceException>(() => _provider.GeneratePermitXmlHash(null));
            Assert.That(exception.Message, Does.Contain("Permit hash generation failed with Exception"));
            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationStarted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash generation started."
             ).MustHaveHappenedOnceExactly();

        }

        [Test]
        public void WhenImportEcdsaPrivateKeyIsCalledWithValidKey_ThenShouldReturnEcdsaInstance()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384); 
            var privateKeyData = ecdsa.ExportECPrivateKey(); 
            var validBase64PrivateKey = Convert.ToBase64String(privateKeyData);

            var result = _provider.ImportEcdsaPrivateKey(validBase64PrivateKey);

            Assert.IsNotNull(result, "The returned ECDsa instance should not be null.");
            Assert.IsInstanceOf<ECDsa>(result, "The returned object should be an instance of ECDsa.");
        }

        [Test]
        [TestCase("InvalidBase64Key")]
        [TestCase("")]
        public void WhenImportEcdsaPrivateKeyIsCalledWithInvalidInputs_ThenShouldThrowPermitServiceException(string privateKey)
        {
            var exception = Assert.Throws<PermitServiceException>(() => _provider.ImportEcdsaPrivateKey(privateKey));
            Assert.That(exception.Message, Does.Contain("Permit private key import failed with Exception"));
        }

        [Test]
        public void WhenSignHashIsCalledWithValidInputs_ThenShouldReturnBase64EncodedSignature()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384); 
            var hashContent = new byte[] { 1, 2, 3, 4, 5 }; 

            var signature = _provider.SignHash(ecdsa, hashContent);

            Assert.IsNotNull(signature, "The returned signature should not be null.");
            Assert.IsNotEmpty(signature, "The returned signature should not be empty.");
            Assert.DoesNotThrow(() => Convert.FromBase64String(signature), "The signature should be a valid Base64-encoded string.");
            Assert.That(signature.Length, Is.EqualTo(128)); 
        }
    }
}