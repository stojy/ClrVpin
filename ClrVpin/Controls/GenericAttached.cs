using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace ClrVpin.Controls;

// attached properties
// - property that has access to everything in the control that it's attached too.
// - setup: create dependency property (to allow binding) and create Get/Set to allow it to bind to a control
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

    // Double
    public static readonly DependencyProperty DoubleProperty =
        DependencyProperty.RegisterAttached("Double", typeof(double), typeof (GenericAttached), new PropertyMetadata(default(double)));
    public static void SetDouble(UIElement element, string value) => element.SetValue(DoubleProperty, value);
    public static string GetDouble(UIElement element) => (string) element.GetValue(DoubleProperty);

    // Double2
    public static readonly DependencyProperty Double2Property =
        DependencyProperty.RegisterAttached("Double2", typeof(double), typeof (GenericAttached), new PropertyMetadata(default(double)));
    public static void SetDouble2(UIElement element, string value) => element.SetValue(Double2Property, value);
    public static string GetDouble2(UIElement element) => (string) element.GetValue(Double2Property);

    // Array
    public static readonly DependencyProperty EnumerableProperty = DependencyProperty.RegisterAttached("Enumerable", typeof(IEnumerable), typeof(GenericAttached), new PropertyMetadata(default(IEnumerable)));
    public static void SetEnumerable(UIElement element, IEnumerable value) => element.SetValue(EnumerableProperty, value);
    public static IEnumerable GetEnumerable(UIElement element) => (IEnumerable) element.GetValue(EnumerableProperty);

    // Command - not working inside a control template?
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(GenericAttached), new PropertyMetadata(null));
    public static void SetCommand(UIElement element, ICommand value) => element.SetValue(CommandProperty, value);
    public static ICommand GetCommand(UIElement element) => (ICommand) element.GetValue(CommandProperty);
}