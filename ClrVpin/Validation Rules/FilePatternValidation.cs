using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using ClrVpin.Controls.FolderSelection;

namespace ClrVpin.Validation_Rules
{
    public class FilePatternValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var path = value as string;

            if (Args.Pattern != null && path?.EndsWith(Args.Pattern) != true)
                return new ValidationResult(false, $"Folder path must end with '{Args.Pattern}'");

            return ValidationResult.ValidResult;
        }

        public Args Args { get; set; }
    }

    // validation property must be added as a dependency property to support binding
    // - separate class used because we can't add dependency property to a ValidationRule, because ValidationRule is not a DependencyObject (e.g. UserControl)!
    // - instead, we associate an instance of this class to the validation rule as a POCO property
    // - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
    public class Args : DependencyObject
    {
        public static DependencyProperty PatternProperty =
            DependencyProperty.Register(nameof(Pattern), typeof(string), typeof(Args), new PropertyMetadata(default(string)));

        public string Pattern
        {
            get => (string)GetValue(PatternProperty);
            set => SetValue(PatternProperty, value);
        }
    }

    // use in xaml for an element that requires the DataContext but it's unavailable because the element doesn't exist within the visual tree
    // - this class creates a proxy object which stores the data context so it can be referenced elsewhere in xaml
    // - Freezable class accommodates this because it can capture the DataContext despite not being in the visual tree
    // - https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            var bindingProxy = new BindingProxy();
            if (bindingProxy.CanFreeze)
                bindingProxy.Freeze();
            return bindingProxy;
        }

        public FolderTypeDetail Data
        {
            get => (FolderTypeDetail)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(FolderTypeDetail), typeof(BindingProxy), new PropertyMetadata(null));
    }
}
