using System.Globalization;
using System.Windows.Controls;
using PropertyChanged;

namespace ClrVpin.Controls.Folder.Validation_Rules;

[AddINotifyPropertyChangedInterface]
public class NotNullValidationRule : ValidationRuleBase
{
    public string Description { get; set; }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        value = GetValueAndClearError<object>(value);

        return value == null
            ? new ValidationResult(false, $"{Description} required")
            : ValidationResult.ValidResult;
    }
}