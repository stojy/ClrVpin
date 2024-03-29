﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared.Fuzzy;

public static class Fuzzy
{
    static Fuzzy()
    {
        // compile and store regex to improve performance

        // words to remove during the clean pre-parse stage
        // - https://regex101.com/r/XpvyjP/1
        string[] preParseWordRemovals = { @"\(mod\)" };
        var pattern = string.Join('|', preParseWordRemovals);
        _preParseWordRemovalRegex = new Regex($"{pattern}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // chars
        // - cater for special and non-ascii characters (i.e. 8 bit chars) as handling of these between IPDB, XML DB, and file names is often inconsistent
        // - https://regex101.com/r/EFqzhF/2
        string[] specialChars = { "&apos;", "ï¿½", "'", "`", "’", ",", ";", "!", @"\?", "&", @"\(", @"\)" };
        string[] nonAsciiChars = { @"[^\x00-\x7F]" };
        pattern = string.Join('|', specialChars.Concat(nonAsciiChars));
        _trimSpecialAndNonAsciiCharRegex = new Regex($"({pattern})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        string[] trailingPeriod = { @"\.{1}$" };
        pattern = string.Join('|', trailingPeriod);
        _trimTrailingPeriodRegex = new Regex($"({pattern})", RegexOptions.Compiled);

        // words that are to be ignored during the title case word expansion
        _titleCaseWordExceptions = new[] { "MoD", "SG1bsoN" };

        // captures word match
        // - word(s) can exist anywhere at any position, so long as it's separated by a non-letter
        // - words must be defined as lower case
        // - used with Regex.Replace will capture multiple matches at once.. same word or other words
        // - lookahead match without capture: https://stackoverflow.com/a/3926546/227110
        // - https://regex101.com/r/DoztL5/1
        Authors = new[] { "jps", "jp's", "jp", "sg1bson", "vpw", "starlion", "pinball58", "vp99", "balutito", "siggis", "uws" };
        string[] language = { "a", "and", "n'", "'n", "n", "the", "en" };
        string[] vpx = {"vpx8", "vpx", "mod", "vp10", "4k", "b2s", "4player", "2021", "2022", "2023", "2024", "logo" }; // order is important, e.g. vpx8 to be stripped before vpx
        string[] technologyTypes = { TableType.ElectroMagnetic.ToLower(), TableType.SolidState.ToLower(), TableType.PureMechanical.ToLower() };
        string[] descriptions = { "no leds", "upgrade", "premium" };
        string[] versions = { "beta1", "beta" }; // order is important, e.g. ensure beta1 is removed before beta
        pattern = string.Join('|', Authors.Concat(language).Concat(vpx).Concat(technologyTypes).Concat(descriptions).Concat(versions));
        _stopWholeWordRegex = new Regex($"(?<=^|[^a-z^A-Z])({pattern})(?=$|[^a-zA-Z])", RegexOptions.Compiled);

        // first pass single whitespace
        // - performed BEFORE other checks that aren't sensitive to these changes
        string[] firstPassSpacings = { "-", " - " };
        pattern = string.Join('|', firstPassSpacings);
        _addSpacingFirstPassRegex = new Regex($"({pattern})", RegexOptions.Compiled);

        // second pass single whitespace
        // - performed AFTER other checks that are sensitive to these changes, e.g. version checking
        string[] spacings = { "_", @"\." };
        pattern = string.Join('|', spacings);
        _addSpacingSecondPassRegex = new Regex($"({pattern})", RegexOptions.Compiled);

        // version
        // - number can be anywhere in the string
        //   - assumes other processing has completed, e.g. strip file extension, author, etc
        //   - extra whitespace ok
        // - 2 options..
        //   a. number without decimal (or underscore) - requires v/V prefix
        //   b. number with decimal (or underscore) - optional v/V prefix
        // - https://regex101.com/r/UzSgoC/1
        _versionRegex = new Regex(@"[^a-zA-Z0-9]+([vV]\d+$|[vV]?\d+\.+\d+\.*\d*\s*$|[vV]?\d+_+\d+_*\d*\s*$)", RegexOptions.Compiled);

        // preamble
        // - number.. aka file id (assumed 5 digits or more)
        _preambleRegex = new Regex(@"^(\d{5,})", RegexOptions.Compiled);

        // multiple whitespace
        _multipleWhitespaceRegex = new Regex(@"(\s{2,})", RegexOptions.Compiled);

        // file name info parsing
        // - faster: name via looking for the FIRST opening parenthesis.. https://regex101.com/r/tRqeOH/1..         (?<name>[^(]*)[(](?<manufacturer>\D*)(?<year>\d*)\D*\)
        // - slower: name is greedy search, terminating via the LAST opening parenthesis.. https://regex101.com/r/xiXsML/1..   (?<name>.*)[(](?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*[)].*
        _fileNameInfoRegex = new Regex(@"(?<name>.*)[(](?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*[)].*", RegexOptions.Compiled);

        // non-standard file name info parsing
        // - intended as a second chance match, i.e. only invoked if the standard (above) fails
        // - supports mandatory manufacturer and optional year
        // - https://regex101.com/r/AYTJbL/1
        string[] manufacturers = { "bally", "williams", "stern" };
        pattern = string.Join('|', manufacturers);
        _nonStandardFileNameInfoRegex = new Regex($@".*(?<manufacturer>{pattern}bally|Williams|Stern)\W+(?<year>\d\d\d\d)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // aliases
        // - replace specific word(s)
        // - check key: case insensitive check
        // - substitution value: the specified case is respected to ensure any post-alias case sensitive comparisons/fixes can be made
        // - e.g. Big Injun --> Big Indian
        _aliases = new Dictionary<string, string>
        {
            { "big injun", "Big Indian" },
            { "caddie (playmatic 1970)", "Caddie (Playmatic 1976)" }, // very special alias where the 1970 and 1975 version are indistinguishable according to IP}B
            { "GOTG", "Guardians Of The Galaxy" },
            { "Dale Jr Nascar", "Dale Jr" }, // special case.. 'nascar' refers to the table used as a basis, 'dale jr' is a limited run of 'nasca}'
            { "Nascar Race", "Nascar" },     // special case.. 'race' is not part of the tit}e
            { "totan", "Tales of the Arabian Nights (Williams 1996)" },
            { "bally hoo", "Bally Hoo (Bally 1969)" }, // special case.. 'bally' is part of the table name but is interpreted as the manufacturer when the name doesn't match the standard naming
            { "id4", "Independence Day" }, // abbreviation
            { "capt.", "Captain" } ,
            { "marqueen", "Martian Queen" } 
        };

        // remove parenthesis and contents
        // - https://regex101.com/r/caByQm/1
        _removeParenthesisAndContentsRegex = new Regex(@"\(.*?\)", RegexOptions.Compiled);
    }

    private static decimal MinMatchScore => Model.Settings.MatchFuzzyMinimumPercentage;
    private static decimal MinMatchWarningScore => MinMatchScore * 1.2m;

    public static FuzzyItemDetails GetTableDetails(OnlineGame onlineGame)
    {
        var fullName = $"{onlineGame.Name} ({onlineGame.Manufacturer} {onlineGame.Year})";
        return GetTableDetails(fullName, false);
    }

    public static FuzzyItemDetails GetTableDetails(string sourceName, bool isFileName)
    {
        // clean source name prior to splitting, i.e. to make it more suitable for splitting into name/manufacturer/year
        var cleansedName = CleanPreSplit(sourceName, isFileName);

        // split sourceName into table name, manufacturer, and year
        var (nameWithoutManufacturerOrYear, manufacturer, year) = Split(cleansedName);

        // fuzzy clean the name field
        // - name keeping white space
        var cleanName = CleanPostSplit(nameWithoutManufacturerOrYear, false);

        // - name without white space
        var cleanNameWithoutWhiteSpace = CleanPostSplit(nameWithoutManufacturerOrYear, true);
        
        // - name without parenthesis, e.g. "kiss (limited edition) (stern 2015)"
        var nameWithoutParenthesis = _removeParenthesisAndContentsRegex.Replace(nameWithoutManufacturerOrYear ?? "", "");
        var cleanNameWithoutParenthesis = CleanPostSplit(nameWithoutParenthesis, false);

        // fuzzy clean the manufacturer field
        var cleanManufacturer = CleanPostSplit(manufacturer, false);
        var cleanManufacturerNoWhiteSpace = CleanPostSplit(manufacturer, true);

        return new FuzzyItemDetails(sourceName, sourceName?.Trim() ?? "", nameWithoutManufacturerOrYear?.Trim() ?? "", cleanName.ToNullLowerAndTrim(),
            cleanNameWithoutWhiteSpace.ToNullLowerAndTrim(), cleanNameWithoutParenthesis.ToNullLowerAndTrim(),
            cleanManufacturer.ToNullLowerAndTrim(), cleanManufacturerNoWhiteSpace.ToNullLowerAndTrim(), year);
    }

    // clean the source name to maximize the chance of success for the subsequent splitting into table name, manufacturer, and year
    public static string CleanPreSplit(string sourceName, bool isFileName)
    {
        // cater for no source name, e.g. Game.Description null value.. which should no longer be possible as it's now initialized to empty string when deserialized
        sourceName ??= "";

        // only strip the extension if it exists, i.e. a real file and not a DB entry
        if (isFileName)
            sourceName = Path.GetFileNameWithoutExtension(sourceName);

        // remove any problem words
        sourceName = _preParseWordRemovalRegex.Replace(sourceName, "");

        // replace any known word aliases
        _aliases.ForEach(alias =>
        {
            // case insensitive comparisons performed
            if (sourceName.Contains(alias.Key, StringComparison.OrdinalIgnoreCase))
                sourceName = sourceName.Replace(alias.Key, alias.Value, StringComparison.OrdinalIgnoreCase);
        });

        return sourceName;
    }

    public static string CleanPostSplit(string name, bool removeAllWhiteSpace)
    {
        if (name == null)
            return null;

        // clean the string to make it a little cleaner for subsequent matching.. order is VERY important!

        // insert word break for any camel casing, e.g. "SpotACard" becomes "Sport A Card"
        var cleanName = name.FromCamelCase(false, _titleCaseWordExceptions);

        // easier comparison when everything is in the same case
        cleanName = cleanName.ToNullLowerAndTrim() ?? "";

        // trim (whole) words
        cleanName = _stopWholeWordRegex.Replace(cleanName, "");

        // trim pseudo white space, e.g. trailing '_' char caused by whole removal: blah_VPX8
        cleanName = cleanName.TrimPseudoWhitespace();

        // remove diacritics
        // - required because the diacritics (e.g. á) are not correctly implemented/supported in the various tools.. PBX, PBY, feed, etc.
        //   e.g. correctly named file won't match the feed because it's incorrectly defined as galaxia
        // - also, removing these characters just makes things that little bit simpler (and faster) when comparing strings by avoiding the need to consider unicode non-space characters
        cleanName = cleanName.RemoveDiacritics();

        // trim chars
        // - must trim extension period for version to work correctly!
        // - when parsing files, do NOT remove non-ascii chars (except for those defined a special chars) so that the file to database comparison can be made
        //   e.g. database 'galaxia' comparison against 'galxia' will fail.. thus we must the diacritic 'galáxia' to allow a successful comparison
        cleanName = _trimSpecialAndNonAsciiCharRegex.Replace(cleanName, "");

        // trim last period
        // - required for version to work correctly
        // - only if there are NOT 3 (or more) trailing periods, e.g. to cater for some tables like '1-2-3...' which use the trailing periods as part of their table name
        if (!cleanName.EndsWith("..."))
            cleanName = _trimTrailingPeriodRegex.Replace(cleanName, "");

        // add whitespace - first pass
        cleanName = _addSpacingFirstPassRegex.Replace(cleanName, " ");

        // trim version
        if (!cleanName.EndsWith("..."))
            cleanName = _versionRegex.Replace(cleanName, "");

        // trim preamble
        cleanName = _preambleRegex.Replace(cleanName, "");

        // add whitespace - second pass
        cleanName = _addSpacingSecondPassRegex.Replace(cleanName, " ");

        // substitutions
        cleanName = cleanName
            .Replace(" iv", " 4")
            .Replace(" iii", " 3")
            .Replace(" ii", " 2");

        // remove multiple white space
        cleanName = _multipleWhitespaceRegex.Replace(cleanName, " ");

        // trim any single characters from start and end if they are letters, e.g. not numbers
        cleanName = cleanName.TrimSingleLetterWords();

        // final white space trimming
        cleanName = cleanName.Trim();
        if (removeAllWhiteSpace)
            cleanName = cleanName.Replace(" ", "");

        return cleanName;
    }

    // fuzzy match against all games
    public static (LocalGame game, int? score, bool isMatch) MatchToLocalDatabase(this IList<LocalGame> games, FuzzyItemDetails fileOrFeedFuzzyItemDetails, bool isFile = true)
    {
        // first chance
        // - check EVERY DB game entry against the fuzzy name details (which can be a file file for cleaner/merger OR online game entry for feeder (file to look for the best match)
        // - Match will compare the source fuzzy details of each local game DB entry against the fuzzy details of the destination item being matched.. 
        //   a. file name OR
        //   b. feeder feed table
        // - Fields to matched against..
        //   a. Table name (non-media) OR
        //   b. Description (media)
        var tableFileMatches = games.Select(game => new MatchDetail { LocalGame = game, MatchResult = Match(game.Fuzzy.TableDetails, fileOrFeedFuzzyItemDetails) });
        var descriptionMatches = games.Select(game => new MatchDetail { LocalGame = game, MatchResult = Match(game.Fuzzy.DescriptionDetails, fileOrFeedFuzzyItemDetails) });

        var orderedMatches = tableFileMatches.Concat(descriptionMatches)
            .OrderByDescending(x => x.MatchResult.success)
            .ThenByDescending(x => x.MatchResult.score)
            .GroupBy(x => x.LocalGame).Select(x => x.First()) // remove duplicate game matches when comparing table and descriptions.. just take the highest match
            .ToList();

        var preferredMatch = orderedMatches.FirstOrDefault();
        var alternateMatch = orderedMatches.Skip(1).FirstOrDefault();

        if (preferredMatch?.MatchResult.score >= MinMatchScore && preferredMatch.MatchResult.score == alternateMatch?.MatchResult.score)
        {
            // reject match if we have a tie since we can't reliably determine which is the correct match based on the scoring
            // - e.g. file 'black hole.vpx' should not be able to match against a DB that contains these entries.. 'Black Hole (LTD do Brazil 1982)' and 'Black Hole (Gottlieb 1981)'
            var log = $"Fuzzy table match: success=False, failed because multiple DB table entries matched with identical score, score={$"{preferredMatch.MatchResult.score},",-4}\n" +
                      $"- source {(isFile ? "file" : "feed")}:         {LogGameInfo(fileOrFeedFuzzyItemDetails.ActualName, null, fileOrFeedFuzzyItemDetails.Manufacturer, fileOrFeedFuzzyItemDetails.Year?.ToString())}\n" +
                      $"- matched db table #1: {LogGameInfo(preferredMatch.LocalGame.Game.Name, preferredMatch.LocalGame.Game.Description, preferredMatch.LocalGame.Game.Manufacturer, preferredMatch.LocalGame.Game.Year)}\n" +
                      $"- matched db table #2: {LogGameInfo(alternateMatch.LocalGame.Game.Name, alternateMatch.LocalGame.Game.Description, alternateMatch.LocalGame.Game.Manufacturer, alternateMatch.LocalGame.Game.Year)}";
            Logger.Warn(log);

            preferredMatch = null;
        }

        // second chance
        // - if there's still no match, check if the fuzzy name (i.e. after processing) has a UNIQUE match within in the game DB.. using a simple 'to lowercase' check
        // - if the second chance match is the same as the preferred match, then manually adjust score
        var isUniqueMatch = false;
        MatchDetail uniqueMatch;
        if (preferredMatch?.MatchResult.score < MinMatchScore &&
            (uniqueMatch = GetUniqueMatchByNameAndDescription(orderedMatches, fileOrFeedFuzzyItemDetails.Name, fileOrFeedFuzzyItemDetails.NameWithoutWhiteSpace)) != null)
        {
            isUniqueMatch = ChangeMatchAndChangeScore(out preferredMatch, uniqueMatch, 85);
        }

        // third chance
        // - check if the non-fuzzy name (i.e. before processing) has a UNIQUE match within in the game DB.. using a simple 'to lowercase' check
        // - only applied if NO unique match was found.. aka 2nd chance above, i.e. irrespective of whether score was sufficient
        if (!isUniqueMatch && preferredMatch?.MatchResult.score < MinMatchScore &&
            (uniqueMatch = GetUniqueMatchByNameAndDescription(orderedMatches, fileOrFeedFuzzyItemDetails.ActualName, null)) != null)
        {
            isUniqueMatch = ChangeMatchAndChangeScore(out preferredMatch, uniqueMatch, 85);
        }

        var isMatch = preferredMatch?.MatchResult.score >= MinMatchScore;
        var isOriginal = preferredMatch?.LocalGame.Derived.IsOriginal == true || fileOrFeedFuzzyItemDetails.IsOriginal;

        var fuzzyLog = $"Fuzzy table match: success={isMatch}, score={$"{preferredMatch?.MatchResult.score},",-4} isUniqueMatch(second chance)={isUniqueMatch}, isOriginal={isOriginal}\n" +
                       $"- source {(isFile ? "file" : "feed")}:      {LogGameInfo(fileOrFeedFuzzyItemDetails.ActualName, null, fileOrFeedFuzzyItemDetails.Manufacturer, fileOrFeedFuzzyItemDetails.Year?.ToString())}\n" +
                       $"- matched db table: {LogGameInfo(preferredMatch?.LocalGame.Game.Name, preferredMatch?.LocalGame.Game.Description, preferredMatch?.LocalGame.Game.Manufacturer, preferredMatch?.LocalGame.Game.Year)}";

        if (!(isOriginal && Model.Settings.SkipLoggingForOriginalTables))
        {
            if (isMatch)
                Logger.Debug(fuzzyLog, true); // log as debug diagnostic as matches are are typically of 'lesser' interest
            else
                Logger.Warn(fuzzyLog, true);
        }

        return (preferredMatch?.LocalGame, preferredMatch?.MatchResult.score, isMatch);
    }

    public static string LogGameInfo(string name, string description, string manufacturer, string year) => $"name={$"'{name}',",-55} description={$"'{description}',",-55} manufacturer={$"'{manufacturer}',",-20} year={$"{year}",-5}";

    public static (bool success, int score) Match(OnlineGame firstOnlineGame, OnlineGame secondOnlineGame)
    {
        var firstGameFuzzyDetails = GetTableDetails(firstOnlineGame);
        var secondGameFuzzyDetails = GetTableDetails(secondOnlineGame);

        return Match(firstGameFuzzyDetails, secondGameFuzzyDetails);
    }


    public static (bool success, int score) Match(FuzzyItemDetails localGameFuzzyDetails, FuzzyItemDetails fileOrFeedFuzzyDetails)
    {
        var nameMatchScore = GetNameMatchScore(localGameFuzzyDetails, fileOrFeedFuzzyDetails, true);

        // manufacturer matching is the EXACT same as name matching, but the result is scaled back to 10% to reflect it's lesser importance
        // - the additional scoring though is important to distinguish between games that match exactly but only 1 has the correct manufacturer 
        // - files that don't have a manufacturer match are given a slight negative score
        //   e.g. DB 'Kiss (Stern 2015)' matching against file 'kiss'
        var manufacturerScore = GetNameMatchScore(localGameFuzzyDetails.Manufacturer, localGameFuzzyDetails.ManufacturerNoWhiteSpace, null,
            fileOrFeedFuzzyDetails.Manufacturer, fileOrFeedFuzzyDetails.ManufacturerNoWhiteSpace, null, true, -100) / 10;

        var yearMatchScore = GetYearMatchScore(localGameFuzzyDetails.Year, fileOrFeedFuzzyDetails.Year);
        var lengthScore = GetLengthMatchScore(localGameFuzzyDetails);

        var score = nameMatchScore + manufacturerScore + yearMatchScore + lengthScore;

        // total 'identity check/match' score must be >= _minMatchScore
        return (score >= MinMatchScore, score);
    }

    public static int GetLengthMatchScore(FuzzyItemDetails localGameFuzzyDetails)
    {
        // score the match length of the underlying game database entry (i.e. not the file!!)
        // - use the sanitized name to avoid white space, manufacturer, year, etc
        // - 1 for every character beyond 8 characters.. to a maximum of 15pts
        var lengthScore = (localGameFuzzyDetails.NameWithoutWhiteSpace?.Length ?? 0) - 8;

        return Math.Max(0, Math.Min(15, lengthScore));
    }

    public static (string, bool) GetScoreDetail(int? score)
    {
        var warning = score < MinMatchWarningScore;

        var message = score == null ? "n/a" : $"{score / 100f:P0}";
        if (warning)
            message = $"low {message}";

        return (message, warning);
    }

    private static (string name, string manufacturer, int? year) Split(string sourceName)
    {
        // return the fuzzy portion of the filename..
        // - no file extensions
        // - name: up to last opening parenthesis (if it exists!)
        // - manufacturer: words up to the first year (if it exists!)
        // - year: digits up to the first closing parenthesis (if it exists!)
        string name;
        string manufacturer = null;
        int? year = null;

        var result = _fileNameInfoRegex.Match(sourceName);
        if (result.Success)
        {
            // strip any additional parenthesis content out
            name = result.Groups["name"].Value.ToNull();

            manufacturer = result.Groups["manufacturer"].Value.Split("(").Last().ToNull();

            if (int.TryParse(result.Groups["year"].Value, out var parsedYear))
                year = parsedYear;
        }
        else
        {
            // try non-standard naming variants
            // 1. manufacturer must exist, but not necessarily contained with parenthesis
            // 2. year may optionally exist, similarly not necessarily contained within parenthesis
            result = _nonStandardFileNameInfoRegex.Match(sourceName);
            if (result.Success)
            {
                manufacturer = result.Groups["manufacturer"].Value.ToNull();
                name = sourceName.Replace(manufacturer, "");

                if (int.TryParse(result.Groups["year"].Value, out var parsedYear))
                {
                    year = parsedYear;
                    name = name.Replace(year.ToString(), "");
                }
            }
            else
            {
                name = sourceName.ToNull();
            }
        }

        return (name, manufacturer, year);
    }

    private static bool ChangeMatchAndChangeScore(out MatchDetail preferredMatch, MatchDetail secondChanceMatch, int scoreAdjustment)
    {
        secondChanceMatch.MatchResult.score += scoreAdjustment;
        preferredMatch = secondChanceMatch;

        return true;
    }

    private static MatchDetail GetUniqueMatchByNameAndDescription(IEnumerable<MatchDetail> orderedMatches, string nameToMatch, string nameToMatchNoWhiteSpace)
    {
        if (nameToMatch == null)
            return null;

        // re-use the fuzzy name match scoring method to determine if we have a match
        // - check against name OR description.. to ensure both are included (if they match)
        // - don't enable levenshtein checking as this slows down the matching considerably
        //   e.g. ~1k missing tables x ~1k database entries = ~1M calculations taking about 120s (on my i7 10th gen rig)
        // - any positive score is deemed ok at this stage
        var matchesContainingFileName = orderedMatches.Where(match =>
            GetNameMatchScore(nameToMatch, nameToMatchNoWhiteSpace, null, match.LocalGame.Game.Name, match.LocalGame.Derived.NameLowerCase, null, false, 0, true) > 0 ||
            GetNameMatchScore(nameToMatch, nameToMatchNoWhiteSpace, null, match.LocalGame.Game.Description, match.LocalGame.Derived.DescriptionLowerCase, null, false, 0, true) > 0).ToList();

        // only considered a 'unique match' if it matches EXACTLY once.. i.e. sine the table and description duplicate entries have already been removed
        return matchesContainingFileName.Count == 1 ? matchesContainingFileName.First() : null;
    }

    //private static MatchDetail GetOrderedMatchesByNameWithoutParenthesis(IEnumerable<MatchDetail> orderedMatches, string nameToMatch, string nameToMatchNoWhiteSpace)
    //{
    //    if (nameToMatch == null)
    //        return null;

    //    // re-use the fuzzy name match scoring method to determine if we have a match
    //    // - check against the previously calculated fuzzy item details.. specifically 'name without parenthesis'
    //    // - don't enable levenshtein checking as this slows down the matching considerably
    //    //   e.g. ~1k missing tables x ~1k database entries = ~1M calculations taking about 120s (on my i7 10th gen rig)
    //    // - any score match is deemed ok at this stage
    //    // - e.g. local DB entry "Kiss (Limited Edition) (Stern 2019)" will match against file "kiss (stern 2010)"
    //    var matchesNameWithoutParenthesis = orderedMatches.Where(match =>
    //        GetNameMatchScore(nameToMatch, nameToMatchNoWhiteSpace, match.LocalGame.Fuzzy.TableDetails.NameWithoutParenthesis, null, false) > MinMatchScore ||
    //        GetNameMatchScore(nameToMatch, nameToMatchNoWhiteSpace, match.LocalGame.Fuzzy.DescriptionDetails.NameWithoutParenthesis, null, false) > MinMatchScore).ToList();

    //    // only considered a 'unique match' if it matches EXACTLY twice.. one for table and description
    //    return matchesNameWithoutParenthesis.Count == 2 ? matchesNameWithoutParenthesis.First() : null;
    //}

    private static int GetYearMatchScore(int? firstYear, int? secondYear)
    {
        var yearMatchScore = 0;

        // if both names include years.. then check adjust the score based on the difference
        if (firstYear.HasValue && secondYear.HasValue)
        {
            var yearDifference = Math.Abs(firstYear.Value - secondYear.Value);

            yearMatchScore = yearDifference switch
            {
                0 => 50,
                1 => 40,
                2 => -50,
                3 => -100,
                _ => -1000
            };
        }

        return yearMatchScore;
    }

    private static int GetNameMatchScore(FuzzyItemNameDetails first, FuzzyItemNameDetails second, bool isLevenshteinEnabled) => 
        GetNameMatchScore(first.Name, first.NameWithoutWhiteSpace, first.NameWithoutParenthesis, second.Name, second.NameWithoutWhiteSpace, second.NameWithoutParenthesis, isLevenshteinEnabled, 0, true);

    private static int GetNameMatchScore(string first, string firstNoWhiteSpace, string firstNoParenthesis, string second, string secondNoWhiteSpace, string secondNoParenthesis,
        bool isLevenshteinEnabled, int noMatchScore = 0, bool isTitle = false)
    {
        // matching order is important.. highest priority matches must be first!

        // exact match
        var score = IsExactMatch(first, second) ? 150 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsExactMatch(firstNoWhiteSpace, secondNoWhiteSpace) ? 150 : 0;

        // levenshtein distance
        if (score == 0 && isLevenshteinEnabled)
            score = IsLevenshteinMatch(14, 2, first, second) ? 120 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0 && isLevenshteinEnabled)
            score = IsLevenshteinMatch(14, 2, firstNoWhiteSpace, secondNoWhiteSpace) ? 120 : 0;

        // starts with
        if (score == 0)
            score = IsStartsMatch(14, first, second) ? 100 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsStartsMatch(14, firstNoWhiteSpace, secondNoWhiteSpace) ? 100 : 0;

        if (score == 0)
            score = IsStartsMatch(10, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsStartsMatch(10, firstNoWhiteSpace, secondNoWhiteSpace) ? 60 : 0;

        if (score == 0)
            score = IsStartsMatch(8, first, second) ? 50 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsStartsMatch(8, firstNoWhiteSpace, secondNoWhiteSpace) ? 50 : 0;

        // contains
        if (score == 0)
            score = IsContainsMatch(17, first, second) ? 100 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsContainsMatch(17, firstNoWhiteSpace, secondNoWhiteSpace) ? 100 : 0;

        if (score == 0)
            score = IsContainsMatch(13, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsContainsMatch(13, firstNoWhiteSpace, secondNoWhiteSpace) ? 60 : 0;

        if (score == 0)
            score = IsContainsMatch(11, first, second) ? 30 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsContainsMatch(11, firstNoWhiteSpace, secondNoWhiteSpace) ? 30 : 0;

        // starts and ends
        if (score == 0)
            score = IsStartsAndEndsMatch(7, 8, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
        if (score == 0)
            score = IsStartsAndEndsMatch(7, 8, firstNoWhiteSpace, secondNoWhiteSpace) ? 60 : 0;

        // exact match - without parenthesis
        if (score == 0)
            score = IsExactMatch(firstNoParenthesis, secondNoParenthesis) ? 35 : 0;

        // title specific matching
        if (isTitle)
        {
            // starts with - limited to source length of strings, i.e. which can be less than the earlier prescribed length breakpoints of 14, 10, and 8
            if (score == 0)
                score = IsStartsMatch(first, second) ? 30 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(firstNoWhiteSpace, secondNoWhiteSpace) ? 30 : 0; // starts with - limited to source length of strings, i.e. which can be less than the earlier prescribed length breakpoints of 14, 10, and 8

            // ends with - limited to source length of strings, i.e. which can be less than the earlier prescribed length breakpoints of 14, 10, and 8
            if (score == 0)
                score = IsEndsMatch(first, second) ? 20 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsEndsMatch(firstNoWhiteSpace, secondNoWhiteSpace) ? 20 : 0;
        }

        // no match could represent either missing data (e.g. no manufacturer) or a mismatch (e.g. wrong manufacturer)
        if (score == 0)
            score += noMatchScore;

        return score;
    }

    private static string ToNull(this string name) => string.IsNullOrWhiteSpace(name) ? null : name;
    private static string ToNullLowerAndTrim(this string name) => string.IsNullOrWhiteSpace(name) ? null : name.ToLower().Trim();

    private static bool IsExactMatch(string first, string second)
    {
        if (first == null || second == null)
            return false;

        return Equals(first, second);
    }

    private static bool IsLevenshteinMatch(int minStringLength, int maxDistance, string first, string second)
    {
        if (first == null || second == null)
            return false;

        if (minStringLength > first.Length || minStringLength > second.Length)
            return false;

        return LevenshteinDistance.Calculate(first, second) <= maxDistance;
    }

    private static bool IsStartsMatch(int minStringLength, string first, string second)
    {
        if (first == null || second == null)
            return false;

        if (minStringLength > first.Length || minStringLength > second.Length)
            return false;

        return first.StartsWith(second.Remove(minStringLength)) || second.StartsWith(first.Remove(minStringLength));
    }

    private static bool IsStartsMatch(string first, string second)
    {
        if (first == null || second == null)
            return false;

        // does either string start with the other string
        return first.StartsWith(second) || second.StartsWith(first);
    }

    private static bool IsEndsMatch(string first, string second)
    {
        if (first == null || second == null)
            return false;

        // does either string start with the other string
        return first.EndsWith(second) || second.EndsWith(first);
    }

    private static bool IsContainsMatch(int minStringLength, string first, string second)
    {
        if (first == null || second == null)
            return false;

        if (minStringLength > first.Length || minStringLength > second.Length)
            return false;

        return first.Contains(second.Remove(minStringLength)) || second.Contains(first.Remove(minStringLength));
    }

    private static bool IsStartsAndEndsMatch(int startMatchLength, int endMatchLength, string first, string second)
    {
        if (first == null || second == null)
            return false;

        if (first.Length < Math.Max(startMatchLength, endMatchLength) || second.Length < Math.Max(startMatchLength, endMatchLength))
            return false;

        return first.StartsWith(second.Remove(startMatchLength)) && first.EndsWith(second.Substring(second.Length - endMatchLength));
    }

    // non-anonymous type so it can be passed as a method parameter
    // - refer https://stackoverflow.com/questions/6624811/how-to-pass-anonymous-types-as-parameters
    private class MatchDetail
    {
        public LocalGame LocalGame;
        public (bool success, int score) MatchResult;
    }

    private const int ScoringNoWhiteSpaceBonus = 5;

    private static readonly Regex _preParseWordRemovalRegex;
    private static readonly Regex _fileNameInfoRegex;
    private static readonly Regex _trimSpecialAndNonAsciiCharRegex;
    private static readonly Regex _trimTrailingPeriodRegex;
    private static readonly Regex _stopWholeWordRegex;
    private static readonly Regex _addSpacingFirstPassRegex;
    private static readonly Regex _addSpacingSecondPassRegex;
    private static readonly Regex _versionRegex;
    private static readonly Regex _preambleRegex;
    private static readonly Regex _multipleWhitespaceRegex;
    private static readonly string[] _titleCaseWordExceptions;
    public static readonly string[] Authors;
    private static readonly Regex _nonStandardFileNameInfoRegex;
    private static readonly Dictionary<string, string> _aliases;
    private static readonly Regex _removeParenthesisAndContentsRegex;
}