using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class FileExistsValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var path = value as string;

        // return ValidationResult false update the UI with 'red warning text' AND doesn't update the binding
        if (string.IsNullOrEmpty(path))
        {
            return Args.IsRequired ? new ValidationResult(false, "Folder is required") : new ValidationResult(true, "Optional folder");
        }

        if (!Directory.Exists(path) && !File.Exists(path))
            return new ValidationResult(false, "Folder does not exist");
            
        return ValidationResult.ValidResult;
    }

    public FileExistsValidationRuleArgs Args { get; set; }
}