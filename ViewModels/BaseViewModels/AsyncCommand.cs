using System.Windows.Input;

namespace BoardGamerApp.ViewModels;

public class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
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

            await _execute();
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