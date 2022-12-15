using NUnit.Framework;

namespace Utils.Tests;

[TestFixture]
public class RoundingTests
{
    [Test]
    [TestCase(null, null)]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(10.5, 10.5)]
    [TestCase(11.5, 11.5)]
    [TestCase(1.25, 1.0)]
    [TestCase(1.26, 1.5)]
    [TestCase(1.75, 2.0)]
    [TestCase(1.76, 2.0)]
    [TestCase(2.75, 3.0)]
    [TestCase(1/3d, 0.5)]
    public void TestToHalf(double? actual, double? expected)
    {
        Assert.That(Rounding.ToHalf(actual), Is.EqualTo(expected));
    }
}