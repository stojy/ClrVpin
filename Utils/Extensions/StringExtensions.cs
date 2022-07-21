using System.Collections.Generic;
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

    public static bool IsEmpty(this string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "-";
    }
}