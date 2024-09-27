using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.Services;

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
