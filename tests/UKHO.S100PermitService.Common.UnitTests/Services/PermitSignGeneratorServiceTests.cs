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
        private IDataKeyService _fakeDataKeyService;
        private IOptions<DataKeyVaultConfiguration> _fakeDataKeyVaultConfiguration;
        private PermitSignGeneratorService _permitSignGeneratorService;

        [SetUp]
        public void SetUp()
        {
            _fakeDigitalSignatureProvider = A.Fake<IDigitalSignatureProvider>();
            _fakeDataKeyService = A.Fake<IDataKeyService>();
            _fakeDataKeyVaultConfiguration = A.Fake<IOptions<DataKeyVaultConfiguration>>();
            _permitSignGeneratorService = new PermitSignGeneratorService(_fakeDigitalSignatureProvider, _fakeDataKeyService, _fakeDataKeyVaultConfiguration);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependency_ThenShouldThrowArgumentNullException()
        {
            Assert.That(() => new PermitSignGeneratorService(null, null, null),
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
