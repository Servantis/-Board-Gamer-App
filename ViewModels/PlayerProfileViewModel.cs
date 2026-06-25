using System.Diagnostics;
using System.Windows.Input;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardGamerApp.ViewModels;

public class PlayerProfileViewModel : ObservableObject
{
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerDeviceRepository _playerDeviceRepository;
    private readonly InstallationService _installationService;

    private string _playerId = string.Empty;
    private string _playerName = string.Empty;
    private string? _email;
    private bool _isBusy;

    public string PlayerId
    {
        get => _playerId;
        set => SetProperty(ref _playerId, value);
    }

    public string PlayerName
    {
        get => _playerName;
        set
        {
            if (SetProperty(ref _playerName, value))
            {
                OnPropertyChanged(nameof(Initials));
            }
        }
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsDebugVisible => Debugger.IsAttached;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                return "?";
            }

            var parts = PlayerName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return parts[0][0].ToString().ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ResetAssignmentCommand { get; }

    public PlayerProfileViewModel(
        CurrentPlayerService currentPlayerService,
        IPlayerRepository playerRepository,
        IPlayerDeviceRepository playerDeviceRepository,
        InstallationService installationService)
    {
        _currentPlayerService = currentPlayerService;
        _playerRepository = playerRepository;
        _playerDeviceRepository = playerDeviceRepository;
        _installationService = installationService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CloseCommand = new AsyncRelayCommand(CloseAsync);
        ResetAssignmentCommand = new AsyncRelayCommand(ResetAssignmentAsync);

        LoadFromCurrentPlayer();
    }

    private void LoadFromCurrentPlayer()
    {
        PlayerId = _currentPlayerService.PlayerId ?? string.Empty;
        PlayerName = _currentPlayerService.PlayerName ?? string.Empty;
        Email = _currentPlayerService.Email;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(PlayerId))
        {
            await Shell.Current.DisplayAlert(
                "Fehler",
                "Es ist kein aktiver Spieler geladen.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            await Shell.Current.DisplayAlert(
                "Fehler",
                "Der Name darf nicht leer sein.",
                "OK");

            return;
        }

        try
        {
            IsBusy = true;

            var cleanName = PlayerName.Trim();
            var cleanEmail = string.IsNullOrWhiteSpace(Email)
                ? null
                : Email.Trim();

            await _playerRepository.UpdatePlayerProfileAsync(
                PlayerId,
                cleanName,
                cleanEmail);

            _currentPlayerService.SetPlayer(
                PlayerId,
                cleanName,
                cleanEmail);

            await Shell.Current.DisplayAlertAsync(
                "Gespeichert",
                "Dein Spielerprofil wurde aktualisiert.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Profil konnte nicht gespeichert werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CloseAsync()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async Task ResetAssignmentAsync()
    {
        if (!Debugger.IsAttached)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Spielerzuordnung zurücksetzen",
            "Soll die lokale Zuordnung dieser App-Installation wirklich zurückgesetzt werden?",
            "Ja",
            "Abbrechen");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var installationId = await _installationService.GetOrCreateInstallationIdAsync();

            await _playerDeviceRepository.UnlinkInstallationAsync(installationId);

            _installationService.ResetInstallationId();
            _currentPlayerService.Clear();

            await Shell.Current.Navigation.PopModalAsync();

            await Shell.Current.GoToAsync("//loading");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Zuordnung konnte nicht zurückgesetzt werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}