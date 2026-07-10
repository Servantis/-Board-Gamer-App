using BoardGamerApp.Messages;
using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class AddPlayerViewModel : ObservableObject
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    public bool HasSearchResults => SearchResults.Any();

    public ObservableCollection<Player> SearchResults { get; } = new();

    [ObservableProperty]
    private string groupId = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Player? selectedPlayer;

    [ObservableProperty]
    private bool isBusy;

    public AddPlayerViewModel(
        IPlayerRepository playerRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        _playerRepository = playerRepository;
        _groupMemberRepository = groupMemberRepository;
    }

    /* Diese Methode wird aufgerufen, wenn sich der Wert von SearchText ändert. Sie setzt SelectedPlayer auf null,
       wenn der Name des ausgewählten Spielers nicht mit dem neuen Suchtext übereinstimmt,
       und ruft dann die Methode SearchPlayersAsync auf, um die Spieler basierend auf dem neuen Suchtext zu suchen. */
    partial void OnSearchTextChanged(string value)
    {
        if (SelectedPlayer != null &&
            SelectedPlayer.Name != value)
        {
            SelectedPlayer = null;
        }

        _ = SearchPlayersAsync();
    }

    partial void OnSelectedPlayerChanged(Player? value)
    {
        if (value == null)
            return;

        SearchResults.Clear();
        OnPropertyChanged(nameof(HasSearchResults));
    }

    private async Task SearchPlayersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            OnPropertyChanged(nameof(HasSearchResults));
            return;
        }


        // Erst wenn der Suchtext mindestens 2 Zeichen lang ist, wird die Suche durchgeführt.
        if (SearchText.Trim().Length < 2)
        {
            SearchResults.Clear();
            OnPropertyChanged(nameof(HasSearchResults));
            return;
        }

        try
        {
            IsBusy = true;
            var players = await _playerRepository.SearchAvailablePlayersAsync(
                GroupId,
                SearchText);

            SearchResults.Clear();
            OnPropertyChanged(nameof(HasSearchResults));

            foreach (var player in players)
            {
                SearchResults.Add(player);
            }
            OnPropertyChanged(nameof(HasSearchResults));
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
    private async Task SelectPlayerAsync(Player player)
    {
        SelectedPlayer = player;
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

        if (SelectedPlayer == null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Bitte wähle einen Spieler aus.",
                "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(GroupId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Es wurde keine Gruppe übergeben.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;

            await _groupMemberRepository.AddMemberAsync(
                GroupId,
                SelectedPlayer.Id);

            // Message bei Änderung der Gruppenmitglieder senden, damit andere ViewModels reagieren können
            WeakReferenceMessenger.Default.Send(
                new GroupMembersChangedMessage(GroupId));

            await Shell.Current.GoToAsync("..");
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
}