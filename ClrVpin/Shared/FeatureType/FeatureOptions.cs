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
    public static FeatureType CreateFeatureType(string description, string tip, bool isActive, Action action, bool isHighlighted = false) => new()
    {
        Description = description,
        Tip = tip,
        IsSupported = true,
        IsHighlighted = isHighlighted,
        IsActive = isActive,
        SelectedCommand = new ActionCommand(action)
    };

    public static FeatureType CreateFeatureType<T>(string name, EnumOption<T> option, bool isActive, bool isHighlightedOverride = false) where T : Enum
    {
        return new FeatureType(Convert.ToInt32(option.Enum))
        {
            Tag = name,
            Description = option.Description,
            Tip = option.Tip,
            IsSupported = true,
            IsHighlighted = option.IsHighlighted || isHighlightedOverride,
            IsActive = isActive,

            IsHelpSupported = option.HelpUrl != null,
            HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(option.HelpUrl) { UseShellExecute = true }))
        };
    }

    // todo; invoke the multi selections overloads
    public static ListCollectionView<FeatureType> CreateFeatureOptionsSingleSelectionView<T>(
        ICollection<EnumOption<T>> enumOptions,
        T highlightedOption,
        Expression<Func<T>> selection,
        Action changedAction,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, (bool, string)> isSupportedFunc = null) where T : Enum
    {
        var selectionAccessor = new Accessor<T>(selection);

        // todo; refactor/combine with CreateFeatureOptionsSelectionsViewInternal

        // create options with a single selection, e.g. style.. radio button, choice chip, etc
        var featureTypes = enumOptions.Select(enumOption =>
        {
            var featureType = CreateFeatureType(selectionAccessor.Name, enumOption, enumOption.Enum.IsEqual(selectionAccessor.Get()), enumOption.Enum.IsEqual(highlightedOption));
            featureType.SelectedCommand = new ActionCommand(() =>
            {
                selectionAccessor.Set(enumOption.Enum);
                changedAction.Invoke();
            });

            SetupIsSupported(enumOptions, option => selectionAccessor.Set(option.Enum), isSupportedFunc, enumOption, featureType);

            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    // todo; invoke the enum version instead?
    // - i.e. avoid need for consumer to convert the selection enum to string manually.. do this automatically for ALL selections, i.e. no more nasty/obscure enum integer in the settings
    public static ListCollectionView<FeatureType> CreateFeatureOptionsMultiSelectionView<T>(
        ICollection<EnumOption<T>> enumOptions,
        Expression<Func<ObservableCollection<string>>> selectionExpression, // todo; support T instead of string??
        Action<FeatureType> changedAction = null,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, (bool, string)> isSupportedFunc = null,
        bool includeSelectAll = true,
        int minimumNumberOfSelections = 0
    ) where T : Enum
    {
        Accessor<ObservableCollection<string>>.TryGetName(selectionExpression, out var name);
        var selections = selectionExpression.Compile().Invoke();

        return CreateFeatureOptionsSelectionsViewInternal(name ?? Guid.NewGuid().ToString(), enumOptions,
            enumOption => selections.Contains(enumOption.Description), enumOption => selections.Toggle(enumOption.Description),
            changedAction, isSupportedFunc, includeSelectAll,
            minimumNumberOfSelections);
    }

    public static ListCollectionView<FeatureType> CreateFeatureOptionsMultiSelectionView<T>(
        ICollection<EnumOption<T>> enumOptions,
        Expression<Func<ObservableCollection<T>>> selectionExpression,
        Action<FeatureType> changedAction = null,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, (bool, string)> isSupportedFunc = null,
        bool includeSelectAll = true,
        int minimumNumberOfSelections = 0) where T : Enum
    {
        Accessor<ObservableCollection<T>>.TryGetName(selectionExpression, out var name);
        var selections = selectionExpression.Compile().Invoke();

        return CreateFeatureOptionsSelectionsViewInternal(name ?? Guid.NewGuid().ToString(), enumOptions,
            enumOption => selections.Contains(enumOption.Enum), enumOption => selections.Toggle(enumOption.Enum),
            changedAction, isSupportedFunc, includeSelectAll,
            minimumNumberOfSelections);
    }

    public static void DisableFeatureType(FeatureType featureType, string message = null)
    {
        // disable feature type so it can't be used, e.g. non-selectable
        featureType.IsActive = false;
        featureType.IsSupported = false;
        if (message != null)
            featureType.Tip += message;
    }

    private static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionsViewInternal<T>(
        string selectionName,
        ICollection<EnumOption<T>> enumOptions,
        Func<EnumOption<T>, bool> containsSelection,
        Action<EnumOption<T>> toggleSelection,
        Action<FeatureType> changedAction = null,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, (bool, string)> isSupportedFunc = null,
        bool includeSelectAll = true,
        int minimumNumberOfSelections = 0
    ) where T : Enum
    {
        // create options with a multiple selection support, e.g. style.. checkbox button, filter chip, etc
        var featureTypes = enumOptions.Select(enumOption =>
        {
            var featureType = CreateFeatureType(selectionName, enumOption, containsSelection(enumOption));

            SetupIsSupported(enumOptions, toggleSelection, isSupportedFunc, enumOption, featureType);

            return featureType;
        }).ToList();

        // command setup performed outside of the loop so we've got access to the featureTypes collection
        featureTypes.ForEach(featureType => SetupSelectedCommand(featureType, featureTypes, enumOptions.First(enumOption => featureType.Id == Convert.ToInt32(enumOption.Enum)),
            toggleSelection, changedAction, minimumNumberOfSelections));

        if (includeSelectAll)
            featureTypes.Add(CreateSelectAll(featureTypes));

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    private static void SetupSelectedCommand<T>(FeatureType featureType, IReadOnlyCollection<FeatureType> featureTypes, EnumOption<T> enumOption,
        Action<EnumOption<T>> toggleSelection, Action<FeatureType> changedAction, int minNumberOfSelections) where T : Enum
    {
        featureType.SelectedCommand = new ActionCommand(() =>
            {
                // prevent the button from being deselected if the total number of selections would be less than the minimum allowed
                // - update ordering..
                //   a. isActive BEFORE the command is invoked.. e.g. button that was previously selected will have IsActive=false when processed here
                //   b. UI       AFTER  the command is invoked.. effectively waiting for a dispatch refresh cycle
                if (featureTypes.Count(x => x.IsActive) < minNumberOfSelections)
                {
                    // prevent the feature going inactive
                    featureType.IsActive = true;
                }
                else
                {
                    // process the selection as per normal
                    toggleSelection(enumOption);
                    changedAction?.Invoke(featureType);
                }
            }
        );
    }

    private static void SetupIsSupported<T>(ICollection<EnumOption<T>> enumOptions, Action<EnumOption<T>> toggleSelection,
        Func<ICollection<EnumOption<T>>, EnumOption<T>, (bool isSupported, string message)> isSupportedFunc,
        EnumOption<T> enumOption, FeatureType featureType) where T : Enum
    {
        // if the feature is not supported, then update both..
        // - model, i.e. selections array of string or eum
        // - viewmodel, i.e. the checkbox/radio/chip/etc
        var result = isSupportedFunc?.Invoke(enumOptions, enumOption);
        if (result?.isSupported == false)
        {
            if (featureType.IsActive)
                toggleSelection(enumOption);

            DisableFeatureType(featureType, result.Value.message);
        }
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