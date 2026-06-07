using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Implementations
{
    public class HostScheduleService: IHostScheduleService
    {
        private readonly IHostSelectionService _selectionService;
        private readonly IGameNightTrigger _trigger;

        private bool _isHostSet = false;

        public HostScheduleService(
            IHostSelectionService selectionService,
            IGameNightTrigger trigger)
        {
            _selectionService = selectionService;
            _trigger = trigger;

        }

        public void EnsureHost(List<GroupMember> members)
        {
            //  Wenn geplanter Termin vorbei: Flag-Zustand Änderung für letzten Host
            if (_trigger.IsGameNightOver())
            {
                System.Diagnostics.Debug.WriteLine("TRIGGER FIRED");
                var currentHost = members.FirstOrDefault(m => m.IsNextHost);

                if (currentHost != null)
                {
                    currentHost.HostedFlag = true;
                    currentHost.IsNextHost = false;
                }

                _isHostSet = false;
            }

            // Wenn alle einmal Gastgeber waren reset des HostedFlags
            if (members.All(m => m.HostedFlag))
            {
                foreach (var m in members)
                {
                    m.HostedFlag = false;
                }
            }

            // Nur einmal pro Zyklus berechnen
            if (!_isHostSet)
            {
                var selected = _selectionService.SelectNextHost(members);

                if (selected != null)
                {
                    selected.IsNextHost = true;
                    _isHostSet = true;
                }
            }
        }
    }
}
