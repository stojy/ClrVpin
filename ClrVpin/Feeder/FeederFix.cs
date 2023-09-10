using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using ClrVpin.Shared.Utils;
using Utils.Extensions;

namespace ClrVpin.Feeder;

public static class FeederFix
{
    static FeederFix()
    {
        // used with Regex.Replace will capture multiple matches at once.. same word or other other words
        // - refer Fuzzy.cs
        _trimAuthorsRegex = new Regex($@"(?<=^|[^a-z^A-Z])({Fuzzy.Authors.StringJoin("|")})(?=$|[^a-zA-Z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public static Dictionary<string, int> FixOnlineDatabase(List<OnlineGame> onlineGames)
    {
        // create statistics dictionary items upfront to ensure the preferred display ordering (for statistics)
        CreateStatistics();

        // perform pre-merge fixes, i.e. fixes that do NOT require any duplicate game collections to be merged
        // - some of this information mus be done BEFORE the rest of the game fixing because the duplicate entries must be correctly removed BEFORE the various collections are created
        onlineGames.ForEach(PreMerge);

        // merge duplicate entries
        FixDuplicateGames(onlineGames);

        // fix game ordering
        // - alphanumerical
        // - after pre-merge and merged so that the table names are correct and unique
        var orderedDames = onlineGames.OrderBy(game => game.Name).ToArray();
        onlineGames.Clear();
        onlineGames.AddRange(orderedDames);

        // perform post-merge fixes, i.e. fixes that DO require duplicate game collections to be merged
        onlineGames.ForEach(onlineGame =>
        {
            // group files into collections so they can be treated generically
            onlineGame.AllFileCollections = new Dictionary<string, FileCollection>
            {
                { OnlineFileTypeEnum.Tables.GetDescription(), new FileCollection(onlineGame.TableFiles) },
                { OnlineFileTypeEnum.Backglasses.GetDescription(), new FileCollection(onlineGame.B2SFiles) },
                { OnlineFileTypeEnum.DMDs.GetDescription(), new FileCollection(onlineGame.AltColorFiles) },
                { OnlineFileTypeEnum.Wheels.GetDescription(), new FileCollection(onlineGame.WheelArtFiles) },
                { OnlineFileTypeEnum.ROMs.GetDescription(), new FileCollection(onlineGame.RomFiles) },
                { OnlineFileTypeEnum.MediaPacks.GetDescription(), new FileCollection(onlineGame.MediaPackFiles) },
                { OnlineFileTypeEnum.Sounds.GetDescription(), new FileCollection(onlineGame.SoundFiles) },
                { OnlineFileTypeEnum.Toppers.GetDescription(), new FileCollection(onlineGame.TopperFiles) },
                { OnlineFileTypeEnum.PuPPacks.GetDescription(), new FileCollection(onlineGame.PupPackFiles) },
                { OnlineFileTypeEnum.POVs.GetDescription(), new FileCollection(onlineGame.PovFiles) },
                { OnlineFileTypeEnum.AlternateSounds.GetDescription(), new FileCollection(onlineGame.AltSoundFiles) },
                { OnlineFileTypeEnum.Rules.GetDescription(), new FileCollection(onlineGame.RuleFiles) }
            };
            onlineGame.AllFileCollectionsList = onlineGame.AllFileCollections.Select(kv => kv.Value).ToList();
            onlineGame.AllFilesFlattenedList = onlineGame.AllFileCollections.Select(kv => kv.Value).SelectMany(x => x);
            onlineGame.ImageFiles = onlineGame.TableFiles.Concat(onlineGame.B2SFiles).ToList();

            // assign helper properties here to avoid re-calculating them later
            onlineGame.YearString = onlineGame.Year.ToString();
            
            // assign Description in the correct preferred format.. the format is VERY important since..
            // - used to update the local DB entry if an update is requested.. used for both fields; Name (vpx) and Description (media)
            // - used by fix online game which references games by the description in order to include all 3 fields.. this is performed earlier on though BEFORE any online fixes are made
            onlineGame.Description = onlineGame.CreateDescription();

            // perform post-merge fixes, e.g. missing image url
            PostMerge(onlineGame);

            // assign the dictionary files (potentially re-arranged, filtered, etc) back to the lists to ensure they are in sync
            //game.TableFiles = game.AllFileCollections[nameof(game.TableFiles)].Cast<TableFile>().ToList();
            onlineGame.TableFiles = onlineGame.TableFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.B2SFiles = onlineGame.B2SFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.WheelArtFiles = onlineGame.WheelArtFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.RomFiles = onlineGame.RomFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.MediaPackFiles = onlineGame.MediaPackFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.AltColorFiles = onlineGame.AltColorFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.SoundFiles = onlineGame.SoundFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.TopperFiles = onlineGame.TopperFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.PupPackFiles = onlineGame.PupPackFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.PovFiles = onlineGame.PovFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.AltSoundFiles = onlineGame.AltSoundFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            onlineGame.RuleFiles = onlineGame.RuleFiles.OrderByDescending(x => x.UpdatedAt).ToList();

            // table download available is very simplistic..
            // - checks if ANY table file URL is not broken
            // - it does NOT consider whether the file is considered 'new' or not, e.g. the filtering such as date range is ignored
            onlineGame.TableDownload = onlineGame.TableFiles.Any(file => file.Urls.Any(url => !url.Broken)) ? TableDownloadOptionEnum.Available : TableDownloadOptionEnum.Unavailable;
            onlineGame.TableFormats = onlineGame.TableFiles.Where(file => !string.IsNullOrWhiteSpace(file.TableFormat)).Select(x => x.TableFormat).Distinct().ToList();
        });

        Logger.Info($"Online database post-fix: count={onlineGames.Count} (manufactured={onlineGames.Count(onlineGame => !onlineGame.IsOriginal)}, original={onlineGames.Count(onlineGame => onlineGame.IsOriginal)})");

        // keep the enum private and return a dictionary with key as the string representation (e.g. for display purposes)
        return _statistics.ToDictionary(item => item.Key.GetDescription(), item => item.Value);
    }

    private static void CreateStatistics()
    {
        // use enum as the key to avoid unnecessary GetDescription() calls when subsequently updating the dictionary
        _statistics = Enum.GetValues<FixStatisticsEnum>().ToDictionary(enumValue => enumValue, _ => 0);
    }

    // fixes that do NOT require the collections to be initialized (which must occur after de-duplicating, aka merging)
    private static void PreMerge(OnlineGame onlineGame)
    {
        FixNamedGames(onlineGame);

        FixTableInvalidCharacters(onlineGame);
        FixManufacturerInvalidCharacters(onlineGame);

        FixTableWhitespace(onlineGame);
        FixManufacturerWhitespace(onlineGame);

        // assign a flag to indicate the  original game state
        // - this isn't technically a fix, but is done here (i.e. instead of FeederResultsVM) as the information is required for subsequent fixes
        onlineGame.IsOriginal = GameDerived.CheckIsOriginal(onlineGame.Manufacturer, onlineGame.Name);
        onlineGame.TableStyleOption = onlineGame.IsOriginal ? TableStyleOptionEnum.Original : TableStyleOptionEnum.Manufactured;

        FixManufacturedIncludesAuthor(onlineGame);

        FixInvalidUrlIpdb(onlineGame);

        FixUrlProtocol(onlineGame);

        FixOriginalTableIncludesIpdbUrl(onlineGame);
    }

    private static void FixDuplicateGames(ICollection<OnlineGame> onlineGames)
    {
        if (!IsActive(FixFeedOptionEnum.DuplicateTable))
            return;

        // duplicate games are determined by whether entries are have duplicate IPDB url references
        // - only applicable for manufactured tables, i.e. we want to skip original tables that incorrectly supply IPDB references
        // - e.g. Star Trek and JP's Star Trek share the same IPDB url
        var duplicateGamesGrouping = onlineGames
            .Where(game => !game.IsOriginal && !game.IpdbUrl.IsEmpty())
            .GroupBy(game => game.IpdbUrl)
            .Where(grouping => grouping.Count() > 1)
            .ToList();

        duplicateGamesGrouping.ForEach(grouping =>
        {
            // determine the 'correct' unique game and designate the others as possible duplicates
            var allGames = grouping.ToList();
            var uniqueGame = FeederUtils.GetUniqueGame(allGames);
            var duplicateGames = grouping.Except(uniqueGame).ToList();

            // discard any duplicate games that don't don't fuzzy match the unique game
            duplicateGames = duplicateGames.Where(duplicateGame =>
            {
                var (success, score) = Fuzzy.Match(uniqueGame, duplicateGame);
                if (!success)
                    Logger.Warn($"Merging duplicate table ignored because fuzzy match failed, score: {score}, IPDB url: {grouping.Key}\n" +
                                $"- unique:    {uniqueGame}\n" +
                                $"- duplicate: {duplicateGame}", true);
                return success;
            }).ToList();

            LogFixed(uniqueGame, FixStatisticsEnum.DuplicateGame, $"duplicate table(s)={duplicateGames.Select(x => x.Description).StringJoin()}");

            if (duplicateGames.Any())
                Logger.Warn($"Merging duplicate tables detected in the online feed, IPDB url: {grouping.Key}\n" +
                            $"- unique:    {uniqueGame}\n" +
                            $"- duplicate: {duplicateGames.Select(x => x.CreateDescription()).StringJoin()}", true);

            // process the duplicates
            duplicateGames.ForEach(duplicate =>
            {
                // merge games collections
                uniqueGame.TableFiles.AddRange(duplicate.TableFiles);
                uniqueGame.B2SFiles.AddRange(duplicate.B2SFiles);
                uniqueGame.WheelArtFiles.AddRange(duplicate.WheelArtFiles);
                uniqueGame.RomFiles.AddRange(duplicate.RomFiles);
                uniqueGame.MediaPackFiles.AddRange(duplicate.MediaPackFiles);
                uniqueGame.AltColorFiles.AddRange(duplicate.AltColorFiles);
                uniqueGame.SoundFiles.AddRange(duplicate.SoundFiles);
                uniqueGame.TopperFiles.AddRange(duplicate.TopperFiles);
                uniqueGame.PupPackFiles.AddRange(duplicate.PupPackFiles);
                uniqueGame.AltSoundFiles.AddRange(duplicate.AltSoundFiles);
                uniqueGame.RuleFiles.AddRange(duplicate.RuleFiles);

                // remove duplicate
                onlineGames.Remove(duplicate);
            });
        });

        Logger.Debug($"- Merged duplicate tables: count={duplicateGamesGrouping.Sum(grouping => grouping.Count() - 1)} (enable diagnostic logging for the table details)");
    }

    private static void PostMerge(OnlineGame onlineGame)
    {
        FixMissingImage(onlineGame);

        FixFileUpdatedTime(onlineGame);

        FixTableCreatedTime(onlineGame);

        FixTableUpdatedTime(onlineGame);

        FixFileOrdering(onlineGame);

        FixInvalidUrlContent(onlineGame);

        FixWrongUrlContent(onlineGame);
    }

    private static void FixUrlProtocol(OnlineGameBase onlineGame)
    {
        // fix (technically upgrade) url to use https instead of http
        if (!IsActive(FixFeedOptionEnum.UpgradeUrlHttps) || !Uri.TryCreate(onlineGame.IpdbUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp)
            return;

        LogFixed(onlineGame, FixStatisticsEnum.UpgradeUrlHttps, $"protocol={uri.Scheme}");
        var uriBuilder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
        onlineGame.IpdbUrl = uriBuilder.Uri.AbsoluteUri;
    }

    private static void FixInvalidUrlIpdb(OnlineGameBase onlineGame)
    {
        // fix invalid IPDB Url
        // - e.g. "Not Available" frequently used for original tables
        if (!IsActive(FixFeedOptionEnum.InvalidUrlIpdb) || (Uri.TryCreate(onlineGame.IpdbUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.InvalidUrlIpdb, $"url={onlineGame.IpdbUrl}");
        onlineGame.IpdbUrl = null;
    }

    private static void FixManufacturedIncludesAuthor(OnlineGameBase onlineGame)
    {
        // remove author of the game for manufactured tables
        // - e.g. JP's Captain Fantastic (Bally 1976)
        if (!IsActive(FixFeedOptionEnum.ManufacturedIncludesAuthor) || GameDerived.CheckIsOriginal(onlineGame.Manufacturer, onlineGame.Name) || !_trimAuthorsRegex.IsMatch(onlineGame.Name))
            return;

        var cleanName = _trimAuthorsRegex.Replace(onlineGame.Name, "").Trim();
        LogFixed(onlineGame, FixStatisticsEnum.ManufacturedIncludesAuthor, $"correct='{cleanName}, manufacturer='{onlineGame.Manufacturer}'");
        onlineGame.Name = cleanName;
    }

    private static void FixManufacturerWhitespace(OnlineGameBase onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.Whitespace) || onlineGame.Manufacturer == onlineGame.Manufacturer.Trim())
            return;

        LogFixed(onlineGame, FixStatisticsEnum.ManufacturerWhitespace, $"manufacturer='{onlineGame.Manufacturer}'");
        onlineGame.Manufacturer = onlineGame.Manufacturer.Trim();
    }

    private static void FixTableWhitespace(OnlineGameBase onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.Whitespace) || onlineGame.Name == onlineGame.Name.Trim())
            return;

        LogFixed(onlineGame, FixStatisticsEnum.NameWhitespace);
        onlineGame.Name = onlineGame.Name.Trim();
    }

    private static void FixTableInvalidCharacters(OnlineGameBase onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.InvalidCharacters) || !onlineGame.Name.HasInvalidFileNameChars(true))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.NameInvalidCharacters);
        onlineGame.Name = onlineGame.Name.RemoveInvalidFileNameChars(true);
    }

    private static void FixManufacturerInvalidCharacters(OnlineGameBase onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.InvalidCharacters) || !onlineGame.Manufacturer.HasInvalidFileNameChars())
            return;

        LogFixed(onlineGame, FixStatisticsEnum.ManufacturerInvalidCharacters);
        onlineGame.Manufacturer = onlineGame.Manufacturer.RemoveInvalidFileNameChars(true);
    }

    private static void FixNamedGames(OnlineGame onlineGame)
    {
        // non-generic fixes for specifically named games
        // - this is very smelly, but treating these as 'exceptional' (and hopefully few!) scenarios, similar to GameDerived.CheckIsOriginal
        // - no need to strip author from the manufactured table as this is done later
        // - todo; report/fix the underlying VPS feed and then remove this code??
        var description = onlineGame.CreateDescription();
        switch (description)
        {
            case "Austin Powers (Stern 2001)":
                FixWrongUrlIpdb(onlineGame, "https://www.ipdb.org/machine.cgi?id=4504");
                break;
            case "JP's Dale Jr. Nascar (Original 2020)": // Limited Edition run of Stern's 2005 'NASCAR'
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5292", "Stern", 2007, "Dale Jr.");
                break;
            case "JP'S Nascar Race (Original 2005)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5093", "Stern");
                break;
            case "JP's Grand Prix (Original 2005)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5120", "Stern");
                break;
            case "JP's Lord Of The Rings (Original 2003)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=4858", "Stern");
                break;
            case "JP's Motor Show (Original 1989)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=3631", "Mr. Game");
                break;
            case "JP's Spider-Man (Original 2011)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5237", "Stern", 2007);
                break;
            case "Siggi's Spider-Man Classic (Stern 2016)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=6328", "Stern", 2016, "Spider-Man (Vault Edition)");
                break;
            case "JP's Street Fighter 2 (Original 1993)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=2403", "Gottlieb");
                break;
            case "JP's Terminator 2 (Original 2020)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=2524", "Williams", 1991, "Terminator 2 Judgment Day");
                break;
            case "JP's Transformers (Original 2011)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5709", "Stern");
                break;
            case "Phychedelic (Gottlieb 1970)":
                FixWrongName(onlineGame, "Psychedelic");
                break;
            case "Martian Queen (LTD ) (LTD 0)":
                FixWrongName(onlineGame, "Martian Queen");
                WrongManufacturerYear(onlineGame, "LTD do Brasil Diverses Eletrnicas Ltda", 1981);
                break;
            case "AC-DC (Stern 2012)":
                FixWrongName(onlineGame, "AC/DC (Let There Be Rock Limited Edition)");
                break;
            case "Hang Glider (Bally ) (Bally 1976)":
                FixWrongName(onlineGame, "Hang Glider");
                break;
            case "Avatar, James Cameron's (Stern 2010)":
                FixWrongName(onlineGame, "James Cameron’s Avatar");
                break;
            case "Metallica (Stern 2013)":
                FixWrongName(onlineGame, "Metallica (Premium Monsters)");
                break;
            case "North Star (Gottlieb ) (Gottlieb 1964)":
                FixWrongName(onlineGame, "North Star");
                break;
            case "Power Play, Bobby Orr's (Bally 1978)":
                FixWrongName(onlineGame, "Bobby Orr Power Play");
                break;
            case "Star Trek (Stern 2013)":
                FixWrongName(onlineGame, "Star Trek (Enterprise Limited Edition)");
                break;
            case "JP's Star Trek LE (Stern 2013)":
                FixWrongName(onlineGame, "Star Trek (Vengeance Premium)");
                break;
            case "The Avengers (Stern 2012)":
                FixWrongName(onlineGame, "The Avengers (Pro)");
                break;
            case "Star Wars - The Empire Strikes Back (Hankin 1980)":
                FixWrongName(onlineGame, "The Empire Strikes Back");
                break;
            case "The Walking Dead (Stern 2014)":
                FixWrongName(onlineGame, "The Walking Dead (Limited Edition)");
                break;
            case "JP's The Walking Dead (Stern 2014)":
                FixWrongName(onlineGame, "The Walking Dead (Pro)");
                break;
            case "Big Indian (Gottlieb 1974)":
                FixWrongUrlIpdb(onlineGame, "https://www.ipdb.org/machine.cgi?id=257");
                break;
            case "JP's Captain Fantastic (Bally 1976)":
                FixWrongName(onlineGame, "Capt. Fantastic and The Brown Dirt Cowboy");
                break;
            case "Captain NEMO Dives Again (Quetzal Pinball 2015)":
                FixWrongName(onlineGame, "Captain Nemo");
                break;
            case "Saloon (Taito do Brasil 1978 1978)":
                WrongManufacturerYear(onlineGame, "Taito do Brasil");
                break;
            case "Night Rider (Bally 1976)":
                // VPX tables are the 1977 SS version
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=4497", "Bally", 1977, tableType: TableType.SolidState);
                break;
            case "Rambo (Original 2021)":
                FixTableType(onlineGame, TableType.SolidState);
                break;
            case "Joker Poker (Gottlieb 1978)":
                // joker poker ia a very special egg because there are 2 variants of the table from the same manufacturer/year that need to be disambiguated
                // - it's a bit of a hack, but we're treating the EM version as a variant.. similar to a 'limited edition'
                if (onlineGame.Type == TableType.SolidState)
                    FixWrongName(onlineGame, "Joker Poker - Limited Edition");
                break;
            case "The Avengers Infinity Quest (Stern 2020)":
            case "Nightmare (Digital Illusions 1992)":
            case "Night of the Living Dead (Pininventions 2014)":
            case "Batman 66 (Stern 2016)":
            case "Mortal Kombat II (Midway 1992)":
            case "Strip Joker Poker (Gottlieb 1978)":
                // if the tables can be identified more generically (e.g. via manufacturer) then add to GameDerived.CheckIsOriginal() instead
                // - implementing via CheckIsOriginal() will also maintain the manufacturer.. which can help with subsequent file matching
                WrongManufacturerYear(onlineGame, "Original");
                break;
        }
    }

    private static void FixGame(OnlineGameBase onlineGame, string ipdbUrl, string manufacturer, int? year = null, string name = null, string tableType = null)
    {
        // assign correct IPDB url and manufacturer
        // - if the game already exists, then it will be picked up later as a duplicate
        FixWrongUrlIpdb(onlineGame, ipdbUrl);

        WrongManufacturerYear(onlineGame, manufacturer, year);

        if (name != null)
            FixWrongName(onlineGame, name);

        if (tableType != null)
            FixTableType(onlineGame, tableType);
    }

    private static void FixWrongUrlContent(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.WrongUrlContent))
            return;

        onlineGame.AllFileCollections.ForEach(kv =>
        {
            kv.Value.ForEach(f =>
                f.Urls.ForEach(urlDetail =>
                {
                    // fix vpuniverse urls - path
                    if (urlDetail.Url?.Contains("//vpuniverse.com/forums") == true)
                    {
                        LogFixed(onlineGame, FixStatisticsEnum.WrongUrlContent, $"type={kv.Key} url={urlDetail.Url}");
                        urlDetail.Url = urlDetail.Url.Replace("//vpuniverse.com/forums", "//vpuniverse.com");
                    }
                })
            );
        });
    }

    private static void FixInvalidUrlContent(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.InvalidUrlContent))
            return;

        onlineGame.AllFileCollections.ForEach(kv =>
        {
            kv.Value.ForEach(f =>
                f.Urls.ForEach(urlDetail =>
                {
                    // fix urls - mark any invalid urls, e.g. Abra Ca Dabra ROM url is a string warning "copyright notices"
                    if (!urlDetail.Broken && !(Uri.TryCreate(urlDetail.Url, UriKind.Absolute, out var generatedUrl) && (generatedUrl.Scheme == Uri.UriSchemeHttp || generatedUrl.Scheme == Uri.UriSchemeHttps)))
                    {
                        LogFixed(onlineGame, FixStatisticsEnum.InvalidUrlContent, $"type={kv.Key} url={urlDetail.Url}");
                        urlDetail.Broken = true;
                    }
                })
            );
        });
    }

    private static void FixFileOrdering(OnlineGame onlineGame)
    {
        // fix file ordering - ensure a game's most recent files are shown first
        // - deliberately no option to disable this feature
        onlineGame.AllFileCollections.ForEach(kv =>
        {
            var orderByDescending = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray();
            if (!kv.Value.SequenceEqual(orderByDescending))
            {
                LogFixed(onlineGame, FixStatisticsEnum.UpdatedTimeOrdering, $"type={kv.Key}");
                kv.Value.Clear();
                kv.Value.AddRange(orderByDescending);
            }
        });
    }

    private static void FixTableUpdatedTime(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.UpdatedTime))
            return;

        // fix game updated timestamp - must not be less than the max file timestamp
        var maxUpdatedAt = onlineGame.AllFilesFlattenedList.Max(x => x.UpdatedAt);
        if (onlineGame.UpdatedAt < maxUpdatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.UpdatedTimeTooLow, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt);
            onlineGame.UpdatedAt = maxUpdatedAt;
        }
        else if (onlineGame.UpdatedAt > maxUpdatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.UpdatedTimeTooHigh, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt, true);
            onlineGame.UpdatedAt = maxUpdatedAt;
        }
    }

    private static void FixTableCreatedTime(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.CreatedTime))
            return;

        // fix game created timestamp - must not be less than any content created at timestamps
        var maxCreatedAt = onlineGame.AllFilesFlattenedList.Max(x => x.CreatedAt);
        if (onlineGame.LastCreatedAt < maxCreatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.CreatedTimeLastTimeTooLow, "createdAt", onlineGame.LastCreatedAt, nameof(maxCreatedAt), maxCreatedAt);
            onlineGame.LastCreatedAt = maxCreatedAt;
        }
    }

    private static void FixFileUpdatedTime(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.UpdatedTime))
            return;

        // fix updated timestamp - must not be lower than the created timestamp
        onlineGame.AllFileCollections.ForEach(kv =>
        {
            kv.Value.Where(f => f.UpdatedAt < f.CreatedAt).ForEach(f =>
            {
                LogFixedTimestamp(onlineGame, FixStatisticsEnum.UpdatedTimeLessThanCreated, "updatedAt", f.UpdatedAt, "   createdAt", f.CreatedAt);
                f.UpdatedAt = f.CreatedAt;
            });
        });
    }

    private static void FixMissingImage(OnlineGameBase onlineGame)
    {
        // fix main image url - assign to the first available image url.. B2S then table
        if (!IsActive(FixFeedOptionEnum.MissingImageUrl) || !onlineGame.ImgUrl.IsEmpty())
            return;

        var imageUrl = onlineGame.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? onlineGame.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;
        if (imageUrl != null)
        {
            LogFixed(onlineGame, FixStatisticsEnum.MissingImage, $"url='{imageUrl}'");
            onlineGame.ImgUrl = imageUrl;
        }
    }

    private static void FixWrongUrlIpdb(OnlineGameBase onlineGame, string ipdbUrl)
    {
        // fix wrong IPDB url
        // - original tables shouldn't reference IPDB (i.e. a manufactured table).. but sometimes happens as a reference to the inspiration table
        if (!IsActive(FixFeedOptionEnum.WrongUrlIpdb) || onlineGame.IpdbUrl == ipdbUrl)
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongUrlIpdb, $"old url={onlineGame.IpdbUrl}, new url={ipdbUrl}");
        onlineGame.IpdbUrl = ipdbUrl;
    }

    private static void FixOriginalTableIncludesIpdbUrl(OnlineGame onlineGame)
    {
        // fix wrong IPDB url
        // - original tables shouldn't reference a manufactured table.. but sometimes happens as a reference to the inspiration table
        if (!IsActive(FixFeedOptionEnum.OriginalTableIncludesIpdbUrl) || !onlineGame.IsOriginal || onlineGame.IpdbUrl == null)
            return;

        LogFixed(onlineGame, FixStatisticsEnum.OriginalTableIncludesIpdbUrl, $"old url={onlineGame.IpdbUrl}, new url={null}");
        onlineGame.IpdbUrl = null;
    }

    private static void FixWrongName(OnlineGameBase onlineGame, string name)
    {
        if (!IsActive(FixFeedOptionEnum.WrongName))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongName, $"old name={onlineGame.Name}, new name={name}");
        onlineGame.Name = name;
    }

    private static void WrongManufacturerYear(OnlineGameBase onlineGame, string manufacturer, int? year = null)
    {
        if (!IsActive(FixFeedOptionEnum.WrongManufacturerYear))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongManufacturerYear, $"old manufacturer={onlineGame.Manufacturer}, new manufacturer={manufacturer}");
        onlineGame.Manufacturer = manufacturer;
        onlineGame.Year = year ?? onlineGame.Year;
    }

    private static void FixTableType(OnlineGameBase onlineGame, string tableType)
    {
        if (!IsActive(FixFeedOptionEnum.WrongType))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongType, $"old type={onlineGame.Type}, new type={tableType}");
        onlineGame.Type = tableType;
    }

    private static bool IsActive(FixFeedOptionEnum option) => Model.Settings.Feeder.SelectedFeedFixOptions.Contains(option);

    private static void LogFixedTimestamp(OnlineGameBase onlineGame, FixStatisticsEnum fixStatisticsEnum, string gameTimeName, DateTime? gameTime, string maxFileTimeName, DateTime? maxFileTime, bool greaterThan = false)
    {
        LogFixed(onlineGame, fixStatisticsEnum, $"game.{gameTimeName} '{gameTime:dd/MM/yy HH:mm:ss}' {(greaterThan ? ">" : "<")} {maxFileTimeName} '{maxFileTime:dd/MM/yy HH:mm:ss}'");
    }

    private static void LogFixed(OnlineGameBase onlineGame, FixStatisticsEnum fixStatisticsEnum, string details = null)
    {
        AddFixStatistic(fixStatisticsEnum);

        var name = $"'{onlineGame.Name[..Math.Min(onlineGame.Name.Length, 35)].Trim()}'";
        Logger.Warn($"Fixed {fixStatisticsEnum,-25} name={name,-35} {details}", true);
    }

    private static void AddFixStatistic(FixStatisticsEnum fixStatisticsEnum)
    {
        _statistics.TryAdd(fixStatisticsEnum, 0);
        _statistics[fixStatisticsEnum]++;
    }

    private static Dictionary<FixStatisticsEnum, int> _statistics;
    private static readonly Regex _trimAuthorsRegex;
}