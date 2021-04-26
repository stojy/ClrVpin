using System.Collections.ObjectModel;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Config //: INotifyPropertyChanged
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

        public string TableFolder
        {
            get => Properties.Settings.Default.TableFolder;
            set
            {
                Properties.Settings.Default.TableFolder = value;
                //OnPropertyChanged(nameof(Properties.Settings.Default.TableFolder));
            }
        }

        public readonly ObservableCollection<string> CheckContentTypes;
        public readonly ObservableCollection<HitType> CheckHitTypes;
        public readonly ObservableCollection<HitType> FixHitTypes;
        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected static void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}