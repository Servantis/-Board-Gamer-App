using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;

namespace BoardGamerApp.ViewModels;

public class LoadingPageViewModel : BaseViewModel
{
    private readonly InstallationService _installationService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerDeviceRepository _playerDeviceRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    private bool _hasInitialized;
    private string _statusText = "Spieler wird geladen...";

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public LoadingPageViewModel(
        InstallationService installationService,
        IPlayerRepository playerRepository,
        IPlayerDeviceRepository playerDeviceRepository,
        CurrentPlayerService currentPlayerService)
    {
        _installationService = installationService;
        _playerRepository = playerRepository;
        _playerDeviceRepository = playerDeviceRepository;
        _currentPlayerService = currentPlayerService;
    }

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;

        await CheckPlayerAssignmentAsync();
    }

    private async Task CheckPlayerAssignmentAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Installation wird geprüft...";

            var installationId = await _installationService.GetOrCreateInstallationIdAsync();

            StatusText = "Spielerzuordnung wird geprüft...";

            var player = await _playerRepository.GetPlayerByInstallationIdAsync(installationId);

            if (player is not null)
            {
                _currentPlayerService.SetPlayer(
                    player.Id,
                    player.Name,
                    player.Email);

                await _playerDeviceRepository.UpdateLastSeenAsync(installationId);

                await Shell.Current.GoToAsync("//home");
                return;
            }

            if (Debugger.IsAttached)
            {
                StatusText = "Keine Zuordnung gefunden. Debug-Auswahl wird geöffnet...";
                await Shell.Current.GoToAsync("//playerSelection");
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Nicht zugeordnet",
                "Diese App-Installation ist noch keinem Spieler zugeordnet. Die Zuordnung ist aktuell nur im Debug-Modus möglich.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Spieler konnte nicht geladen werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}