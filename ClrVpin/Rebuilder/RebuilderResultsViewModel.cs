using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class RebuilderResultsViewModel : ResultsViewModel
    {
        public RebuilderResultsViewModel(ObservableCollection<Game> games)
        {
            Games = games;
            Initialise();
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Results (Matched Files)",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) / 2,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        protected override IList<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the rebuilder configuration
            // - rebuilder only supports one destination content type, but display them all as a list for consistency with ScannerResultsViewModel
            var filteredContentTypes = Config.ContentTypes.Select(contentType => new FeatureType((int)contentType.Enum)
            {
                Description = contentType.Description,
                Tip = contentType.Tip,
                IsSupported = false, // don't allow user to deselect the destination type
                IsActive = Model.Config.GetDestinationContentType().Enum == contentType.Enum,
                SelectedCommand = new ActionCommand(UpdateHitsView)
            });

            return filteredContentTypes.ToList();
        }

        protected override IList<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the rebuilder configuration
            // - valid hits are also visible, enabled by default since these files are copied across without any file name fixing
            var filteredContentTypes = Config.AllHitTypes.Select(hitType => new FeatureType((int)hitType.Enum)
            {
                Description = hitType.Description,
                IsSupported = Settings.Rebuilder.SelectedMatchTypes.Contains(hitType.Enum) || hitType.Enum == HitTypeEnum.Valid,
                IsActive = Settings.Rebuilder.SelectedMatchTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateHitsView)
            });

            return filteredContentTypes.ToList();
        }
    }
}