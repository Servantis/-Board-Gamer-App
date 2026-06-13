using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BoardGamerApp.Services.Services.Database;

namespace BoardGamerApp.ViewModels
{
    public class GroupMembersViewModel
    {
        private readonly IHostSelectionService _hostService;
        private readonly IHostScheduleService _scheduleService;
        private readonly PlayerRepository _playerRepository;

        public ObservableCollection<GroupMember> Members { get; private set; }

        public ICommand SelectNextHostCommand { get; }
        public ICommand SimulateTriggerCommand { get; }

        // Letzte Gastgeber ermitteln, wenn Flag gesetzt ist
        public IEnumerable<GroupMember> LastHosts =>
            Members
           .Where(m => m.LastHostedDate != default)
           .OrderByDescending(m => m.LastHostedDate);

        public GroupMembersViewModel(IHostSelectionService hostService, IHostScheduleService scheduleService, PlayerRepository playerRepository)
        {
            _hostService = hostService;
            _scheduleService = scheduleService;
            _playerRepository = playerRepository;

            // InitializeMembers();
            Members = new ObservableCollection<GroupMember>();

            _scheduleService.EnsureHost(Members.ToList());
            SelectNextHostCommand = new RelayCommand(SelectNextHost);
            SimulateTriggerCommand = new RelayCommand(SimulateTrigger);

            _ = LoadMembersAsync();
        }

        // Testdaten, später über DB oder Service
        private void InitializeMembers()
        {
            Members = new ObservableCollection<GroupMember>
            {
                new()
                {
                    Name = "Max",
                    LastName = "Mustermann",
                    Email = "max@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 5, 1),
                    IsNextHost = false
                },
                new()
                {
                    Name = "Anna",
                    LastName = "Meyer",
                    Email = "anna@test.de",
                    HostedFlag = false,
                    LastHostedDate = new DateTime(2023, 1, 1),
                    IsNextHost = false
                },
                new()
                {
                    Name = "Paul",
                    LastName = "Schmidt",
                    Email = "paul@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 3, 1),
                    IsNextHost = false
                },
                new()
                {
                    Name = "Tom",
                    LastName = "Tester",
                    Email = "tom@test.de",
                    HostedFlag = false,
                    LastHostedDate = new DateTime(2022, 1, 1),
                    IsNextHost = false
                },
                new()
                {
                    Name = "Richard",
                    LastName = "Müller",
                    Email = "richard@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 4, 1),
                    IsNextHost = false
                }
            };
        }
        private async Task LoadMembersAsync()
        {
            var players = await _playerRepository.GetPlayersAsync();
            System.Diagnostics.Debug.WriteLine("LoadMembers: " + players );
            Members.Clear();

            foreach (var player in players)
            {
                Members.Add(player);
                System.Diagnostics.Debug.WriteLine(
             $"{player.Name} | LastName='{player.LastName}'");
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

        public ICommand ManageMembersCommand { get; }

        public GroupMembersViewModel()
        {
            ManageMembersCommand = new RelayCommand(OpenMemberManagement);
        }

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
    }
}