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
using Utils.Extensions;

namespace ClrVpin.Merger
{
    [AddINotifyPropertyChangedInterface]
    public class MergerResultsViewModel : ResultsViewModel
    {
        public MergerResultsViewModel(ObservableCollection<LocalGame> games, ICollection<FileDetail> gameFiles, ICollection<FileDetail> unmatchedFiles)
        {
            Games = games;
            _unmatchedFiles = unmatchedFiles;
            _gameFiles = gameFiles;

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
                Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) / 2,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ResultsTemplate") as DataTemplate
            };
            Window.Show();

            await ShowSummary();
        }

        protected override ListCollectionView<FeatureType> CreateAllContentFeatureTypesView()
        {
            var featureView = FeatureOptions.CreateFeatureOptionsSelectionsView(Settings.GetFixableContentTypes(), 
                new ObservableCollection<string> {Settings.GetSelectedDestinationContentType().Description}, _ => UpdateHitsView(), false);

            // merger content types is a 'special egg'.. unlike cleaner, the list is readonly
            featureView.ForEach(featureType => featureType.IsSupported = false);
            featureView.First(x => x.Id == (int)Settings.GetSelectedDestinationContentType().Enum).IsActive = true;

            return featureView;
        }

        protected override ListCollectionView<FeatureType> CreateAllHitFeatureTypesView()
        {
            // show all hit types, but assign enabled and active based on the merger configuration
            // - valid hits are also visible, enabled by default since these files are copied across without any file name fixing
            var hitFeaturesView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.AllHitTypes, Settings.Merger.SelectedMatchTypes.Clone(), _ => UpdateHitsView());
            hitFeaturesView.ForEach(hitFeature =>
            {
                hitFeature.IsSupported = Settings.Merger.SelectedMatchTypes.Contains((HitTypeEnum)hitFeature.Id) ||
                                         (HitTypeEnum)hitFeature.Id == HitTypeEnum.CorrectName ||
                                         hitFeature.Id == FeatureOptions.SelectAllId;
            });
            return hitFeaturesView;
        }

        private async Task ShowSummary()
        {
            var detail = CreatePercentageStatistic("Unmatched Files", _unmatchedFiles.Count, _gameFiles.Concat(_unmatchedFiles).Count());
            var isSuccess = _unmatchedFiles.Count == 0;

            await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Files Merged") : Notification.ShowWarning(DialogHostName, "Unmatched Files Found", null, detail));
        }

        private readonly ICollection<FileDetail> _gameFiles;
        private readonly ICollection<FileDetail> _unmatchedFiles;

        private const int WindowMargin = 0;
    }
}