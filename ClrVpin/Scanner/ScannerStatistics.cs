using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ByteSizeLib;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerStatistics
    {
        public ScannerStatistics(ObservableCollection<Game> games, TimeSpan scanStopWatch, ICollection<FixFileDetail> fixFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;

            CreateStatistics(fixFiles);
        }

        public string Statistics { get; set; }
        public Window Window { get; private set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = left,
                Top = top,
                Width = 400,
                Height = 885,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close()
        {
            Window.Close();
        }

        private void CreateStatistics(ICollection<FixFileDetail> fixFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics(fixFiles)}\n" +
                $"{CreateTotalStatistics(fixFiles)}";
        }

        private string CreateHitTypeStatistics(ICollection<FixFileDetail> fixFileDetails)
        {
            // for every hit type, create stats against every content type
            var hitStatistics = Config.HitTypes.Select(hitType =>
            {
                var prefix = hitType.Enum switch
                {
                    HitTypeEnum.Unknown => "Removed ",
                    HitTypeEnum.Missing => "",
                    _ => "Fixed/Removed "
                };
                var title = $"{prefix}{hitType.Description}";
                
                var contents = "";
                if (hitType.Enum != HitTypeEnum.Unknown)
                {
                    // all known content has an associated game
                    contents = string.Join("\n", Config.ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetContentStatistics(contentType.Description, hitType.Enum)}"));
                }
                else
                {
                    // unknown matches aren't attributed to a game.. so we treat them a little differently
                    contents = string.Join("\n", Config.ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetContentUnknownStatistics(contentType.Enum, hitType.Enum, fixFileDetails)}"));
                }

                return $"{title}\n{contents}";
            });

            var overview = "Criteria statistics for each content type";
            return $"{overview}\n\n{string.Join("\n\n", hitStatistics)}";
        }

        private string GetContentUnknownStatistics(ContentTypeEnum contentType, HitTypeEnum hitType, ICollection<FixFileDetail> fixFileDetails)
        {
            if (!Model.Config.CheckContentTypes.Contains(contentType.GetDescription()) || !Model.Config.CheckHitTypes.Contains(hitType))
                return "skipped";

            var matchingFiles = fixFileDetails.Where(x => x.HitType == HitTypeEnum.Unknown && x.ContentType == contentType).ToList();

            return CreateFileStatistic(matchingFiles.Count, matchingFiles.Sum(x => x.Size));
        }
        
        private string GetContentStatistics(string contentType, HitTypeEnum hitType)
        {
            if (!Model.Config.CheckContentTypes.Contains(contentType) || !Model.Config.CheckHitTypes.Contains(hitType))
                return "skipped";

            return _games.Count(g => g.Content.ContentHitsCollection.First(x => x.Type == contentType).Hits.Any(hit => hit.Type == hitType)) + "/" + _games.Count;
        }

        private string CreateTotalStatistics(ICollection<FixFileDetail> fixFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.Valid).ToList();

            var eligibleHits = _games.Count * Model.Config.CheckContentTypes.Count;

            var fixFilesIgnored = fixFiles.Where(x => x.Ignored).ToList();
            var fixFilesIgnoredSize = fixFilesIgnored.Sum(x => x.Size);
            var fixFilesDeleted = fixFiles.Where(x => x.Deleted).ToList();
            var fixFilesDeletedSize = fixFilesDeleted.Sum(x => x.Size);
            var fixFilesRenamed = fixFiles.Where(x => x.Renamed).ToList();
            var fixFilesRenamedSize = fixFilesRenamed.Sum(x => x.Size);

            // identify unknown fix files for separate statistics
            var unknownFiles = fixFiles.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var unknownFilesIgnored = unknownFiles.Where(x => x.Ignored && !x.Renamed).ToList();
            var unknownFilesIgnoredSize = unknownFilesIgnored.Sum(x => x.Size);
            var unknownFilesDeleted = unknownFiles.Where(x => x.Deleted).ToList();
            var unknownFilesDeletedSize = unknownFilesDeleted.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   "\nTotals" +
                   $"\n{"- Available Games",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{_games.Count * Config.ContentTypes.Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Fixed and Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(fixFiles.Count, fixFiles.Sum(x => x.Size))}" +
                   $"\n{"- Renamed Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesRenamed.Count, fixFilesRenamedSize)}" +
                   $"\n{"- Deleted Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n{"  (Unknown Files)",StatisticsKeyWidth}{CreateFileStatistic(unknownFilesDeleted.Count, unknownFilesDeletedSize)}" +
                   $"\n{"- Ignored Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"  (Unknown Files)",StatisticsKeyWidth}{CreateFileStatistic(unknownFilesIgnored.Count, unknownFilesIgnoredSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.TotalSeconds:f2}s";
        }

        private static string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private readonly ObservableCollection<Game> _games;
        private readonly TimeSpan _scanStopWatch;

        private const int StatisticsKeyWidth = -25;
    }
}