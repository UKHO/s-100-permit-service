using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class KeyVaultServiceTests
    {
        private ILogger<KeyVaultService> _fakeLogger;
        private ICacheProvider _fakeCacheProvider;
        private ISecretClient _fakeSecretClient;
        private ICertificateClient _fakeCertificateSecretClient;
        private IKeyVaultService _keyVaultService;
        private const string CertificateName = "testCertificate";

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<KeyVaultService>>();
            _fakeCacheProvider = A.Fake<ICacheProvider>();
            _fakeSecretClient = A.Fake<ISecretClient>();
            _fakeCertificateSecretClient = A.Fake<ICertificateClient>();

            _keyVaultService = new KeyVaultService(_fakeLogger, _fakeCacheProvider, _fakeSecretClient, _fakeCertificateSecretClient);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependencies_ThenShouldThrowArgumentNullException()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => new KeyVaultService(null, _fakeCacheProvider, _fakeSecretClient, _fakeCertificateSecretClient),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'logger')"));

                Assert.That(() => new KeyVaultService(_fakeLogger, null, _fakeSecretClient, _fakeCertificateSecretClient),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'cacheProvider')"));

                Assert.That(() => new KeyVaultService(_fakeLogger, _fakeCacheProvider, null, _fakeCertificateSecretClient),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'secretClient')"));

                Assert.That(() => new KeyVaultService(_fakeLogger, _fakeCacheProvider, _fakeSecretClient, null),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'certificateSecretClient')"));
            });
        }

            [Test]
        public void WhenNoSecretsInMemoryCacheOrKeyVault_ThenThrowException()
        {
            A.CallTo(() => _fakeCacheProvider.GetCacheValue(A<string>.Ignored)).Returns(string.Empty);

            var result = () => _keyVaultService.GetSecretKeys("pqr");

            result.Should().Throw<PermitServiceException>().WithMessage("No Secrets found in Secret Key Vault, failed with Exception :{Message}");
        }

        [Test]
        public void WhenValidSecretKeyIsPassed_ThenFetchValueFromMemoryCache()
        {
            var secret = SetCacheKeyValue();
            var secretKey = secret.FirstOrDefault(s => s.Key == "abc").Value;

            A.CallTo(() => _fakeCacheProvider.GetCacheValue(A<string>.Ignored)).Returns(secretKey);

            var result = _keyVaultService.GetSecretKeys("abc");

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SecretKeyFoundInCache.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Secret Key found in Cache."
            ).MustHaveHappenedOnceExactly();

            result.Should().NotBeNull();
            result.Equals(secretKey);
        }

        [Test]
        public void WhenSecretKeyPassedWhichIsNotInMemoryCache_ThenFetchSecretsFromKeyVault()
        {
            var secretKey = "mpn";
            var secretValue = "";

            A.CallTo(() => _fakeCacheProvider.GetCacheValue(A<string>.Ignored)).Returns(string.Empty);

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).Returns(GetSecret(secretKey, secretValue));

            var result = _keyVaultService.GetSecretKeys(secretKey);

            A.CallTo(() => _fakeCacheProvider.SetCache(A<string>.Ignored, A<string>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.AddingNewSecretKeyInCache.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "New Secret Key added in Cache."
             ).MustHaveHappenedOnceExactly();

            result.Should().Be(secretValue);
        }

        [Test]
        public void WhenCertificateIsInCache_ThenReturnsCertificateAndLogsInfo()
        {
            var expectedCertificateBytes = new byte[] { 1, 2, 3, 4 };

            A.CallTo(() => _fakeCacheProvider.GetCertificateCacheValue(CertificateName)).Returns(expectedCertificateBytes);

            var result = _keyVaultService.GetCertificate(CertificateName);

            Assert.That(result, Is.EqualTo(expectedCertificateBytes));
            A.CallTo(() => _fakeCertificateSecretClient.GetCertificate(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.CertificateFoundInCache.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Certificate found in Cache."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenCertificateIsNotInCache_ThenFetchesFromKeyVaultAndCachesIt()
        {
            var keyVaultCertificate = A.Fake<KeyVaultCertificate>();

            A.CallTo(() => _fakeCacheProvider.GetCertificateCacheValue(CertificateName))!.Returns([]);
            A.CallTo(() => _fakeCertificateSecretClient.GetCertificate(CertificateName)).Returns(keyVaultCertificate);

            _keyVaultService.GetCertificate(CertificateName);

            A.CallTo(() => _fakeCertificateSecretClient.GetCertificate(CertificateName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeCacheProvider.SetCertificateCache(CertificateName, null)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenCertificateClientThrowsException_ThenPermitServiceExceptionIsThrown()
        {
            A.CallTo(() => _fakeCacheProvider.GetCertificateCacheValue(CertificateName))!.Returns(null);
            A.CallTo(() => _fakeCertificateSecretClient.GetCertificate(CertificateName)).Throws(new Exception("KeyVault failure"));

            var ex = Assert.Throws<PermitServiceException>(() => _keyVaultService.GetCertificate(CertificateName));

            Assert.That(ex.Message, Is.EqualTo("No Certificate found in Certificate Key Vault, failed with Exception :{Message}"));
        }

        private static KeyVaultSecret GetSecret(string key, string value)
        {
            return new KeyVaultSecret(key, value);
        }

        private static Dictionary<string, string> SetCacheKeyValue()
        {
            var secrets = new Dictionary<string, string>() {
                { "abc", "M_IDabc"},
                { "pqr", "M_IDpqr"},
                { "xyz","M_IDxyz"} };
            return secrets;
        }
    }
}