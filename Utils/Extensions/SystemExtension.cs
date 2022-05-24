using System.Text.Json;

namespace Utils.Extensions
{
    public static class SystemExtension
    {
        public static T Clone<T>(this T source)
        {
            var serialized = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(serialized);
        }

        public static bool IsEqual<T>(this T source, T other)
        {
            var serializedSource = JsonSerializer.Serialize(source);
            var serializedOther = JsonSerializer.Serialize(other);
            return serializedSource == serializedOther;
        }
        
        public static bool IsEqual<T>(this T source, string serializedOther)
        {
            var serializedSource = JsonSerializer.Serialize(source);
            return serializedSource == serializedOther;
        }
    }}
