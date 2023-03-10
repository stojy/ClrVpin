using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class FileExistsValidationRule : ValidationRuleBase
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var path = GetValueAndClearError<string>(value);

        if (string.IsNullOrWhiteSpace(path))
            return Args.IsRequired ? new ValidationResult(false, "Folder is required") : ValidationResult.ValidResult;

        if (!Directory.Exists(path) && !File.Exists(path))
            return new ValidationResult(false, "Folder does not exist");
            
        return ValidationResult.ValidResult;
    }

    public FileExistsValidationRuleArgs Args { get; set; }
}