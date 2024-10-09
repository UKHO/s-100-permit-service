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
    public class ManufacturerKeyServiceTests
    {
        private ILogger<ManufacturerKeyService> _fakeLogger;
        private ICacheProvider _fakeCacheProvider;
        private ISecretClient _fakeSecretClient;
        private IManufacturerKeyService _manufacturerKeyService;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<ManufacturerKeyService>>();
            _fakeCacheProvider = A.Fake<ICacheProvider>();
            _fakeSecretClient = A.Fake<ISecretClient>();

            _manufacturerKeyService = new ManufacturerKeyService(_fakeLogger, _fakeCacheProvider, _fakeSecretClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ManufacturerKeyService(null, _fakeCacheProvider, _fakeSecretClient);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCacheProvider = () => new ManufacturerKeyService(_fakeLogger, null, _fakeSecretClient);
            nullCacheProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("cacheProvider");

            Action nullSecretClient = () => new ManufacturerKeyService(_fakeLogger, _fakeCacheProvider, null);
            nullSecretClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("secretClient");
        }

        [Test]
        public void WhenApplicationStarts_ThenFetchSecretsFromKeyVaultInMemoryCache()
        {
            var secretKey = "mpn";
            var secretValue = "M_IDmpm";
            A.CallTo(() => _fakeSecretClient.GetPropertiesOfSecrets()).Returns(GetSecretProperties(secretKey));

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).Returns(GetSecret(secretKey, secretValue));

            A.CallTo(() => _fakeCacheProvider.SetCacheKey(A<string>.Ignored, A<string>.Ignored));

            _manufacturerKeyService.CacheManufacturerKeys();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ManufacturerKeyCachingStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Caching Of Manufacturer Keys started."
            ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.ManufacturerKeyCachingEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Caching Of Manufacturer Keys End."
           ).MustHaveHappened();
        }

        [Test]
        public void WhenNoSecretsInMemoryCache_ThenThrowException()
        {
            var result = () => _manufacturerKeyService.CacheManufacturerKeys();
            result.Should().Throw<PermitServiceException>().WithMessage("No Secrets found in Manufacturer Key Vault");

            A.CallTo(() => _fakeSecretClient.GetPropertiesOfSecrets()).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenNoSecretsInMemoryCacheOrKeyVault_ThenThrowException()
        {
            A.CallTo(() => _fakeCacheProvider.GetCacheKey(A<string>.Ignored)).Returns(string.Empty);

            var result = () => _manufacturerKeyService.GetManufacturerKeys("pqr");
            result.Should().Throw<PermitServiceException>().WithMessage("No Secrets found in Manufacturer Key Vault, failed with Exception :{Message}");
        }

        [Test]
        public void WhenValidSecretKeyIsPassed_ThenFetchValueFromMemoryCache()
        {
            var secret = SetCacheKeyValue();
            var secretKey = secret.FirstOrDefault(s => s.Key == "abc").Value;

            A.CallTo(() => _fakeCacheProvider.GetCacheKey(A<string>.Ignored)).Returns(secretKey);

            var result = _manufacturerKeyService.GetManufacturerKeys("abc");

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).MustNotHaveHappened();

            result.Should().NotBeNull();
            result.Equals(secretKey);
        }

        [Test]
        public void WhenSecretKeyPassedWhichIsNotInMemoryCache_ThenFetchSecretsFromKeyVault()
        {
            var secretKey = "mpn";
            var secretValue = "";

            A.CallTo(() => _fakeCacheProvider.GetCacheKey(A<string>.Ignored)).Returns(string.Empty);

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).Returns(GetSecret(secretKey, secretValue));

            var result = _manufacturerKeyService.GetManufacturerKeys(secretKey);

            A.CallTo(() => _fakeCacheProvider.SetCacheKey(A<string>.Ignored, A<string>.Ignored)).MustHaveHappened();

            result.Should().NotBeNull();
            result.Should().Be(secretValue);
        }

        private static KeyVaultSecret GetSecret(string key, string value)
        {
            return new KeyVaultSecret(key, value);
        }

        private static IEnumerable<SecretProperties> GetSecretProperties(string secret)
        {
            return [new SecretProperties(secret)];
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