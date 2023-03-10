using System.Windows;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

// refer comments in FileExistsValidationRuleArgs
[AddINotifyPropertyChangedInterface]
public class FilePatternValidationRuleArgs : DependencyObject
{
    public static readonly DependencyProperty PatternProperty =
        DependencyProperty.Register(nameof(Pattern), typeof(string), typeof(FilePatternValidationRuleArgs), new PropertyMetadata(default(string)));

    public string Pattern
    {
        get => (string)GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }
}