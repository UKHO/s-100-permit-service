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

            jsonFiles.Length.Should().Be(8, "there should be exactly 8 json files in the StubData\\UserPermits folder");
        }

        [Test]
        public void WhenConfigureHoldingsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var ResponseFileDirectory = @"StubData\Holdings";
            var _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(9, "there should be exactly 9 json files in the StubData\\Holdings folder");
        }

        [Test]
        public void WhenConfigureProductKeyServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var ResponseFileDirectory = @"StubData\PKS";
            var _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(7, "there should be exactly 7 json files in the StubData\\PKS folder");
        }
    }
}