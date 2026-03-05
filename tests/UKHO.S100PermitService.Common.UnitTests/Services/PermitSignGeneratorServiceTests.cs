using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Models.PermitSign;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Transformers;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitSignGeneratorServiceTests
    {
        private const string TestServiceUri = "http://localhost:5000";
        private const string TestPrivateKeyName = "test-data-server-private-key";
        private const string TestCertificateName = "test-data-server-cert";
        private const string TestCertificateNameKVS = "test-data-server";

        private IDigitalSignatureProvider _fakeDigitalSignatureProvider;
        private IKeyVaultService _fakeKeyVaultService;
        private IOptions<DataKeyVaultConfiguration> _fakeDataKeyVaultConfiguration;
        private PermitSignGeneratorService _permitSignGeneratorService;
        private IXmlTransformer _fakeXmlTransformer;
        private ILogger<PermitSignGeneratorService> _fakeLogger;

        [SetUp]
        public void SetUp()
        {
            _fakeDigitalSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            _fakeKeyVaultService = A.Fake<IKeyVaultService>();
            _fakeDataKeyVaultConfiguration = Options.Create(new DataKeyVaultConfiguration() 
            { 
                ServiceUri = TestServiceUri, 
                DsPrivateKey = TestPrivateKeyName, 
                DsCertificate = TestCertificateName, 
                DsCertificateSecret = TestCertificateNameKVS, 
                UseSecretStringForCert = false 
            });
            _fakeXmlTransformer = A.Fake<IXmlTransformer>();
            _fakeLogger = A.Fake<ILogger<PermitSignGeneratorService>>();
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer, _fakeLogger);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => new PermitSignGeneratorService(null, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer, _fakeLogger),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'digitalSignatureProvider')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, null, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer, _fakeLogger),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'keyVaultService')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, null, _fakeXmlTransformer, _fakeLogger),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'dataKeyVaultConfiguration')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, null, _fakeLogger),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'xmlTransformer')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer, null),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'logger')"));
            });
        }

        [Test]
        public async Task WhenGeneratePermitSignXmlIsCalled_ThenShouldCallDigitalSignatureProviderAndReturnExpectedResult()
        {
            const string PermitXmlContent = "<Permit>test</Permit>";
            var hash = new byte[] { 1, 2, 3 };
            const string Signature = "testBase64signature";
            const string PrivateKeySecret = "testPrivateKeySecret";
            const string XmlContent = "testXmlContent";
            
            // Create a real self-signed certificate for testing
            var certificate = CreateSelfSignedCertificate("CN=TestSubject", "CN=TestIssuer");
            var certificateBytes = certificate.Export(X509ContentType.Cert);

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).Returns(hash);
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).Returns(PrivateKeySecret);
            A.CallTo(() => _fakeKeyVaultService.GetCertificate(A<string>.Ignored)).Returns(certificateBytes);
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHashWithPrivateKey(PrivateKeySecret, hash)).Returns(Signature);
            A.CallTo(() => _fakeDigitalSignatureProvider.CreateStandaloneDigitalSignature(A<X509Certificate2>.Ignored, A<string>.Ignored)).Returns(new StandaloneDigitalSignature());
            A.CallTo(() => _fakeXmlTransformer.SerializeToXml(A<StandaloneDigitalSignature>.Ignored)).Returns(XmlContent);

            var result = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(PermitXmlContent);

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeKeyVaultService.GetCertificate(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHashWithPrivateKey(PrivateKeySecret, hash)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.CreateStandaloneDigitalSignature(A<X509Certificate2>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeXmlTransformer.SerializeToXml(A<StandaloneDigitalSignature>.Ignored)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.EqualTo(XmlContent));
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