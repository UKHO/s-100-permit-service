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
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new KeyVaultService(null, _fakeCacheProvider, _fakeSecretClient, _fakeCertificateSecretClient);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCacheProvider = () => new KeyVaultService(_fakeLogger, null, _fakeSecretClient, _fakeCertificateSecretClient);
            nullCacheProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("cacheProvider");

            Action nullSecretClient = () => new KeyVaultService(_fakeLogger, _fakeCacheProvider, null, _fakeCertificateSecretClient);
            nullSecretClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("secretClient");
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