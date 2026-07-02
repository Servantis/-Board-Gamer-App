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
    public ObservableCollection<GamingGroup> AssignedGroups { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string playerId;

    public GroupOverviewViewModel(
        GroupOverviewRepository groupOverviewRepository,
        DatabaseService databaseService)
	{
        _groupOverviewRepository = groupOverviewRepository;
        _databaseService = databaseService;
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

            var games = await _groupOverviewRepository.GetGroupsByPlayerIdAsync(playerId);

            foreach (var game in games)
            {
                AssignedGroups.Add(game);
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
}