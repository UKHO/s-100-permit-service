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
            var result = _fakePermitXmlService.MapPermit(DateTimeOffset.Parse("2024-09-02+01:00"), "fakeDataServerIdentifier", "fakeDataServerName", "fakeUserPermit", 1, new List<products>());

            Assert.That(result.header.dataServerName, Is.EqualTo(expectedResult.header.dataServerName));
            Assert.That(result.header.dataServerIdentifier, Is.EqualTo(expectedResult.header.dataServerIdentifier));
            Assert.That(result.header.issueDate, Is.EqualTo(expectedResult.header.issueDate));
            Assert.That(result.header.userpermit, Is.EqualTo(expectedResult.header.userpermit));
            Assert.That(result.header.version, Is.EqualTo(expectedResult.header.version));
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
            var result = _fakePermitXmlService.MapPermit(DateTimeOffset.Parse("2024-09-02+05:30"), "fakeDataServerIdentifier", "fakeDataServerName", "fakeUserPermit", 1, products);

            Assert.That(result.products[0].id, Is.EqualTo(expectedResult.products[0].id));
            Assert.That(result.products[0].datasetPermit[0].issueDate, Is.EqualTo(expectedResult.products[0].datasetPermit[0].issueDate));
            Assert.That(result.products[0].datasetPermit[0].editionNumber, Is.EqualTo(expectedResult.products[0].datasetPermit[0].editionNumber));
            Assert.That(result.products[0].datasetPermit[0].encryptedKey, Is.EqualTo(expectedResult.products[0].datasetPermit[0].encryptedKey));
            Assert.That(result.products[0].datasetPermit[0].expiry, Is.EqualTo(expectedResult.products[0].datasetPermit[0].expiry));
            Assert.That(result.products[0].datasetPermit[0].filename, Is.EqualTo(expectedResult.products[0].datasetPermit[0].filename));
        }
    }
}
