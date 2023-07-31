using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using Utils.Extensions;

namespace ClrVpin.Shared.Utils;

internal static class ContentUtils
{
    public static IList<string> GetContentFileNames(ContentType contentType, string folder)
    {
        // for each supported extension file type (e.g. vpx, vpt), retrieve all the files with a matching extension in the specified folder
        var supportedFiles = contentType.ExtensionsList.Select(ext => Directory.EnumerateFiles(folder, ext));

        return supportedFiles.SelectMany(x => x).ToList();
    }

    public static IEnumerable<FileDetail> GetNonContentFileDetails(ContentType contentType, string folder)
    {
        // return all files that don't match the supported file extensions
        var supportedExtensions = contentType.ExtensionsList.Select(x => x.TrimStart('*').ToLower()).ToList();
        var kindredExtensions = contentType.KindredExtensionsList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimStart('*').ToLower());
        supportedExtensions.AddRange(kindredExtensions);

        var allFiles = Directory.EnumerateFiles(folder).Select(x => x.ToLower());

        var unsupportedFiles = allFiles.Where(file => !supportedExtensions.Any(file.EndsWith));

        var unsupportedFixFiles = unsupportedFiles.Select(file => new FileDetail(contentType.Enum, HitTypeEnum.Unsupported, FixFileTypeEnum.Skipped, file, new FileInfo(file).Length));

        return unsupportedFixFiles.ToList();
    }

    public static async Task<List<FileDetail>> MatchContentToLocalAsync(List<LocalGame> games, Action<string, float> updateProgress, ContentType[] contentTypes, bool includeUnsupportedFiles)
    {
        var unmatchedFiles = await Task.Run(() => MatchContentToLocal(games, updateProgress, contentTypes, includeUnsupportedFiles));
        return unmatchedFiles;
    }

    public static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    public static IEnumerable<FileDetail> MatchFilesToLocal(IList<LocalGame> localGames, IEnumerable<string> contentFiles, ContentType contentType,
        Func<LocalGame, ContentHits> getContentHits, Action<string, int> updateProgress)
    {
        var unmatchedSupportedFiles = new List<FileDetail>();

        // for each file, associate it with a game or if one can't be found, then mark it as unmatched
        // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
        contentFiles.ForEach((contentFile, i) =>
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(contentFile);
            updateProgress(fileNameWithoutExtension, i + 1);

            LocalGame matchedLocalGame;

            // check for hit..
            // - a file only match one DB entry.. but a game DB entry can have multiple file hits
            // - if DB entry has more than 1 file hit, then the best match is used and the other are marked as duplicates.. e.g. wrong case, fuzzy matched, etc.
            // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)

            // exact match
            if ((matchedLocalGame = localGames.FirstOrDefault(game => Content.GetName(game, contentType.Category) == fileNameWithoutExtension)) != null)
            {
                // if a match already exists, then assume this match is a duplicate name with wrong extension
                // - file extension order is important as it determines the priority of the preferred extension
                var contentHits = getContentHits(matchedLocalGame);
                contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, contentFile);
            }
            // wrong case match
            else if ((matchedLocalGame = localGames.FirstOrDefault(localGame =>
                         string.Equals(Content.GetName(localGame, contentType.Category), fileNameWithoutExtension, StringComparison.CurrentCultureIgnoreCase))) != null)
            {
                getContentHits(matchedLocalGame).Add(HitTypeEnum.WrongCase, contentFile);
            }
            // media matches table name
            else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedLocalGame = localGames.FirstOrDefault(localGame => localGame.Game.Name == fileNameWithoutExtension)) != null)
            {
                getContentHits(matchedLocalGame).Add(HitTypeEnum.TableName, contentFile);
            }
            // fuzzy matching
            else
            {
                var fuzzyFileNameDetails = Fuzzy.Fuzzy.GetTableDetails(contentFile, true);
                (matchedLocalGame, var score, var isMatch) = localGames.MatchToLocalDatabase(fuzzyFileNameDetails);
                if (isMatch)
                {
                    getContentHits(matchedLocalGame).Add(HitTypeEnum.Fuzzy, contentFile, score);
                }
                else
                {
                    // unmatched
                    // - e.g. possible for..
                    //   a. table --> new table files added AND the database not updated yet
                    //   b. table support and media --> as per pinball OR extra/redundant files exist where there is no table (yet!)
                    unmatchedSupportedFiles.Add(new FileDetail(contentType.Enum, HitTypeEnum.Unknown, FixFileTypeEnum.Skipped, contentFile, new FileInfo(contentFile).Length));
                }
            }
        });

        return unmatchedSupportedFiles;
    }

    private static List<FileDetail> MatchContentToLocal(List<LocalGame> games, Action<string, float> updateProgress, IEnumerable<ContentType> checkContentTypes, bool includeUnsupportedFiles)
    {
        var unmatchedFiles = new List<FileDetail>();

        // retrieve all supported files within the folder
        // - for each content type, match files (from the configured content folder location) with the correct file extension(s) to a table
        // - file matching is performed irrespective of the configured matching type (e.g. invalid case, fuzzy, etc) --> refer MatchFilesToLocal
        var contentTypeSupportedFiles = checkContentTypes.Select(contentType => new
        {
            contentType,
            supportedFiles = GetContentFileNames(contentType, contentType.Folder).ToList()
        }).ToList();

        var totalFilesCount = contentTypeSupportedFiles.Sum(details => details.supportedFiles.Count);
        var fileCount = 0;
        contentTypeSupportedFiles.ForEach(details =>
        {
            var supportedFiles = details.supportedFiles;
            var contentType = details.contentType;

            // for the specified content type, match all retrieved files to local database game entries
            // - any files that can't be matched are designated as 'unknownFiles'.. which form part of 'unmatchedFiles'
            var unmatchedSupportedFiles = MatchFilesToLocal(games, supportedFiles, contentType, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum),
                (fileName, _) => updateProgress($"{contentType.Description}: {fileName}", ++fileCount / (float)totalFilesCount));

            // unmatched files = unmatchedSupportedFiles (supported file type, but failed to match) + unsupportedFiles (unsupported file type)
            unmatchedFiles.AddRange(unmatchedSupportedFiles);

            // identify any unsupported files, i.e. files in the directory that don't have a matching extension
            if (includeUnsupportedFiles)
            {
                var unsupportedFiles = GetNonContentFileDetails(contentType, contentType.Folder);

                // only applicable for media file types, since the 'table files' typically include misc support files (e.g. vbs, pdf, txt, etc)
                if (contentType.Category == ContentTypeCategoryEnum.Media)
                    unmatchedFiles.AddRange(unsupportedFiles);
            }
        });

        // update each table status as missing if their were no matches
        AddMissingStatus(games);

        // unmatchedFiles = unknownFiles + unsupportedFiles
        return unmatchedFiles;
    }

    private static void AddMissingStatus(List<LocalGame> games)
    {
        games.ForEach(game =>
        {
            // add missing content
            game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
            {
                if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName || hit.Type == HitTypeEnum.WrongCase))
                    contentHitCollection.Add(HitTypeEnum.Missing, Content.GetName(game, contentHitCollection.ContentType.Category));
            });
        });
    }
}