using System.Windows.Data;

namespace ClrVpin.Extensions;

public static class BindingExtensions
{
    public static object GetValue(this BindingExpression bindingExpression)
    {
        // use reflection to retrieve the bound value of the binding expression
        return bindingExpression?.ResolvedSource?.GetType().GetProperty(bindingExpression.ResolvedSourcePropertyName)?.GetValue(bindingExpression.ResolvedSource);
    }
}