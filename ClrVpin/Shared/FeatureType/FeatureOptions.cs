using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared.FeatureType;

public static class FeatureOptions
{
    public static ListCollectionView<FeatureType> CreateFeatureOptionsSelectionView<T>(IEnumerable<EnumOption<T>> enumOptions, T highlightedOption, Expression<Func<T>> selectedExpression,
        Action changedAction) where T : Enum
    {
        var accessor = new Accessor<T>(selectedExpression);

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
                    changedAction.Invoke();
                })
            };

            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    //private IEnumerable<FeatureType> CreateCheckContentTypes(IEnumerable<ContentType> contentTypes)
    //{
    //    // show all hit types
    //    var featureTypes = contentTypes.Select(contentType =>
    //    {
    //        var featureType = new FeatureType((int)contentType.Enum)
    //        {
    //            Description = contentType.Description,
    //            Tip = contentType.Tip,
    //            IsSupported = true,
    //            IsActive = Settings.Cleaner.SelectedCheckContentTypes.Contains(contentType.Description),
    //            SelectedCommand = new ActionCommand(() =>
    //            {
    //                Settings.Cleaner.SelectedCheckContentTypes.Toggle(contentType.Description);
    //                UpdateIsValid();
    //            })
    //        };

    //        return featureType;
    //    }).ToList();

    //    return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });
    //}

}