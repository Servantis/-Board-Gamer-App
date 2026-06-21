using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BoardGamerApp.ViewModels;

public class GroupMembersViewModel : ObservableObject
{
    private readonly IGroupMemberRepository _groupMemberRepository;

    private bool _isBusy;
    private string _statusText = "Gruppenmitglieder werden geladen...";

    public ObservableCollection<GroupMemberListItem> Members { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand SelectNextHostCommand { get; }
    public ICommand SimulateTriggerCommand { get; }
    public ICommand ManageMembersCommand { get; }

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

    public GroupMembersViewModel(IGroupMemberRepository groupMemberRepository)
    {
        _groupMemberRepository = groupMemberRepository;

        RefreshCommand = new AsyncRelayCommand(LoadMembersAsync);
        SelectNextHostCommand = new AsyncRelayCommand(SelectNextHostAsync);
        SimulateTriggerCommand = new AsyncRelayCommand(SimulateTriggerAsync);
        ManageMembersCommand = new AsyncRelayCommand(OpenMemberManagementAsync);

        _ = LoadMembersAsync();
    }

    private async Task LoadMembersAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Gruppenmitglieder werden geladen...";

            var members = await _groupMemberRepository.GetMembersAsync();

            System.Diagnostics.Debug.WriteLine($"LoadMembers: {members.Count}");

            MarkNextHost(members);

            Members.Clear();

            foreach (var member in members)
            {
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
        await Shell.Current.GoToAsync(nameof(GroupManagementPage));
    }
}