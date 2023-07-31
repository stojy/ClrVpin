using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils.Extensions;

public static class StringExtensions
{
    // convert from camel case to regular title cased text
    // e.g. SpinACard --> Spin A Card
    public static string FromCamelCase(this string value, IEnumerable<string> ignoreWords = null)
    {
        // handle ignoreWords by converting matches to lowercase so they are effectively ignored when it comes to the title case checking
        ignoreWords?.ForEach(x => value = value.Replace(x, x.ToLower()));

        var newString = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var currentLetter = value[i];
            var previousLetter = i >= 1 ? value[i - 1] : (char?)null;
            var earlierLetter = i >= 2 ? value[i - 2] : (char?)null;

            if (char.IsUpper(currentLetter) && previousLetter != null &&
                (char.IsLower(previousLetter.Value) || char.ToLowerInvariant(previousLetter.Value) == 'a' && (earlierLetter == null || char.IsLower(earlierLetter.Value))))
                newString.Append(" ");
            newString.Append(currentLetter);
        }

        return newString.ToString();
    }

    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsEmpty(this string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "-";
    }

    public static string RemoveChars(this string value, params char[] unwantedChars)
    {
        // x3 times quicker to remove via Split than Replace!
        // - https://stackoverflow.com/a/16974999/227110
        return value == null ? null : string.Join("", value.Split(unwantedChars));
    }
    
    public static bool ContainsAny(this string value, params string[] items)
    {
        return items.Any(item => value?.Contains(item) == true);
    }
}