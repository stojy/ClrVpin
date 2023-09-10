using System.Collections.Generic;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using NUnit.Framework;
using Utils.Extensions;

// ReSharper disable StringLiteralTypo
namespace ClrVpin.Tests.Shared;

public class FuzzyTests
{
    [SetUp]
    public void Setup()
    {
        Model.Settings = new Models.Settings.Settings();
    }

    [Test]
    [TestCase("Indiana Jones (Williams 1993) blah.directb2s", true, "indiana jones", "indianajones", "indiana jones", "williams", "williams", 1993)]
    [TestCase("Indiana Jones (Williams 1993 blah) blah.directb2s", true, "indiana jones", "indianajones", "indiana jones", "williams", "williams", 1993, TestName = "year doesn't need to be at the end of the parenthesis")]
    [TestCase("Indiana Jones (Williams) blah.directb2s", true, "indiana jones", "indianajones", "indiana jones", "williams", "williams", null)]
    [TestCase("Indiana Jones (1993) blah.directb2s", true, "indiana jones", "indianajones", "indiana jones", null, null, 1993)]
    [TestCase("Indiana Jones.directb2s", true, "indiana jones", "indianajones", "indiana jones", null, null, null)]
    [TestCase("Indiana Jones (blah) (Williams 1993).directb2s", true, "indiana jones blah", "indianajonesblah", "indiana jones", "williams", "williams", 1993,
        TestName = "double parenthesis - first parenthesis is used #1 - and parenthesis stripped")]
    [TestCase("AC-DC (Let There Be Rock Limited Edition) (Stern 2012).directb2s", true, "c dc let there be rock limited edition", "cdcletthereberocklimitededition", "c dc", "stern", "stern", 2012,
        TestName = "double parenthesis - first parenthesis is used #1 - and parenthesis stripped, also - becomes space")]
    [TestCase("Batman (66 Limited Edition) (Stern 2016)", true, "batman 66 limited edition", "batman66limitededition", "batman", "stern", "stern", 2016,
        TestName = "double parenthesis - first parenthesis is used #3 - and parenthesis stripped")]
    [TestCase("", true, null, null, null, null, null, null, TestName = "empty string")]
    [TestCase(null, true, null, null, null, null, null, null, TestName = "null string")]
    [TestCase("123", true, "123", "123", "123", null, null, null, TestName = "number title")]
    [TestCase("123 (Williams 1993)", true, "123", "123", "123", "williams", "williams", 1993, TestName = "number title with manufacturer and year")]
    [TestCase("123 (Williams)", true, "123", "123", "123", "williams", "williams", null, TestName = "number title with manufacturer only")]
    [TestCase("123 (1993)", true, "123", "123", "123", null, null, 1993, TestName = "number titleand with year only")]
    [TestCase("123 blah (Williams 1993)", true, "123 blah", "123blah", "123 blah", "williams", "williams", 1993, TestName = "number and word title with manufacturer and year")]
    [TestCase("123 blah (1993)", true, "123 blah", "123blah", "123 blah", null, null, 1993, TestName = "number title with word and year only")]
    [TestCase("1-2-3 (1971)", true, "1 2 3", "123", "1 2 3", null, null, 1971, TestName = "dashes removed.. white space and no white space")]
    [TestCase("Mr. and Mrs. Pac-Man (Bally 1982) 1.0.vpx", true, "mr mrs pac man", "mrmrspacman", "mr mrs pac man", "bally", "bally", 1982, TestName = "file name with internal periods")]
    [TestCase("Mr. and Mrs. Pac-Man (Bally 1982) 1.0", false, "mr mrs pac man", "mrmrspacman", "mr mrs pac man", "bally", "bally", 1982, TestName = "game name with internal periods")]
    [TestCase("1462262523_TheFlintstones(Williams1994)v1.26.vpx", true, "flintstones", "flintstones", "flintstones", "williams", "williams", 1994, TestName = "file name with camelcase instead of whitespace")]
    [TestCase("1462262523_The Flintstones(Williams1994)v1.26.vpx", true, "flintstones", "flintstones", "flintstones", "williams", "williams", 1994, TestName = "file name starts with 'the' keyword")]
    [TestCase("Twilight Zone SG1bsoN Mod V3.vpx", true, "twilight zone", "twilightzone", "twilight zone", null, null, null, TestName = "file name with special author camelcase SG1bsoN")]
    [TestCase("Whirlwind 4K 1.1.vpx", true, "whirlwind", "whirlwind", "whirlwind", null, null, null, TestName = "ignore word: 4k #1 - no manufacturer/year")]
    [TestCase(@"C:\vp\_downloaded\wheel images\V1 (IDSA 1986) Logo.png", true, "v1", "v1", "v1", "idsa", "idsa", 1986, TestName = "name stripped completely: empty string converted to null")]
    [TestCase("1-2-3... (Automaticos 1973)", false, "1 2 3", "123", "1 2 3", "automaticos", "automaticos", 1973, TestName = "name has trailing periods")]
    [TestCase("1-2-3... (Automaticos 1973).vpx", true, "1 2 3", "123", "1 2 3", "automaticos", "automaticos", 1973, TestName = "name has trailing periods")]
    [TestCase("1-2-3... (My MANufacturer   is me 1973).vpx", true, "1 2 3", "123", "1 2 3", "my manufacturer is me", "mymanufacturerisme", 1973, TestName = "manufacturer variant - multiple spaces and capitilisation")]
    [TestCase("1-2-3... (My.Manufacturer Is.&Me 1973).vpx", true, "1 2 3", "123", "1 2 3", "my manufacturer is me", "mymanufacturerisme", 1973, TestName = "manufacturer variant - period and &.. both chars stripped")]
    [TestCase("Kiss (Limited Edition) (Stern 2015)", false, "kiss limited edition", "kisslimitededition", "kiss", "stern", "stern", 2015, TestName = "double parenthesis - table variant and manufacturer/year")]
    [TestCase("Kiss (Limited Edition) (gold) (Stern 2015)", false, "kiss limited edition gold", "kisslimitededitiongold", "kiss", "stern", "stern", 2015, TestName = "tripple parenthesis - table variant and manufacturer/year")]
    public void GetTableDetailsTest(string sourceName, bool isFileName, string expectedName, string expectedNameWithoutWhiteSpace, string expectedNameWithoutParenthesis, string expectedManufacturer, string expectedManufacturerNoWhiteSpace,
        int? expectedYear)
    {
        var fuzzyDetails = Fuzzy.GetTableDetails(sourceName, isFileName);

        Assert.That(fuzzyDetails.Name, Is.EqualTo(expectedName));
        Assert.That(fuzzyDetails.NameWithoutWhiteSpace, Is.EqualTo(expectedNameWithoutWhiteSpace));
        Assert.That(fuzzyDetails.NameWithoutParenthesis, Is.EqualTo(expectedNameWithoutParenthesis));
        Assert.That(fuzzyDetails.Manufacturer, Is.EqualTo(expectedManufacturer));
        Assert.That(fuzzyDetails.ManufacturerNoWhiteSpace, Is.EqualTo(expectedManufacturerNoWhiteSpace));
        Assert.That(fuzzyDetails.Year, Is.EqualTo(expectedYear));
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
    [TestCase("2in1_", true, "2in1", TestName = "1_ is not confused with a parital version number")]
    [TestCase("the black knight", false, "black knight", TestName = "remove 'the'")]
    [TestCase("black&apos; knight", false, "black knight", TestName = "remove '&apos;'")]
    [TestCase("black' knight", false, "black knight", TestName = "remove '''")]
    [TestCase("black¿½ knight", false, "black knight", TestName = "remove '¿½'")]
    [TestCase("blackï¿½ knight", false, "blacki knight", TestName = "replace diacriticï")]
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
    [TestCase("black&knight", false, "blackknight", TestName = "strip '&'")]
    [TestCase("black & knight", false, "black knight", TestName = "strip ' & '")]
    [TestCase("1 2 3 (Premier 1989)", true, "123premier1989", TestName = "#1 white space & parenthesis - removed")]
    [TestCase("1-2-3-(Premier 1989)", false, "1 2 3 premier 1989", TestName = "#2 white space  & parenthesis- removed")]
    [TestCase("1 2   3 (Premier 1989)", false, "1 2 3 premier 1989", TestName = "#3 white space  & parenthesis- removed")]
    [TestCase("1 2 3 (Premier 1989)", false, "1 2 3 premier 1989", TestName = "#4 white space - kept")]
    [TestCase("1234 blah", false, "1234 blah", TestName = "trim number preamble")]
    [TestCase("12345 blah", false, "blah", TestName = "trim number preamble")]
    [TestCase("Vp10-The Walking Dead 1.1.vpx", false, "walking dead", TestName = "remove vp10")]
    [TestCase("24`™ Pin·bot Motörhead galáxia (Electromecánica)", false, "24 pinbot motorhead galaxia electromecanica", TestName = "remove special and non-ascii characters - file.. ` is removed, but non-ascii remain")]
    public void CleanPostSplitTest(string fileName, bool removeAllWhiteSpace, string expectedName)
    {
        // confirm CleanPostSplit provides exact results - i.e. ignore scoring
        var cleanName = Fuzzy.CleanPostSplit(fileName, removeAllWhiteSpace);

        Assert.That(cleanName, Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(null, false, "", TestName = "handle null")]
    [TestCase("1826898402_Shrek1.0", false, "1826898402_Shrek1.0", TestName = "no change #1 - file extensions n/a")]
    [TestCase("1826898402_Shrek1.0.vpx", false, "1826898402_Shrek1.0.vpx", TestName = "no change #2 - keep file extension")]
    [TestCase("1826898402_Shrek1.0.vpx", true, "1826898402_Shrek1.0", TestName = "drop file extension")]
    [TestCase("1826898402_Shrek(Stern2008)(MOD)1.0.vpx", true, "1826898402_Shrek(Stern2008)1.0", TestName = "remove '(MOD)'")]
    [TestCase("13Big INjun123.vpx", true, "13Big Indian123", TestName = "substitute alias #1 - big injun")]
    [TestCase("Bally Hoo.vpx", true, "Bally Hoo (Bally 1969)", TestName = "substitute alias #2 - bally hoo")]
    public void CleanPreSplitTest(string fileName, bool isFileName, string expectedName)
    {
        // confirm CleanPreSplit provides exact results - i.e. ignore scoring
        var cleanName = Fuzzy.CleanPreSplit(fileName, isFileName);

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
    [TestCase("Whirlwind (Williams 1990)", "Whirlwind 4K 1.1.vpx", true, TestName = "ignore word: 4k #2 - no manufacturer/year")]
    [TestCase("Wizard (Bally 1975)", "Wizard! VPX v1.03 - pinball58.vpx", true, TestName = "nasty: no manufacturer, no year, file has exclaimation, 2 digit version, hyphyen and author AFTER version")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americs Most Haunted (spooky 2014) b2s v3.directb2s", true, TestName = "single character wrong: levenshtein distance 1")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americ Most Haunted (spooky 2014) b2s v3.directb2s", true, TestName = "single character wrong: levenshtein distance 2")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Ameri Most Haunted (spooky 2014) b2s v3.directb2s", false, TestName = "single character wrong: levenshtein distance 3")]
    [TestCase("Mum (Spooky Pinball LLC 2014)", "Mom (spooky 2014) b2s v3.directb2s", false, TestName = "single character wrong: levenshtein distance 1, but length too short")]
    [TestCase("Big Brave (Gottlieb 1974)", "Big_Brave_VP99_EN_4player_b2s.directb2s", true, TestName = "various exception words: author, 4player, b2s, etc")]
    [TestCase("Lord Of The Rings (Stern 2003)", "Lord_of_the_Rings_VPW_2022.directb2s", true, TestName = "created year in title")]
    [TestCase("V1 (IDSA 1986)", "V1 (IDSA 1986) Logo.png", true, TestName = "correct name #1: name cleansing does  not removes all contents because name is not mistaken for a version number")]
    [TestCase("V1.1 (IDSA 1986)", "V1.1 (IDSA 1986) Logo.png", true, TestName = "correct name #2: name cleansing does  not removes all contents because name is not mistaken for a version number")]
    [TestCase("Sir Lancelot (Peyper 1994)", "Sir Lancelot (Peyper 1984) Logo.png", false, TestName = "perfect name match: but wrong decade")]
    public void MatchTest(string gameName, string fileName, bool expectedSuccess)
    {
        // confirm match is successful, i.e. does NOT require an exact clean match
        var isMatch = Fuzzy.Match(Fuzzy.GetTableDetails(gameName, false), Fuzzy.GetTableDetails(fileName, true)).success;

        Assert.That(isMatch, Is.EqualTo(expectedSuccess));
    }

    [Test]
    // exact match
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern)", 179, TestName = "exact name and missing year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1993)", 229, TestName = "exact name and exact year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1994)", 219, TestName = "exact name and +/-1 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1995)", 129, TestName = "exact name and +/-2 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1996)", 79, TestName = "exact name and +/-3 year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks (Stern 1997)", -821, TestName = "exact name and +/-3 year")]
    // starts with
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern)", 129, TestName = "starts name 15char and missing year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern 1993)", 179, TestName = "starts name 15char and exact year")]
    [TestCase("Indiana Jones Rocks (Stern 1993)", "Indiana Jones Rocks Baby (Stern 1995)", 79, TestName = "starts name 15char and +/-2 year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern)", 84, TestName = "starts name 10char and missing year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1993)", 134, TestName = "starts name 10char and exact year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1992)", 124, TestName = "starts name 10char and +/-1 year")]
    [TestCase("Indiana Jones (Stern 1993)", "Indiana Jones Rocks (Stern 1991)", 34, TestName = "starts name 10char and +/-1 year")]
    [TestCase("CARtoon baby (Stern 1993)", "CARtoons (Stern 1993)", 68, TestName = "starts name 7 char and exact year - no match")]
    [TestCase("CARtoons baby (Stern 1993)", "CARtoons (Stern 1993)", 124, TestName = "starts name 8 char and exact year")]
    [TestCase("CARty baby (Stern 1993)", "CARty (Stern 1993)", 101, TestName = "starts name match #1 - correct manufacturer/year")]
    [TestCase("The Getaway High Speed II (Williams 1992)", "Getaway_(Williams 1992)_mod_1.0.vpx", 109, TestName = "starts name match #1 - clean name matches and correct manufacturer/year")]
    [TestCase("The Getaway High Speed II (Williams 1992)", "Getawayii_(Williams 1992)_mod_1.0.vpx", 74, TestName = "starts name match #2 - no name name match and correct manufacturer/year")]
    [TestCase("The awesome Getaway High Speed II (Williams 1992)", "Getaway_(Williams 1992)_mod_1.0.vpx", 80, TestName = "starts name match #3 - no name name match and correct manufacturer/year")]
    [TestCase("Getaway (Williams 1992)", "the getaway high speed ii (Williams 1992)_mod_1.0.vpx", 100, TestName = "starts name match #4 - clean file name matches and correct manufacturer/year with lesser length")]
    [TestCase("Avatar (Stern 2010)", "Avatar, James Cameron's (Stern 2010)", 100, TestName = "starts name match #5 - name has starts with match, manufacturer is exact match")]
    [TestCase("Avatar (Stern 2010)", "Avatar, James Cameron's (Sterny 2010)", 75, TestName = "starts name match #6 - name has starts with match, but manufacturer does not because manufacturer is deliberatley not supported")]
    // contains
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern)", 133, TestName = "contains name 20char and missing year")]
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern 1993)", 183, TestName = "contains name 20char and exact year")]
    [TestCase("Indiana Jones Rocks Baby (Stern 1993)", "OMG Indiana Jones Rocks Baby (Stern 1994)", 173, TestName = "contains name 20char and +/-1 year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern)", 85, TestName = "contains name 13char and missing year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1993)", 135, TestName = "contains name 13char and exact year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1994)", 125, TestName = "contains name 13char and +/-1 year")]
    [TestCase("Indiana Jones R (Stern 1993)", "OMG Indiana Jones Rocks (Stern 1995)", 35, TestName = "contains name 13char and +/-2 year")]
    [TestCase("Back To The Future (Data East 1990)", "Back To The Future Starlion MoD 1.0.directb2s", 149, TestName = "manufacturer and year missing")]
    [TestCase("Cowboy Eight Ball (LTD 1981)", "Cowboy Eight Ball (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", 202, TestName = "after chars removed - perfect match, no manufacturer/year")]
    [TestCase("Cowboy Eight Ball (LTD 1981)", "Cowboy Eight Ball 213 (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", 152, TestName = "after chars removed - partial match, no manufacturer/year")]
    [TestCase("Junkyard Cats (Bailey 2012)", "Junkyard Cats_1.07 (3 Screen).directB2S", 149, TestName = "single digit in parethensis - don't mistake for manufacturer/year")]
    [TestCase("Junkya blah blah Cats Dogs (Bailey 2012)", "Junkya whatever whatever Cats Dogs.vpx", 69, TestName = "match start and end - start: 7chars, end: 8chars, no manufacturer/year")]
    [TestCase("Dirty Harry (Williams 1995)", "Dirty Harry 2.0 shiny mod.vpx", 57, TestName = "10 char name match, no manufacturer/year")]
    [TestCase("Blahblah (Williams 1990)", "Blahblah 4K 1.1.vpx", 145, TestName = "ignore word: 4k - no manufacturer/year")]
    [TestCase("Whirlwind (Williams 1990)", "Whirlwind 4K 1.1.vpx", 146, TestName = "match without white space (no hyphen): should score higher, no manufacturer/year")]
    [TestCase("Whirl-Wind (Gottlieb 1958)", "Whirlwind 4K 1.1.vpx", 141, TestName = "match with whitespace (hyphen converts to whitespace): should match lower, no manufacturer/year")]
    [TestCase("Americas Most Haunted (Spooky Pinball LLC 2014)", "Americs Most Haunted (spooky 2014) b2s v3.directb2s", 176, TestName = "match with Levenshtein distance, unmatched manufacturer")]
    [TestCase("V1 (IDSA 1986) Logo", "V1 (IDSA 1986) Logo.png", 220, TestName = "perfect match #1: match score pass because the cleansed name is not confused with a partial version.. since 'v1' prefix has non-whiteppace)")]
    [TestCase(" V1 (IDSA 1986) Logo", "V1 (IDSA 1986) Logo.png", 220, TestName = "perfect match #2: match score pass because cleansed name is not confused with a partial version.. since 'v1' prefix has non-whiteppace AFTER the string is trimmed)")]
    [TestCase("aV1 (IDSA 1986) Logo", "V1 (IDSA 1986) Logo.png", 65, TestName = "perfect match #3: match score failed because the cleaned name does not have the version number parsed due to prefix non-whitespace")]
    [TestCase(@"123 (Talleres de Llobregat 1973)", @"1-2-3... (Automaticos 1973).vpx", 190, TestName = "trailing periods, manufacturer mismatch")]
    [TestCase(@"The Walking Dead (Stern 2014)", @"'The Walking Dead (Stern 2014)", 223, TestName = "manufacturer - exact match ")]
    [TestCase(@"The Walking Dead (Stern 2014)", @"'The Walking Dead (Zen Studios 2014)", 198, TestName = "manufacturer - wrong")]
    [TestCase(@"The Walking Dead (Stern is a big company 2014)", @"'The Walking Dead (Stirn is a big company 2014)", 220, TestName = "manufacturer - misspelt 1 char but manufacturer too small")]
    [TestCase(@"The Walking Dead (Stern 2014)", @"'The Walking Dead (Stirn 2014)", 198, TestName = "manufacturer - misspelt name 1 char but manufacturer too small, manufacturer name wrong by 1 char")]
    [TestCase(@"Whoa Nellie Big Juicy Melons (Stern 2015)", @"Whoa Nellie! Big Juicy Melons (WhizBang Pinball 2011)", -840, TestName = "manufacturer - incorrect, also year wrong")]
    [TestCase(@"Whoa Nellie Big Juicy Melons (Stern 2015)", @"Whoa Nellie! Big Juicy Melons (Stern 2015)", 235, TestName = "manufacturer - correct")]
    [TestCase(@"X-Men LE (Stern 2012)", "X-Men (Stern 2012)", 100, TestName = "name - too short to get decent match, but matches completely with a 'starts with'")]
    [TestCase(@"X-Men (Stern 2012)", "X-Men (Stern 2012)", 220, TestName = "name - short name exact match")]
    [TestCase("Batman 66 (Stern 2016)", "The Batman (Original 2022)", -975, TestName = "low match - database to feed #2, manufacturer mismatch")]
    [TestCase("Batman 66 (Stern 2016)", "Batman 66 (Original 2018)", 95, TestName = "low match - database to feed #1, manufacturer mismatch")]
    [TestCase("Batman 66 (Original 2018)", "Batman 66 (Stern 2016).vpx", 95, TestName = "low match - database (after feed update) to file, no manufacturer/year")]
    [TestCase("Aces & Kings (Williams 1970)", "Aces and Kings (Williams 1970).vpx", 221, TestName = "And vs &.. both should be stripped to ensure a strong match")]
    [TestCase("Guns N' Roses (Data East 1994)", "Guns and Roses (Data East 1994).vpx", 221, TestName = "N' abbreviation for 'and'.. should be stripped")]
    [TestCase("Surf 'n Safari (Gottlieb 1991)", "Surf and Safari (Gottlieb 1992).vpx", 212, TestName = "'n abbreviation for 'and'.. should be stripped")]
    [TestCase("Batman (66 Limited Edition) (Stern 2016)", "The Batman (Original 2022)", -961, TestName = "low match - double parenthesis.. the first being part of the title, manufacturer mismatch")]
    [TestCase("AC-DC (Let There Be Rock Limited Edition) (Stern 2012)", "AC-DC Let There Be Rock (Stern 2013).vpx", 175, TestName = "double parethensis - name contents exist in the first set")]
    [TestCase("Pinball (Stern 1977)", "Pinball EM (Stern 1977).vpx", 220, TestName = "strip pinball type from name - EM")]
    [TestCase("Pinball (Stern 1977)", "Pinball SS (Stern 1977).vpx", 220, TestName = "strip pinball type from name - SS")]
    [TestCase("Pinball (Stern 1977)", "Pinball PM (Stern 1977).vpx", 220, TestName = "strip pinball type from name - PM")]
    [TestCase("Roller Derby (Bally 1960)", "Bally Roller Derby 2.0.vpx", 173, TestName = "non-standard naming.. extract manufacturer from name")]
    [TestCase("Wolverine (Zen Studios 2013)", "X-Men Wolverine LE (Stern 2012).vpx", 31, TestName = "partial name match and no manufacturer/year.. expected to fail")]
    [TestCase("Mac's Galaxy (MAC S.A. 1986)", "Mac Galaxy (MAC 1986).vpx", 42, TestName = "similar match, but 's' preventing a better score")]
    [TestCase("James Camerons Avatar (Limited Edition) (Stern 2010)", "Avatar, James Cameron's (Stern 2010)", 145, TestName = "online matching scenario - database desc to online (forward).. expected hit against 'james cameron'")]
    [TestCase("Avatar, James Cameron's (Stern 2010)", "James Camerons Avatar (Limited Edition) (Stern 2010)", 141,
        TestName = "online matching scenario - online to database desc (reverse.. does not happen).. unexpected(?) lower score")]
    [TestCase("Avatar, James Cameron's (Stern 2010)", "Avatar (Stern 2010).vpx", 111, TestName = "online matching scenario - has 'starts with' match, after DB name & desc sync'd.. unable to match existing file (same as name)")]
    [TestCase("Batman (Data East 1991)", "Batman Balutito MOD.directb2s", 145, TestName = "author + mod file description, no manufacturer/year")]
    [TestCase("Big Indian (Gottlieb 1974)", "Big injun.directb2s", 146, TestName = "alias #1 - table with dual names, no manufacturer/year")]
    [TestCase("Caddie (Playmatic 1975)", "Caddie (Playmatic 1970).directb2s", 210, TestName = "alias #2 - very special case where 1970 and 1975 tables are indistinguishable as per IPDB")]
    [TestCase("Heavy Metal (Rowamet 1981)", "Heavy_Metal_No LEDs.directb2s", 147, TestName = "description - no LEDs, no manufacturer/year")]
    [TestCase("Kiss (Stern 2015)", "KISS Stern 2015.directb2s", 220, TestName = "non-standard file format - manufacturer and year not in parenthesis")]
    [TestCase("Kiss (Limited Edition) (Stern 2015)", "KISS Stern 2015.directb2s", 110, TestName = "double parenthesis.. the first is the table variant and considered part of name & hence lower match score despite manufacturer/year match")]
    [TestCase("Kiss (Limited Edition)", "KISS Stern 2015.directb2s", 145, TestName = "single parenthesis #1.. 'limited edition' is interpreted as the manufacturer, hence an exact match on title")]
    [TestCase("Kiss (Stern 2015)", "KISS (Limited Edition) Stern 2015.directb2s", 145, TestName = "single parenthesis #2.. 'limited edition' is interpreted as the manufacturer, hence an exact match on title")]
    [TestCase("Kiss (Limited Edition) (Stern 2015)", "KISS (Limited Edition) Stern 2015.directb2s", 35,
        TestName = "double parenthesis.. db first parenthesis part of table name, file parenthesis considered part of manufacturer/year (which is incorrect).. hence very low match")]
    [TestCase("Kiss (Limited Edition) (Stern 2015)", "KISS (Limited Edition) (Stern 2015).directb2s", 230,
        TestName = "double parenthesis.. db and file both match, hence very high score")]
    [TestCase("NFL (Stern 2001)", "NFL (Vikings) (Stern 2001).directb2s", 100, TestName = "double parenthesis - match without parenthesis, without any bonus score for the tilte length")]
    [TestCase("Shrek (Stern 2008)", "1826898402_Shrek(Stern2008)(MOD)1.0.vpx", 220, TestName = "pre-parsing should remove '(MOD)' so that the file name can be properly split")]
    [TestCase("Fire! (Williams 1987)", "Fire!_Upgrade.vpx", 145, TestName = "keyword: upgrade")]
    [TestCase("Guardians Of The Galaxy (Stern 2017)", "GOTG_2.0.0.vpx", 154, TestName = "alias #3: regular substitution")]
    [TestCase("Guns N' Roses (Data East 1994)", "Guns N Roses (Data East 1994)_Bigus(MOD)3.0.vpx", 221, TestName = "language word: treat N same as 'N and N'")]
    [TestCase("Dale Jr. (Stern 2007)", "JP's Dale Jr Nascar v4.vpx", 145, TestName = "alias #4: special case for 'dale jr nascar'")]
    [TestCase("Nascar (Stern 2005)", "JP's Nascar Race v4.vpx", 145, TestName = "alias #5: special case for 'nascar race'")]
    [TestCase("Frank Thomas' Big Hurt (Gottlieb 1995)", "Big Hurt (Gottlieb 1995)_Bigus(MOD)1.1.vpx", 100, TestName = "ending match #1")]
    [TestCase("Lord Of The Rings (Stern 2003)", "Lord of the Rings Siggis Mod 2.0.vpx", 148, TestName = "author check: siggis")]
    [TestCase("Black Hole (LTD do Brazil 1982)", "LTD Black Hole.vpx", 16, TestName = "failed match - 'ends with' match (because of LTD prefix) and no manufacturer/year match")]
    [TestCase("Black Hole (Gottlieb 1981)", "Black Hole.vpx", 146, TestName = "exact name match - without manufacturer/year")]
    [TestCase("Tales of the Arabian Nights (Williams 1996)", "TOTAN4k 1.5.vpx", 232, TestName = "alias: totan")]
    [TestCase("2 in 1 (Bally 1964)", "2in1_(Bally 1964)_rev_1.00.vpx", 215, TestName = "trailing number and underscore (1_) should not be confused with an incomplete version number")]
    [TestCase("Bally Hoo (Bally 1969)", "Bally Hoo.vpx", 129, TestName = "alias substitution to cater for 'bally' matching the name instead of the manufacturer")]
    [TestCase("Independence Day (Sega 1996)", "ID4 (Sega 1996)_Bigus(MOD)2.0.vpx", 227, TestName = "alias substitution.. ID4 abbreviation to Independence Day")]
    [TestCase("Galaxia (LTD do Brasil 1975)", "Galáxia (LTD do Brasil 1975).vpx", 220, TestName = "ignore diacritics for string comparison - a == á")]
    [TestCase("Capt. Fantastic and The Brown Dirt Cowboy (Bally 1976)", "JP's Captain Fantastic (Bally 1975) v3.1 - Pup.vpx", 175, TestName = "abbreviation alias - capt. to captain")]
    public void MatchScoreTest(string databaseName, string fileOrFeedName, int expectedScore)
    {
        // exactly same as MatchTest.. with a score validation
        var (success, score) = Fuzzy.Match(Fuzzy.GetTableDetails(databaseName, false), Fuzzy.GetTableDetails(fileOrFeedName, true));

        Assert.That(score, Is.EqualTo(expectedScore));
        Assert.That(success, Is.EqualTo(expectedScore >= 100));
    }

    [Test]
    [TestCase("too small", 0)]
    [TestCase("a little bigger", 4)]
    [TestCase("a lot lot lot bigger", 7)]
    [TestCase("a lot lot lot lot lot bigger", 13)]
    [TestCase("this one maxes out the upper size limit", 15)]
    public void MatchLengthTest(string name, int expectedScore)
    {
        var fuzzyNameFileDetails = Fuzzy.GetTableDetails(name, true);
        var score = Fuzzy.GetLengthMatchScore(fuzzyNameFileDetails);

        Assert.That(score, Is.EqualTo(expectedScore));
    }

    [Test]
    public void DatabaseMultipleGamesMatchTest()
    {
        var localGames = new List<LocalGame>
        {
            new() { Game = new Game { IpdbId = "1", Name = "Cowboy Eight Ball (LTD 1981)", Description = "Cowboy Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" } },
            new() { Game = new Game { IpdbId = "2", Name = "Cowboy Eight Ball 2 (LTD 1981)", Description = "Cowboy Eight Ball 2 (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" } },
            new() { Game = new Game { IpdbId = "3", Name = "Eight Ball (LTD 1981)", Description = "Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" } },
            new() { Game = new Game { IpdbId = "4", Name = "Eight Ball 2 (LTD 1981)", Description = "Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)" } },
            new() { Game = new Game { IpdbId = "5", Name = "Mary Shelley's Frankenstein (Sega 1995)", Description = "Mary Shelley's Frankenstein (Sega 1995)" } },
            new() { Game = new Game { IpdbId = "6", Name = "Transformers (Stern 2011)", Description = "Transformers (Pro) (Stern 2011)" } },
            new() { Game = new Game { IpdbId = "7", Name = "V1 (IDSA 1986)", Description = "V1 (IDSA 1986) Logo" } },
            new() { Game = new Game { IpdbId = "8", Name = "X-Men LE (Stern 2012)", Description = "X-Men Wolverine LE (Stern 2012)" } },
            new() { Game = new Game { IpdbId = "9", Name = "Kiss (Limited Edition) (Stern 2015)", Description = "Kiss (Limited Edition) (Stern 2015)" } },
            new() { Game = new Game { IpdbId = "10", Name = "Kiss (Bally 1979)", Description = "Kiss (Bally 1979)" } }
        };

        localGames.ForEach((localGame, index) =>
        {
            GameDerived.Init(localGame, index);
            localGame.Fuzzy.TableDetails = Fuzzy.GetTableDetails(localGame.Game.Name, false);
            localGame.Fuzzy.DescriptionDetails = Fuzzy.GetTableDetails(localGame.Game.Description, false);
        });

        // exact match #1
        var fileDetails = Fuzzy.GetTableDetails("Cowboy Eight Ball (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        var (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo("1"));
        Assert.That(isMatch, Is.True);

        // exact match #2 - i.e. not the first match
        fileDetails = Fuzzy.GetTableDetails("Cowboy Eight Ball 2 (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo("2"));
        Assert.That(isMatch, Is.True);

        // no match because the we have a tie for highest scoring - i.e. unable to determine the winner
        fileDetails = Fuzzy.GetTableDetails("Eight Ball 2 blah (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo(null));
        Assert.That(isMatch, Is.False);

        // partial match
        fileDetails = Fuzzy.GetTableDetails("Blah Cowboy Eight Ball blah (LTD do Brasil Diversï¿½es Eletrï¿½nicas Ltda 1981).f4v", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo("2"));
        Assert.That(isMatch, Is.True);

        // no match chosen - i.e. not the first match
        fileDetails = Fuzzy.GetTableDetails("what the heck is this file.f4v", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game, Is.Not.Null);
        Assert.That(isMatch, Is.False);

        // partial match, but not long enough to score - 'wolverine' is 9 long, but 11 is required
        fileDetails = Fuzzy.GetTableDetails("Wolverine (Zen Studios 2013).vpx", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game, Is.Not.Null);
        Assert.That(isMatch, Is.False);

        // partial match - extra score because file only has 1 match in the games DB
        fileDetails = Fuzzy.GetTableDetails("Frankenstein.vpx", true);
        (game, _, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo("5"));
        Assert.That(isMatch, Is.True);

        // partial match - NO extra score because file has multiple matches in the games DB
        fileDetails = Fuzzy.GetTableDetails("Ball.vpx", true);
        (game, var score, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.Not.Null);
        Assert.That(score, Is.EqualTo(22));
        Assert.That(isMatch, Is.False);

        // ??
        fileDetails = Fuzzy.GetTableDetails("Transformers Marcade Mod v1.2.vpx", true);
        (game, score, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        Assert.That(game?.Derived.Ipdb, Is.EqualTo("6"));
        Assert.That(score, Is.EqualTo(147));
        Assert.That(isMatch, Is.True);

        // third chance - no name score match, no unique fuzzy file name match.. but a unique hit on the raw (non-cleaned) table name
        // - NO LONGER APPLICABLE NOW THAT VERSION NUMBERING HAS BEEN IMPROVED TO NO LONGER INCORRECTLY CONSIDER THIS A NON-FIRST CHANCE MATCH
        //fileDetails = Fuzzy.GetTableDetails("V1.2 (IDSA 1986) Logo.png", true);
        //(game, score, isMatch) = localGames.MatchToLocalDatabase(fileDetails);
        //Assert.That(game?.Derived.Ipdb, Is.EqualTo("7"));
        //Assert.That(score, Is.EqualTo(150));
        //Assert.That(isMatch, Is.True);
    }
}