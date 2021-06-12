using System.Globalization;
using System.Windows.Controls;

namespace ClrVpin.Validation_Rules
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public string Description { get; set; } = "Field";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, $"{Description} required")
                : ValidationResult.ValidResult;
        }
    }
}
