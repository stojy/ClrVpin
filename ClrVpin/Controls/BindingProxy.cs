using System.Windows;

namespace ClrVpin.Controls;

// refer BindingProxyFolder.cs
// ReSharper disable once UnusedType.Global
public class BindingProxy : FrameworkElement
{

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
}