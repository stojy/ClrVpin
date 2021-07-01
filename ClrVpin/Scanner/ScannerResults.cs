using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerResults : Results
    {
        public ScannerResults(ObservableCollection<Game> games)
        {
            Games = games;
            Initialise();
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Table Results (Issues)",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) / 3,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        protected override IEnumerable<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Config.HitTypes.Select(hitType => new FeatureType
            {
                Description = hitType.Description,
                IsSupported = Model.Config.SelectedCheckHitTypes.Contains(hitType.Enum),
                IsActive = Model.Config.SelectedCheckHitTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes.ToList();
        }

        protected override IEnumerable<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Config.ContentTypes.Select(contentType => new FeatureType
            {
                Description = contentType.Description,
                Tip = contentType.Tip,
                IsSupported = Model.Config.SelectedCheckContentTypes.Contains(contentType.Description),
                IsActive = Model.Config.SelectedCheckContentTypes.Contains(contentType.Description),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes;
        }
    }
}