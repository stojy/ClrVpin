using System.Windows;
using ClrVpin.Controls.Folder;
using PropertyChanged;

namespace ClrVpin.Controls;

// use in xaml for an element that requires the DataContext but it's unavailable because the element doesn't exist within the visual tree
// - this class creates a proxy object which stores the data context so it can be referenced elsewhere in xaml by the element that has no DataContext of it's own.. poor little guy!
// - Freezable class accommodates this because it can capture the DataContext despite not being in the visual tree
// - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx

// - REMOVED 8/3/23 SINCE THIS DOES NOT WORK!!
//   deriving from FrameworkElement instead of freezable to avoid..
//   1. misleading(?) binding error at runtime
//      "Cannot find governing FrameworkElement or FrameworkContentElement for target element"
//      https://stackoverflow.com/questions/40507978/datatemplate-binding-spam-output-window-with-error-cannot-find-governing-framew
//   2. warnings about resource can be frozen.. which for reasons unknown, i'm unable to get working
[AddINotifyPropertyChangedInterface]
public class BindingProxyFolder : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxyFolder();

    public FolderTypeDetail Data
    {
        get => (FolderTypeDetail)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(FolderTypeDetail), typeof(BindingProxyFolder), new PropertyMetadata(null));
}