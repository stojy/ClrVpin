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
        protected Statistics(ObservableCollection<Game> games, TimeSpan elapsedTime, ICollection<FixFileDetail> fixFileDetails, ICollection<FixFileDetail> otherFileDetails)
        {
            _fixFileDetails = fixFileDetails;
            _otherFileDetails = otherFileDetails;

            ElapsedTime = elapsedTime;
            Games = games;
        }

        protected ObservableCollection<Game> Games { get; }
        protected TimeSpan ElapsedTime { get; }

        public string Result { get; set; }
        public Window Window { get; protected set; }

        protected virtual IList<HitTypeEnum> SelectedFixHitTypes { get; set; }
        protected virtual IList<HitTypeEnum> SelectedCheckHitTypes { get; set; }
        protected virtual IList<HitType> HitTypes { get; set; }
        protected virtual IList<ContentType> ContentTypes { get; set; }
        protected virtual IList<string> SelectedCheckContentTypes { get; set; }

        protected abstract string FixedTerm { get; }   
        protected abstract string FixableTerm { get; } 

        protected abstract int TotalCount { get; } // Games.Count;

        protected void CreateStatistics()
        {
            Result =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(_fixFileDetails)}";
        }

        protected abstract string CreateTotalStatistics(ICollection<FixFileDetail> fixFiles);

        protected static string CreateFileStatistic(ICollection<FixFileDetail> removedFiles)
        {
            return CreateFileStatistic(removedFiles.Count, removedFiles.Sum(x => x.Size));
        }

        protected static string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private string CreateHitTypeStatistics()
        {
            // for every hit type, create stats against every content type
            var hitStatistics = HitTypes.Select(hitType =>
            {
                string contents;
                if (hitType.Enum.In(HitTypeEnum.Unknown, HitTypeEnum.Unsupported))
                {
                    // other files (unknown and unsupported) matches aren't attributed to a game.. so we treat them a little differently
                    contents = string.Join("\n", ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetOtherFilesContentStatistics(contentType.Enum, hitType.Enum)}"));
                }
                else
                {
                    // all known content has an associated game
                    contents = string.Join("\n", ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetFixedFilesContentStatistics(contentType.Enum, hitType.Enum)}"));
                }

                return $"{hitType.Description}\n{contents}";
            });

            return $"Criteria statistics for each content type\n\n{string.Join("\n\n", hitStatistics)}";
        }

        private string GetOtherFilesContentStatistics(ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            if (!SelectedCheckContentTypes.Contains(contentType.GetDescription()) || !SelectedCheckHitTypes.Contains(hitType))
                return "skipped";

            return CreateMissingFileStatistics(contentType, hitType);
        }

        private string GetFixedFilesContentStatistics(ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            if (!SelectedCheckContentTypes.Contains(contentType.GetDescription()) || !SelectedCheckHitTypes.Contains(hitType))
                return "skipped";

            var renamePrefix = hitType == HitTypeEnum.Missing ? "irreparable" : SelectedFixHitTypes.Contains(hitType) ? FixedTerm : FixableTerm;

            var statistics = $"{renamePrefix} {Games.Sum(g => g.Content.ContentHitsCollection.First(x => x.Type == contentType).Hits.Count(hit => hit.Type == hitType))}/{TotalCount}";

            // don't show removed for missing files, since it's n/a
            if (hitType != HitTypeEnum.Missing && IsRemoveSupported)
                statistics += $", {CreateMissingFileStatistics(contentType, hitType)}";

            return statistics;
        }

        public abstract bool IsRemoveSupported { get; }

        private string CreateMissingFileStatistics(ContentTypeEnum contentType, HitTypeEnum hitType)
        {
            var removePrefix = SelectedFixHitTypes.Contains(hitType) ? IsRemoveSupported ? "removed" : "ignored" : "removable";
            return $"{removePrefix} {CreateFileStatistic(_otherFileDetails.Where(x => x.HitType == hitType && x.ContentType == contentType).ToList())}";
        }

        private readonly ICollection<FixFileDetail> _fixFileDetails;
        private ICollection<FixFileDetail> _otherFileDetails;

        protected const int StatisticsKeyWidth = -26;
    }
}