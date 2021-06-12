using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace ClrVpin.Validation_Rules
{
    public class FileExistsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var path = value as string;
            //if (string.IsNullOrEmpty(path))
            //    return new ValidationResult(false, "Folder is required");
            
            if (!Directory.Exists(path) && !File.Exists(path))
                return new ValidationResult(false, "Folder does not exist");
            
            return ValidationResult.ValidResult;
        }
    }
}
