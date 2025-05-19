using FakeItEasy;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
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
            _fakeDataKeyVaultConfiguration = A.Fake<IOptions<DataKeyVaultConfiguration>>();
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            var fakeSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            var fakeKeyVaultService = A.Fake<IKeyVaultService>();
            var fakeOptions = A.Fake<IOptions<DataKeyVaultConfiguration>>();

            Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(null, fakeKeyVaultService, fakeOptions));
            Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(fakeSignatureProvider, null, fakeOptions));
            Assert.Throws<ArgumentNullException>(() => new PermitSignGeneratorService(fakeSignatureProvider, fakeKeyVaultService, null));
            Assert.That(() => new PermitSignGeneratorService(null, null, null),
                Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'digitalSignatureProvider')"));
        }

        [Test]
        public async Task WhenGeneratePermitSignXmlIsCalled_ThenShouldCallDigitalSignatureProviderAndReturnExpectedResult()
        {
            var fakeSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            var fakeKeyVaultService = A.Fake<IKeyVaultService>();
            var fakeOptions = A.Fake<IOptions<DataKeyVaultConfiguration>>();
            var dataKeyVaultConfiguration = new DataKeyVaultConfiguration { DsPrivateKey = "testPrivateKeyName" };
            A.CallTo(() => fakeOptions.Value).Returns(dataKeyVaultConfiguration);

            const string PermitXmlContent = "<Permit>test</Permit>";
            var hash = new byte[] { 1, 2, 3 };
            var privateKey = ECDsa.Create();
            const string Signature = "testBase64signature";
            const string PrivateKeySecret = "testPrivateKeySecret";

            A.CallTo(() => fakeSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).Returns(hash);
            A.CallTo(() => fakeKeyVaultService.GetSecretKeys(dataKeyVaultConfiguration.DsPrivateKey)).Returns(PrivateKeySecret);
            A.CallTo(() => fakeSignatureProvider.ImportEcdsaPrivateKey(PrivateKeySecret)).Returns(privateKey);
            A.CallTo(() => fakeSignatureProvider.SignHash(privateKey, hash)).Returns(Signature);

            var permitSignGeneratorService = new PermitSignGeneratorService(fakeSignatureProvider, fakeKeyVaultService, fakeOptions);

            var result = await permitSignGeneratorService.GeneratePermitSignXmlAsync(PermitXmlContent);

            A.CallTo(() => fakeSignatureProvider.GeneratePermitXmlHash(PermitXmlContent)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeKeyVaultService.GetSecretKeys(dataKeyVaultConfiguration.DsPrivateKey)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeSignatureProvider.ImportEcdsaPrivateKey(PrivateKeySecret)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeSignatureProvider.SignHash(privateKey, hash)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.EqualTo(string.Empty));
        }
    }
}
