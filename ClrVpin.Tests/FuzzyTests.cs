using System.Collections.Generic;
using ClrVpin.Models;
using ClrVpin.Shared;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo
namespace ClrVpin.Tests;

public class FuzzyTests
{
    [Test]
    [TestCase("Indiana Jones (Williams 1993) blah.directb2s", true, "indiana jones", "indianajones", "williams", 1993)]
    [TestCase("Indiana Jones (Williams 1993 blah) blah.directb2s", true, "indiana jones", "indianajones", "williams", 1993, TestName = "year doesn't need to be at the end of the parenthesis")]
    [TestCase("Indiana Jones (Williams) blah.directb2s", true, "indiana jones", "indianajones", "williams", null)]
    [TestCase("Indiana Jones (1993) blah.directb2s", true, "indiana jones", "indianajones", null, 1993)]
    [TestCase("Indiana Jones.directb2s", true, "indiana jones", "indianajones", null, null)]
    [TestCase("Indiana Jones (blah) (Williams 1993).directb2s", true, "indiana jones", "indianajones", "williams", 1993, TestName = "only last most parenthesis is used")]
    [TestCase("", true, null, null, null, null, TestName = "empty string")]
    [TestCase(null, true, null, null, null, null, TestName = "null string")]
    [TestCase("123", true, "123", "123", null, null, TestName = "number title")]
    [TestCase("123 (Williams 1993)", true, "123", "123", "williams", 1993, TestName = "number title with manufacturer and year")]
    [TestCase("123 (Williams)", true, "123", "123", "williams", null, TestName = "number title with manufacturer only")]
    [TestCase("123 (1993)", true, "123", "123", null, 1993, TestName = "number titleand with year only")]
    [TestCase("123 blah (Williams 1993)", true, "123 blah", "123blah", "williams", 1993, TestName = "number and word title with manufacturer and year")]
    [TestCase("123 blah (1993)", true, "123 blah", "123blah", null, 1993, TestName = "number title with word and year only")]
    [TestCase("1-2-3 (1971)", true, "1 2 3", "123", null, 1971, TestName = "dashes removed.. white space and no white space")]
    [TestCase("Mr. and Mrs. Pac-Man (Bally 1982) 1.0.vpx", true, "mr mrs pac man", "mrmrspacman", "bally", 1982, TestName = "file name with internal periods")]
    [TestCase("Mr. and Mrs. Pac-Man (Bally 1982) 1.0", false, "mr mrs pac man", "mrmrspacman", "bally", 1982, TestName = "game name with internal periods")]
    [TestCase("1462262523_TheFlintstones(Williams1994)v1.26.vpx", true, "flintstones", "flintstones", "williams", 1994, TestName = "file name with camelcase instead of whitespace")]
    [TestCase("1462262523_The Flintstones(Williams1994)v1.26.vpx", true, "flintstones", "flintstones", "williams", 1994, TestName = "file name starts with 'the' keyword")]
    [TestCase("Twilight Zone SG1bsoN Mod V3.vpx", true, "twilight zone", "twilightzone", null, null, TestName = "file name with special author camelcase SG1bsoN")]
    [TestCase("Whirlwind 4K 1.1.vpx", true, "whirlwind", "whirlwind", null, null, TestName = "ignore word: 4k")]
    public void GetNameDetailsTest(string sourceName, bool isFileName, string expectedName, string expectedNameNoWhiteSpace, string expectedManufacturer, int? expectedYear)
    {
        var (name, nameNoWhiteSpace, manufacturer, year) = Fuzzy.GetNameDetails(sourceName, isFileName);

        Assert.That(name, Is.EqualTo(expectedName));
        Assert.That(nameNoWhiteSpace, Is.EqualTo(expectedNameNoWhiteSpace));
        Assert.That(manufacturer, Is.EqualTo(expectedManufacturer));
        Assert.That(year, Is.EqualTo(expectedYear));
    }

    [Test]
    [TestCase("medieval madness", false, "medieval madness", TestName = "nothing to do")]
    [TestCase("medieval madness.vpx", false, "medieval madness", TestName = "remove extension")]
    [TestCase("Twilight Zone Mod .vpx", true, "twilightzone", TestName = "remove mod")]
    [TestCase("Twilight Zone Mod.vpx", true, "twilightzone", TestName = "remove mod without trailing space")]
    [TestCase("Twilight Zone SG1bsoN.vpx", true, "twilightzone", TestName = "remove author 1")]
    [TestCase("Baywatch Starlion MoD", true, "baywatch", TestName = "remove author 1")]
    [TestCase("Twilight            Zone  baby.vpx", false, "twilight zone baby", TestName = "remove multiple spaces")]
    [TestCase("Twilight Zone V3.vpx", true, "twilightzone", TestName = "remove version - single digit")]
    [TestCase("Twilight Zone 3.vpx", true, "twilightzone3", TestName = "remove version - single digit without prefix prefix - number to remain")]
    [TestCase("Twilight Zone 3.0.vpx", true, "twilightzone", TestName = "remove version - double digit")]
    [TestCase("Twilight Zone 3_0.vpx", true, "twilightzone", TestName = "remove version - double digit with underscore")]
    [TestCase("Twilight Zone 30.00.10.vpx", true, "twilightzone", TestName = "remove version - triple digit")]
    [TestCase("Twilight Zone v1.2.vpx", true, "twilightzone", TestName = "remove version - double digit with prefix")]
    [TestCase("Twilight Zone v1.2 blah.vpx", true, "twilightzonev12blah", TestName = "remove version - version not at the end - number to remain")]
    [TestCase("Twilight Zone v1.2 - blah.vpx", true, "twilightzonev12blah", TestName = "remove version - version before hyphen and not at the end - number to remain")]
    [TestCase("Twilight Zone v1.2 - VP10.vpx", true, "twilightzone", TestName = "remove version with decimal - version before hyphen and IS at the end (VP10 keyword removed) - number to be stripped")]
    [TestCase("Twilight Zone v1_2 - VP10.vpx", true, "twilightzone", TestName = "remove version with underscore - version before hyphen and IS at the end (VP10 keyword removed) - number to be stripped")]
    [TestCase("._Twilight Zone SG1bsoN Mod V3.vpx", true, "twilightzone", TestName = "remove multple.. author, version, mod, etc")]
    [TestCase("the black knight", false, "black knight", TestName = "remove 'the'")]
    [TestCase("black&apos; knight", false, "black knight", TestName = "remove '&apos;'")]
    [TestCase("black' knight", false, "black knight", TestName = "remove '''")]
    [TestCase("blackï¿½ knight", false, "black knight", TestName = "remove 'ï¿½'")]
    [TestCase("black` knight", false, "black knight", TestName = "remove '`'")]
    [TestCase("black’ knight", false, "black knight", TestName = "remove '’'")]
    [TestCase("black, knight", false, "black knight", TestName = "remove ','")]
    [TestCase("black; knight", false, "black knight", TestName = "remove ';'")]
    [TestCase("black knight!", false, "black knight", TestName = "remove '!'")]
    [TestCase("black? knight", false, "black knight", TestName = "remove '?'")]
    [TestCase("JP's black knight", false, "black knight", TestName = "remove 'JP's")]
    [TestCase("JPs black knight", false, "black knight", TestName = "remove 'JPs")]
    [TestCase("black and knight", false, "black knight", TestName = "remove 'and'")]
    [TestCase("black a knight", false, "black knight", TestName = "remove ' a '")]
    [TestCase("a black knight", false, "black knight", TestName = "remove starting 'a '")]
    [TestCase("black.knight.blah", false, "black knight blah", TestName = "replace '.'")]
    [TestCase("black-knight", false, "black knight", TestName = "remove '-'")]
    [TestCase("black - knight", false, "black knight", TestName = "remove ' - '")]
    [TestCase("black_knight", false, "black knight", TestName = "remove '_'")]
    [TestCase("black&knight", false, "black and knight", TestName = "replace '&'")]
    [TestCase("black & knight", false, "black and knight", TestName = "replace ' & '")]
    [TestCase("1 2 3 (Premier 1989)", true, "123(premier1989)", TestName = "#1 white space - removed")]
    [TestCase("1-2-3-(Premier 1989)", false, "1 2 3 (premier 1989)", TestName = "#2 white space - removed")]
    [TestCase("1 2   3 (Premier 1989)", false, "1 2 3 (premier 1989)", TestName = "#3 white space - removed")]
    [TestCase("1 2 3 (Premier 1989)", false, "1 2 3 (premier 1989)", TestName = "#4 white space - kept")]
    [TestCase("1234 blah", false, "1234 blah", TestName = "trim number preamble")]
    [TestCase("12345 blah", false, "blah", TestName = "trim number preamble")]
    [TestCase("Vp10-The Walking Dead 1.1.vpx", false, "walking dead", TestName = "remove vp10")]
    [TestCase("24™ Pin·bot Motörhead (Electromecánica)", false, "24 pinbot motrhead (electromecnica)", TestName = "remove non-ascii characters")]
    public void CleanTest(string fileName, bool removeAllWhiteSpace, string expectedName)
    {
        // confirm Clean provides exact results - i.e. ignore scoring
        var cleanName = Fuzzy.Clean(fileName, removeAllWhiteSpace);

        Assert.That(cleanName, Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase("medieval madness", "medieval madness (Williams 2006)", true, TestName = "#1 match without manufacturer and year")]
    [TestCase("medieval madness (Williams 2006)", "medieval madness", true, TestName = "#2 match without manufacturer and year")]
    [TestCase("medieval madnes (Williams 2006)", "medieval madness", true, TestName = "15 char minimum")]
    [TestCase("medieval madness (Williams 2006)", "medieval madness (blah 2006)", true)]
    [TestCase("medieval madness (  Williams 2006)", "medieval madness (blah 2006)", true)]
    [TestCase("medieval midness (Williams 2006)", "medieval madness", true, TestName = "typo: Levenshtein distance match")]
    [TestCase("ali (Stern 1980)", "ali", true, TestName = "short name exact match")]
    [TestCase("ali (Williams 2006)", "alien (blah)", false, TestName = "#1 - minimum 15 characters required for partial match")]
    [TestCase("black knight 2000", "black knight", false, TestName = "#2 - minimum 15 characters required for partial match")]
    [TestCase("black knight returns 2000", "black knight", false, TestName = "#3 - minimum 15 characters required for partial match")]
    [TestCase("black knight returns 2000", "black knight retur", true, TestName = "#4 - minimum 15 characters required for partial match")]
    [TestCase("Rocky and Bullwinkle And Friends (Data East 1993)", "Adventures of Rocky and Bullwinkle and Friends (1993).directb2s", true, TestName = "#1 contains - 20 characters satisified")]
    [TestCase("Rocky and Bull", "Adventures of Rocky and Bullwinkle and Friends (1993).directb2s", false, TestName = "#1 contains - characters not satisified")]
    [TestCase("Indiana Jones (Stern 2008)", @"C:\temp\_download\vp\Backglasses\Indiana Jones (Stern 2008) by Starlion.directb2s", true, TestName = "full path")]
    [TestCase("Indiana Jones The Pinball Adventure (1993).directb2s", @"Indiana Jones The Pinball Adventure (Williams 1993).directb2s", true, TestName = "misc")]
    [TestCase("The Getaway High Speed II (Williams 1992)", @"C:\temp\_MegaSync\b2s\Getaway, The - High Speed II v1.04.directb2s", true, TestName = "full path 2")]
    [TestCase("The Getaway High Speed 2 (Williams 1992)", @"C:\temp\_MegaSync\b2s\Getaway, The - High Speed II v1.04.directb2s", true, TestName = "roman numeral conversion - II")]
    [TestCase("The Getaway High Speed 3 (Williams 1992)", @"C:\temp\_MegaSync\b2s\Getaway, The - High Speed III v1.04.directb2s", true, TestName = "roman numeral conversion - III")]
    [TestCase("The Getaway High Speed 4 (Williams 1992)", @"C:\temp\_MegaSync\b2s\Getaway, The - High Speed IV v1.04.directb2s", true, TestName = "roman numeral conversion - IV")]
    [TestCase("Lights...Camera...Action! (Premier 1989).blah", @"Lights Camera Action (1989).directb2s", true, TestName = "ellipsis")]
    [TestCase("Lights...Camera...Action! (Premier 1989)", @"Lights Camera Action (1989).directb2s", true, TestName = "ellipsis - without file extension is supported")]
    [TestCase("AC-DC LUCI Premium (Stern 2013).directb2s", "AC-DC LUCI (Stern 2013).directb2s", true, TestName = "remove 'premium'")]
    [TestCase("Amazon Hunt baby baby VPX 1.6.directb2s", "Amazon Hunt baby baby (1983).directb2s", true, TestName = "remove 'vpx'")]
    [TestCase("Twilight Zone (Bally 1993)", "Twilight Zone SG1bsoN Mod V3.vpx", true, TestName = "13 character starts with match")]
    [TestCase("Mr. and Mrs. Pac-Man (Bally 1982)", "Mr. and Mrs. Pac-Man (Bally 1982) 1.0.vpx", true, TestName = "game name with period - confirm not mistaken as a file extension")]
    [TestCase("Lortium (Juegos Populares 1987)", "Lortium_VPX_1_1.vpx", true, TestName = "game name with period - confirm not mistaken as a file extension")]
    [TestCase("Cactus Canyon (Bally 1998)", "659655879_CactusCanyon(Bally1998)VPW1.0.2.vpx", true, TestName = "number preamble")]
    [TestCase("The Flintstones (Williams 1994)", "1462262523_TheFlintstones(Williams1994)v1.26.vpx", true, TestName = "file starts with 'the' without any word breaks")]
    [TestCase("Spot A Card (Gottlieb 1960)", "197295192_SpotACard(Gottlieb1960).vpx", true, TestName = "file contains 'a' without any word breaks")]
    [TestCase("Pirates of the Caribbean (Stern 2006)", "912446039_PiratesoftheCaribbean(Stern2006)-EBv1.vpx", true,
        TestName = "file contains 'of' and 'the' which don't align to word boundarys and can't be removed - matching start and end instead")]
    [TestCase("Spider-Man Classic Edition (Stern 2007)", "Spider-Man Classic_VPWmod_V1.0.1.vpx", true, TestName = "file and game have same start string, but different trailing string")]
    [TestCase("Transformers (Stern 2011)", "Transformers Marcade Mod v1.2.vpx", false, TestName = "partial name match is insufficient.. only 12 chars")]
    [TestCase("Whirlwind (Williams 1990)", "Whirlwind 4K 1.1.vpx", true, TestName = "ignore word: 4k")]
    [TestCase("Wizard (Bally 1975)", "Wizard! VPX v1.03 - pinball58.vpx", true, TestName = "nasty: no manufacturer, no year, file has exclaimation, 2 digit version, hyphyen and author AFTER version")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americs Most Haunted (spooky 2014) b2s v3.directb2s", true, TestName = "single character wrong: levenshtein distance 1")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americ Most Haunted (spooky 2014) b2s v3.directb2s", true, TestName = "single character wrong: levenshtein distance 2")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Ameri Most Haunted (spooky 2014) b2s v3.directb2s", false, TestName = "single character wrong: levenshtein distance 3")]
    [TestCase("Mum (Spooky Pinball LLC 2014)", "Mom (spooky 2014) b2s v3.directb2s", false, TestName = "single character wrong: levenshtein distance 1, but length too short")]
    [TestCase("Big Brave (Gottlieb 1974)", "Big_Brave_VP99_EN_4player_b2s.directb2s", true, TestName = "various exception words: author, 4player, b2s, etc")]
    [TestCase("Lord Of The Rings (Stern 2003)", "Lord_of_the_Rings_VPW_2022.directb2s", true, TestName = "created year in title")]
    public void MatchTest(string gameName, string fileName, bool expectedSuccess)
    {
        // confirm match is successful, i.e. does NOT require an exact clean match
        var isMatch = Fuzzy.Match(gameName, Fuzzy.GetNameDetails(fileName, true)).success;

        Assert.That(isMatch, Is.EqualTo(expectedSuccess));
    }

    [Test]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern)", true, 159 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and missing year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1993)", true, 209 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and exact year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1994)", true, 199 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and +/-1 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1995)", true, 109 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and +/-2 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1996)", false, 59 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and +/-3 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1997)", false, -841 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "exact name and +/-3 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern)", true, 109 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 15char and missing year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern 1993)", true, 159 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 15char and exact year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern 1994)", true, 149 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 15char and +/-1 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern 1995)", false, 59 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 15char and +/-2 year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern)", false, 64 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 10char and missing year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1993)", true, 114 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 10char and exact year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1992)", true, 104 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 10char and +/-1 year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1991)", false, 14 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 10char and +/-1 year")]
    [TestCase("CARtoon baby (Stern 1993)", "CARtoon (Stern 1993)", false, 53, TestName = "starts name 7 char and exact year")]
    [TestCase("CARtoons baby (Stern 1993)", "CARtoons (Stern 1993)", true, 104 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "starts name 8 char and exact year")]
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern)", true, 113 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 20char and missing year")]
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern 1993)", true, 163 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 20char and exact year")]
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern 1994)", true, 153 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 20char and +/-1 year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern)", false, 65 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 13char and missing year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1993)", true, 115 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 13char and exact year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1994)", true, 105 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 13char and +/-1 year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1995)", false, 15 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 13char and +/-2 year")]
    [TestCase("Back To The Future Starlion MoD 1.0.directb2s", "Back To The Future (Data East 1990)", true, 115 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "contains name 13char and +/-2 year")]
    [TestCase("Cowboy Eight Ball (LTD 1981)", "Cowboy Eight Ball (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true, 207 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "after chars removed - perfect match")]
    [TestCase("Cowboy Eight Ball (LTD 1981)", "Cowboy Eight Ball 213 (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true, 157 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "after chars removed - partial match")]
    [TestCase("Junkyard Cats (Bailey 2012)", "Junkyard Cats_1.07 (3 Screen).directB2S", true, 154 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "single digit in parethensis - don't mistake for year")]
    [TestCase("Junkya blah blah Cats Dogs (Bailey 2012)", "Junkya whatever whatever Cats Dogs.vpx", false, 74 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "match start and end - start: 7chars, end: 8chars")]
    [TestCase("Dirty Harry (Williams 1995)", "Dirty Harry 2.0 shiny mod.vpx", false, 62 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "10 char name match, but not manufacturer or year match")]
    [TestCase("Blahblah (Williams 1990)", "Blahblah 4K 1.1.vpx", true, 150 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "ignore word: 4k")]
    [TestCase("Whirlwind (Williams 1990)", "Whirlwind 4K 1.1.vpx", true, 151 + Fuzzy.ScoringNoWhiteSpaceBonus, TestName = "match without white space (no hyphen): should score higher")]
    [TestCase("Whirl-Wind (Gottlieb 1958)", "Whirlwind 4K 1.1.vpx", true, 151, TestName = "match with whitespace (hyphen converts to whitespace): should match lower")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americs Most Haunted (spooky 2014) b2s v3.directb2s", true, 186, TestName = "match with Levenshtein distance")]
    public void MatchScoreTest(string gameDetail, string fileDetail, bool expectedSuccess, int expectedScore)
    {
        // exactly same as MatchTest.. with a score validation
        var (success, score) = Fuzzy.Match(gameDetail, Fuzzy.GetNameDetails(fileDetail, true));

        Assert.That(score, Is.EqualTo(expectedScore));
        Assert.That(success, Is.EqualTo(expectedSuccess));
    }

    [Test]
    [TestCase("too small", 0)]
    [TestCase("a little bigger", 4)]
    [TestCase("a lot lot lot bigger", 7)]
    [TestCase("a lot lot lot lot lot bigger", 13)]
    [TestCase("this one maxes out the upper size limit", 15)]
    public void MatchLengthTest(string name, int expectedScore)
    {
        var fuzzyNameFileDetails = Fuzzy.GetNameDetails(name, true);
        var score = Fuzzy.GetLengthMatchScore(fuzzyNameFileDetails);

        Assert.That(score, Is.EqualTo(expectedScore));
    }

    [Test]
    public void DatabaseMultipleGamesMatchTest()
    {
        var games = new List<Game>
        {
            new Game { Ipdb = "1", TableFile = "Cowboy Eight Ball (LTD 1981)", Description = "Cowboy Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" },
            new Game { Ipdb = "2", TableFile = "Cowboy Eight Ball 2 (LTD 1981)", Description = "Cowboy Eight Ball 2 (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" },
            new Game { Ipdb = "3", TableFile = "Eight Ball (LTD 1981)", Description = "Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" },
            new Game { Ipdb = "4", TableFile = "Eight Ball 2 (LTD 1981)", Description = "Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" },
            new Game { Ipdb = "5", TableFile = "Mary Shelley's Frankenstein (Sega 1995)", Description = "Mary Shelley's Frankenstein (Sega 1995)" },
            new Game { Ipdb = "6", TableFile = "Transformers (Stern 2011)", Description = "Transformers (Pro) (Stern 2011)" }
        };

        // exact match #1
        var fileDetails = Fuzzy.GetNameDetails("Cowboy Eight Ball (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        var (game, _) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("1"));

        // exact match #2 - i.,e. not the first match
        fileDetails = Fuzzy.GetNameDetails("Cowboy Eight Ball 2 (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("2"));

        // longest match chosen - i.e. not the first match
        fileDetails = Fuzzy.GetNameDetails("Eight Ball 2 blah (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("4"));

        // partial match
        fileDetails = Fuzzy.GetNameDetails("Blah Cowboy Eight Ball blah (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("2"));

        // no match chosen - i.e. not the first match
        fileDetails = Fuzzy.GetNameDetails("what the heck is this file.f4v", true);
        (game, _) = games.Match(fileDetails);
        Assert.IsNull(game);

        // partial match - extra score because file only has 1 match in the games DB
        fileDetails = Fuzzy.GetNameDetails("Frankenstein.vpx", true);
        (game, _) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("5"));

        // partial match - NO extra score because file has multiple matches in the games DB
        fileDetails = Fuzzy.GetNameDetails("Ball.vpx", true);
        (game, var score) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo(null));
        Assert.That(score, Is.EqualTo(15));

        fileDetails = Fuzzy.GetNameDetails("Transformers Marcade Mod v1.2.vpx", true);
        (game, score) = games.Match(fileDetails);
        Assert.That(game?.Ipdb, Is.EqualTo("6"));
        Assert.That(score, Is.EqualTo(114 + Fuzzy.ScoringNoWhiteSpaceBonus));
    }
}