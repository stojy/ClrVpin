using System;
using System.Text.Json;
using ClrVpin.Converters;
using NUnit.Framework;

namespace ClrVpin.Tests.Converters;

[TestFixture]
public class UnixToNullableDateTimeConverterTests
{
    [Test]
    [TestCase("1613967667240", "2021-02-22 12:21:07.24+08:00", TestName = "unix time in milliseconds (+8 TZ)")]
    [TestCase("1613967667", "2021-02-22 12:21:07+08:00", TestName = "unix time in seconds (+8 TZ)")]
    [TestCase("\"invalid\"", null, TestName = "invalid time")]
    [TestCase("null", null, TestName = "null time")]
    [TestCase("\"\"", null, TestName = "empty time")]
    public void TestDeserialize(string serializedTime, string expectedDeserializedTime)
    {
        var serialized = $"{{\"NullableTime\":{serializedTime}}}";
        DateTime? expectedTime = null;
        if (DateTime.TryParse(expectedDeserializedTime, out var expectedParsedTime))
            expectedTime = expectedParsedTime;
        
        var jsonSerializerOptions = new JsonSerializerOptions {Converters = { new UnixToNullableDateTimeConverter() }};
        var deserialized = JsonSerializer.Deserialize<TestClass>(serialized, jsonSerializerOptions);
            
        Assert.That(deserialized.NullableTime, Is.EqualTo(expectedTime));
        Assert.That(deserialized.NullableTime?.Ticks, Is.EqualTo(expectedTime?.Ticks));
    }

    class TestClass
    {
        public DateTime? NullableTime { get; set; }
    }
}