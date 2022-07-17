using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Logging;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using Utils.Extensions;

namespace ClrVpin.Importer;

public static class ImporterFix
{
    static ImporterFix()
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
        onlineGames.ForEach((game, index) =>
        {
            game.Index = index + 1;

            // group files into collections so they can be treated generically
            game.AllFiles = new Dictionary<string, FileCollection>
            {
                { nameof(game.TableFiles), new FileCollection(game.TableFiles) },
                { nameof(game.B2SFiles), new FileCollection(game.B2SFiles) },
                { nameof(game.RuleFiles), new FileCollection(game.RuleFiles) },
                { nameof(game.AltColorFiles), new FileCollection(game.AltColorFiles) },
                { nameof(game.AltSoundFiles), new FileCollection(game.AltSoundFiles) },
                { nameof(game.MediaPackFiles), new FileCollection(game.MediaPackFiles) },
                { nameof(game.PovFiles), new FileCollection(game.PovFiles) },
                { nameof(game.PupPackFiles), new FileCollection(game.PupPackFiles) },
                { nameof(game.RomFiles), new FileCollection(game.RomFiles) },
                { nameof(game.SoundFiles), new FileCollection(game.SoundFiles) },
                { nameof(game.TopperFiles), new FileCollection(game.TopperFiles) },
                { nameof(game.WheelArtFiles), new FileCollection(game.WheelArtFiles) }
            };
            game.AllFilesList = game.AllFiles.Select(kv => kv.Value).SelectMany(x => x);
            game.ImageFiles = game.TableFiles.Concat(game.B2SFiles).ToList();

            // perform post-merge fixes, e.g. missing image url
            PostMerge(game);

            // copy the dictionary files (potentially re-arranged, filtered, etc) back to the lists to ensure they are in sync
            game.TableFiles = game.AllFiles[nameof(game.TableFiles)].Cast<TableFile>().ToList();
            game.B2SFiles = game.B2SFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.WheelArtFiles = game.WheelArtFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.RomFiles = game.RomFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.MediaPackFiles = game.MediaPackFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.AltColorFiles = game.AltColorFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.SoundFiles = game.SoundFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.TopperFiles = game.TopperFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.PupPackFiles = game.PupPackFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.PovFiles = game.PovFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.AltSoundFiles = game.AltSoundFiles.OrderByDescending(x => x.UpdatedAt).ToList();
            game.RuleFiles = game.RuleFiles.OrderByDescending(x => x.UpdatedAt).ToList();
        });

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

        FixTableWhitespace(onlineGame);

        FixManufacturerWhitespace(onlineGame);

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
        // - only works for manufactured tables of course
        // - e.g. Star Trek and JP's Star Trek share the same IPDB url
        var duplicateGames = onlineGames.Where(game => !game.IpdbUrl.IsEmpty()).GroupBy(game => game.IpdbUrl).Where(x => x.Count() > 1).ToList();

        duplicateGames.ForEach(grouping =>
        {
            // assign the unique and duplicate(s)
            var game = ImporterUtils.GetUniqueGame(grouping.ToList());
            var duplicates = grouping.Except(game).ToList();

            LogFixed(game, FixStatisticsEnum.DuplicateGame, $"duplicate table(s)={duplicates.Select(x => x.Description).StringJoin()}");

            Logger.Warn($"Merging duplicate tables detected in the online feed, IPDB url: {grouping.Key}\n" +
                        $"- unique:    {game}\n" +
                        $"- duplicate: {duplicates.Select(x => x.Description).StringJoin()}");

            // process the duplicates
            duplicates.ForEach(duplicate =>
            {
                // merge games collections
                game.TableFiles.AddRange(duplicate.TableFiles);
                game.B2SFiles.AddRange(duplicate.B2SFiles);
                game.WheelArtFiles.AddRange(duplicate.WheelArtFiles);
                game.RomFiles.AddRange(duplicate.RomFiles);
                game.MediaPackFiles.AddRange(duplicate.MediaPackFiles);
                game.AltColorFiles.AddRange(duplicate.AltColorFiles);
                game.SoundFiles.AddRange(duplicate.SoundFiles);
                game.TopperFiles.AddRange(duplicate.TopperFiles);
                game.PupPackFiles.AddRange(duplicate.PupPackFiles);
                game.AltSoundFiles.AddRange(duplicate.AltSoundFiles);
                game.RuleFiles.AddRange(duplicate.RuleFiles);

                // remove duplicate
                onlineGames.Remove(duplicate);
            });
        });
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

        var uriBuilder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
        onlineGame.IpdbUrl = uriBuilder.Uri.AbsoluteUri;
    }

    private static void FixInvalidUrlIpdb(OnlineGameBase onlineGame)
    {
        // fix invalid IPDB Url
        // - e.g. "Not Available" frequently used for original tables
        if (!IsActive(FixFeedOptionEnum.InvalidUrlIpdb) || (Uri.TryCreate(onlineGame.IpdbUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.InvalidIpdbUrl, $"url={onlineGame.IpdbUrl}");
        onlineGame.IpdbUrl = null;
    }

    private static void FixManufacturedIncludesAuthor(OnlineGameBase onlineGame)
    {
        // remove author of the game for manufactured tables
        // - e.g. JP's Captain Fantastic (Bally 1976)
        if (!IsActive(FixFeedOptionEnum.ManufacturedTableIncludesAuthor) || GameDerived.CheckIsOriginal(onlineGame.Manufacturer, onlineGame.Name) || !_trimAuthorsRegex.IsMatch(onlineGame.Name))
            return;

        var cleanName = _trimAuthorsRegex.Replace(onlineGame.Name, "").Trim();
        LogFixed(onlineGame, FixStatisticsEnum.ManufacturedContainsAuthor, $"correct='{cleanName}, manufacturer='{onlineGame.Manufacturer}'");
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

    private static void FixNamedGames(OnlineGame onlineGame)
    {
        // non-generic fixes for specifically named games
        // - this is very smelly, but treating these as 'exceptional' (and hopefully few!) scenarios, similar to GameDerived.CheckIsOriginal
        // - todo; report/fix the underlying VPS feed and then remove this code??
        switch (onlineGame.Description)
        {
            case "Austin Powers (Stern 2001)":
                FixWrongUrlIpdb(onlineGame, "https://www.ipdb.org/machine.cgi?id=4504");
                break;
            case "JP's Dale Jr. Nascar (Original 2020)":
                FixGame(onlineGame, "https://www.ipdb.org/machine.cgi?id=5292", "Stern", 2007);
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
                WrongName(onlineGame, "Psychedelic");
                break;
            case "Martian Queen (LTD ) (LTD 0)":
                WrongName(onlineGame, "Martian Queen");
                WrongManufacturer(onlineGame, "LTD do Brasil Diverses Eletrnicas Ltda", 1981);
                break;
        }
    }

    private static void FixGame(OnlineGameBase onlineGame, string ipdbUrl, string manufacturer, int? year = null, string name = null)
    {
        // assign correct IPDB url and manufacturer
        // - if the game already exists, then it will be picked up later as a duplicate
        FixWrongUrlIpdb(onlineGame, ipdbUrl);

        WrongManufacturer(onlineGame, manufacturer, year);

        if (name != null)
            WrongName(onlineGame, name);
    }

    private static void FixWrongUrlContent(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.WrongUrlContent))
            return;

        onlineGame.AllFiles.ForEach(kv =>
        {
            kv.Value.ForEach(f =>
                f.Urls.ForEach(urlDetail =>
                {
                    // fix vpuniverse urls - path
                    if (urlDetail.Url?.Contains("//vpuniverse.com/forums") == true)
                    {
                        LogFixed(onlineGame, FixStatisticsEnum.WrongUrl, $"type={kv.Key} url={urlDetail.Url}");
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

        onlineGame.AllFiles.ForEach(kv =>
        {
            kv.Value.ForEach(f =>
                f.Urls.ForEach(urlDetail =>
                {
                    // fix urls - mark any invalid urls, e.g. Abra Ca Dabra ROM url is a string warning "copyright notices"
                    if (!urlDetail.Broken && !(Uri.TryCreate(urlDetail.Url, UriKind.Absolute, out var generatedUrl) && (generatedUrl.Scheme == Uri.UriSchemeHttp || generatedUrl.Scheme == Uri.UriSchemeHttps)))
                    {
                        LogFixed(onlineGame, FixStatisticsEnum.InvalidUrl, $"type={kv.Key} url={urlDetail.Url}");
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
        onlineGame.AllFiles.ForEach(kv =>
        {
            var orderByDescending = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray();
            if (!kv.Value.SequenceEqual(orderByDescending))
            {
                LogFixed(onlineGame, FixStatisticsEnum.FileUpdateTimeOrdering, $"type={kv.Key}");
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
        var maxUpdatedAt = onlineGame.AllFilesList.Max(x => x.UpdatedAt);
        if (onlineGame.UpdatedAt < maxUpdatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.TableUpdatedTimeTooLow, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt);
            onlineGame.UpdatedAt = maxUpdatedAt;
        }
        else if (onlineGame.UpdatedAt > maxUpdatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.TableUpdatedTimeTooHigh, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt, true);
            onlineGame.UpdatedAt = maxUpdatedAt;
        }
    }

    private static void FixTableCreatedTime(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.CreatedTime))
            return;

        // fix game created timestamp - must not be less than any file timestamps
        var maxCreatedAt = onlineGame.AllFilesList.Max(x => x.CreatedAt);
        if (onlineGame.LastCreatedAt < maxCreatedAt)
        {
            LogFixedTimestamp(onlineGame, FixStatisticsEnum.TableCreatedTime, "createdAt", onlineGame.LastCreatedAt, nameof(maxCreatedAt), maxCreatedAt);
            onlineGame.LastCreatedAt = maxCreatedAt;
        }
    }

    private static void FixFileUpdatedTime(OnlineGame onlineGame)
    {
        if (!IsActive(FixFeedOptionEnum.UpdatedTime))
            return;

        // fix updated timestamp - must not be lower than the created timestamp
        onlineGame.AllFiles.ForEach(kv =>
        {
            kv.Value.Where(f => f.UpdatedAt < f.CreatedAt).ForEach(f =>
            {
                LogFixedTimestamp(onlineGame, FixStatisticsEnum.FileUpdatedTime, "updatedAt", f.UpdatedAt, "   createdAt", f.CreatedAt);
                f.UpdatedAt = f.CreatedAt;
            });
        });
    }

    private static void FixMissingImage(OnlineGameBase onlineGame)
    {
        // fix image url - assign to the first available image url.. B2S then table
        if (!IsActive(FixFeedOptionEnum.MissingImageUrl) || onlineGame.ImgUrl != null)
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
        // - original tables shouldn't reference a manufactured table.. but sometimes happens as a reference to the inspiration table
        if (!IsActive(FixFeedOptionEnum.WrongUrlIpdb) || onlineGame.IpdbUrl == ipdbUrl)
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongIpdbUrl, $"old url={onlineGame.IpdbUrl}, new url={ipdbUrl}");
        onlineGame.IpdbUrl = ipdbUrl;
    }
    
    private static void FixOriginalTableIncludesIpdbUrl(OnlineGame onlineGame)
    {
        // fix wrong IPDB url
        // - original tables shouldn't reference a manufactured table.. but sometimes happens as a reference to the inspiration table
        if (!IsActive(FixFeedOptionEnum.OriginalTableIncludesIpdbUrl) || !onlineGame.IsOriginal || onlineGame.IpdbUrl == null)
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongIpdbUrl, $"old url={onlineGame.IpdbUrl}, new url={null}");
        onlineGame.IpdbUrl = null;
    }

    private static void WrongName(OnlineGameBase onlineGame, string name)
    {
        if (!IsActive(FixFeedOptionEnum.WrongName))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongName, $"new name={name}");
        onlineGame.Name = name;
    }

    private static void WrongManufacturer(OnlineGameBase onlineGame, string manufacturer, int? year = null)
    {
        if (!IsActive(FixFeedOptionEnum.WrongManufacturerAndYear))
            return;

        LogFixed(onlineGame, FixStatisticsEnum.WrongManufacturer, $"old manufacturer={onlineGame.Manufacturer}, new manufacturer={manufacturer}");
        onlineGame.Manufacturer = manufacturer;
        onlineGame.Year = year ?? onlineGame.Year;
    }

    private static bool IsActive(FixFeedOptionEnum option) => Model.Settings.Importer.SelectedFeedFixOptions.Contains(option);

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