using ClrVpin.Shared;
using NUnit.Framework;

namespace ClrVpin.Tests.Shared
{
    [TestFixture]
    internal class FileUtilsTests
    {
        [Test]
        [TestCase(null, false)]
        [TestCase("file", false)]
        [TestCase("file.txt", false)]
        [TestCase("file/", false)]
        [TestCase("path/file", false)]
        [TestCase("file:", true)]
        [TestCase("fi:le", true)]
        [TestCase(@"directory\file", false)]
        [TestCase(@"directory\file", false)]
        [TestCase("Star Trek: The Next Generation (Williams 1993).mp3", true)]
        [TestCase(@"C:\vp\apps\PinballX\Media\Visual Pinball\Launch Audio\Star Trek: The Next Generation (Williams 1993).mp3", true)]
        public void TestContainsInvalidChars(string file, bool expectedInvalid)
        {
            Assert.That(file.HasInvalidFileNameChars(), Is.EqualTo(expectedInvalid));
        }
    }
}
