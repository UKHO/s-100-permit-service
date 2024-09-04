using FluentAssertions;
using NUnit.Framework;

namespace UKHO.S100PermitService.StubService.UnitTests.StubData
{
    [TestFixture]
    public class ConfigureStubTests
    {
        private const string ResponseFileDirectory = @"StubData\UserPermits";
        private readonly string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);
        [Test]
        public void WhenConfigureUserPermitsServiceStub_ThenEnsureRequiredNumberOfJsonFilesAreInPlace()
        {
            var jsonFiles = Directory.GetFiles(_responseFileDirectoryPath, "*.json");

            jsonFiles.Length.Should().Be(8, "there should be exactly 8 JSON files in the stubs folder");
        }
    }
}