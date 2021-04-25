using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Utils;

namespace ClrVpin.Models
{
    [Serializable]
    public class Config
    {
        // abstract the underlying settings designer class because..
        // - users are agnostic to the underlying Properties.Settings.Default get/set implementation
        // - simpler xaml binding to avoid need for either..
        //   - StaticResource reference; prone to errors if Default isn't referenced in Path (i.e. else new class used)
        //     e.g. <properties:Settings x:Key="Settings"/>
        //          Text="{Binding Source={StaticResource Settings}, Path=Default.FrontendFolder}"
        //   - Static reference; too long
        //     e.g. Text="{Binding Source={x:Static p:Settings.Default}, Path=FrontendFolder}"
        //   - vs a simple regular data binding
        //     e.g. Text="{Binding FrontendFolder}"

        static Config()
        {
            CheckContentTypes = new ObservableStringCollection<string>(Properties.Settings.Default.CheckContentTypes).Observable;
            CheckHitTypes = new ObservableCollectionJson<HitType>(Properties.Settings.Default.CheckHitTypes, value => Properties.Settings.Default.CheckHitTypes = value).Observable;
        }

        public static string FrontendFolder
        {
            get => Properties.Settings.Default.FrontendFolder;
            set => Properties.Settings.Default.FrontendFolder = value;
        }

        public static string TableFolder
        {
            get => Properties.Settings.Default.TableFolder;
            set => Properties.Settings.Default.TableFolder = value;
        }

        public static ObservableCollection<string> CheckContentTypes;

        public static readonly ObservableCollection<HitType> CheckHitTypes;
        public static readonly List<HitType> FixHitTypes = new List<HitType>(Hit.Types);
    }
}