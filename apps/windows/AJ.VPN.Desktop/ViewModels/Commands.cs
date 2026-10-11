using System.Windows.Input;
namespace AJ.VPN.Desktop.ViewModels;
public sealed class RelayCommand(Action<object?> execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute(parameter);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
public sealed class AsyncCommand(Func<Task> execute, Action<Exception> onError) : ICommand
{
    private bool running;
    public bool CanExecute(object? parameter) => !running;
    public event EventHandler? CanExecuteChanged;
    public async void Execute(object? parameter)
    {
        if (running) return;
        running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await execute(); }
        catch (Exception ex) { onError(ex); }
        finally { running = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
}
