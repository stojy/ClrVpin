using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ByteSizeLib;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Shared
{
    [AddINotifyPropertyChangedInterface]
    public abstract class Statistics
    {
        protected Statistics(ObservableCollection<Game> games, TimeSpan elapsedTime, ICollection<FileDetail> gameFiles, ICollection<FileDetail> unknownFiles)
        {
            GameFiles = gameFiles;
            UnknownFiles = unknownFiles;

            ElapsedTime = elapsedTime;
            Games = games;
        }

        protected ObservableCollection<Game> Games { get; }
        protected TimeSpan ElapsedTime { get; }

        public string Result { get; set; }
        public Window Window { get; protected set; }

        public IList<HitTypeEnum> SelectedFixHitTypes { get; set; }
        public IList<HitTypeEnum> SelectedCheckHitTypes { get; set; }
        public IList<HitType> SupportedHitTypes { get; set; }
        public IList<ContentType> SupportedContentTypes { get; set; }
        public IList<string> SelectedCheckContentTypes { get; set; }

        public string FixedTerm { get; protected set; }
        public string FixableTerm { get; protected set; }

        public int TotalCount { get; protected set; } // Games.Count;

        public bool IsRemoveUnknownSupported { get; protected set; }

        protected void CreateStatistics()
        {
            Result =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics()}";
        }

        protected abstract string CreateTotalStatistics();

        protected static string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private string CreateHitTypeStatistics()
        {
            // for every hit type, create stats against every content type
            var hitStatistics = SupportedHitTypes.Select(hitType =>
            {
                string contents;
                if (hitType.Enum.In(HitTypeEnum.Unknown, HitTypeEnum.Unsupported))
                {
                    // other files (unknown and unsupported) matches aren't attributed to a game.. so we treat them a little differently
                    contents = string.Join("\n", SupportedContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetUnknownFilesContentStatistics(contentType.Enum, hitType.Enum)}"));
                }
                else
                {
                    // all known content has an associated game
                    contents = string.Join("\n", SupportedContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetGameFilesContentStatistics(contentType.Enum, hitType.Enum)}"));
                }

                return $"{hitType.Description}\n{contents}";
            });

            return $"Criteria statistics for each content type\n\n{string.Join("\n\n", hitStatistics)}";
        }

        private string GetGameFilesContentStatistics(ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            if (!SelectedCheckContentTypes.Contains(contentType.GetDescription()) || !SelectedCheckHitTypes.Contains(hitType))
                return "skipped";

            var renamePrefix = hitType == HitTypeEnum.Missing ? "irreparable" : SelectedFixHitTypes.Contains(hitType) ? FixedTerm : FixableTerm;

            var statistics = $"{renamePrefix} {Games.Sum(g => g.Content.ContentHitsCollection.First(x => x.Type == contentType).Hits.Count(hit => hit.Type == hitType))}/{TotalCount}";

            // don't show removed for missing files since it's n/a
            if (hitType != HitTypeEnum.Missing && IsRemoveUnknownSupported)
                statistics += $", {CreateRemovedFileStatistics(GameFiles, contentType, hitType)}";

            return statistics;
        }

        private string GetUnknownFilesContentStatistics(ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            if (!SelectedCheckContentTypes.Contains(contentType.GetDescription()) || !SelectedCheckHitTypes.Contains(hitType))
                return "skipped";

            return CreateRemovedFileStatistics(UnknownFiles, contentType, hitType);
        }

        private string CreateRemovedFileStatistics(ICollection<FileDetail> files, ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            string removePrefix;
            if (SelectedFixHitTypes.Contains(hitType))
                removePrefix = IsRemoveUnknownSupported ? "removed" : "ignored";
            else
                removePrefix = "removable";

            return $"{removePrefix} {CreateFileStatistic(files.Where(x => x.HitType == hitType && x.ContentType == contentType).ToList())}";
        }

        private static string CreateFileStatistic(ICollection<FileDetail> removedFiles)
        {
            return CreateFileStatistic(removedFiles.Count, removedFiles.Sum(x => x.Size));
        }

        protected readonly ICollection<FileDetail> GameFiles;
        protected readonly ICollection<FileDetail> UnknownFiles;
        protected const int StatisticsKeyWidth = -26;
    }
}