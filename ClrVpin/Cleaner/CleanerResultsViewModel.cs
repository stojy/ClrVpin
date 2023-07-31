using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Cleaner;

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
            Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) * 2 / 3,
            Content = this,
            Resources = parentWindow.Resources,
            ContentTemplate = parentWindow.FindResource("ResultsTemplate") as DataTemplate
        };
        Window.Show();

        await ShowSummary();
    }

    protected override ListCollectionView<FeatureType> CreateAllContentFeatureTypesView()
    {
        // show all content types, but assign enabled and active based on the cleaner configuration
        var contentFeaturesView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(Settings.GetFixableContentTypes(), () => Settings.Cleaner.SelectedCheckContentTypes.Clone(), _ => UpdateHitsView());
        contentFeaturesView.ForEach(featureType =>
        {
            featureType.IsSupported = Settings.Cleaner.SelectedCheckContentTypes.Contains(featureType.Description) || featureType.Id == FeatureOptions.SelectAllId;
        });
        return contentFeaturesView;
    }

    protected override ListCollectionView<FeatureType> CreateAllHitFeatureTypesView()
    {
        // show all hit types, but assign enabled and active based on the cleaner configuration
        // - for completeness the valid hits are also visible, but disabled by default since no fixes were required
        var hitFeaturesView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.AllHitTypes, () => Settings.Cleaner.SelectedCheckHitTypes.Clone(), _ => UpdateHitsView());
        hitFeaturesView.ForEach(featureType =>
        {
            featureType.IsSupported = Settings.Cleaner.SelectedCheckHitTypes.Contains((HitTypeEnum)featureType.Id) ||
                                      (HitTypeEnum)featureType.Id == HitTypeEnum.CorrectName ||
                                      featureType.Id == FeatureOptions.SelectAllId;
        });
        return hitFeaturesView;
    }

    private async Task ShowSummary()
    {
        var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
        var eligibleFiles = Games.Count * Settings.Cleaner.SelectedCheckContentTypes.Count;
        var missingFilesCount = eligibleFiles - validHits.Count;

        var detail = CreatePercentageStatistic("Missing Files", missingFilesCount, eligibleFiles);
        var isSuccess = missingFilesCount == 0;

        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Files Are Clean") : Notification.ShowWarning(DialogHostName, "Missing or Incorrect Files", null, detail));
    }

    private const int WindowMargin = 0;
}