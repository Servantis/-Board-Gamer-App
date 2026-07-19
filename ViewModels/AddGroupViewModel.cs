using BoardGamerApp.Models;
using BoardGamerApp.Services;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardGamerApp.ViewModels;

public partial class AddGroupViewModel : ObservableObject
{
    private readonly GroupOverviewRepository _groupOverviewRepository;
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly IGroupMemberRepository _groupMemberRepository;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private bool isBusy;

    public AddGroupViewModel(
        GroupOverviewRepository groupOverviewRepository,
        CurrentPlayerService currentPlayerService,
        IGroupMemberRepository groupMemberRepository)
    {
        _groupOverviewRepository = groupOverviewRepository;
        _currentPlayerService = currentPlayerService;
        _groupMemberRepository = groupMemberRepository;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Bitte gib einen Gruppennamen ein.",
                    "OK");

                return;
            }

            if (string.IsNullOrWhiteSpace(_currentPlayerService.PlayerId))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Es ist kein Spieler angemeldet.",
                    "OK");

                return;
            }

            if (await _groupOverviewRepository.ExistsAsync(Name.Trim()))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Eine Gruppe mit diesem Namen existiert bereits.",
                    "OK");

                return;
            }

            var group = new GamingGroup
            {
                Name = Name.Trim(),
                Description = Description?.Trim(),
                CreatedByPlayerId = _currentPlayerService.PlayerId
            };

            await _groupOverviewRepository.AddGroupAsync(group);
            await _groupMemberRepository.AddMemberAsync(
                group.Id,
                _currentPlayerService.PlayerId!,
                "owner"
            );

            // lade den owner 
            var members =
                await _groupMemberRepository
                    .GetGroupMembersByGroupIdAsync(group.Id);

            var owner = members.FirstOrDefault(
                m => m.Role == "owner");

            // setze ihn als nächsten host
            if (owner != null)
            {
                owner.IsNextHost = true;

                await _groupMemberRepository
                    .UpdateMemberAsync(owner);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Die Gruppe konnte nicht erstellt werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}