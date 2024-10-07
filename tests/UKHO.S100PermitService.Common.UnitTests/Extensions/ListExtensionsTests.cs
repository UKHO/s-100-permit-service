using FluentAssertions;
using UKHO.S100PermitService.Common.Extensions;

namespace UKHO.S100PermitService.Common.UnitTests.Extensions
{
    [TestFixture]
    public class ListExtensionsTests
    {
        [Test]

        [TestCase(null)]
        [TestCase("empty")]

        public void WhenListIsNullOrEmpty_ThenReturnsTrue(string? listType)
        {
            List<string>? list = listType is null ? null : [];

            var result = ListExtensions.IsNullOrEmpty(list);

            result.Should().BeTrue();
        }

        [Test]
        public void WhenListIsNotEmpty_ThenReturnsFalse()
        {
            var list = new List<string> { "TEST1", "TEST2" };
            var result = ListExtensions.IsNullOrEmpty(list);

            result.Should().BeFalse();
        }
    }
}