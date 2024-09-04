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
            string ResponseFileDirectory = @"StubData\UserPermits";
            string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(8, "there should be exactly 8 JSON files in the StubData\\UserPermits folder");
        }

        [Test]
        public void WhenConfigureHoldingsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            string ResponseFileDirectory = @"StubData\Holdings";
            string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(8, "there should be exactly 8 JSON files in the StubData\\Holdings folder");
        }
    }
}