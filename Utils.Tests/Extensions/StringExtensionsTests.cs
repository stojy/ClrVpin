using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Utils.Extensions;
using Assert = NUnit.Framework.Assert;

// ReSharper disable StringLiteralTypo

namespace Utils.Tests.Extensions;

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

    [Test]
    [TestCase("abcdefgh", new[] { 'b', 'f' }, "acdegh")]
    [TestCase("abc--de-fgh", new[] { '-' }, "abcdefgh")]
    [TestCase(null, new[] { '-' }, null)]
    public void TestRemoveChars(string source, char[] unwantedChars, string expectedResult)
    {
        Assert.That(source.RemoveChars(unwantedChars), Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase("music and sound mod", new[] { "music mod" }, false)]
    [TestCase("music and sound mod", new[] { "music" }, true)]
    [TestCase("music and sound mod", new[] { "sound mod" }, true)]
    [TestCase(null, new[] { "sound mod" }, false)]
    [TestCase(null, new[] { "" }, false)]
    public void TestContainsAny(string source, string[] items, bool expectedResult)
    {
        Assert.That(source.ContainsAny(items), Is.EqualTo(expectedResult));
    }

    [TestMethod]
    [TestCase("Galáxia", "Galaxia")]
    [TestCase("Cáfế", "Cafe")]
    [TestCase("crème brûlée", "creme brulee")]
    [TestCase("Ю", "Ю", Description = "no conversion.. character is not a diacritic")]
    [TestCase("ÃŘ", "AR")]
    public void TestRemoveDiacritics(string source, string expectedResult)
    {
        Assert.That(source.RemoveDiacritics(), Is.EqualTo(expectedResult));
    }
}