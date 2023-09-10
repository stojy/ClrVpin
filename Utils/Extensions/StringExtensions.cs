using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Utils.Extensions;

public static class StringExtensions
{
    // convert from camel case to regular title cased source
    // e.g. splitOnNumbers=false.. SpinACard --> Spin A Card
    // e.g. splitOnNumbers=true.. TitleFightBeta1 --> Title Fight Beta 1
    public static string FromCamelCase(this string source, bool splitOnNumbers = false, IEnumerable<string> ignoreWords = null)
    {
        // handle ignoreWords by converting matches to lowercase so they are effectively ignored when it comes to the title case checking
        ignoreWords?.ForEach(x => source = source.Replace(x, x.ToLower()));

        var newString = new StringBuilder();
        for (var i = 0; i < source.Length; i++)
        {
            var currentLetter = source[i];
            var previousLetter = i >= 1 ? source[i - 1] : (char?)null;
            var earlierLetter = i >= 2 ? source[i - 2] : (char?)null;

            if (previousLetter != null &&
                (char.IsUpper(currentLetter) || (splitOnNumbers && char.IsNumber(currentLetter) && !char.IsNumber(previousLetter.Value))) &&
                (char.IsLower(previousLetter.Value) || (splitOnNumbers && char.IsNumber(previousLetter.Value)) || (char.ToLowerInvariant(previousLetter.Value) == 'a' && (earlierLetter == null || char.IsLower(earlierLetter.Value)))))
            {
                newString.Append(' ');
            }

            newString.Append(currentLetter);
        }

        return newString.ToString();
    }

    public static bool IsNullOrWhiteSpace(this string source) => string.IsNullOrWhiteSpace(source);

    public static bool IsEmpty(this string source) => string.IsNullOrWhiteSpace(source) || source == "-";

    public static string RemoveChars(this string source, params char[] unwantedChars) =>
        // x3 times quicker to remove via Split than Replace!
        // - https://stackoverflow.com/a/16974999/227110
        source == null ? null : string.Join("", source.Split(unwantedChars));

    public static bool ContainsAny(this string source, params string[] items)
    {
        return items.Any(item => source?.Contains(item) == true);
    }

    public static string RemoveDiacritics(this string source)
    {
        // replace diacritic glyph (e.g. acute and grave) with the base character
        // - e.g. é to e, ç to c, ế to e
        // - https://en.m.wikipedia.org/wiki/Diacritic
        var baseCharacters = source
            // formD
            // - decompose (aka split) a single composed unicode character into it's parts.. base character and it's "non spacing marks" (aka diacritic symbols)
            // - unicode can be encoded as either a single composed char or multiple decomposed chars
            //   e.g.  ế composed (U+1EBF) = decomposed U+0065 (e), U+0302 (circumflex accent), U+0301 (acute accent)
            // - https://stackoverflow.com/questions/3288114/what-does-nets-string-normalize-do
            //   https://stackoverflow.com/a/249126/227110
            .Normalize(NormalizationForm.FormD)
            // remove diacritic symbols
            // - e.g. for ế, remove U+0302 (circumflex accent), U+0301 (acute accent)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(baseCharacters.ToArray());
    }

    public static bool EqualsIgnoreDiacritics(this string source, string other) =>
        // IgnoreNonSpace - ignore the non-spacing characters that form the diacritic symbols
        // - https://stackoverflow.com/questions/55548264/how-can-i-get-true-if-we-compare-a-to-%C3%A1
        // - https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareoptions?view=netframework-4.7.2
        string.Compare(source, other, CultureInfo.InvariantCulture, CompareOptions.IgnoreNonSpace) == 0;

    public static string TrimPseudoWhitespace(this string source) => 
        source?.TrimStart(_pseudoWhiteSpaceChars).TrimEnd(_pseudoWhiteSpaceChars);

    public static string TrimSingleLetterWords(this string source)
    {
        if (source == null)
            return null;
        
        // trim any whitespace before we start
        source = source.TrimPseudoWhitespace();

        // trim any single letter character words from beginning and end
        var splitWords = source.Split(" ");
        
        var firstWordChars = splitWords.First().ToCharArray();
        if (firstWordChars.Length == 1 && char.IsLetter(firstWordChars.First()))
            source = source.TrimStart(firstWordChars.First());
        
        var lastWordChars = splitWords.Last().ToCharArray();
        if (lastWordChars.Length == 1 && char.IsLetter(lastWordChars.First()))
            source = source.TrimEnd(lastWordChars.First());

        return source.TrimPseudoWhitespace();
    }

    private static readonly char[] _pseudoWhiteSpaceChars = { ' ', '_', '-' };
}