using System.Collections.Generic;
using NUnit.Framework;
using Utils.Extensions;

namespace Utils.Tests.Extensions;

[TestFixture]
internal class LinqExtensionsTests
{
    [Test]
    public void TestContainsAll()
    {
        var collection = new[] { 1, 2, 3 };
        
        Assert.That(collection.ContainsAll(4, 5), Is.False);
        Assert.That(collection.ContainsAll(3, 4, 5), Is.False);
        Assert.That(collection.ContainsAll(1, 2, 3, 4, 5), Is.False);
        Assert.That(collection.ContainsAll(1, 2, 3), Is.True);
        Assert.That(collection.ContainsAll(1, 2), Is.True);
        Assert.That(collection.ContainsAll(3), Is.True);
        Assert.That(collection.ContainsAll(), Is.False);

        var otherCollection = new[] { 1, 2 };
        Assert.That(collection.ContainsAll(otherCollection), Is.True);
    }

    [Test]
    public void TestContainsAny()
    {
        var collection = new[] { 1, 2, 3 };
        
        // params arg(s)
        Assert.That(collection.ContainsAny(4, 5), Is.False);
        Assert.That(collection.ContainsAny(4, 5, 3), Is.True);
        Assert.That(collection.ContainsAny(2), Is.True);
        Assert.That(collection.ContainsAny(), Is.False);

        // array arg.. invokes params overload
        var otherCollection = new[] { 1, 2 };
        Assert.That(collection.ContainsAny(otherCollection), Is.True);

        // list check.. invokes IEnumerable overload
        var otherCollectionList = new List<int> { 1, 2 };
        Assert.That(collection.ContainsAny(otherCollectionList), Is.True);
    }

    [Test]
    [TestCase(new byte[] { 1, 2, 3}, new byte[] {1, 3}, -1)]
    [TestCase(new byte[] { 1, 2, 3}, new byte[] {}, -1)]
    [TestCase(new byte[] { 1, 2, 3}, new byte[] {1}, 0)]
    [TestCase(new byte[] { 1, 2, 3}, new byte[] {2, 3}, 1)]
    [TestCase(new byte[] { 1, 2, 3, 20, 30, 40}, new byte[] {20, 30, 40}, 3)]
    [TestCase(new byte[] { 1, 2}, new byte[] {1, 2, 3}, -1)]
    [TestCase(new byte[] { }, new byte[] {1, 2, 3}, -1)]
    [TestCase(new byte[] { }, new byte[] {}, -1)]
    public void TestIndexOf(byte[] haystack, byte[] needle, int expectedIndex)
    {
        Assert.That(haystack.IndexOf(needle), Is.EqualTo(expectedIndex));
    }
}