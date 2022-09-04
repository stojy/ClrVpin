using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

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
    public new Uri Source
    {
        get => _sourceUri;
        set
        {
            _sourceUri = value;

            var key = GetKey(this);
            if (!_sharedDictionaries.ContainsKey(key))
            {
                Debug.WriteLine($"SharedResourceDictionary: cache miss.. {key}");

                // because the resource dictionary has been cached yet, invoke the extensive(!) base.Source setter to initialise the dictionary and assign to the parent merged dictionary
                base.Source = value;

                _sharedDictionaries.Add(key, this);
            }
            else
            {
                // explicitly add to the parent merged dictionary using the cached resource dictionary, i.e. don't let WPF create a new resource dictionary instance
                MergedDictionaries.Add(_sharedDictionaries[key]);
            }
        }
    }

    // calculate a key for every URI permutation.. absolute path, local path, relative path, forward vs backward folder slash, different casing, etc
    // - examples..
    //   a. absolute: pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml --> /MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml
    //   b. relative: \Controls\Styles.xaml, ..\Styles.xaml, ..\styles.xaml --> /controls/styles.xaml
    private static string GetKey(SharedResourceDictionary resourceDictionary)
    {
        string key;
        if (resourceDictionary.Source.IsAbsoluteUri)
        {
            // nothing to do here since the source URI is already an absolute path, e.g. reference to an external assembly resource dictionary
            key = resourceDictionary.Source.AbsolutePath.ToLower();
        }
        else
        {
            // a relative path means the resource file is local to the caller
            // - use the base uri to calculate an absolute uri from the supplied relative path
            // - since it's our local app for simplicity we can strip.. schema "pack", authority "application:,,,", and optional(?) "xxx;component"
            var baseUri = ((IUriContext)resourceDictionary).BaseUri;
            if (baseUri?.IsAbsoluteUri != true)
                throw new ArgumentException("parent BaseUri is null or not absolute");

            // use Path library to do the heavy lifting for merging the absolute and relative paths.. requires treating the path temporarily as a file path
            // - strip the 'xxx;component' segment from base path since it doesn't comply with a file path.. also cater for it's absence as WPF doesn't always supply it
            // - also remove any inconsistencies with forward/backward slash and casing to avoid duplicate entries
            var baseWithoutComponent = string.Join("", baseUri.Segments.Where(segment => !segment.ToLower().Contains(";component")));
            var basePath = Path.GetDirectoryName(@$"z:{baseWithoutComponent}");
            var fullPath = Path.GetFullPath(resourceDictionary.Source.OriginalString, basePath!);
            key = fullPath.Replace("z:", "").Replace(@"\", "/").ToLower();
        }
        
        return key;
    }

    private Uri _sourceUri;
    private static readonly Dictionary<string, ResourceDictionary> _sharedDictionaries = new();
}