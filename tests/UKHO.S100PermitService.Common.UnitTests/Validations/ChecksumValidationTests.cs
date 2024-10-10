using FluentAssertions;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Validations
{
    [TestFixture]
    public class ChecksumValidationTests
    {
        [Test]
        public void WhenChecksumMatches_ThenIsValidReturnsTrue_()
        {
            const string Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L2";

            var result = ChecksumValidation.IsValid(Upn);

            result.Should().BeTrue();
        }

        [Test]
        public void WhenChecksumDoesNotMatch_ThenIsValidReturnsFalse_()
        {
            const string UpnInvalidChecksum = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF0J0K1L2";

            var result = ChecksumValidation.IsValid(UpnInvalidChecksum);

            result.Should().BeFalse();
        }
    }
}
