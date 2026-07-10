using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BoardGamerApp.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace BoardGamerApp.ViewModels;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class GroupMembersViewModel : ObservableObject
{
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    private bool _isBusy;
    private string _statusText = "Gruppenmitglieder werden geladen...";
    private string _groupId = string.Empty;

    [ObservableProperty]
    private string groupName = string.Empty;

    // Wenn eine GroupPage anhand einer groupId geöffnet wird, wird hier die entsprechende GroupId gesetzt
    // und Mitglieder der Gruppe geladen
    public string GroupId
    {
        get => _groupId;
        set
        {
            if (SetProperty(ref _groupId, value))
            {
                _ = LoadGroupAsync();
                _ = LoadMembersAsync();
            }
        }
    }

    public ObservableCollection<GroupMemberListItem> Members { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand SelectNextHostCommand { get; }
    public ICommand SimulateTriggerCommand { get; }
    public ICommand ManageMembersCommand { get; }
    public ICommand OpenAddPlayerPageCommand { get; }
    public ICommand RemoveMemberCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public IEnumerable<GroupMemberListItem> RotationMembers =>
        Members
            .Where(member => member.Status == "active")
            .OrderBy(member => member.RotationOrder ?? int.MaxValue)
            .ThenBy(member => member.PlayerName);
    public GroupMembersViewModel(IGroupMemberRepository groupMemberRepository, CurrentPlayerService currentPlayerService)
    {
        _groupMemberRepository = groupMemberRepository;

        RefreshCommand = new AsyncRelayCommand(LoadMembersAsync);
        SelectNextHostCommand = new AsyncRelayCommand(SelectNextHostAsync);
        SimulateTriggerCommand = new AsyncRelayCommand(SimulateTriggerAsync);
        ManageMembersCommand = new AsyncRelayCommand(OpenMemberManagementAsync);
        OpenAddPlayerPageCommand = new AsyncRelayCommand(OpenAddPlayerPageAsync);
        _currentPlayerService = currentPlayerService;

        RemoveMemberCommand =
        new AsyncRelayCommand<GroupMemberListItem>(
            RemoveMemberAsync);

        WeakReferenceMessenger.Default.Register<GroupMembersChangedMessage>(
            this,
            async (recipient, message) =>
            {
                System.Diagnostics.Debug.WriteLine("MESSAGE RECEIVED");
                if (message.Value == GroupId)
                {
                    await LoadMembersAsync();
                }
            });
    }

    private async Task LoadMembersAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Gruppenmitglieder werden geladen...";

            // Prüfe (wenn nicht null oder "", " ") , ob eine GroupId vorhanden ist
            List<GroupMemberListItem> members;

            if (!string.IsNullOrWhiteSpace(GroupId))
            {
                // GroupId wird ans Repository gegeben 
                members = await _groupMemberRepository.GetMembersByGroupIdAsync(GroupId);
            }
            else
            {
                // Fallback 
                members = await _groupMemberRepository.GetMembersAsync();
            }

            System.Diagnostics.Debug.WriteLine($"LoadMembers: {members.Count}");

            MarkNextHost(members);

            Members.Clear();

            // prüfen, ob der aktuelle Spieler Mitglieder verwalten darf
            var canRemoveMembers = CanCurrentPlayerRemoveMembers(members);

            foreach (var member in members)
            {
                member.CanRemove = canRemoveMembers &&
                    member.PlayerId != _currentPlayerService.PlayerId;

                Members.Add(member);
            }

            StatusText = Members.Count == 0
                ? "Keine Gruppenmitglieder gefunden."
                : $"{Members.Count} Gruppenmitglieder geladen.";

            OnPropertyChanged(nameof(RotationMembers));
        }
        catch (Exception ex)
        {
            StatusText = "Fehler beim Laden der Gruppenmitglieder.";

            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Gruppenmitglieder konnten nicht geladen werden: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(GroupId))
            return;

        var group = await _groupMemberRepository.GetGroupByIdAsync(GroupId);

        if (group != null)
        {
            GroupName = group.Name;
        }
    }

    private static void MarkNextHost(List<GroupMemberListItem> members)
    {
        foreach (var member in members)
        {
            member.IsNextHost = false;
            member.HostedFlag = false;
        }

        var nextHost = members
            .Where(member => member.Status == "active")
            .OrderBy(member => member.RotationOrder ?? int.MaxValue)
            .ThenBy(member => member.PlayerName)
            .FirstOrDefault();

        if (nextHost is not null)
        {
            nextHost.IsNextHost = true;
        }
    }

    private async Task SelectNextHostAsync()
    {
        var nextHost = RotationMembers.FirstOrDefault();

        if (nextHost is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Keine Mitglieder",
                "Es gibt aktuell kein aktives Gruppenmitglied für die Gastgeberrotation.",
                "OK");

            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Nächster Gastgeber",
            $"Nach aktueller Rotation wäre '{nextHost.PlayerName}' der nächste Gastgeber.",
            "OK");
    }

    private async Task SimulateTriggerAsync()
    {
        var nextHost = RotationMembers.FirstOrDefault();

        if (nextHost is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Keine Mitglieder",
                "Es gibt aktuell kein aktives Gruppenmitglied für die Gastgeberrotation.",
                "OK");

            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Simulation",
            $"Simulation: '{nextHost.PlayerName}' wäre aktuell der nächste Gastgeber. Die echte Gastgeber-Historie wird später über Termine bzw. Events abgebildet.",
            "OK");
    }

    private async Task OpenMemberManagementAsync()
    {
        // mit Übergabe der enthaltenen GroupId
        await Shell.Current.GoToAsync($"{nameof(GroupManagementPage)}?groupId={GroupId}");
    }
    private async Task OpenAddPlayerPageAsync()
    {
        // mit Übergabe der enthaltenen GroupId
        await Shell.Current.GoToAsync(
            $"{nameof(AddPlayerPage)}?groupId={GroupId}");
    }

    // Ist der ausgewählte player admin oder owner ?
    private bool CanCurrentPlayerRemoveMembers(
        List<GroupMemberListItem> members)
    {
        var currentPlayerId = _currentPlayerService.PlayerId;

        var currentMember =
            members.FirstOrDefault(x =>
                x.PlayerId == currentPlayerId);

        if (currentMember == null)
            return false;

        return currentMember.Role == "owner"
            || currentMember.Role == "admin";
    }

    // Mitglied einer Gruppe entfernen
    private async Task RemoveMemberAsync(
      GroupMemberListItem member)
    {
        if (member == null)
            return;

        var confirm =
            await Shell.Current.DisplayAlertAsync(
                "Mitglied entfernen",
                $"Soll {member.PlayerName} wirklich entfernt werden?",
                "Ja",
                "Nein");

        if (!confirm)
            return;

        try
        {
            await _groupMemberRepository.SoftDeleteGroupMemberAsync(
                GroupId,
                member.PlayerId);

            // Sende Nachricht, dass sich die Mitgliederliste geändert hat
            WeakReferenceMessenger.Default.Send(
                 new GroupMembersChangedMessage(GroupId));
                
            Members.Remove(member);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                ex.Message,
                "OK");
        }
    }

    // zeig mir den aktuellen Stand der Seite 
    public async Task RefreshAsync()
    {
        await LoadMembersAsync();
    }

    // Unregister, wenn das ViewModel nicht mehr benötigt wird, um Memory Leaks zu vermeiden
    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}