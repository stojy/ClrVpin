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
    public class RebuilderResults : Results
    {
        public RebuilderResults(ObservableCollection<Game> games)
        {
            Games = games;
            Initialise();
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Rebuilder Table Results (Issues)",
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

        protected override IEnumerable<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the rebuilder configuration
            // - rebuilder only supports one destination content type, but display them all as a list for consistency with ScannerResults
            var filteredContentTypes = Config.ContentTypes.Select(contentType => new FeatureType
            {
                Description = contentType.Description,
                Tip = contentType.Tip,
                IsSupported = false, // don't allow user to deselect the destination type
                IsActive = Config.GetDestinationContentType().Enum == contentType.Enum,
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes;
        }

        protected override IEnumerable<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the rebuilder configuration
            var filteredContentTypes = Config.HitTypes.Select(hitType => new FeatureType
            {
                Description = hitType.Description,
                IsSupported = Model.Config.SelectedMatchTypes.Contains(hitType.Enum),
                IsActive = Model.Config.SelectedMatchTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes.ToList();
        }
    }
}