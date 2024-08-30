using FluentAssertions;
using NUnit.Framework;

namespace UKHO.S100PermitService.StubService.UnitTests.StubData
{
    [TestFixture]
    public class ConfigureStubTests
    {
        [Test]
        public void WhenConfigureUserPermitsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var stubFolderPath = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "StubData", "UserPermits");

            var jsonFiles = Directory.GetFiles(stubFolderPath, "*.json");

            jsonFiles.Length.Should().Be(1, "there should be exactly 1 JSON files in the stubs folder");
        }
    }
}