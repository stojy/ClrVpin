using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerResultsViewModel : ResultsViewModel
    {
        public ScannerResultsViewModel(ObservableCollection<Game> games)
        {
            Games = games;
            Initialise();
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Results (Issues and Fixes)",
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

        protected override IList<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Settings.GetFixableContentTypes().Select(contentType => new FeatureType((int)contentType.Enum)
            {
                Description = contentType.Description,
                Tip = contentType.Tip,
                
                // todo; use id
                IsSupported = Settings.Scanner.SelectedCheckContentTypes.Contains(contentType.Description),
                IsActive = Settings.Scanner.SelectedCheckContentTypes.Contains(contentType.Description),
                SelectedCommand = new ActionCommand(UpdateHitsView)
            });

            return filteredContentTypes.ToList();
        }

        protected override IList<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the scanner configuration
            // - for completeness the valid hits are also visible, but disabled by default since no fixes were required
            var filteredContentTypes = StaticSettings.AllHitTypes.Select(hitType => new FeatureType((int)hitType.Enum)
            {
                Description = hitType.Description,
                IsSupported = Settings.Scanner.SelectedCheckHitTypes.Contains(hitType.Enum) || hitType.Enum == HitTypeEnum.Valid,
                IsActive = Settings.Scanner.SelectedCheckHitTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateHitsView)
            });

            return filteredContentTypes.ToList();
        }
    }
}