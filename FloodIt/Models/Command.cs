using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace FloodIt.Models
{
    public class Command : ICommand
    {
        readonly bool _executeUsesParameter;
        readonly bool _canExecuteUsesParameter;
        readonly Action<object> _executeObj;
        readonly Predicate<object> _canExecuteObj;

        readonly Action _execute;
        readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public Command(Action execute) : this(execute, null) { }
        public Command(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;

            _executeUsesParameter = _canExecuteUsesParameter = false;
        }

        public Command(Action<object> execute) : this(execute, (Func<bool>)null) { }
        public Command(Action<object> execute, Func<bool> canExecute)
        {
            _executeObj = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;

            _executeUsesParameter = true;
            _canExecuteUsesParameter = false;
        }
        public Command(Action<object> execute, Predicate<object> canExecute)
        {
            _executeObj = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteObj = canExecute;

            _executeUsesParameter = _canExecuteUsesParameter = true;
        }

        public bool CanExecute(object parameter) => _canExecuteUsesParameter switch
        {
            true => _canExecuteObj == null || _canExecuteObj(parameter),
            false => _canExecute == null || _canExecute()
        };

        public void Execute(object parameter)
        {
            if (_executeUsesParameter)
                _executeObj(parameter);
            else
                _execute();
        }
    }

    public class Command<T> : ICommand
        where T : class
    {
        readonly Action<T> _executeT;
        readonly Predicate<T> _canExecuteT;
        readonly Func<bool> _canExecute;
        readonly bool _canExecuteUsesT;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public Command(Action<T> execute) : this(execute, (Func<bool>)null) { }
        public Command(Action<T> execute, Func<bool> canExecute)
        {
            _executeT = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _canExecuteUsesT = false;
        }
        public Command(Action<T> execute, Predicate<T> canExecute)
        {
            _executeT = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteT = canExecute;
            _canExecuteUsesT = true;
        }

        public bool CanExecute(object parameter) => _canExecuteUsesT switch
        {
            true => _canExecuteT == null || _canExecuteT(parameter as T),
            false => _canExecute == null || _canExecute()
        };
        public void Execute(object parameter) => _executeT(parameter as T);
    }
}
