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

            jsonFiles.Length.Should().Be(14, "there should be exactly 14 json files in the StubData\\UserPermits folder");
        }

        [Test]
        public void WhenConfigureHoldingsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var responseFileDirectory = @"StubData\Holdings";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(15, "there should be exactly 15 json files in the StubData\\Holdings folder");
        }

        [Test]
        public void WhenConfigureProductKeyServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var responseFileDirectory = @"StubData\ProductKeyService";
            var responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, responseFileDirectory);

            var jsonFiles = Directory.GetFiles(responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(13, "there should be exactly 13 json files in the StubData\\ProductKeyService folder");
        }
    }
}