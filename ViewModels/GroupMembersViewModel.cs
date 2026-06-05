using BoardGamerApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BoardGamerApp.ViewModels
{
    public class GroupMembersViewModel
    {
        public ObservableCollection<GroupMember> Members { get; }

        public GroupMembersViewModel()
        {
            LastHosts = new ObservableCollection<GroupMember>(
                Members.Where(m => m.HostedFlag));
            // später Service aufruf implementieren
            // z.B. Members = await _groupService.GetMembersAsync();
            Members = new ObservableCollection<GroupMember>
            {
                new ()
                {
                    Name = "Max",
                    LastName = "Mustermann",
                    Email = "max@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 5, 1)
                },

                new()
                {
                    Name = "Anna",
                    LastName = "Meyer",
                    Email = "anna@test.de",
                    HostedFlag = false,
                    LastHostedDate = new DateTime()
                },

                new()
                {
                    Name = "Paul",
                    LastName = "Schmidt",
                    Email = "paul@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 3, 1)
                },
                   new()
                {
                    Name = "Tom",
                    LastName = "Tester",
                    Email = "tom@test.de",
                    HostedFlag = false,
                    LastHostedDate = new DateTime()
                },

                new()
                {
                    Name = "Richard",
                    LastName = "Müller",
                    Email = "richard@test.de",
                    HostedFlag = true,
                    LastHostedDate = new DateTime(2026, 4, 1)
                }
            };
        }
        public ObservableCollection<GroupMember> LastHosts { get; }
    }
}
