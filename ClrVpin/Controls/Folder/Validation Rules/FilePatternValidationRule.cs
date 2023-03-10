using System.Globalization;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class FilePatternValidationRule : ValidationRuleBase
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var path = GetValueAndClearError<string>(value);

        if (Args.Pattern != null && !string.IsNullOrEmpty(path) && path.EndsWith(Args.Pattern) != true)
            return new ValidationResult(false, $"Folder must be named '{Args.Pattern}'");

        return new ValidationResult(true, "");
    }

    public FilePatternValidationRuleArgs Args { get; set; }
}