using FakeItEasy;
using UKHO.S100PermitService.Common.Handlers;

namespace UKHO.S100PermitService.Common.UnitTests.Handler
{
    [TestFixture]
    public class WaitAndRetryPolicyTests
    {
        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullWaitAndRetryClient = () => new WaitAndRetryPolicy(null);
            Assert.That(nullWaitAndRetryClient, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("waitAndRetryConfiguration"));
        }
    }
}
