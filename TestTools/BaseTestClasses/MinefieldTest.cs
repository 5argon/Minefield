using NUnit.Framework;

namespace E7.Minefield
{
    public abstract class MinefieldTest
    {
        [SetUp]
        public void MinefieldTestSetUp()
        {
            Utility.MinefieldTesting = true;
        }

        [TearDown]
        public void MinefieldTestTearDown()
        {
            Utility.MinefieldTesting = false;
        }
    }
}