using System.Diagnostics.CodeAnalysis;
using ClrVpin.Shared;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ClrVpin.Tests
{
    public class TableUtilsTests
    {

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        [Test]
        [TestCase("Indiana Jones (Williams 1993) blah.directb2s", "indiana jones", "williams", 1993)]
        [TestCase("Indiana Jones (Williams) blah.directb2s", "indiana jones", "williams", null)]
        [TestCase("Indiana Jones (1993) blah.directb2s", "indiana jones", null, 1993)]
        [TestCase("Indiana Jones.directb2s", "indiana jones", null, null)]
        [TestCase("Indiana Jones (blah) (Williams 1993).directb2s", "indiana jones", "williams", 1993, TestName = "only last most parenthesis is used")]
        [TestCase("", null, null, null, TestName = "empty string")]
        [TestCase("123", "123", null, null, TestName = "number title")]
        [TestCase("123 (Williams 1993)", "123", "williams", 1993, TestName = "number title with manufacturer and year")]
        [TestCase("123 (Williams)", "123", "williams", null, TestName = "number title with manufacturer only")]
        [TestCase("123 (1993)", "123", null, 1993, TestName = "number titleand with year only")]
        [TestCase("123 blah (Williams 1993)", "123 blah", "williams", 1993, TestName = "number and word title with manufacturer and year")]
        [TestCase("123 blah (1993)", "123 blah", null, 1993, TestName = "number title with word and year only")]
        public void FuzzyGetFuzzyFileNameDetailsTest(string fileName, string expectedName, string expectedManufacturer, int? expectedYear)
        {
            var (name, manufacturer, year) = TableUtils.GetFuzzyFileNameDetails(fileName);

            Assert.That(name, Is.EqualTo(expectedName));
            Assert.That(manufacturer, Is.EqualTo(expectedManufacturer));
            Assert.That(year, Is.EqualTo(expectedYear));
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        [Test]
        [TestCase("medieval madness", "medieval madness", true)]
        [TestCase("medieval madness.vpx", "medieval madness", true)]
        [TestCase("medieval madness.vpx", "medieval madness", true)]
        [TestCase("medieval madness", "medieval madness.vpx", true)]
        [TestCase("medieval madness", "medieval madness.vpx", true)]
        [TestCase("medieval madness", "medieval madness (Williams 2006)", true)]
        [TestCase("medieval madness (Williams 2006)", "medieval madness", true)]
        [TestCase("medieval madnes (Williams 2006)", "medieval madness", true)]
        [TestCase("medieval madness (Williams 2006)", "medieval madness (blah 2006)", true)]
        [TestCase("medieval madness (  Williams 2006)", "medieval madness (blah 2006)", true)]
        [TestCase(" medieval madness (Williams 2006)", "medieval madness (blah 2006)", true, TestName = "trim whitespace")]
        [TestCase("medieval   madness (Williams 2006)", "medieval madness (blah 2006)", true, TestName = "remove x2 whitespace")]
        [TestCase("medieval    madness (Williams 2006)", "medieval madness (blah 2006)", true, TestName = "remove x4 whitespace")]
        [TestCase("medieval     madness (Williams 2006)", "medieval madness (blah 2006)", true, TestName = "remove x5 whitespace")]
        [TestCase("medieval              madness (Williams 2006)", "medieval madness (blah 2000)", false, TestName = "remove lots of whitespace")]
        [TestCase("medieval madnesas (Williams 2006)", "medieval madness", false, TestName = "typo")]
        [TestCase("ali (Stern 1980)", "ali", true, TestName = "short name exact match")]
        [TestCase("ali (Williams 2006)", "alien (blah)", false, TestName = "#1 - minimum 15 characters required for partial match")]
        [TestCase("black knight 2000", "black knight", false, TestName = "#2 - minimum 15 characters required for partial match")]
        [TestCase("black knight returns 2000", "black knight", false, TestName = "#3 - minimum 15 characters required for partial match")]
        [TestCase("black knight returns 2000", "black knight retur", true, TestName = "#4 - minimum 15 characters required for partial match")]
        [TestCase("the black knight", "black knight", true, TestName = "remove 'the'")]
        [TestCase("black&apos; knight", "black knight", true, TestName = "remove '&apos;'")]
        [TestCase("black' knight", "black knight", true, TestName = "remove '''")]
        [TestCase("black` knight", "black knight", true, TestName = "remove '`'")]
        [TestCase("black, knight", "black knight", true, TestName = "remove ','")]
        [TestCase("black; knight", "black knight", true, TestName = "remove ';'")]
        [TestCase("black knight!", "black knight", true, TestName = "remove '!'")]
        [TestCase("black-knight", "black knight", true, TestName = "remove '-'")]
        [TestCase("black - knight", "black knight", true, TestName = "remove ' - '")]
        [TestCase("black_knight", "black knight", true, TestName = "remove '_'")]
        [TestCase("black&knight", "black and knight", true, TestName = "replace '&'")]
        [TestCase("black & knight", "black and knight", true, TestName = "replace ' & '")]
        [TestCase("Rocky and Bullwinkle And Friends (Data East 1993)", "Adventures of Rocky and Bullwinkle and Friends (1993).directb2s", true, TestName = "#1 contains - 20 characters satisified")]
        [TestCase("Rocky and Bullwinkl", "Adventures of Rocky and Bullwinkle and Friends (1993).directb2s", false, TestName = "#1 contains - 20 characters not satisified")]
        [TestCase("Indiana Jones (Stern 2008)", "Indiana Jones (Stern 2006) by Starlion.directb2s", false, TestName = "#0 - check year - exact match must not exceed 1 either")]
        [TestCase("Indiana Jones (Stern 2008)", "Indiana Jones (Williams 2010).directb2s", false, TestName = "#1 check year - exceeds 1")]
        [TestCase("Indiana Jones (Stern 2006)", "Indiana Jones (Williams 2008).directb2s", false, TestName = "#2 check year - exceeds 1")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (Williams 1994).directb2s", true, TestName = "#3 check year - 1 ok")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (Williams 1992).directb2s", true, TestName = "#4 check year - 1 ok")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (Williams 1993).directb2s", true, TestName = "#5 check year - match ok")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (Williams).directb2s", true, TestName = "#6 check year - year missing")]
        [TestCase("Indiana Jones (Stern)", "Indiana Jones (Williams1993).directb2s", true, TestName = "#7 check year - year missing")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (1995).directb2s", false, TestName = "#8 check year - year only, but too large")]
        [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones (1994).directb2s", true, TestName = "#9 check year - year only, ok")]
        public void FuzzyMatchTest(string first, string second, bool expectedIsMatch)
        {
            var isMatch = TableUtils.FuzzyMatch(first, second);

            Assert.That(isMatch, Is.EqualTo(expectedIsMatch));
        }
    }
}