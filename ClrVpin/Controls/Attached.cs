using System.Windows;

namespace ClrVpin.Controls;

public class Attached
{
    // String
    public static readonly DependencyProperty StringProperty =
        DependencyProperty.RegisterAttached("String", typeof(string), typeof (Attached), new PropertyMetadata(default(string)));
    public static void SetString(UIElement element, string value) => element.SetValue(StringProperty, value);
    public static string GetString(UIElement element) => (string) element.GetValue(StringProperty);

    // String2
    public static readonly DependencyProperty String2Property =
        DependencyProperty.RegisterAttached("String2", typeof(string), typeof (Attached), new PropertyMetadata(default(string)));
    public static void SetString2(UIElement element, string value) => element.SetValue(String2Property, value);
    public static string GetString2(UIElement element) => (string) element.GetValue(String2Property);

    // ExtensionDouble
    public static readonly DependencyProperty DoubleProperty =
        DependencyProperty.RegisterAttached("Double", typeof(double), typeof (Attached), new PropertyMetadata(default(double)));
    public static void SetDouble(UIElement element, string value) => element.SetValue(DoubleProperty, value);
    public static string GetDouble(UIElement element) => (string) element.GetValue(DoubleProperty);
}