using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;

namespace BoardGamerApp.Services.Implementations
{
    public class HostScheduleService : IHostScheduleService
    {
        private readonly IHostSelectionService _selectionService;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly GameNightRepository _gameNightRepository;

        public HostScheduleService(
            IHostSelectionService selectionService,
            IGroupMemberRepository groupMemberRepository,
            GameNightRepository gameNightRepository)
        {
            _selectionService = selectionService;
            _groupMemberRepository = groupMemberRepository;
            _gameNightRepository = gameNightRepository;
        }

        public async Task ProcessHostChangeAsync(string groupId)
        {
             Debug.WriteLine( $"[HOST] ProcessHostChangeAsync gestartet ({groupId})");

            // Mitglieder der Gruppe laden
            var members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            Debug.WriteLine("[HOST STATE BEFORE PROCESS]");
            foreach (var m in members)
            {
                
                Debug.WriteLine(
                    $"[HOST] Player  => " +
                    $"{m.PlayerId} " +
                    $"Hosted={m.HostedFlag} " +
                    $"Next={m.IsNextHost}");
                
            }

            var currentHost = members.FirstOrDefault(m => m.IsNextHost);

            Debug.WriteLine(
                $"[CURRENT HOST] => {currentHost?.PlayerId}");

            // Sollte niemals eintreten, da EnsureNextHostExistsAsync()
            // vorher ausgeführt wird.
            if (!members.Any(m => m.IsNextHost))
            {
                
                Debug.WriteLine(
                    "[HOST] Abbruch: Kein NextHost vorhanden.");
                
                return;
            }

            // Bei Aufruf der ProcessHostChange-Methode (wenn night.Status = completed)
            // den aktuellen Host abschließen
            var currentNextHost =
                    members.FirstOrDefault(m => m.IsNextHost);

                Debug.WriteLine(
                    $"[HOST] Aktueller Host => " +
                    $"{currentNextHost?.PlayerId}");

                if (currentNextHost != null)
                {
                    currentNextHost.HostedFlag = true;
                    currentNextHost.IsNextHost = false;

                    Debug.WriteLine(
                        $"[HOST] Setze Host abgeschlossen => " +
                        $"{currentNextHost.PlayerId}");

                    await _groupMemberRepository
                        .UpdateMemberAsync(currentNextHost);
                }
            
            // Aktuellen Stand neu laden
            members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            var cycleCompleted =
                   members.Count > 0 &&
                   members.All(m => m.HostedFlag);

             Debug.WriteLine(  $"[HOST] Zyklus abgeschlossen => {cycleCompleted}");


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

                 Debug.WriteLine(   "[HOST] Alle HostedFlags wurden zurückgesetzt.");

                // Stand nach Reset neu laden
                members = await _groupMemberRepository
                    .GetGroupMembersByGroupIdAsync(groupId);
            }

            // Nur auswählen, wenn aktuell niemand als nächster Host markiert ist
            if (!members.Any(m => m.IsNextHost))
            {
                var lastHostPlayerId = await _gameNightRepository
                                       .GetLastCompletedHostPlayerIdAsync(groupId);
                
                    Debug.WriteLine(
                        $"[HOST] Letzter abgeschlossener Host => " +
                        $"{lastHostPlayerId}");
                

                var selectedHost =
                    _selectionService.SelectNextHost(members, lastHostPlayerId,cycleCompleted);
                
                Debug.WriteLine(
                    $"[HOST] Neuer Host => " +
                    $"{selectedHost?.PlayerId}");
                
                if (selectedHost != null)
                {
                    await _groupMemberRepository
                        .UpdateMemberAsync(selectedHost);

                    var nextGameNight =
                        await _gameNightRepository
                            .GetNextPlannedGameNightAsync(groupId);

                    if (nextGameNight != null)
                    {
                        
                        Debug.WriteLine(
                            $"[HOST] Trage Host in nächste GameNight ein => " +
                            $"{selectedHost.PlayerId}");
                        
                        await _gameNightRepository.AssignHostAsync(
                            nextGameNight.Id,
                            selectedHost.PlayerId);
                    }
                }
            }
        }

        // Wenn kein Host (z.B. bei Gruppenerstellung) gesetzt ist,
        // setze den Owner der Gruppe als nächsten Host
        public async Task EnsureNextHostExistsAsync(string groupId)
        {
            var members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            if (members.Any(m => m.IsNextHost))
            {
                
                Debug.WriteLine(
                    "[HOST] NextHost bereits vorhanden.");
                
                return;
            }

            var owner = members.FirstOrDefault(
                m => m.Role == BoardGamerConstants.GroupRoles.Owner);

            if (owner == null)
            {
                 Debug.WriteLine(   "[HOST] Kein Owner gefunden.");

                return;
            }

            Debug.WriteLine(
    $"[ENSURE] Setze Owner als NextHost => {owner.PlayerId}");
            owner.IsNextHost = true;

            await _groupMemberRepository
                .UpdateMemberAsync(owner);

             Debug.WriteLine(   $"[HOST] Owner als erster Host gesetzt: {owner.PlayerId}");
        }
    }
}