using System.Windows.Input;

namespace BoardGamerApp.ViewModels;

public class AsyncCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncCommand(
        Func<T?, Task> execute,
        Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        if (parameter is T typedParameter)
        {
            return _canExecute?.Invoke(typedParameter) ?? true;
        }

        return _canExecute?.Invoke(default) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            ChangeCanExecute();

            if (parameter is T typedParameter)
            {
                await _execute(typedParameter);
            }
            else
            {
                await _execute(default);
            }
        }
        finally
        {
            _isExecuting = false;
            ChangeCanExecute();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void ChangeCanExecute()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}