using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Models.Shared;

public static class FeatureOptions
{
    public static ListCollectionView<FeatureType> CreateFeatureOptionsView<T>(IEnumerable<EnumOption<T>> enumOptions, T highlightedOption, Expression<Func<T>> selectionExpression,
        ICommand changedCommand) where T : Enum
    {
        var accessor = new Accessor<T>(selectionExpression);

        // all table style options
        var featureTypes = enumOptions.Select(option =>
        {
            var featureType = new FeatureType(Convert.ToInt32(option.Enum))
            {
                Tag = typeof(T).Name,
                Description = option.Description,
                Tip = option.Tip,
                IsSupported = true,
                IsHighlighted = option.Enum.IsEqual(highlightedOption),
                IsActive = option.Enum.IsEqual(accessor.Get()),
                SelectedCommand = new ActionCommand(() =>
                {
                    accessor.Set(option.Enum);
                    changedCommand.Execute(null);
                })
            };

            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }
}