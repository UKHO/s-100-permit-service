using NUnit.Framework;

namespace UKHO.S100PermitService.API.UnitTests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(string))]
    public class Tests<T>
    {
        [Test]
        public void TestType()
        {
            Assert.Pass($"The generic test type is {typeof(T)}");
        }
    }
}
