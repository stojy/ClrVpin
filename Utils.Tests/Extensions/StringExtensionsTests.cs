using NUnit.Framework;
using Utils.Extensions;
// ReSharper disable StringLiteralTypo

namespace Utils.Tests.Extensions
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        [TestCase("SpinACard", "Spin A Card")]
        [TestCase("aSpinACard", "a Spin A Card")]
        [TestCase("aSpinaCard", "a Spina Card")]
        [TestCase("SpinACARD", "Spin A CARD")]
        [TestCase("ACard", "A Card")]
        [TestCase("ASpinACard", "A Spin A Card")]
        [TestCase("ACARD", "A CARD")]
        [TestCase("BCARD", "BCARD")]
        [TestCase("A", "A")]
        [TestCase("AA", "A A")]
        [TestCase("AAA", "A AA")]
        [TestCase("TheHouse", "The House")]
        [TestCase("123TheHouse", "123The House")]
        [TestCase("AC/DC Let There BeRock", "A C/DC Let There Be Rock")] // first word "AC" has "A" inserted as a new word.. an unfortunate side effect
        public void TestFromCamelCase(string name, string expectedName)
        {
            Assert.That(name.FromCamelCase(), Is.EqualTo(expectedName));
        }
    }
}