using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Utils
{
    // keep json in sync with collection..
    // - deserialize List<T>
    // - create observable collection
    // - if observable changes, then update the original json (via callback)
    public class ObservableCollectionJson<T>
    {
        public ObservableCollectionJson(string json, Action<string> setJsonCallback)
        {
            var obj = json == "" ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json);
            Observable = new ObservableCollection<T>(((IList<T>) obj)!);

            Observable.CollectionChanged += (_, _) =>
            {
                // since performance isn't a concern, keep things simple and verbatim overwrite the settings.. i.e. ignore event type add/remove/move/et
                setJsonCallback(JsonSerializer.Serialize(Observable));
            };
        }

        public ObservableCollection<T> Observable { get; set; }
    }
}