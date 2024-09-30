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
            var ResponseFileDirectory = @"StubData\UserPermits";
            var _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(10, "there should be exactly 10 json files in the StubData\\UserPermits folder");
        }

        [Test]
        public void WhenConfigureHoldingsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var ResponseFileDirectory = @"StubData\Holdings";
            var _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(11, "there should be exactly 11 json files in the StubData\\Holdings folder");
        }

        [Test]
        public void WhenConfigureProductKeyServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var ResponseFileDirectory = @"StubData\ProductKeyService";
            var _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(7, "there should be exactly 7 json files in the StubData\\ProductKeyService folder");
        }
    }
}