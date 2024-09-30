using Azure.Security.KeyVault.Secrets;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class ManufacturerKeyServiceTests
    {
        private IOptions<ManufacturerKeyConfiguration> _fakeManufacturerKeyVault;
        private ILogger<ManufacturerKeyService> _fakeLogger;
        private ICacheProvider _fakeCacheProvider;
        private ISecretClient _fakeSecretClient;
        private IManufacturerKeyService _manufacturerKeyService;

        [SetUp]
        public void Setup()
        {
            _fakeManufacturerKeyVault = A.Fake<IOptions<ManufacturerKeyConfiguration>>();
            _fakeLogger = A.Fake<ILogger<ManufacturerKeyService>>();
            _fakeCacheProvider = A.Fake<ICacheProvider>();
            _fakeSecretClient = A.Fake<ISecretClient>();

            _fakeManufacturerKeyVault.Value.CacheTimeoutInHours = 2;

            _manufacturerKeyService = new ManufacturerKeyService(_fakeManufacturerKeyVault, _fakeLogger, _fakeCacheProvider, _fakeSecretClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ManufacturerKeyService(_fakeManufacturerKeyVault, null, _fakeCacheProvider, _fakeSecretClient);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCacheProvider = () => new ManufacturerKeyService(_fakeManufacturerKeyVault, _fakeLogger, null, _fakeSecretClient);
            nullCacheProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("cacheProvider");

            Action nullSecretClient = () => new ManufacturerKeyService(_fakeManufacturerKeyVault, _fakeLogger, _fakeCacheProvider, null);
            nullSecretClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("secretClient");
        }

        [Test]
        public void WhenApplicationStarts_ThenFetchSecretsFromKeyVaultInMemoryCache()
        {
            _fakeManufacturerKeyVault.Value.ServiceUri = "https://test.com/";
            var secretKey = "mpn";
            var secretValue = "M_IDmpm";            

            A.CallTo(() => _fakeSecretClient.GetPropertiesOfSecrets()).Returns(GetSecretProperties(secretKey));

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).Returns(GetSecret(secretKey, secretValue));

            _manufacturerKeyService.CacheManufacturerKeys();

            A.CallTo(() => _fakeCacheProvider.SetCacheKey(A<string>.Ignored, A<string>.Ignored, A<TimeSpan>.Ignored)).MustHaveHappened();

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
            _fakeManufacturerKeyVault.Value.ServiceUri = "https://test.com/";

            var result = () => _manufacturerKeyService.CacheManufacturerKeys();
            result.Should().Throw<PermitServiceException>().WithMessage("No Secrets found in Manufacturer Keyvault");

            A.CallTo(() => _fakeSecretClient.GetPropertiesOfSecrets()).MustHaveHappenedOnceExactly();
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
        public void WhenSecretKeyPassedWhichIsNotInMemoryCache_ThenFetchSecretsFromKeyvault()
        {
            var secretKey = "mpn";
            var secretValue = "";

            A.CallTo(() => _fakeCacheProvider.GetCacheKey(A<string>.Ignored)).Returns(string.Empty);

            A.CallTo(() => _fakeSecretClient.GetSecret(A<string>.Ignored)).Returns(GetSecret(secretKey, secretValue));            

            var result = _manufacturerKeyService.GetManufacturerKeys(secretKey);

            A.CallTo(() => _fakeCacheProvider.SetCacheKey(A<string>.Ignored, A<string>.Ignored, A<TimeSpan>.Ignored)).MustHaveHappened();

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