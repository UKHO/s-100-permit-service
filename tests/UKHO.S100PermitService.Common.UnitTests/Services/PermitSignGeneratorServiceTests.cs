using FakeItEasy;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitSignGeneratorServiceTests
    {
        private IDigitalSignatureProvider _fakeDigitalSignatureProvider;
        private IKeyVaultService _fakeKeyVaultService;
        private IOptions<DataKeyVaultConfiguration> _fakeDataKeyVaultConfiguration;
        private PermitSignGeneratorService _permitSignGeneratorService;

        [SetUp]
        public void SetUp()
        {
            _fakeDigitalSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            _fakeKeyVaultService = A.Fake<IKeyVaultService>();
            _fakeDataKeyVaultConfiguration = Options.Create(new DataKeyVaultConfiguration() { ServiceUri = "http://localhost:5000", DsPrivateKey = "test-data-server-private-key", DsCertificate = "test-data-server" });
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => new PermitSignGeneratorService(null, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'digitalSignatureProvider')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, null, _fakeDataKeyVaultConfiguration),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'keyVaultService')"));

                Assert.That(() => new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, null),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'dataKeyVaultConfiguration')"));
            });
        }

        [Test]
        public async Task WhenGeneratePermitSignXmlIsCalled_ThenShouldCallDigitalSignatureProviderAndReturnExpectedResult()
        {
            const string PermitXmlContent = "<Permit>test</Permit>";
            var hash = new byte[] { 1, 2, 3 };
            const string Signature = "testBase64signature";
            const string PrivateKeySecret = "testPrivateKeySecret";

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).Returns(hash);
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).Returns(PrivateKeySecret);
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHashWithPrivateKey(PrivateKeySecret,hash)).Returns(Signature);

            var result = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(PermitXmlContent);

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeKeyVaultService.GetSecretKeys(_fakeDataKeyVaultConfiguration.Value.DsPrivateKey)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDigitalSignatureProvider.SignHashWithPrivateKey(PrivateKeySecret, hash)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.EqualTo(string.Empty));
        }
    }
}
