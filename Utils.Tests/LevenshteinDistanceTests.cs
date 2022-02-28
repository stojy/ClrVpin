using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Utils.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class LevenshteinDistanceTests
    {
        [Test]
        [TestCase("Mum", "Mum", 0)]
        [TestCase("Mum", "Mam", 1)]
        [TestCase("Mum", "Muma", 1)]
        [TestCase("Mum", "Mauma", 2)]
        [TestCase("climax", "volmax", 3)]
        public void TestCalculate(string first, string second, int expectedScore)
        {
            Assert.That(LevenshteinDistance.Calculate(first, second), Is.EqualTo(expectedScore));
        }
    }
}
