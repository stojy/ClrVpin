using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared.FeatureType;

public static class FeatureOptions
{
    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionView<T>(IEnumerable<EnumOption<T>> enumOptions, T highlightedOption, 
        Expression<Func<T>> selection, Action changedAction) where T : Enum
    {
        var memberAccessor = new Accessor<T>(selection);

        // create options with a single selection, e.g. style.. radio button, choice chip, etc
        var featureTypes = enumOptions.Select(option =>
        {
            var featureType = new FeatureType(Convert.ToInt32(option.Enum))
            {
                Tag = typeof(T).Name,
                Description = option.Description,
                Tip = option.Tip,
                IsSupported = true,
                IsHighlighted = option.Enum.IsEqual(highlightedOption),
                IsActive = option.Enum.IsEqual(memberAccessor.Get()),
                SelectedCommand = new ActionCommand(() =>
                {
                    memberAccessor.Set(option.Enum);
                    changedAction.Invoke();
                })
            };

            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionsView<T>(IEnumerable<EnumOption<T>> enumOptions, 
        ObservableCollection<string> selections, Action changedAction) where T : Enum
    {
        // create options with a multiple selection support, e.g. style.. checkbox button, filter chip, etc
        var featureTypes = enumOptions.Select(option =>
        {
            var featureType = new FeatureType(Convert.ToInt32(option.Description))
            {
                Description = option.Description,
                Tip = option.Tip,
                IsSupported = true,
                IsActive = selections.Contains(option.Description),
                SelectedCommand = new ActionCommand(() =>
                {
                    selections.Toggle(option.Description);
                    changedAction();
                })
            };

            return featureType;
        }).ToList();

        //return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });

        return new ListCollectionView<FeatureType>(featureTypes);
    }
}