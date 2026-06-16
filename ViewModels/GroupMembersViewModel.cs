using BoardGamerApp.Data;
using BoardGamerApp.Models;
using BoardGamerApp.Services.Implementations;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.Services.Services.Database;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BoardGamerApp.ViewModels
{
    public class GroupMembersViewModel
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
           .Where(m => m.LastHostedDate != default)
           .OrderByDescending(m => m.LastHostedDate);

        public GroupMembersViewModel(IHostSelectionService hostService, IHostScheduleService scheduleService, IPlayerService playerService)
        {
            _hostService = hostService;
            _scheduleService = scheduleService;
            _playerService = playerService;

            // InitializeMembers();
            Members = new ObservableCollection<GroupMember>();

            _scheduleService.EnsureHost(Members.ToList());
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

                // Änderungen an den Eigenschaften eines Mitglieds überwachen
                player.PropertyChanged += OnMemberPropertyChanged;
                Members.Add(player);
            }

            _scheduleService.EnsureHost(Members.ToList());
        }

        // zentrale Datenzugriffsmethode (später über DB)
        public List<GroupMember> GetMembers()
        {
            return Members.ToList();
        }

        private void SelectNextHost()
        {
            var selected = _hostService.SelectNextHost(Members.ToList());
          
            if (selected != null)
            {
                foreach (var m in Members)
                    m.IsNextHost = false;

                selected.IsNextHost = true;
            }
        }


        private async Task PersistHostStateAsync()
        {
            await _playerService.SavePlayersAsync(Members);
        }

        public ICommand ManageMembersCommand { get; }

        // Aufruf zur MemberManagementPage
        private async void OpenMemberManagement()
        {
            // Komponente muss noch implementiert werden
           // await Shell.Current.GoToAsync(nameof(MemberManagementPage));
        }

        private void SimulateTrigger()
        {
            _scheduleService.EnsureHost(Members.ToList());
        }

        private async void OnMemberPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (sender is not GroupMember member)
                return;
            System.Diagnostics.Debug.WriteLine(
    $"PropertyChanged: {member.Name} -> {e.PropertyName}");

            await _playerService.SavePlayerAsync(member);

            // DEBUGGING: gibt mir das aus, was in der Tabelle plyer persistiert wurde
            var reloaded = await _playerService.GetPlayerByIdAsync(member.Id);

            System.Diagnostics.Debug.WriteLine(
                $"DB CHECK → {reloaded.Name} | Hosted={reloaded.HostedFlag} | Next={reloaded.IsNextHost} | DatumType ={reloaded.LastHostedDate}");

        }
    }
}