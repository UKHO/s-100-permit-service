using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace UKHO.S100PermitService.StubService.UnitTests.StubData
{
    [TestFixture]
    public class ConfigureStubTests
    {
        [Test]
        public void WhenConfigureProductKeyServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var responseFileDirectory = @"StubData\ProductKeyService";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            Assert.That(jsonFiles.Length, Is.EqualTo(13), "there should be exactly 13 json files in the StubData\\ProductKeyService folder");
        }
    }
}