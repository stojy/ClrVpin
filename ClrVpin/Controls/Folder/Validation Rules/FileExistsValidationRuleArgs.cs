using System.Windows;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

// validation property must be added as a dependency property to support binding
// - separate class used because we can't add dependency property to a ValidationRule, because ValidationRule is not a DependencyObject (e.g. UserControl)!
// - instead..
//   - create a 'property class' which itself is a DependencyObject
//   - add an instance of this class to the validation rule (as a POCO property) and let the xaml bind to the contained property which is a dependency property
// - refer also notes in xaml usage: FolderSelection.xaml
// - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
// - https://stackoverflow.com/questions/3862385/wpf-validationrule-with-dependency-property
[AddINotifyPropertyChangedInterface]
public class FileExistsValidationRuleArgs : DependencyObject
{
    public static readonly DependencyProperty PatternProperty =
        DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(FileExistsValidationRuleArgs), new PropertyMetadata(default(bool)));

    public bool IsRequired
    {
        get => (bool)GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }
}