using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClrPin.Controls
{
    // https://stackoverflow.com/questions/4497825/wpf-mvvm-how-to-handle-double-click-on-treeviewitems-in-the-viewmodel?rq=1
    //[ContentProperty("MouseDoubleClickBehaviour")]
    public class MouseDoubleClick
    {
        public static DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(MouseDoubleClick), new UIPropertyMetadata(CommandChanged));

        public static DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(MouseDoubleClick), new UIPropertyMetadata(null));

        public static void SetCommand(DependencyObject target, ICommand value)
        {
            target.SetValue(CommandProperty, value);
        }

        public static void SetCommandParameter(DependencyObject target, object value)
        {
            target.SetValue(CommandParameterProperty, value);
        }

        public static object GetCommandParameter(DependencyObject target) => target.GetValue(CommandParameterProperty);

        private static void CommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is Control control)
            {
                if (e.NewValue != null && e.OldValue == null)
                    control.MouseDoubleClick += OnMouseDoubleClick;
                else if (e.NewValue == null && e.OldValue != null)
                    control.MouseDoubleClick -= OnMouseDoubleClick;
            }
        }

        private static void OnMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var control = (Control) sender;
            var command = (ICommand) control.GetValue(CommandProperty);
            var commandParameter = control.GetValue(CommandParameterProperty);

            // don't fire for parent element (i.e. which are unselected)
            // - mentioned in SO.. but ever witnessed
            if (sender is TreeViewItem {IsSelected: false})
                return;

            if (command.CanExecute(commandParameter))
                command.Execute(commandParameter);
        }
    }
}