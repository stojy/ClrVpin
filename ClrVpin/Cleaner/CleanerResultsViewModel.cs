using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using PropertyChanged;
using Utils;

namespace ClrVpin.Cleaner
{
    [AddINotifyPropertyChangedInterface]
    public class CleanerResultsViewModel : ResultsViewModel
    {
        public CleanerResultsViewModel(ObservableCollection<LocalGame> games)
        {
            Games = games;
            Initialise();
        }

        public async Task Show(Window parentWindow, double left, double top, double width)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Results",
                Left = left,
                Top = top,
                Width = width,
                Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) / 3,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ResultsTemplate") as DataTemplate
            };
            Window.Show();

            await ShowSummary();
        }

        protected override IList<FeatureType> CreateAllContentFeatureTypes()
        {
            // show all content types, but assign enabled and active based on the cleaner configuration
            var featureTypes = Settings.GetFixableContentTypes().Select(contentType => new FeatureType((int)contentType.Enum)
            {
                Description = contentType.Description,
                Tip = contentType.Tip,

                // todo; use id
                IsSupported = Settings.Cleaner.SelectedCheckContentTypes.Contains(contentType.Description),
                IsActive = Settings.Cleaner.SelectedCheckContentTypes.Contains(contentType.Description),
                SelectedCommand = new ActionCommand(UpdateHitsView)
            }).ToList();

            return featureTypes.Concat(new[] { FeatureOptions.CreateSelectAll(featureTypes) }).ToList();
        }

        protected override IList<FeatureType> CreateAllHitFeatureTypes()
        {
            // show all hit types, but assign enabled and active based on the cleaner configuration
            // - for completeness the valid hits are also visible, but disabled by default since no fixes were required
            var featureTypes = StaticSettings.AllHitTypes.Select(hitType => new FeatureType((int)hitType.Enum)
            {
                Description = hitType.Description,
                IsSupported = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum) || hitType.Enum == HitTypeEnum.CorrectName,
                IsActive = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateHitsView)
            }).ToList();

            return featureTypes.Concat(new[] { FeatureOptions.CreateSelectAll(featureTypes) }).ToList();
        }


        private async Task ShowSummary()
        {
            var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
            var eligibleFiles = Games.Count * Settings.Cleaner.SelectedCheckContentTypes.Count;
            var missingFilesCount = eligibleFiles - validHits.Count;

            var detail = CreatePercentageStatistic("Missing Files", missingFilesCount, eligibleFiles);
            var isSuccess = missingFilesCount == 0;

            await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Files Are Good") : Notification.ShowWarning(DialogHostName, "Missing or Incorrect Files", null, detail));
        }

        private const int WindowMargin = 0;
    }
}