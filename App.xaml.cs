using BoardGamerApp.Services;
using BoardGamerApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGamerApp;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly AppShell _appShell;

    public App(DatabaseService databaseService, AppShell appShell)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _appShell = appShell;

        _ = InitializeDatabaseAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Datenbank initialisiert: {_databaseService.GetDatabasePath()}"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Fehler beim Initialisieren der Datenbank: {ex.Message}"
            );
        }
    }
}