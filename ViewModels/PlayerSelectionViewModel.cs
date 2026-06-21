using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BoardGamerApp.ViewModels;

public class PlayerSelectionViewModel : BaseViewModel
{
    private readonly InstallationService _installationService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerDeviceRepository _playerDeviceRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    private string _statusText = "Wähle einen vorhandenen Spieler aus.";
    private bool _hasLoaded;

    public ObservableCollection<Player> Players { get; } = new();

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand LoadPlayersCommand { get; }
    public ICommand LinkPlayerCommand { get; }

    public PlayerSelectionViewModel(
        InstallationService installationService,
        IPlayerRepository playerRepository,
        IPlayerDeviceRepository playerDeviceRepository,
        CurrentPlayerService currentPlayerService)
    {
        _installationService = installationService;
        _playerRepository = playerRepository;
        _playerDeviceRepository = playerDeviceRepository;
        _currentPlayerService = currentPlayerService;

        LoadPlayersCommand = new AsyncCommand(LoadPlayersAsync);
        LinkPlayerCommand = new AsyncCommand<Player>(LinkPlayerAsync);
    }

    public async Task InitializeAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;

        await LoadPlayersAsync();
    }

    private async Task LoadPlayersAsync()
    {
        try
        {
            if (!Debugger.IsAttached)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Nicht verfügbar",
                    "Die Spielerauswahl ist nur verfügbar, wenn der Debugger verbunden ist.",
                    "OK");

                await Shell.Current.GoToAsync("//loading");
                return;
            }

            IsBusy = true;
            StatusText = "Spieler werden geladen...";

            Players.Clear();

            var players = await _playerRepository.GetActivePlayersAsync();

            foreach (var player in players)
            {
                Players.Add(player);
            }

            StatusText = Players.Count == 0
                ? "Es wurden keine aktiven Spieler gefunden."
                : "Wähle einen vorhandenen Spieler aus.";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Spieler konnten nicht geladen werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LinkPlayerAsync(Player? selectedPlayer)
    {
        if (selectedPlayer is null)
        {
            return;
        }

        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                "Spieler verknüpfen",
                $"Soll diese App-Installation mit '{selectedPlayer.Name}' verknüpft werden?",
                "Ja",
                "Abbrechen");

            if (!confirm)
            {
                return;
            }

            IsBusy = true;
            StatusText = $"'{selectedPlayer.Name}' wird verknüpft...";

            var installationId = await _installationService.GetOrCreateInstallationIdAsync();

            await _playerDeviceRepository.LinkInstallationToPlayerAsync(
                selectedPlayer.Id,
                installationId,
                DeviceInfo.Current.Name,
                DeviceInfo.Current.Platform.ToString());

            _currentPlayerService.SetPlayer(
                selectedPlayer.Id,
                selectedPlayer.Name,
                selectedPlayer.Email);

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Spieler konnte nicht verknüpft werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}