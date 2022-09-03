using System;
using System.Collections.Generic;
using System.Windows;

namespace ClrVpin.Controls;

// Ensure resource dictionary is only loaded once
// - used because WPF creates a separate resource dictionary instance EVERY time it is loaded :(
// - solutions..
//   a. load only once somewhere common, e.g. within app.xaml --> a bit naff
//   b. cache the dictionary - as per below.
// - results in significant performance improvements..
//   a. control/template loading
//   b. control/template usage.. presumably because there are less resources to lookup (unconfirmed)
//   c. less memory usage
// - https://stackoverflow.com/questions/6693320/mergeddictionaries-and-resource-lookup
//   http://wpftutorial.net/MergedDictionaryPerformance.html
public class SharedResourceDictionary : ResourceDictionary
{
    // Gets or sets the uniform resource identifier (URI) to load resources from.
    public new Uri Source
    {
        // ReSharper disable once UnusedMember.Global
        get => _sourceUri;
        set
        {
            _sourceUri = value;

            if (!_sharedDictionaries.ContainsKey(value))
            {
                // if the dictionary is not yet loaded, load it by setting the source of the base class
                // - in addition the underlying ResourceDictionary does MANY other updates to the resource dictionary
                base.Source = value;

                // cache it in case it's accessed later
                _sharedDictionaries.Add(value, this);
            }
            else
            {
                // instead of allowing ResourceDictionary to create a new resource, use the cached copy instead
                MergedDictionaries.Add(_sharedDictionaries[value]);
            }
        }
    }

    private Uri _sourceUri;

    // Internal cache of loaded dictionaries
    private static readonly Dictionary<Uri, ResourceDictionary> _sharedDictionaries = new();
}