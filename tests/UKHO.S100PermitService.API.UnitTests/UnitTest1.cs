using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace UKHO.S100PermitService.API.UnitTests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(string))]
    public class UnitTest1<T>
    {
        [Test]
        public void TestType()
        {
            Assert.Pass($"The generic test type is {typeof(T)}");
        }
    }
}