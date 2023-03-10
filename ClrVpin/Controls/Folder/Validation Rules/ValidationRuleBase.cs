using System.Windows.Controls;
using System.Windows.Data;
using ClrVpin.Extensions;

namespace ClrVpin.Controls.Folder.Validation_Rules;

public abstract class ValidationRuleBase : ValidationRule
{
    // validation rule workflow overview: https: //learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-7.0#validation-process
    protected static T GetValueAndClearError<T>(object value) where T : class
    {
        // value is BindingExpression when a non-default ValidationStep is used, i.e. anything except RawProposedValue
        if (value is BindingExpression bindingExpression)
        {
            // explicitly clear any existing errors
            // - required because *new* validation errors are actually ignored!!
            // - this is done automatically by the framework if a successful validation result is returned, but this is never the case when switching between errors
            // - https://stackoverflow.com/questions/3148064/wpf-validation-clearing-all-validation-errors
            if (bindingExpression.HasError)
                Validation.ClearInvalid(bindingExpression);

            value = bindingExpression.GetValue();
        }

        return value as T;
    }
}