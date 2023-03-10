using System.Globalization;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class NotEmptyValidationRule : ValidationRuleBase
{
    public string Description { get; set; } = "Field";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        value = GetValueAndClearError<string>(value);

        return string.IsNullOrWhiteSpace((value ?? "").ToString())
            ? new ValidationResult(false, $"{Description} required")
            : ValidationResult.ValidResult;
    }
}