using System.Collections;
using System.Windows;

namespace ClrVpin.Extensions;

public static class ResourceDictionaryExtensions
{
    public static void WalkDictionary(this ResourceDictionary resourceDictionary, bool freeze = true)
    {
        // required for .net 3.5 to fix a lazy hydration strong reference memory leak
        // - https://stackoverflow.com/questions/6857355/memory-leak-when-using-sharedresourcedictionary
        //   http: //blog.lexique-du-net.com/index.php?post/2011/03/27/What-Dynamic-resources-creates-Memory-leaks-in-WPF-3.5-%28SP1%29
        // - load every resource in every resource dictionary to ensure no references to non-hydrated resources are made
        // - whilst no longer required for .net4, it's still useful (especially in combination with SharedResourceDictionary)..
        //   a. invoke resource initialization during app startup instead of on demand (i.e. when user is clicking through the UI)
        //   b. validate resources to find errors *before* they are used
        //   c. apply actions to resources, e.g. Freeze()
        foreach (DictionaryEntry resourceDictionaryEntry in resourceDictionary)
        {
            // freeze all freezable resources to reduce memory and improve performance.. by removing change monitoring
            // - https://stackoverflow.com/questions/58696168/when-to-actually-use-freeze-for-wpf-controls
            //   https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/freezable-objects-overview?view=netframeworkdesktop-4.8
            if (freeze && resourceDictionaryEntry.Value is Freezable { CanFreeze: true } freezable) 
                freezable.Freeze();
        }
            
        foreach (var rd in resourceDictionary.MergedDictionaries)
            rd.WalkDictionary(freeze);
    }
}