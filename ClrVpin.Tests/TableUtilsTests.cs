using System.Diagnostics.CodeAnalysis;
using ClrVpin.Shared;
using NUnit.Framework;

namespace ClrVpin.Tests
{
    public class TableUtilsTests
    {
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
        [TestCase("medieval madness (Williams 2006)", "medieval madness (blah 2000)", true)]
        [TestCase("medieval madness (  Williams 2006)", "medieval madness (blah 2000)", true)]
        [TestCase(" medieval madness (Williams 2006)", "medieval madness (blah 2000)", true, TestName = "trim whitespace")]
        [TestCase("medieval   madness (Williams 2006)", "medieval madness (blah 2000)", true, TestName = "remove x2 whitespace")]
        [TestCase("medieval    madness (Williams 2006)", "medieval madness (blah 2000)", true, TestName = "remove x4 whitespace")]
        [TestCase("medieval     madness (Williams 2006)", "medieval madness (blah 2000)", true, TestName = "remove x5 whitespace")]
        [TestCase("medieval              madness (Williams 2006)", "medieval madness (blah 2000)", false, TestName = "remove lots of whitespace")]
        [TestCase("medieval madnesas (Williams 2006)", "medieval madness", false)]
        [TestCase("ali (Williams 2006)", "alien (blah)", false, TestName = "#1 - minimum 15 characters required for partial match")]
        [TestCase("black knight 2000", "black knight", false, TestName = "#2 - minimum 15 characters required for partial match")]
        [TestCase("black knight returns 2000", "black knight", false, TestName = "#3 - minimum 15 characters required for partial match")]
        [TestCase("black knight returns 2000", "black knight retur", true, TestName = "#4 - minimum 15 characters required for partial match")]
        [TestCase("the black knight", "black knight", true, TestName = "remove 'the'")]
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
        public void FuzzyMatchTest(string first, string second, bool expectedIsMatch)
        {
            var isMatch = TableUtils.FuzzyMatch(first, second);

            Assert.That(isMatch, Is.EqualTo(expectedIsMatch));
        }
    }
}