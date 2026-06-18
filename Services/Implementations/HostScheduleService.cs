using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;

namespace BoardGamerApp.Services.Implementations
{
    public class HostScheduleService : IHostScheduleService
    {
        private readonly IHostSelectionService _selectionService;
        private readonly IGameNightTrigger _trigger;

        public HostScheduleService(
            IHostSelectionService selectionService,
            IGameNightTrigger trigger)
        {
            _selectionService = selectionService;
            _trigger = trigger;
        }

        public void ProcessHostChange(List<GroupMember> members)
        {
            // Wenn geplanter Termin vorbei: letzten Host abschließen
            if (_trigger.IsGameNightOver())
            {
                var currentHost = members.FirstOrDefault(m => m.IsNextHost);

                if (currentHost != null)
                {
                    currentHost.HostedFlag = true;
                    currentHost.IsNextHost = false;
                    currentHost.LastHostedDate = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine(
                        $"Hostname: {currentHost.Name}");

                    System.Diagnostics.Debug.WriteLine(
                        $"LastHostedDate: {currentHost.LastHostedDate}");
                }
            }

            // Wenn alle einmal Gastgeber waren -> Reset
            if (members.All(m => m.HostedFlag))
            {
                foreach (var m in members)
                {
                    m.HostedFlag = false;
                }
            }

            // Nur auswählen, wenn aktuell niemand als nächster Host markiert ist
            if (!members.Any(m => m.IsNextHost))
            {
                _selectionService.SelectNextHost(members);
            }
        }
    }
}