using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClrVpin.Shared
{
    public static class Fuzzy
    {
        public static (string name, string nameNoWhiteSpace, string manufacturer, int? year) GetFileDetails(string fileName)
        {
            // return the fuzzy portion of the filename..
            // - no file extensions
            // - name: up to last opening parenthesis (if it exists!)
            // - manufacturer: words up to the first year (if it exists!)
            // - year: digits up to the first closing parenthesis (if it exists!)
            string name;
            string manufacturer = null;
            int? year = null;

            fileName = Path.GetFileNameWithoutExtension(fileName ?? "");
            var result = _fuzzyFileNameRegex.Match(fileName);

            if (result.Success)
            {
                // strip any additional parenthesis content out
                name = result.Groups["name"].Value.ToNull();

                manufacturer = result.Groups["manufacturer"].Value.Split("(").Last().ToNull();

                if (int.TryParse(result.Groups["year"].Value, out var parsedYear))
                    year = parsedYear;
            }
            else
                name = fileName.ToNull();

            // fuzzy clean the name field
            name = Clean(name, false);
            var nameNoWhiteSpace = Clean(name, true);

            return (name, nameNoWhiteSpace, manufacturer, year);
        }

        public static bool Match(string first, (string name, string nameNoWhiteSpace, string manufacturer, int? year) secondFuzzy)
        {
            var firstFuzzy = GetFileDetails(first);

            var exactMatch = IsExactMatch(firstFuzzy.name, secondFuzzy.name) || IsExactMatch(firstFuzzy.nameNoWhiteSpace, secondFuzzy.nameNoWhiteSpace);

            var startsMatch = IsStartsMatch(firstFuzzy.name, secondFuzzy.name) || IsStartsMatch(firstFuzzy.nameNoWhiteSpace, secondFuzzy.nameNoWhiteSpace);

            var containsMatch = IsContainsMatch(firstFuzzy.name, secondFuzzy.name) || IsContainsMatch(firstFuzzy.nameNoWhiteSpace, secondFuzzy.nameNoWhiteSpace);

            // if both names include years.. then they must match
            var yearMismatch = firstFuzzy.year.HasValue && secondFuzzy.year.HasValue && Math.Abs(firstFuzzy.year.Value - secondFuzzy.year.Value) > 1;

            return !yearMismatch && (exactMatch || startsMatch || containsMatch);
        }

        private static string ToNull(this string name) => name == "" ? null : name.ToLower().Trim();

        private static bool IsExactMatch(string firstFuzzyName, string secondFuzzyName) => firstFuzzyName == secondFuzzyName;

        private static bool IsContainsMatch(string firstFuzzyName, string secondFuzzyName) =>
            firstFuzzyName.Length >= 20 && secondFuzzyName.Length >= 20 && (firstFuzzyName.Contains(secondFuzzyName) || secondFuzzyName.Contains(firstFuzzyName));

        private static bool IsStartsMatch(string firstFuzzyName, string secondFuzzyName) =>
            firstFuzzyName.Length >= 15 && secondFuzzyName.Length >= 15 && (firstFuzzyName.StartsWith(secondFuzzyName) || secondFuzzyName.StartsWith(firstFuzzyName));

        private static string Clean(string first, bool removeAllWhiteSpace)
        {
            // clean the string to make it a little cleaner for subsequent matching
            // - order is important!
            var fuzzyClean = first?.ToLower()
                    .Replace("the", "")
                    .Replace("&apos;", "")
                    .Replace("'", "")
                    .Replace("`", "")
                    .Replace(",", "")
                    .Replace(";", "")
                    .Replace("!", "")
                    .Replace("?", "")
                    .Replace("-", " ")
                    .Replace(" - ", "")
                    .Replace("_", " ")
                    .Replace(".", " ")
                    .Replace("&", " and ")
                    .Replace(" iv", " 4")
                    .Replace(" iii", " 3")
                    .Replace(" ii", " 2")
                    .Replace("    ", " ")
                    .Replace("   ", " ")
                    .Replace("  ", " ")
                    .TrimStart()
                    .Trim()
                ;

            if (removeAllWhiteSpace)
                fuzzyClean = fuzzyClean?.Replace(" ", "");

            return fuzzyClean;
        }

        // regex
        // - faster: name via looking for the first opening parenthesis.. https://regex101.com/r/CxKJK1/1
        // - slower: name is greedy search using the last opening parenthesis.. https://regex101.com/r/xiXsML/1.. @"(?<name>.*)\((?<manufacturer>\D*)(?<year>\d*)\).*"
        private static readonly Regex _fuzzyFileNameRegex = new Regex(@"(?<name>[^(]*)\((?<manufacturer>\D*)(?<year>\d*)\)", RegexOptions.Compiled);
    }
}