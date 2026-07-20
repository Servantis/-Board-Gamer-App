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
    public ObservableCollection<SyncOutboxEntry> Entries { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int entryCount;


    [ObservableProperty]
    private string lastSyncMessage = string.Empty;

    public SyncOutboxDebugViewModel(
        DatabaseService databaseService,
        AppSyncService appSyncService)
    {
        _databaseService = databaseService;
        _appSyncService = appSyncService;
    }

    public SyncOutboxDebugViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            await Shell.Current.DisplayAlertAsync(
                "Nicht verfügbar",
                "Diese Seite ist nur verfügbar, wenn ein Debugger attached ist.",
                "OK");

            await Shell.Current.GoToAsync("..");
            return;
        }
#else
        await Shell.Current.GoToAsync("..");
        return;
#endif

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


}