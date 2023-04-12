using ClrVpin.Shared.Utils;
using NUnit.Framework;

namespace ClrVpin.Tests.Shared
{
    [TestFixture]
    internal class FileUtilsTests
    {
        [Test]
        [TestCase(null, false, false)]
        [TestCase("file", true, false)]
        [TestCase("file.txt", true, false)]
        [TestCase("file/", false, false)]
        [TestCase("path/file", false, false)]
        [TestCase("AC/DC", true, true)]
        [TestCase("file:", true, true)]
        [TestCase("fi:le", true, true)]
        [TestCase(@"directory\file", false, false)]
        [TestCase(@"directory\file", false, false)]
        [TestCase("Star Trek: The Next Generation (Williams 1993).mp3", true, true)]
        [TestCase(@"C:\vp\apps\PinballX\Media\Visual Pinball\Launch Audio\Star Trek: The Next Generation (Williams 1993).mp3", false, true)]
        public void TestContainsInvalidChars(string path, bool isFile, bool expectedInvalid)
        {
            Assert.That(path.HasInvalidFileNameChars(isFile), Is.EqualTo(expectedInvalid));
        }
    }
}
