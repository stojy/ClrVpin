using System;
using System.Windows.Input;

namespace Utils
{
    public class ActionCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<object, bool> _canExecute;

        public ActionCommand(Action<T> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke((T)parameter);
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
    }
}