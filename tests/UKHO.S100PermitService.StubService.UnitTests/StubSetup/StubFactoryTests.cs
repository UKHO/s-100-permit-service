using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.Stubs;
using UKHO.S100PermitService.StubService.StubSetup;

namespace UKHO.S100PermitService.StubService.UnitTests.StubSetup
{
    [TestFixture]
    public class StubFactoryTests
    {
        private StubFactory _stubFactory;
        private IOptions<ProductKeyServiceConfiguration> _productKeyServiceConfiguration;

        [SetUp]
        public void SetUp()
        {
            _productKeyServiceConfiguration = A.Fake<IOptions<ProductKeyServiceConfiguration>>();

            _stubFactory = new StubFactory(_productKeyServiceConfiguration);
        }

        [Test]
        [TestCase(null, "*productKeyServiceConfiguration*")]
        public void WhenConstructorCalledWithNullParameter_ThenThrowsArgumentNullException(Type? productKeyServiceConfigType, string expectedMessage)
        {
            var productKeyServiceConfiguration = productKeyServiceConfigType == null ? null : _productKeyServiceConfiguration;

            Action act = () =>
            {
                _ = new StubFactory(productKeyServiceConfiguration!);
            };

            act.Should().Throw<ArgumentNullException>().WithMessage(expectedMessage);
        }

        [Test]
        public void WhenCreateProductKeyServiceStub_ThenReturnProductKeyServiceStub()
        {
            var result = _stubFactory.CreateProductKeyServiceStub();

            result.Should().BeOfType<ProductKeyServiceStub>();
        }
    }
}