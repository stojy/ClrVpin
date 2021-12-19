using System.Windows;

namespace ClrVpin.Controls;

public class GenericAttached
{
    // String
    public static readonly DependencyProperty StringProperty =
        DependencyProperty.RegisterAttached("String", typeof(string), typeof (GenericAttached), new PropertyMetadata(default(string)));
    public static void SetString(UIElement element, string value) => element.SetValue(StringProperty, value);
    public static string GetString(UIElement element) => (string) element.GetValue(StringProperty);

    // String2
    public static readonly DependencyProperty String2Property =
        DependencyProperty.RegisterAttached("String2", typeof(string), typeof (GenericAttached), new PropertyMetadata(default(string)));
    public static void SetString2(UIElement element, string value) => element.SetValue(String2Property, value);
    public static string GetString2(UIElement element) => (string) element.GetValue(String2Property);

    // ExtensionDouble
    public static readonly DependencyProperty DoubleProperty =
        DependencyProperty.RegisterAttached("Double", typeof(double), typeof (GenericAttached), new PropertyMetadata(default(double)));
    public static void SetDouble(UIElement element, string value) => element.SetValue(DoubleProperty, value);
    public static string GetDouble(UIElement element) => (string) element.GetValue(DoubleProperty);

    // ExtensionDouble
    public static readonly DependencyProperty Double2Property =
        DependencyProperty.RegisterAttached("Double2", typeof(double), typeof (GenericAttached), new PropertyMetadata(default(double)));
    public static void SetDouble2(UIElement element, string value) => element.SetValue(Double2Property, value);
    public static string GetDouble2(UIElement element) => (string) element.GetValue(Double2Property);
}