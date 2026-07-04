using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Repositories;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

public partial class GroupOverviewViewModel : ObservableObject
{
    private readonly GroupOverviewRepository _groupOverviewRepository;
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;

    public ObservableCollection<GamingGroup> AssignedGroups { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string playerId;

    public GroupOverviewViewModel(
        GroupOverviewRepository groupOverviewRepository,
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService)
	{
        _groupOverviewRepository = groupOverviewRepository;
        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;
    }

    [RelayCommand]
    public async Task LoadGroupsByPlayerIdAsync()
    {

        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            AssignedGroups.Clear();
            var playerId = _currentPlayerService.PlayerId;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Es ist kein Spieler ausgewählt.",
                    "OK");
                return;
            }

            var assignedGroups = await _groupOverviewRepository.GetGroupsByPlayerIdAsync(playerId);

            foreach (var group in assignedGroups)
            {
                group.CanDelete = IsGroupOwner(group);
                AssignedGroups.Add(group);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Deine zugeordneten Gruppen konnten nicht geladen werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteOrLeaveGroupAsync(GamingGroup group)
    {
        if (group == null)
            return;

        try
        {
            IsBusy = true;

            if (IsGroupOwner(group))
            {
                var confirm = await Shell.Current.DisplayAlertAsync(
                    "Gruppe löschen",
                    $"Soll '{group.Name}' wirklich gelöscht werden?",
                    "Ja",
                    "Nein");

                if (!confirm)
                    return;

                await _groupOverviewRepository.DeleteGroupAsync(group.Id);
            }
            else
            {
                var confirm = await Shell.Current.DisplayAlertAsync(
                    "Gruppe verlassen",
                    $"Möchtest du '{group.Name}' verlassen?",
                    "Ja",
                    "Nein");

                if (!confirm)
                    return;

                await _groupOverviewRepository.LeaveGroupAsync(
                    group.Id,
                    _currentPlayerService.PlayerId!);
            }

            AssignedGroups.Remove(group);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                ex.Message,
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenAddGroupPageAsync()
    {
        await Shell.Current.GoToAsync(nameof(AddGroupPage));
    }

    private bool IsGroupOwner(GamingGroup group)
    {
        return group.CreatedByPlayerId ==
               _currentPlayerService.PlayerId;
    }
}