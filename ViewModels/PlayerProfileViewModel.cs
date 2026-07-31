using System.Diagnostics;
using System.Net.Mail;
using System.Windows.Input;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardGamerApp.ViewModels;

public class PlayerProfileViewModel : ObservableObject, IQueryAttributable
{
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerDeviceRepository _playerDeviceRepository;
    private readonly InstallationService _installationService;

    private string _playerId = string.Empty;
    private string _playerName = string.Empty;
    private string? _email;
    private string _emailError = string.Empty;
    private bool _isLoadingProfile;
    private bool _hasEditedEmail;
    private bool _hasTriedSave;
    private bool _isBusy;
    private bool _isCreateMode;
    private bool _returnToLoading;

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
        set
        {
            if (SetProperty(ref _email, value))
            {
                if (_isLoadingProfile)
                {
                    EmailError = string.Empty;
                    return;
                }

                _hasEditedEmail = true;
                ValidateEmailInput(value, showError: true);
            }
        }
    }

    public string EmailError
    {
        get => _emailError;
        private set
        {
            if (SetProperty(ref _emailError, value))
            {
                OnPropertyChanged(nameof(HasEmailError));
            }
        }
    }

    public bool HasEmailError => !string.IsNullOrWhiteSpace(EmailError);

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private static bool IsDebugMode
    {
        get
        {
#if DEBUG
            return true;
#else
            return Debugger.IsAttached;
#endif
        }
    }

    public bool IsDebugVisible => IsDebugMode;

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (SetProperty(ref _isCreateMode, value))
            {
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(SaveButtonText));
                OnPropertyChanged(nameof(CanResetAssignment));
            }
        }
    }

    public bool CanResetAssignment => IsDebugVisible && !IsCreateMode;

    public string PageTitle => IsCreateMode
        ? "Neuen Spieler anlegen"
        : "Spielerprofil";

    public string SaveButtonText => IsCreateMode
        ? "Spieler anlegen"
        : "Speichern";

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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var mode = query.TryGetValue("mode", out var modeValue)
            ? modeValue?.ToString()
            : null;

        _returnToLoading = query.TryGetValue("returnToLoading", out var returnToLoadingValue)
            && bool.TryParse(returnToLoadingValue?.ToString(), out var returnToLoading)
            && returnToLoading;

        if (string.Equals(mode, "create", StringComparison.OrdinalIgnoreCase))
        {
            EnableCreateMode();
        }
    }

    private void EnableCreateMode()
    {
        if (!IsDebugMode)
        {
            return;
        }

        _isLoadingProfile = true;

        try
        {
            IsCreateMode = true;
            PlayerId = string.Empty;
            PlayerName = string.Empty;
            Email = string.Empty;
            EmailError = string.Empty;
            _hasEditedEmail = false;
            _hasTriedSave = false;
        }
        finally
        {
            _isLoadingProfile = false;
        }
    }

    private void LoadFromCurrentPlayer()
    {
        _isLoadingProfile = true;

        try
        {
            PlayerId = _currentPlayerService.PlayerId ?? string.Empty;
            PlayerName = _currentPlayerService.PlayerName ?? string.Empty;
            Email = _currentPlayerService.Email?.Trim();

            _hasEditedEmail = false;
            _hasTriedSave = false;
            EmailError = string.Empty;
        }
        finally
        {
            _isLoadingProfile = false;
        }
    }

    private async Task SaveAsync()
    {
        if (IsCreateMode)
        {
            await CreateAndAssignPlayerAsync();
            return;
        }

        await UpdateExistingPlayerAsync();
    }

    private async Task CreateAndAssignPlayerAsync()
    {
        if (!IsDebugMode)
        {
            await Shell.Current.DisplayAlertAsync(
                "Nicht erlaubt",
                "Neue Spieler können aktuell nur im Debug-Modus angelegt werden.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Der Name darf nicht leer sein.",
                "OK");

            return;
        }

        _hasTriedSave = true;

        if (!TryNormalizeEmail(Email, out var cleanEmail, out var validationError))
        {
            EmailError = validationError ?? "Bitte gib eine gültige E-Mail-Adresse ein.";

            await Shell.Current.DisplayAlertAsync(
                "Ungültige E-Mail-Adresse",
                EmailError,
                "OK");

            return;
        }

        try
        {
            IsBusy = true;

            var cleanName = PlayerName.Trim();

            var player = await _playerRepository.CreatePlayerAsync(
     cleanName,
     cleanEmail);

            var installationId = await _installationService.GetOrCreateInstallationIdAsync();

            var deviceName = Microsoft.Maui.Devices.DeviceInfo.Current.Name;
            var platform = Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString();

            await _playerDeviceRepository.LinkInstallationToPlayerAsync(
                player.Id,
                installationId,
                deviceName,
                platform);

            _currentPlayerService.SetPlayer(
                player.Id,
                player.Name,
                player.Email);

            PlayerId = player.Id;
            PlayerName = player.Name;
            Email = player.Email;
            IsCreateMode = false;
            _hasEditedEmail = false;
            _hasTriedSave = false;
            EmailError = string.Empty;

            await Shell.Current.DisplayAlertAsync(
                "Spieler angelegt",
                "Der Spieler wurde angelegt und diesem Gerät zugeordnet.",
                "OK");

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Spieler konnte nicht angelegt werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateExistingPlayerAsync()
    {
        if (string.IsNullOrWhiteSpace(PlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Es ist kein aktiver Spieler geladen.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Der Name darf nicht leer sein.",
                "OK");

            return;
        }

        _hasTriedSave = true;

        if (!TryNormalizeEmail(Email, out var cleanEmail, out var validationError))
        {
            EmailError = validationError ?? "Bitte gib eine gültige E-Mail-Adresse ein.";

            await Shell.Current.DisplayAlertAsync(
                "Ungültige E-Mail-Adresse",
                EmailError,
                "OK");

            return;
        }

        try
        {
            IsBusy = true;

            var cleanName = PlayerName.Trim();

            await _playerRepository.UpdatePlayerProfileAsync(
                PlayerId,
                cleanName,
                cleanEmail);

            _currentPlayerService.SetPlayer(
                PlayerId,
                cleanName,
                cleanEmail);

            Email = cleanEmail;
            _hasEditedEmail = false;
            _hasTriedSave = false;
            EmailError = string.Empty;

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

    private void ValidateEmailInput(string? value, bool showError)
    {
        if (TryNormalizeEmail(value, out _, out var validationError))
        {
            EmailError = string.Empty;
            return;
        }

        if (showError || _hasEditedEmail || _hasTriedSave)
        {
            EmailError = validationError ?? "Bitte gib eine gültige E-Mail-Adresse ein.";
            return;
        }

        EmailError = string.Empty;
    }

    private static bool TryNormalizeEmail(
        string? value,
        out string? normalizedEmail,
        out string? validationError)
    {
        normalizedEmail = null;
        validationError = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            // E-Mail ist optional.
            return true;
        }

        var email = value.Trim();

        if (email.Contains(',') || email.Contains(';'))
        {
            validationError = "Bitte gib nur eine einzelne E-Mail-Adresse ein.";
            return false;
        }

        if (email.Contains('<') || email.Contains('>'))
        {
            validationError = "Bitte gib nur die reine E-Mail-Adresse ein, zum Beispiel name@example.de.";
            return false;
        }

        if (email.Any(char.IsWhiteSpace))
        {
            validationError = "Die E-Mail-Adresse darf keine Leerzeichen enthalten.";
            return false;
        }

        MailAddress mailAddress;

        try
        {
            mailAddress = new MailAddress(email);
        }
        catch
        {
            validationError = "Die E-Mail-Adresse hat kein gültiges Format.";
            return false;
        }

        if (!string.Equals(mailAddress.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            validationError = "Bitte gib nur die reine E-Mail-Adresse ein, zum Beispiel name@example.de.";
            return false;
        }

        var atIndex = email.LastIndexOf('@');

        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            validationError = "Die E-Mail-Adresse muss ein @-Zeichen und eine Domain enthalten.";
            return false;
        }

        var domain = email[(atIndex + 1)..];

        if (!domain.Contains('.'))
        {
            validationError = "Die Domain der E-Mail-Adresse muss einen Punkt enthalten, zum Beispiel example.de.";
            return false;
        }

        if (domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains(".."))
        {
            validationError = "Die Domain der E-Mail-Adresse ist ungültig.";
            return false;
        }

        var domainParts = domain.Split('.');

        if (domainParts.Any(part => string.IsNullOrWhiteSpace(part)))
        {
            validationError = "Die Domain der E-Mail-Adresse ist ungültig.";
            return false;
        }

        normalizedEmail = mailAddress.Address.Trim().ToLowerInvariant();
        return true;
    }

    private async Task CloseAsync()
    {
        if (IsCreateMode && _returnToLoading)
        {
            await Shell.Current.GoToAsync("//loading");
            return;
        }

        if (Shell.Current.Navigation.ModalStack.Count > 0)
        {
            await Shell.Current.Navigation.PopModalAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private async Task ResetAssignmentAsync()
    {
        if (!IsDebugMode || IsCreateMode)
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

            if (Shell.Current.Navigation.ModalStack.Count > 0)
            {
                await Shell.Current.Navigation.PopModalAsync();
            }

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
