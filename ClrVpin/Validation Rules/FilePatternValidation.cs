using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class FilePatternValidation : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var path = value as string;

        if (Args.Pattern != null && path?.EndsWith(Args.Pattern) != true)
            return new ValidationResult(false, $"Folder path must end with '{Args.Pattern}'");

        return ValidationResult.ValidResult;
    }

    public Args Args { get; set; }
}

// validation property must be added as a dependency property to support binding
// - separate class used because we can't add dependency property to a ValidationRule, because ValidationRule is not a DependencyObject (e.g. UserControl)!
// - instead, we associate an instance of this class to the validation rule as a POCO property
// - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
[AddINotifyPropertyChangedInterface]
public class Args : DependencyObject
{
    public static readonly DependencyProperty PatternProperty =
        DependencyProperty.Register(nameof(Pattern), typeof(string), typeof(Args), new PropertyMetadata(default(string)));

    public string Pattern
    {
        get => (string)GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }
}