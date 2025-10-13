using System;
using System.Windows.Input;

namespace GerenciadorAulas
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter is T tParam) return _canExecute(tParam);
            return _canExecute(default);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T tParam) _execute(tParam);
            else _execute(default);
        }
    }
}
