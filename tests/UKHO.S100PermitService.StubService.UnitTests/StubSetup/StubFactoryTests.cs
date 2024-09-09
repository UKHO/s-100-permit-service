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
        private IOptions<HoldingsServiceConfiguration> _holdingsServiceConfiguration;
        private IOptions<ProductKeyServiceConfiguration> _productKeyServiceConfiguration;
        private IOptions<UserPermitsServiceConfiguration> _userPermitsServiceConfiguration;

        [SetUp]
        public void SetUp()
        {
            _holdingsServiceConfiguration = A.Fake<IOptions<HoldingsServiceConfiguration>>();
            _productKeyServiceConfiguration = A.Fake<IOptions<ProductKeyServiceConfiguration>>();
            _userPermitsServiceConfiguration = A.Fake<IOptions<UserPermitsServiceConfiguration>>();

            _stubFactory = new StubFactory(_holdingsServiceConfiguration, _productKeyServiceConfiguration, _userPermitsServiceConfiguration);
        }

        [Test]
        [TestCase(null, typeof(ProductKeyServiceConfiguration), typeof(UserPermitsServiceConfiguration), "*holdingsServiceConfiguration*")]
        [TestCase(typeof(HoldingsServiceConfiguration), null, typeof(UserPermitsServiceConfiguration), "*productKeyServiceConfiguration*")]
        [TestCase(typeof(HoldingsServiceConfiguration), typeof(ProductKeyServiceConfiguration), null, "*userPermitsServiceConfiguration*")]
        public void WhenConstructorCalledWithNullParameter_ThenThrowsArgumentNullException(Type? holdingsServiceConfigType, Type? productKeyServiceConfigType, Type? userPermitsServiceConfigType, string expectedMessage)
        {
            var holdingsServiceConfiguration = holdingsServiceConfigType == null ? null : _holdingsServiceConfiguration;
            var productKeyServiceConfiguration = productKeyServiceConfigType == null ? null : _productKeyServiceConfiguration;
            var userPermitsServiceConfiguration = userPermitsServiceConfigType == null ? null : _userPermitsServiceConfiguration;

            Action act = () =>
            {
                _ = new StubFactory(holdingsServiceConfiguration!, productKeyServiceConfiguration!, userPermitsServiceConfiguration!);
            };

            act.Should().Throw<ArgumentNullException>().WithMessage(expectedMessage);
        }

        [Test]
        public void WhenCreateHoldingsServiceStub_ThenReturnHoldingsServiceStub()
        {
            var result = _stubFactory.CreateHoldingsServiceStub();

            result.Should().BeOfType<HoldingsServiceStub>();
        }

        [Test]
        public void WhenCreateProductKeyServiceStub_ThenReturnProductKeyServiceStub()
        {
            var result = _stubFactory.CreateProductKeyServiceStub();

            result.Should().BeOfType<ProductKeyServiceStub>();
        }

        [Test]
        public void WhenCreateUserPermitsServiceStub_ThenReturnUserPermitsServiceStub()
        {
            var result = _stubFactory.CreateUserPermitsServiceStub();

            result.Should().BeOfType<UserPermitsServiceStub>();
        }
    }
}