using System;
using NUnit.Framework;

namespace Utils.Tests;

public class AccessorTest
{
    [Test]
    public void TestProperty()
    {
        var testClass = new TestClass { Property = "a" };
        
        var stringAccessor = new Accessor<string>(() => testClass.Property);

        Assert.That(stringAccessor.Get(), Is.EqualTo("a"));
        Assert.That(testClass.Property, Is.EqualTo("a"));

        stringAccessor.Set("b");
        Assert.That(stringAccessor.Get(), Is.EqualTo("b"));
        Assert.That(testClass.Property, Is.EqualTo("b"));

        testClass.Property = "c";
        Assert.That(stringAccessor.Get(), Is.EqualTo("c"));
        Assert.That(testClass.Property, Is.EqualTo("c"));
    }

    [Test]
    public void TestField()
    {
        var testClass = new TestClass { Field = "a" };

        var stringAccessor = new Accessor<string>(() => testClass.Field);

        Assert.That(stringAccessor.Get(), Is.EqualTo("a"));
        Assert.That(testClass.Field, Is.EqualTo("a"));
        
        stringAccessor.Set("b");
        Assert.That(stringAccessor.Get(), Is.EqualTo("b"));
        Assert.That(testClass.Field, Is.EqualTo("b"));

        testClass.Field = "c";
        Assert.That(stringAccessor.Get(), Is.EqualTo("c"));
        Assert.That(testClass.Field, Is.EqualTo("c"));
    }
    
    [Test]
    public void TestNonMember()
    {
       Assert.That(() => new Accessor<string>(() => "blah"), Throws.Exception.TypeOf<ArgumentException>().With.Message.EqualTo("expression must be return a field or property"));
    }
    
    [Test]
    public void TestNonMemberClass()
    { 
        var testClass = new TestClass();
        
        Assert.That(() => new Accessor<string>(() => testClass.GetString()), Throws.Exception.TypeOf<ArgumentException>().With.Message.EqualTo("expression must be return a field or property"));
    }


    private class TestClass
    {
        // ReSharper disable once PropertyCanBeMadeInitOnly.Local
        public string Property { get; set; }

        public string Field;

        public string GetString() => "blah";
    }
}