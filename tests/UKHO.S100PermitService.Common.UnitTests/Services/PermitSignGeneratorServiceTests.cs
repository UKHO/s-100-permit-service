using FakeItEasy;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        private IDigitalSignatureProvider _fakeDigitalSignatureProvider;
        private IKeyVaultService _fakeKeyVaultService;
        private IOptions<DataKeyVaultConfiguration> _fakeDataKeyVaultConfiguration;
        private PermitSignGeneratorService _permitSignGeneratorService;
        private IXmlTransformer _fakeXmlTransformer;

        [SetUp]
        public void SetUp()
        {
            _fakeDigitalSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            _fakeKeyVaultService = A.Fake<IKeyVaultService>();
            _fakeDataKeyVaultConfiguration = Options.Create(new DataKeyVaultConfiguration() { ServiceUri = "http://localhost:5000", DsPrivateKey = "test-data-server-private-key", DsCertificate = "test-data-server" });
            _fakeXmlTransformer = A.Fake<IXmlTransformer>();
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            var nullDigitalSignatureProvider = Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(null, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer));
            Assert.That(nullDigitalSignatureProvider.ParamName, Is.EqualTo("digitalSignatureProvider"));

            var nullDataKeyService = Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, null, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer));
            Assert.That(nullDataKeyService.ParamName, Is.EqualTo("keyVaultService"));

            var nullDataKeyVaultConfiguration = Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, null, _fakeXmlTransformer));
            Assert.That(nullDataKeyVaultConfiguration.ParamName, Is.EqualTo("dataKeyVaultConfiguration"));

            var nullXmlTransformer = Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, null));
            Assert.That(nullXmlTransformer.ParamName, Is.EqualTo("xmlTransformer"));
        }

        [Test]
        public async Task WhenGeneratePermitSignXmlIsCalled_ThenShouldCallDigitalSignatureProviderAndReturnExpectedResult()
        {
            const string PermitXmlContent = "<Permit>test</Permit>";
            var hash = new byte[] { 1, 2, 3 };
            var privateKey = ECDsa.Create();
            const string Signature = "testBase64signature";
            const string PrivateKeySecret = "testPrivateKeySecret";
            const string XmlContent = "testXmlContent";

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).Returns(hash);
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).Returns(PrivateKeySecret);
            A.CallTo(() => _fakeDigitalSignatureProvider.ImportEcdsaPrivateKey(PrivateKeySecret)).Returns(privateKey);
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHash(privateKey, hash)).Returns(Signature);
            A.CallTo(() => _fakeDigitalSignatureProvider.CreateStandaloneDigitalSignature(A<X509Certificate2>.Ignored, A<string>.Ignored)).Returns(new StandaloneDigitalSignature());
            A.CallTo(() => _fakeXmlTransformer.SerializeToXml(A<StandaloneDigitalSignature>.Ignored)).Returns(XmlContent);

            var result = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(PermitXmlContent);

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.ImportEcdsaPrivateKey(PrivateKeySecret)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHash(privateKey, hash)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.CreateStandaloneDigitalSignature(A<X509Certificate2>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeXmlTransformer.SerializeToXml(A<StandaloneDigitalSignature>.Ignored)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.EqualTo(XmlContent)); 
        }      
    }
}