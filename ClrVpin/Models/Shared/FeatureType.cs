using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models.Shared
{
    [AddINotifyPropertyChangedInterface]
    public class FeatureType
    {
        public FeatureType(int id)
        {
            Id = id;
        }

        public int Id { get; set; } // unique identifier with the scope of the other feature types, e.g. HitType.Enum
        public string Description { get; set; }
        public string Tip { get; set; }
        public bool IsSupported { get; set; }
        public bool IsNeverSupported { get; set; }
        public bool IsActive { get; set; }
        public ICommand SelectedCommand { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsHelpSupported { get; set; }
        public ActionCommand HelpAction { get; set; }
        public bool IsSpecial { get; set; }

        public static FeatureType CreateSelectAll(List<FeatureType> featureTypes)
        {
            // a generic select/clear all feature type
            var selectAll = new FeatureType(-1)
            {
                Description = "Select/Clear All",
                Tip = "Select or clear all criteria/options",
                IsSupported = true,
                IsActive = featureTypes.All(x => x.IsActive),
                IsSpecial = true
            };

            selectAll.SelectedCommand = new ActionCommand(() =>
            {
                // select/clear every sibling feature type
                featureTypes.ForEach(featureType =>
                {
                    // don't set state if it's not supported
                    if (!featureType.IsSupported)
                        return;

                    // update is active state before invoking command
                    // - required in this order because this is how it would normally be seen if the underlying feature was changed via the UI
                    var wasActive = featureType.IsActive;
                    featureType.IsActive = selectAll.IsActive;

                    // invoke action by only toggling on/off if not already in the on/off state
                    // - to ensure the underlying model is updated
                    if (selectAll.IsActive && !wasActive || !selectAll.IsActive && wasActive)
                        featureType.SelectedCommand.Execute(null);
                });
            });

            return selectAll;
        }
    }
}