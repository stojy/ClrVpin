using PropertyChanged;
using System.Windows;

namespace ClrVpin.Controls;

// use in xaml for an element that requires the DataContext but it's unavailable because the element doesn't exist within the visual tree
// - this class creates a proxy object which stores the data context so it can be referenced elsewhere in xaml by the element that has no DataContext of it's own.. poor little guy!
// - Freezable class accommodates this because it can capture the DataContext despite not being in the visual tree
// - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx

[AddINotifyPropertyChangedInterface]
public class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxy();

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
}