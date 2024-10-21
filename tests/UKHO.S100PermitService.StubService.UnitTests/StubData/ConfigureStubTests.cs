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
            var responseFileDirectory = @"StubData\UserPermits";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(16, "there should be exactly 16 json files in the StubData\\UserPermits folder");
        }

        [Test]
        public void WhenConfigureHoldingsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var responseFileDirectory = @"StubData\Holdings";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(17, "there should be exactly 17 json files in the StubData\\Holdings folder");
        }

        [Test]
        public void WhenConfigureProductKeyServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var responseFileDirectory = @"StubData\ProductKeyService";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(17, "there should be exactly 17 json files in the StubData\\ProductKeyService folder");
        }
    }
}