using FakeItEasy;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
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

            Assert.Multiple(() =>
            {
                Assert.That(signature, Is.Not.Null, "The returned signature should not be null.");
                Assert.That(signature, Is.Not.Empty, "The returned signature should not be empty.");
                Assert.DoesNotThrow(() => Convert.FromBase64String(signature), "The signature should be a valid Base64-encoded string.");

                var derSignature = Convert.FromBase64String(signature);

                Assert.That(derSignature[0], Is.EqualTo(0x30), "DER signature should start with ASN.1 SEQUENCE (0x30).");
                Assert.That(derSignature.Length, Is.GreaterThan(2), "DER signature should have a valid length.");

                var sequenceLength = derSignature[1];
                Assert.That(sequenceLength, Is.EqualTo(derSignature.Length - 2), "The SEQUENCE length should match the actual length of the DER signature.");

                Assert.That(derSignature[2], Is.EqualTo(0x02), "The first component should be an ASN.1 INTEGER (0x02).");
                var rLength = derSignature[3];
                Assert.That(rLength, Is.GreaterThan(0), "The length of r should be greater than 0.");
                Assert.That(rLength + 4, Is.LessThan(derSignature.Length), "The r component should fit within the DER signature.");

                var sStartIndex = 4 + rLength;
                Assert.That(derSignature[sStartIndex], Is.EqualTo(0x02), "The second component should be an ASN.1 INTEGER (0x02).");
                var sLength = derSignature[sStartIndex + 1];
                Assert.That(sLength, Is.GreaterThan(0), "The length of s should be greater than 0.");
                Assert.That(sStartIndex + 2 + sLength, Is.EqualTo(derSignature.Length), "The s component should fit exactly within the DER signature.");
            });
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

        [Test]
        public void WhenCreateStandaloneDigitalSignatureHasValidCertificateAndSignature_ThenReturnsExpectedResult()
        {
            var certificate = CreateSelfSignedCertificate("CN=TestSubject", "CN=TestIssuer");
            var signatureBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("signature"));

            var result = _digitalSignatureProvider.CreateStandaloneDigitalSignature(certificate, signatureBase64);

            Assert.Multiple(() =>
            {
                Assert.That(result != null);
                Assert.That(result.Filename, Does.Contain(PermitServiceConstants.PermitXmlFileName));
                Assert.That(result.Certificate.SchemeAdministrator.Id, Does.Contain("TestIssuer"));
                Assert.That(result.Certificate.CertificateMetadata.Id, Does.Contain("TestSubject"));
                Assert.That(result.Certificate.CertificateMetadata.Issuer, Does.Contain("TestIssuer"));
                Assert.That(result.Certificate.CertificateMetadata.Value, Does.Contain(Convert.ToBase64String(certificate.RawData)));
                Assert.That(result.DigitalSignatureInfo.Id, Does.Contain(PermitServiceConstants.DigitalSignatureId));
                Assert.That(result.DigitalSignatureInfo.CertificateRef, Does.Contain("TestSubject"));
                Assert.That(result.DigitalSignatureInfo.Value, Does.Contain(signatureBase64));
            });
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.StandaloneDigitalSignatureGenerationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "StandaloneDigitalSignature generation process started."
            ).MustHaveHappened();
            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.StandaloneDigitalSignatureGenerationCompleted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "StandaloneDigitalSignature generation process completed."
             ).MustHaveHappened();
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string subject, string issuer)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var subjectName = new X500DistinguishedName(subject);
            var req = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
            var cert = req.Create(new X500DistinguishedName(issuer), X509SignatureGenerator.CreateForECDsa(ecdsa), DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), Guid.NewGuid().ToByteArray());
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }
    }
}