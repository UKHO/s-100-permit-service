using FluentAssertions;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitXmlServiceTests
    {
        private PermitXmlService _fakePermitXmlService;

        [SetUp]
        public void Setup()
        {
            _fakePermitXmlService = new PermitXmlService();
        }

        [Test]
        public void WhenAppropriateHeaderParametersArePassed_ThenProperModelIsMapped()
        {
            var expectedResult = new Permit()
            {
                header = new header()
                {
                    dataServerIdentifier = "fakeDataServerIdentifier",
                    dataServerName = "fakeDataServerName",
                    issueDate = "2024-09-02+01:00",
                    userpermit = "fakeUserPermit",
                    version = 1
                },
                products = new List<products>().ToArray()

            };
            var result = _fakePermitXmlService.MapDataToPermit(DateTimeOffset.Parse("2024-09-02+01:00"), "fakeDataServerIdentifier", "fakeDataServerName", "fakeUserPermit", 1, new List<products>());

            result.header.dataServerName.Should().Be(expectedResult.header.dataServerName);
            result.header.dataServerIdentifier.Should().Be(expectedResult.header.dataServerIdentifier);
            result.header.issueDate.Should().Be(expectedResult.header.issueDate);
            result.header.userpermit.Should().Be(expectedResult.header.userpermit);
            result.header.version.Should().Be(expectedResult.header.version);
        }

        [Test]
        public void WhenAppropriateProductParametersArePassed_ThenProperModelIsMapped()
        {
            var products = new List<products>()
            {
                new products()
                {
                     id = "fakeID",
                     datasetPermit = new productsProductDatasetPermit[]
                     {
                         new productsProductDatasetPermit() {
                             issueDate = "2024-09-02+01:00",
                             editionNumber = 1,
                             encryptedKey = "fakeencryptedkey",
                             expiry = DateTime.Now,
                             filename = "fakefilename",

                         }
                     }
                }
            };
            var expectedResult = new Permit()
            {
                header = new header()
                {
                    dataServerIdentifier = "fakeDataServerIdentifier",
                    dataServerName = "fakeDataServerName",
                    issueDate = "2024-09-02+01:00",
                    userpermit = "fakeUserPermit",
                    version = 1
                },
                products = products.ToArray()
            };
            var result = _fakePermitXmlService.MapDataToPermit(DateTimeOffset.Parse("2024-09-02+05:30"), "fakeDataServerIdentifier", "fakeDataServerName", "fakeUserPermit", 1, products);

            result.products[0].id.Should().Be(expectedResult.products[0].id);
            result.products[0].datasetPermit[0].issueDate.Should().Be(expectedResult.products[0].datasetPermit[0].issueDate);
            result.products[0].datasetPermit[0].editionNumber.Should().Be(expectedResult.products[0].datasetPermit[0].editionNumber);
            result.products[0].datasetPermit[0].encryptedKey.Should().Be(expectedResult.products[0].datasetPermit[0].encryptedKey);
            result.products[0].datasetPermit[0].expiry.Should().Be(expectedResult.products[0].datasetPermit[0].expiry);
            result.products[0].datasetPermit[0].filename.Should().Be(expectedResult.products[0].datasetPermit[0].filename);
        }
    }
}
