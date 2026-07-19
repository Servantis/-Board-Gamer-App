using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;

namespace BoardGamerApp.Services.Implementations
{
    public class HostScheduleService : IHostScheduleService
    {
        private readonly IHostSelectionService _selectionService;
        private readonly IGameNightTrigger _trigger;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly GameNightRepository _gameNightRepository;

        public HostScheduleService(
            IHostSelectionService selectionService,
            IGameNightTrigger trigger,
            IGroupMemberRepository groupMemberRepository,
            GameNightRepository gameNightRepository)
        {
            _selectionService = selectionService;
            _trigger = trigger;
            _groupMemberRepository = groupMemberRepository;
            _gameNightRepository = gameNightRepository;
        }

        public async Task ProcessHostChangeAsync(string groupId)
        {
            // Debug.WriteLine( $"[HOST] ProcessHostChangeAsync gestartet ({groupId})");

            // Mitglieder der Gruppe laden
            var members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            foreach (var m in members)
            {
                /*
                Debug.WriteLine(
                    $"[HOST] MEMBER => " +
                    $"{m.PlayerId} " +
                    $"Hosted={m.HostedFlag} " +
                    $"Next={m.IsNextHost}");
                */
            }

            // Sicherheitsprüfung:
            // Sollte niemals eintreten, da EnsureNextHostExistsAsync()
            // vorher ausgeführt werden sollte.
            if (!members.Any(m => m.IsNextHost))
            {
                /*
                Debug.WriteLine(
                    "[HOST] Abbruch: Kein NextHost vorhanden.");
                */
                return;
            }

            // Wenn geplanter Termin vorbei: aktuellen Host abschließen
            if (_trigger.IsGameNightOver())
            {
                var currentHost =
                    members.FirstOrDefault(m => m.IsNextHost);
                /*
                                Debug.WriteLine(
                                    $"[HOST] Aktueller Host => " +
                                    $"{currentHost?.PlayerId}");
                */
                if (currentHost != null)
                {
                    currentHost.HostedFlag = true;
                    currentHost.IsNextHost = false;
/*
                    Debug.WriteLine(
                        $"[HOST] Setze Host abgeschlossen => " +
                        $"{currentHost.PlayerId}");
*/
                    await _groupMemberRepository
                        .UpdateMemberAsync(currentHost);
                }
            }

            // Aktuellen Stand neu laden
            members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            var cycleCompleted =
                   members.Count > 0 &&
                   members.All(m => m.HostedFlag);

            // Debug.WriteLine(  $"[HOST] Zyklus abgeschlossen => {cycleCompleted}");


            // Wenn alle einmal Gastgeber waren -> Reset
            if (members.Count > 0 &&
                members.All(m => m.HostedFlag))
            {
                foreach (var m in members)
                {
                    m.HostedFlag = false;

                    await _groupMemberRepository
                        .UpdateMemberAsync(m);
                }

                // Debug.WriteLine(   "[HOST] Alle HostedFlags wurden zurückgesetzt.");

                // Stand nach Reset neu laden
                members = await _groupMemberRepository
                    .GetGroupMembersByGroupIdAsync(groupId);
            }

            // Nur auswählen, wenn aktuell niemand als nächster Host markiert ist
            if (!members.Any(m => m.IsNextHost))
            {
                var lastHostPlayerId = await _gameNightRepository
                                       .GetLastCompletedHostPlayerIdAsync(groupId);
                /*
                    Debug.WriteLine(
                        $"[HOST] Letzter abgeschlossener Host => " +
                        $"{lastHostPlayerId}");
                */

                var selectedHost =
                    _selectionService.SelectNextHost(members, lastHostPlayerId,cycleCompleted);
                /*
                Debug.WriteLine(
                    $"[HOST] Neuer Host => " +
                    $"{selectedHost?.PlayerId}");
                */
                if (selectedHost != null)
                {
                    await _groupMemberRepository
                        .UpdateMemberAsync(selectedHost);

                    var nextGameNight =
                        await _gameNightRepository
                            .GetNextPlannedGameNightAsync(groupId);

                    if (nextGameNight != null)
                    {
                        /*
                        Debug.WriteLine(
                            $"[HOST] Trage Host in nächste GameNight ein => " +
                            $"{selectedHost.PlayerId}");
                        */
                        await _gameNightRepository.AssignHostAsync(
                            nextGameNight.Id,
                            selectedHost.PlayerId);
                    }
                }
            }
        }

        public async Task EnsureNextHostExistsAsync(string groupId)
        {
            var members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            if (members.Any(m => m.IsNextHost))
            {
                /*
                Debug.WriteLine(
                    "[HOST] NextHost bereits vorhanden.");
                */
                return;
            }

            var owner = members.FirstOrDefault(
                m => m.Role == BoardGamerConstants.GroupRoles.Owner);

            if (owner == null)
            {
                // Debug.WriteLine(   "[HOST] Kein Owner gefunden.");

                return;
            }

            owner.IsNextHost = true;

            await _groupMemberRepository
                .UpdateMemberAsync(owner);

            // Debug.WriteLine(   $"[HOST] Owner als erster Host gesetzt: {owner.PlayerId}");
        }
    }
}