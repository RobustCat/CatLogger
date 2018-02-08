using System;
using System.Windows.Input;

namespace CatLogger.Utilities
{
    public class DelegateCommand<T> : ICommand
    {
        public DelegateCommand(Action<T> executeMethod)
        {
            ExecuteMethod = executeMethod;
        }

        public DelegateCommand(Func<T, bool> canExecuteMethod, Action<T> executeMethod)
        {
            CanExecuteMethod = canExecuteMethod;
            ExecuteMethod = executeMethod;
        }

        public event EventHandler CanExecuteChanged;

        private Func<T, bool> CanExecuteMethod { get; set; }

        private Action<T> ExecuteMethod { get; set; }

        public bool CanExecute(object parameter)
        {
            if (CanExecuteMethod != null)
            {
                return CanExecuteMethod((T)parameter);
            }
            else
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            if (CanExecute((T)parameter))
            {
                ExecuteMethod((T)parameter);
            }
        }

        public void NotifyCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}