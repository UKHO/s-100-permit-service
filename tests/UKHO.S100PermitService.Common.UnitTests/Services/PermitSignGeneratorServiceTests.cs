using FakeItEasy;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Transformer;

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
            _fakeDataKeyVaultConfiguration = A.Fake<IOptions<DataKeyVaultConfiguration>>();
            _fakeXmlTransformer = A.Fake<IXmlTransformer>();
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeKeyVaultService, _fakeDataKeyVaultConfiguration, _fakeXmlTransformer);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            Assert.That(() => new PermitSignGeneratorService(null, null, null, null),
                Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'digitalSignatureProvider')"));
        }

        [Test]
        public async Task WhenGeneratePermitSignXmlIsCalled_ThenShouldCallDigitalSignatureProviderAndReturnExpectedResult()
        {
            var content = "TestContent";
            var expectedHash = new byte[] { 1, 2, 3, 4, 5 };
            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(content)).Returns(expectedHash);

            var result = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(content);

            A.CallTo(() => _fakeDigitalSignatureProvider.GeneratePermitXmlHash(content)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.EqualTo(string.Empty));
        }
    }
}
