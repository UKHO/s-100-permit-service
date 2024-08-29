using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
        private HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private ProductKeyServiceConfiguration _productKeyServiceConfiguration;
        private ILogger<HoldingsServiceStub> _holdingsServiceStubLogger;
        private ILogger<ProductKeyServiceStub> _productKeyServiceStubLogger;

        [SetUp]
        public void SetUp()
        {
            _holdingsServiceConfiguration = A.Fake<HoldingsServiceConfiguration>();
            _productKeyServiceConfiguration = A.Fake<ProductKeyServiceConfiguration>();
            _holdingsServiceStubLogger = A.Fake<ILogger<HoldingsServiceStub>>();
            _productKeyServiceStubLogger = A.Fake<ILogger<ProductKeyServiceStub>>();

            _stubFactory = new StubFactory(_holdingsServiceConfiguration, _productKeyServiceConfiguration, _holdingsServiceStubLogger, _productKeyServiceStubLogger);
        }

        [Test]
        [TestCase(null, typeof(ProductKeyServiceConfiguration), typeof(ILogger<HoldingsServiceStub>), typeof(ILogger<ProductKeyServiceStub>), "*holdingsServiceConfiguration*")]
        [TestCase(typeof(HoldingsServiceConfiguration), null, typeof(ILogger<HoldingsServiceStub>), typeof(ILogger<ProductKeyServiceStub>), "*productKeyServiceConfiguration*")]
        [TestCase(typeof(HoldingsServiceConfiguration), typeof(ProductKeyServiceConfiguration), null, typeof(ILogger<ProductKeyServiceStub>), "*holdingsServiceStubLogger*")]
        [TestCase(typeof(HoldingsServiceConfiguration), typeof(ProductKeyServiceConfiguration), typeof(ILogger<HoldingsServiceStub>), null, "*productKeyServiceStubLogger*")]
        public void WhenConstructorCalledWithNullParameter_ThenThrowsArgumentNullException(Type? holdingsConfigType, Type? productKeyConfigType, Type? holdingsLoggerType, Type? productKeyLoggerType, string expectedMessage)
        {
            var holdingsServiceConfiguration = holdingsConfigType == null ? null : _holdingsServiceConfiguration;
            var productKeyServiceConfiguration = productKeyConfigType == null ? null : _productKeyServiceConfiguration;
            var holdingsServiceStubLogger = holdingsLoggerType == null ? null : _holdingsServiceStubLogger;
            var productKeyServiceStubLogger = productKeyLoggerType == null ? null : _productKeyServiceStubLogger;

            Action act = () => new StubFactory(holdingsServiceConfiguration!, productKeyServiceConfiguration!, holdingsServiceStubLogger!, productKeyServiceStubLogger!);

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
    }
}
