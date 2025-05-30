using UKHO.S100PermitService.Common.Extensions;

namespace UKHO.S100PermitService.Common.UnitTests.Extensions
{
    [TestFixture]
    public class CertificateExtensionTests
    {
        [TestCase("CN=Example, O=Org, C=GB", "Example", TestName = "ValidContent_ReturnsCn")]
        [TestCase("O=Org, C=GB", "UnknownCN", TestName = "ContentWithoutCn_ReturnsUnknownCn")]
        [TestCase("", "UnknownCN", TestName = "EmptyContent_ReturnsUnknownCn")]
        public void GetCnFromCertificate_ValidatesVariousScenarios(string content, string expectedCn)
        {
            var result = content.GetCnFromCertificate();

            Assert.That(result, Is.EqualTo(expectedCn), "The method should return the correct CN value or 'UnknownCN'.");
        }
    }
}
