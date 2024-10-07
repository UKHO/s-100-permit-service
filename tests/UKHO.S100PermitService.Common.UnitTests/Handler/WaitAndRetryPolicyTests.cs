using FakeItEasy;
using FluentAssertions;
using UKHO.S100PermitService.Common.Handlers;

namespace UKHO.S100PermitService.Common.UnitTests.Handler
{
    [TestFixture]
    public class WaitAndRetryPolicyTests
    {
        private IWaitAndRetryPolicy _fakeWaitAndRetryPolicy;

        [SetUp]
        public void Setup()
        {
            _fakeWaitAndRetryPolicy = A.Fake<IWaitAndRetryPolicy>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullWaitAndRetryClient = () => new WaitAndRetryPolicy(null);
            nullWaitAndRetryClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("waitAndRetryConfiguration");
        }
    }
}
