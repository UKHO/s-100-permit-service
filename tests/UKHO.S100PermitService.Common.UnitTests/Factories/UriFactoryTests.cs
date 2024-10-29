using FluentAssertions;
using UKHO.S100PermitService.Common.Factories;

namespace UKHO.S100PermitService.Common.UnitTests.Factories
{
    [TestFixture]
    public class UriFactoryTests
    {
        private IUriFactory _uriFactory;

        private const string BaseUrl = "https://localhost:5000";
        [SetUp]
        public void SetUp()
        {
            _uriFactory = new UriFactory();
        }

        [TestCase("/userpermits/{0}/s100", "2", "https://localhost:5000/userpermits/2/s100")]
        [TestCase("/holdings/{0}/s100", "2", "https://localhost:5000/holdings/2/s100")]
        [TestCase("/keys/s100", "", "https://localhost:5000/keys/s100")]
        public void CreateUri_ShouldReturnCorrectUri(string endpointFormat, string licenceId, string expectedUri)
        {
            var args = new object[] { licenceId };

            var result = _uriFactory.CreateUri(BaseUrl, endpointFormat, args);

            result.Should().Be(expectedUri);
        }
    }
}