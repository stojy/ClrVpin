using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClrVpin.Importer;

public class UnixToNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // conversions above UnixMaxSeconds are assumed to be in milliseconds (conversions beyond this range in .net6 are rejected)
        if (reader.TryGetInt64(out var time))
            return time is < UnixMaxSeconds and > UnixMinSeconds ? DateTimeOffset.FromUnixTimeSeconds(time).LocalDateTime : DateTimeOffset.FromUnixTimeMilliseconds(time).LocalDateTime;
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) => throw new NotSupportedException();

    private const long UnixMaxSeconds = 253_402_300_799; // 31/12/9999 = DateTime.MaxTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
    private const long UnixMinSeconds = -62_135_596_800; // 1/1/0001
}