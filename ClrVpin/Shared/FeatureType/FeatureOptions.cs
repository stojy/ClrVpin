using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared.FeatureType;

public static class FeatureOptions
{
    public static FeatureType CreateFeatureType<T>(EnumOption<T> option, bool isActive, bool isHighlightedOverride = false) where T : Enum
    {
        return new FeatureType(Convert.ToInt32(option.Enum))
        {
            Tag = nameof(T),
            Description = option.Description,
            Tip = option.Tip,
            IsSupported = true,
            IsHighlighted = option.IsHighlighted || isHighlightedOverride,
            IsActive = isActive,

            IsHelpSupported = option.HelpUrl != null,
            HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(option.HelpUrl) { UseShellExecute = true }))
        };
    }

    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionView<T>(
        ICollection<EnumOption<T>> enumOptions, 
        T highlightedOption,
        Expression<Func<T>> selection, 
        Action changedAction,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, bool> isSupportedFunc = null) where T : Enum
    {
        var memberAccessor = new Accessor<T>(selection);

        // todo; refactor/combine with CreateFeatureOptionsSelectionsViewInternal

        // create options with a single selection, e.g. style.. radio button, choice chip, etc
        var featureTypes = enumOptions.Select(enumOption =>
        {
            var featureType = CreateFeatureType(enumOption, enumOption.Enum.IsEqual(memberAccessor.Get()), enumOption.Enum.IsEqual(highlightedOption));
            featureType.SelectedCommand = new ActionCommand(() =>
            {
                memberAccessor.Set(enumOption.Enum);
                changedAction.Invoke();
            });

            SetupIsSupported(enumOptions, option => memberAccessor.Set(option.Enum), isSupportedFunc, enumOption, featureType);

            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionsView<T>(
        ICollection<EnumOption<T>> enumOptions, 
        ObservableCollection<string> selections,
        Action<FeatureType> changedAction = null, 
        Func<ICollection<EnumOption<T>>, EnumOption<T>, bool> isSupportedFunc = null,
        bool includeSelectAll = true) where T : Enum
    {
        return CreateFeatureOptionsSelectionsViewInternal(enumOptions,
            enumOption => selections.Contains(enumOption.Description), enumOption => selections.Toggle(enumOption.Description),
            changedAction, isSupportedFunc, includeSelectAll);
    }

    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionsView<T>(
        ICollection<EnumOption<T>> enumOptions, 
        ObservableCollection<T> selections,
        Action<FeatureType> changedAction = null, 
        Func<ICollection<EnumOption<T>>, EnumOption<T>, bool> isSupportedFunc = null,
        bool includeSelectAll = true) where T : Enum
    {
        return CreateFeatureOptionsSelectionsViewInternal(enumOptions,
            enumOption => selections.Contains(enumOption.Enum), enumOption => selections.Toggle(enumOption.Enum),
            changedAction, isSupportedFunc, includeSelectAll);
    }

    private static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionsViewInternal<T>(
        ICollection<EnumOption<T>> enumOptions, 
        Func<EnumOption<T>, bool> containsSelection,
        Action<EnumOption<T>> toggleSelection,
        Action<FeatureType> changedAction = null,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, bool> isSupportedFunc = null,
        bool includeSelectAll = true) where T : Enum
    {
        // create options with a multiple selection support, e.g. style.. checkbox button, filter chip, etc
        var featureTypes = enumOptions.Select(enumOption =>
        {
            var featureType = CreateFeatureType(enumOption, containsSelection(enumOption));
            featureType.SelectedCommand = new ActionCommand(() =>
            {
                toggleSelection(enumOption);
                changedAction?.Invoke(featureType);
            });

            SetupIsSupported(enumOptions, toggleSelection, isSupportedFunc, enumOption, featureType);

            return featureType;
        }).ToList();

        if (includeSelectAll)
            featureTypes.Add(CreateSelectAll(featureTypes));

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    private static void SetupIsSupported<T>(ICollection<EnumOption<T>> enumOptions, Action<EnumOption<T>> toggleSelection, Func<ICollection<EnumOption<T>>, EnumOption<T>, bool> isSupportedFunc, EnumOption<T> enumOption, FeatureType featureType) where T : Enum
    {
        // if the feature is not supported, then update both..
        // - model, i.e. selections array of string or eum
        // - viewmodel, i.e. the checkbox/radio/chip/etc
        if (!(isSupportedFunc?.Invoke(enumOptions, enumOption) ?? true))
        {
            if (featureType.IsActive)
            {
                toggleSelection(enumOption);
                featureType.IsActive = false;
            }

            featureType.IsSupported = false;
        }
    }

    public static void DisableFeatureType(FeatureType featureType, string message = null)
    {
        // disable feature type so it can't be used, e.g. non-selectable
        featureType.IsActive = false;
        featureType.IsSupported = false;
        featureType.Tip += message ?? Model.OptionsDisabledMessage;
    }

    private static FeatureType CreateSelectAll(List<FeatureType> featureTypes)
    {
        // a generic select/clear all feature type
        var selectAll = new FeatureType(SelectAllId)
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
                if ((selectAll.IsActive && !wasActive) || (!selectAll.IsActive && wasActive))
                    featureType.SelectedCommand.Execute(null);
            });
        });

        return selectAll;
    }

    public const int SelectAllId = -1;
}