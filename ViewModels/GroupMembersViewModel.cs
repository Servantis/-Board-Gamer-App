using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardGamerApp.ViewModels
{
    public class GroupMembersViewModel : ObservableObject
    {
        private readonly IHostSelectionService _hostService;
        private readonly IHostScheduleService _scheduleService;
        private readonly IPlayerService _playerService;

        public ObservableCollection<GroupMember> Members { get; private set; }

        public ICommand SelectNextHostCommand { get; }
        public ICommand SimulateTriggerCommand { get; }

        // Letzte Gastgeber ermitteln, wenn Flag gesetzt ist
        public IEnumerable<GroupMember> LastHosts =>
            Members
            .Where(m => m.HostedFlag)
        .OrderByDescending(m => m.LastHostedDate);

        public GroupMembersViewModel(IHostSelectionService hostService, IHostScheduleService scheduleService, IPlayerService playerService)
        {
            _hostService = hostService;
            _scheduleService = scheduleService;
            _playerService = playerService;

            // Initialize Members
            Members = new ObservableCollection<GroupMember>();

            SelectNextHostCommand = new RelayCommand(SelectNextHost);
            SimulateTriggerCommand = new RelayCommand(SimulateTrigger);

            ManageMembersCommand = new RelayCommand(OpenMemberManagement);

            _ = LoadMembersAsync();
        }

        private async Task LoadMembersAsync()
        {
            var players = await _playerService.GetPlayersAsync();
            System.Diagnostics.Debug.WriteLine("LoadMembers: " + players );
            Members.Clear();

            foreach (var player in players)
            {
                Members.Add(player);
            }
        }

        private void SelectNextHost()
        {
            _hostService.SelectNextHost(Members.ToList());
        }

        public ICommand ManageMembersCommand { get; }

        // Aufruf zur MemberManagementPage
        private async void OpenMemberManagement()
        {
            // Komponente muss noch implementiert werden
           // await Shell.Current.GoToAsync(nameof(MemberManagementPage));
        }

        private async void SimulateTrigger()
        {
            _scheduleService.ProcessHostChange(Members.ToList());

            OnPropertyChanged(nameof(LastHosts));

            await _playerService.SavePlayersAsync(Members);
        }
    }
}