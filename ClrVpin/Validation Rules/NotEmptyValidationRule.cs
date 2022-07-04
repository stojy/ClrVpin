using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using ClrVpin.Extensions;

namespace ClrVpin.Validation_Rules;

public class NotEmptyValidationRule : ValidationRule
{
    public string Description { get; set; } = "Field";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        // value is BindingExpression when a ValidationStep is used, i.e. not the bound value
        if (value is BindingExpression bindingExpression)
            value = bindingExpression.GetValue();

        return string.IsNullOrWhiteSpace((value ?? "").ToString())
            ? new ValidationResult(false, $"{Description} required")
            : ValidationResult.ValidResult;
    }
}