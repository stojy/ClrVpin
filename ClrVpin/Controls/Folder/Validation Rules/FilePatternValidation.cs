using System.Globalization;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

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

    public FilePatternValidationArgs Args { get; set; }
}