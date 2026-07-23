using BoardGamerApp.Models;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BoardGamerApp.ViewModels;

public partial class SyncOutboxDebugViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly AppSyncService _appSyncService;
    private readonly ApiCredentialService _apiCredentialService;
    public ObservableCollection<SyncOutboxEntry> Entries { get; } = new();


    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int entryCount;


    [ObservableProperty]
    private string lastSyncMessage = string.Empty;

    [ObservableProperty]
    private string apiKeyStatusText = "API-Key: nicht geprüft";

    [ObservableProperty]
    private bool hasStoredApiKey;


    public SyncOutboxDebugViewModel(
    DatabaseService databaseService,
    AppSyncService appSyncService,
    ApiCredentialService apiCredentialService)
    {
        _databaseService = databaseService;
        _appSyncService = appSyncService;
        _apiCredentialService = apiCredentialService;
    }

    public SyncOutboxDebugViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
#if !DEBUG
        // In Release-Builds ist dieses Debug-Menü ohnehin schon über AppShell (Flyout-Item
        // unsichtbar) nicht erreichbar - dieser Rücksprung ist nur eine zusätzliche
        // Absicherung, falls die Seite doch einmal direkt aufgerufen wird.
        await Shell.Current.GoToAsync("..");
        return;
#endif

        // Früher gab es hier zusätzlich eine Prüfung auf Debugger.IsAttached (mit Alert +
        // sofortigem GoToAsync("..") zurück), falls kein Debugger hängt. Das führte aber
        // dazu, dass die App beim Öffnen dieser Seite über das Flyout einfrieren konnte:
        // kurz nach dem Start (z. B. bei "Run and Debug" in VS Code) kann Debugger.IsAttached
        // noch kurz "false" liefern, obwohl ein Debugger dabei ist, sich gerade erst
        // anzuhängen - dann wurde hier, noch während die Flyout-Schließanimation lief, eine
        // ZWEITE Shell-Navigation ("...") ausgelöst, was auf Android zu einem UI-Deadlock
        // führen konnte. Da AppShell das Flyout-Item ohnehin schon per Debugger.IsAttached
        // aus- bzw. einblendet, war diese zweite, redundante Prüfung hier nicht nötig.
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            Entries.Clear();

            var entries = await _databaseService.GetPendingSyncOutboxEntriesAsync(500);

            foreach (var entry in entries)
            {
                Entries.Add(entry);
            }

            EntryCount = Entries.Count;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Die Sync-Outbox konnte nicht geladen werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowPayloadAsync(SyncOutboxEntry? entry)
    {
        if (entry is null)
            return;

        await Shell.Current.DisplayAlertAsync(
            $"Payload: {entry.Operation} {entry.EntityName}",
            entry.PayloadJson,
            "OK");
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(SyncOutboxEntry? entry)
    {
        if (entry is null)
            return;

        var message =
            $"ID:\n{entry.Id}\n\n" +
            $"Entity:\n{entry.EntityName}\n\n" +
            $"Entity-ID:\n{entry.EntityId}\n\n" +
            $"Operation:\n{entry.Operation}\n\n" +
            $"CreatedAt:\n{entry.CreatedAt}\n\n" +
            $"RetryCount:\n{entry.RetryCount}\n\n" +
            $"LastError:\n{entry.LastError ?? "-"}";

        await Shell.Current.DisplayAlertAsync(
            "Sync-Outbox Details",
            message,
            "OK");
    }

    [RelayCommand]
    private async Task PushPendingOutboxAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var result = await _appSyncService.PushPendingOutboxAsync();

            lastSyncMessage = result.Message;
        }
        catch (Exception ex)
        {
            lastSyncMessage = $"Sync fehlgeschlagen: {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Sync fehlgeschlagen",
                ex.Message,
                "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task PushInitialSnapshotAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var result = await _appSyncService.PushInitialSnapshotAsync();

            LastSyncMessage = result.Message;

            await Shell.Current.DisplayAlertAsync(
                "Initialdaten-Sync",
                result.Message,
                "OK");
        }
        catch (Exception ex)
        {
            LastSyncMessage = $"Initialdaten-Sync fehlgeschlagen: {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Initialdaten-Sync fehlgeschlagen",
                ex.Message,
                "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task PullServerChangesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var result = await _appSyncService.PullServerChangesAsync();

            LastSyncMessage = result.Message;

            await Shell.Current.DisplayAlertAsync(
                "Serverdaten ziehen",
                result.Message,
                "OK");
        }
        catch (Exception ex)
        {
            LastSyncMessage = $"Pull fehlgeschlagen: {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Pull fehlgeschlagen",
                ex.Message,
                "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private async Task RefreshApiKeyStatusAsync()
    {
        var apiKey = await _apiCredentialService.GetApiKeyAsync();

        hasStoredApiKey = !string.IsNullOrWhiteSpace(apiKey);

        apiKeyStatusText = hasStoredApiKey
            ? $"API-Key gespeichert. Länge: {apiKey!.Length}"
            : "Kein API-Key gespeichert.";
    }

    [RelayCommand]
    private async Task SetApiKeyAsync()
    {
        var apiKey = await Shell.Current.DisplayPromptAsync(
            "API-Key einrichten",
            "Bitte gib den API-Key für die BoardGamer-API ein.",
            "Speichern",
            "Abbrechen",
            placeholder: "X-Api-Key",
            maxLength: 300,
            keyboard: Keyboard.Text);

        if (apiKey is null)
            return;

        apiKey = apiKey.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await Shell.Current.DisplayAlertAsync(
                "API-Key fehlt",
                "Der API-Key darf nicht leer sein.",
                "OK");

            return;
        }

        await _apiCredentialService.SaveApiKeyAsync(apiKey);

        await RefreshApiKeyStatusAsync();

        await Shell.Current.DisplayAlertAsync(
            "API-Key gespeichert",
            "Der API-Key wurde sicher auf diesem Gerät gespeichert.",
            "OK");
    }

    [RelayCommand]
    private async Task ClearApiKeyAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "API-Key löschen",
            "Möchtest du den gespeicherten API-Key von diesem Gerät entfernen?",
            "Löschen",
            "Abbrechen");

        if (!confirmed)
            return;

        _apiCredentialService.ClearApiKey();

        await RefreshApiKeyStatusAsync();

        await Shell.Current.DisplayAlertAsync(
            "API-Key gelöscht",
            "Der API-Key wurde von diesem Gerät entfernt.",
            "OK");
    }

}