using System.Windows;
using ClrVpin.Controls.FolderSelection;

namespace ClrVpin.Controls;

// use in xaml for an element that requires the DataContext but it's unavailable because the element doesn't exist within the visual tree
// - this class creates a proxy object which stores the data context so it can be referenced elsewhere in xaml
// - Freezable class accommodates this because it can capture the DataContext despite not being in the visual tree
// - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
public class BindingProxyFolder : DependencyObject
{
    public FolderTypeDetail Data
    {
        get => (FolderTypeDetail)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(FolderTypeDetail), typeof(BindingProxyFolder), new PropertyMetadata(null));
}