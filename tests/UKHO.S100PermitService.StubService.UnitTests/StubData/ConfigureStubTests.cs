using FluentAssertions;
using NUnit.Framework;

namespace UKHO.S100PermitService.StubService.UnitTests.StubData
{
    [TestFixture]
    public class ConfigureStubTests
    {
        [Test]
        public void WhenConfigureStub_ThenRequiredJsonFilesAreInPlace()
        {
            var stubFolderPath = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "_files", "ProductKeyServiceStub");
            var requestFilePath = Path.Combine(stubFolderPath, "request1.json");
            var responseFilePath = Path.Combine(stubFolderPath, "response1.json");

            var requestFileExists = File.Exists(requestFilePath);
            var responseFileExists = File.Exists(responseFilePath);

            requestFileExists.Should().BeTrue("request1.json should exist in the stubs folder");
            responseFileExists.Should().BeTrue("response1.json should exist in the stubs folder");
        }

        [Test]
        public void WhenConfigureStub_ThenRequiredNumberOfJsonFilesAreInPlace()
        {
            var stubFolderPath = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "_files", "ProductKeyServiceStub");

            var jsonFiles = Directory.GetFiles(stubFolderPath, "*.json");

            jsonFiles.Length.Should().Be(2, "there should be exactly 2 JSON files in the stubs folder");
        }
    }
}