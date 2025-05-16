using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.UnitTests.Providers
{
    [TestFixture]
    public class DigitalSignatureProviderTests
    {
        private ILogger<DigitalSignatureProvider> _fakeLogger;
        private DigitalSignatureProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<DigitalSignatureProvider>>();
            _provider = new DigitalSignatureProvider(_fakeLogger);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullLogger_ThenShouldThrowArgumentNullException()
        {
            Assert.That(() => new DigitalSignatureProvider(null),
                Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'logger')"));
        }

        [Test]
        public void WhenGeneratePermitXmlHashHasDifferentValidContent_ThenShouldReturnHashesOfSameLengthAndLogMessages()
        {
            var content1 = "TestContents";
            var content2 = "TestContents12345";

            var result1 = _provider.GeneratePermitXmlHash(content1);
            var result2 = _provider.GeneratePermitXmlHash(content2);

            Assert.That(result1.Length, Is.EqualTo(result2.Length), "The hash lengths for both inputs should be the same.");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash generation started."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash successfully generated."
            ).MustHaveHappenedTwiceExactly();
        }

        [Test]
        public void WhenGeneratePermitXmlHashHasNullContent_ThenShouldThrowPermitServiceException()
        {
            var exception = Assert.Throws<PermitServiceException>(() => _provider.GeneratePermitXmlHash(null));
            Assert.That(exception.Message, Does.Contain("Permit hash generation failed with Exception"));
            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.PermitHashGenerationStarted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit hash generation started."
             ).MustHaveHappenedOnceExactly();

        }
    }
}