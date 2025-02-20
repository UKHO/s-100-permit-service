using FluentAssertions;
using NUnit.Framework;

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

            jsonFiles.Length.Should().Be(20, "there should be exactly 20 json files in the StubData\\ProductKeyService folder");
        }
    }
}