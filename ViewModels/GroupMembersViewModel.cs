using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BoardGamerApp.ViewModels;

public class GroupMembersViewModel : ObservableObject
{
    private readonly IHostSelectionService _hostService;
    private readonly IHostScheduleService _scheduleService;
    private readonly IGroupMemberRepository _groupMemberRepository;

    public ObservableCollection<GroupMember> Members { get; private set; }

    public ICommand SelectNextHostCommand { get; }
    public ICommand SimulateTriggerCommand { get; }
    public ICommand ManageMembersCommand { get; }

    public IEnumerable<GroupMember> LastHosts =>
        Members
            .Where(member => member.HostedFlag)
            .OrderByDescending(member => member.LastHostedDate);

    public GroupMembersViewModel(
        IHostSelectionService hostService,
        IHostScheduleService scheduleService,
        IGroupMemberRepository groupMemberRepository)
    {
        _hostService = hostService;
        _scheduleService = scheduleService;
        _groupMemberRepository = groupMemberRepository;

        Members = new ObservableCollection<GroupMember>();

        SelectNextHostCommand = new RelayCommand(SelectNextHost);
        SimulateTriggerCommand = new RelayCommand(SimulateTrigger);
        ManageMembersCommand = new RelayCommand(OpenMemberManagement);

        _ = LoadMembersAsync();
    }

    private async Task LoadMembersAsync()
    {
        var members = await _groupMemberRepository.GetMembersAsync();

        System.Diagnostics.Debug.WriteLine($"LoadMembers: {members.Count}");

        Members.Clear();

        foreach (var member in members)
        {
            Members.Add(member);
        }

        OnPropertyChanged(nameof(LastHosts));
    }

    private void SelectNextHost()
    {
        _hostService.SelectNextHost(Members.ToList());

        OnPropertyChanged(nameof(LastHosts));
    }

    private async void OpenMemberManagement()
    {
        await Shell.Current.GoToAsync(nameof(GroupManagementPage));
    }

    private async void SimulateTrigger()
    {
        _scheduleService.ProcessHostChange(Members.ToList());

        OnPropertyChanged(nameof(LastHosts));

        await _groupMemberRepository.SaveMembersAsync(Members);
    }
}