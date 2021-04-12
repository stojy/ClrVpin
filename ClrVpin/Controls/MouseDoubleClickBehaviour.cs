//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Interactivity;

//namespace ClrVpin.Controls
//{
//    //https://stackoverflow.com/questions/4497825/wpf-mvvm-how-to-handle-double-click-on-treeviewitems-in-the-viewmodel?rq=1
//    public class MouseDoubleClickBehavior : Behavior<Control>
//    {
//        public static readonly DependencyProperty CommandProperty =
//            DependencyProperty.Register("Command", typeof(ICommand), typeof(MouseDoubleClickBehavior), new PropertyMetadata(default(ICommand)));

//        public static readonly DependencyProperty CommandParameterProperty =
//            DependencyProperty.Register("CommandParameter", typeof(object), typeof(MouseDoubleClickBehavior), new PropertyMetadata(default(object)));

//        public ICommand Command
//        {
//            get => (ICommand) GetValue(CommandProperty);
//            set => SetValue(CommandProperty, value);
//        }

//        public object CommandParameter
//        {
//            get => GetValue(CommandParameterProperty);
//            set => SetValue(CommandParameterProperty, value);
//        }

//        protected override void OnAttached()
//        {
//            base.OnAttached();
//            AssociatedObject.MouseDoubleClick += OnMouseDoubleClick;
//        }

//        protected override void OnDetaching()
//        {
//            AssociatedObject.MouseDoubleClick -= OnMouseDoubleClick;
//            base.OnDetaching();
//        }

//        private void OnMouseDoubleClick(object sender, RoutedEventArgs e)
//        {
//            if (Command == null) return;
//            Command.Execute( /*commandParameter*/null);
//        }
//    }
//}