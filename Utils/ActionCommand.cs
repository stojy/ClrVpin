using System;
using System.Windows.Input;

namespace Utils
{
    public class ActionCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<object, bool> _canExecute;

        public ActionCommand(Action execute, Func<object, bool> canExecute = null)
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
            _execute?.Invoke();
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
    }
}