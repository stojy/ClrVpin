using System;
using System.Collections.ObjectModel;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
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

        public Config()
        {
            CheckContentTypes = new ObservableStringCollection<string>(Properties.Settings.Default.CheckContentTypes).Observable;
            CheckHitTypes = new ObservableCollectionJson<HitType>(Properties.Settings.Default.CheckHitTypes, value => Properties.Settings.Default.CheckHitTypes = value).Observable;
            FixHitTypes = new ObservableCollectionJson<HitType>(Properties.Settings.Default.FixHitTypes, value => Properties.Settings.Default.FixHitTypes = value).Observable;
        }

        public string FrontendFolder
        {
            get => Properties.Settings.Default.FrontendFolder;
            set => Properties.Settings.Default.FrontendFolder = value;
        }

        public string FrontendDatabaseFolder
        {
            get => Properties.Settings.Default.FrontendDatabaseFolder;
            set => Properties.Settings.Default.FrontendDatabaseFolder = value;
        }

        public string FrontendTableAudioFolder
        {
            get => Properties.Settings.Default.FrontendTableAudioFolder;
            set => Properties.Settings.Default.FrontendTableAudioFolder = value;
        }

        public string FrontendLaunchAudioFolder
        {
            get => Properties.Settings.Default.FrontendLaunchAudioFolder;
            set => Properties.Settings.Default.FrontendLaunchAudioFolder = value;
        }

        public string FrontendTableVideosFolder
        {
            get => Properties.Settings.Default.FrontendTableVideosFolder;
            set => Properties.Settings.Default.FrontendTableVideosFolder = value;
        }

        public string FrontendBackglassVideosFolder
        {
            get => Properties.Settings.Default.FrontendBackglassVideosFolder;
            set => Properties.Settings.Default.FrontendBackglassVideosFolder = value;
        }

        public string FrontendWheelImagesFolder
        {
            get => Properties.Settings.Default.FrontendWheelImagesFolder;
            set => Properties.Settings.Default.FrontendWheelImagesFolder = value;
        }

        public string TableFolder
        {
            get => Properties.Settings.Default.TableFolder;
            set => Properties.Settings.Default.TableFolder = value;
        }

        public readonly ObservableCollection<string> CheckContentTypes;
        public readonly ObservableCollection<HitType> CheckHitTypes;
        public readonly ObservableCollection<HitType> FixHitTypes;

    }
}