using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;
using System.Globalization;

namespace BoardGamerApp.Services.Implementations
{
    public class HostScheduleService : IHostScheduleService
    {
        // Anzahl Tage zwischen dem Abschluss eines Termins und dem automatisch
        // angelegten Folgetermin (siehe CreateFollowUpGameNightIfNeededAsync).
        private const int FollowUpDays = 14;

        private readonly IHostSelectionService _selectionService;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly GameNightRepository _gameNightRepository;
        private readonly DatabaseService _databaseService;

        public HostScheduleService(
            IHostSelectionService selectionService,
            IGroupMemberRepository groupMemberRepository,
            GameNightRepository gameNightRepository,
            DatabaseService databaseService)
        {
            _selectionService = selectionService;
            _groupMemberRepository = groupMemberRepository;
            _gameNightRepository = gameNightRepository;
            _databaseService = databaseService;
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

        /// <summary>
        /// Legt automatisch einen neuen Termin an, FollowUpDays (14) Tage nach dem gerade
        /// abgeschlossenen Termin, mit dem neuen Gastgeber aber NUR, wenn für die Gruppe
        /// nicht ohnehin schon ein künftiger geplanter Termin existiert. Existiert bereits
        /// einer (z. B. weil manuell schon vorausgeplant wurde), übernimmt bereits
        /// ProcessHostChangeAsync (siehe AssignHostAsync) die Zuordnung des neuen
        /// Gastgebers zu diesem vorhandenen Termin hier ist dann nichts mehr zu tun.
        ///
        /// Muss NACH ProcessHostChangeAsync aufgerufen werden: dort wird der neue
        /// Gastgeber ausgewählt und als IsNextHost=true markiert (oder der bisherige
        /// IsNextHost bleibt unverändert, falls gerade kein Wechsel fällig war) in
        /// beiden Fällen lesen wir hier einfach den aktuell als IsNextHost markierten
        /// Gastgeber aus, statt die Auswahl-Logik zu duplizieren.
        ///
        /// Der Ort des neuen Termins ergibt sich genau wie bei der manuellen
        /// Terminanlage (siehe EventViewModel.GetOwnedLocationAsync) automatisch aus
        /// dem eigenen Ort des neuen Gastgebers in dieser Gruppe. Hat der neue Gastgeber
        /// (noch) keinen eigenen Ort hinterlegt, wird der Termin trotzdem angelegt, nur
        /// eben ohne Ort (LocationId bleibt null).
        /// </summary>
        public async Task CreateFollowUpGameNightIfNeededAsync(string groupId)
        {
            var nextGameNight = await _gameNightRepository
                .GetNextPlannedGameNightAsync(groupId);

            if (nextGameNight != null)
            {
                // Es gibt schon einen künftigen Termin für diese Gruppe der neue
                // Gastgeber wurde ihm bereits über AssignHostAsync zugewiesen.
                return;
            }

            var members = await _groupMemberRepository
                .GetGroupMembersByGroupIdAsync(groupId);

            var nextHost = members.FirstOrDefault(m => m.IsNextHost);

            if (nextHost == null)
            {
                // Ohne feststehenden nächsten Gastgeber können wir keinen sinnvollen
                // Folgetermin anlegen (sollte durch EnsureNextHostExistsAsync/
                // ProcessHostChangeAsync eigentlich immer schon gesetzt sein).
                return;
            }

            var locations = await _databaseService.GetNotDeletedAsync<GameLocation>();

            var ownedLocation = locations.FirstOrDefault(
                l => l.GroupId == groupId && l.OwnerPlayerId == nextHost.PlayerId);

            var referenceNight = await _gameNightRepository.GetLastCompletedGameNightAsync(groupId);

            var referenceDate = referenceNight != null
                ? ParseScheduledAt(referenceNight.ScheduledAt)
                : DateTime.Now;

            var scheduledAt = referenceDate
                .AddDays(FollowUpDays)
                .ToUniversalTime()
                .ToString("o");

            var followUpNight = new GameNight
            {
                GroupId = groupId,
                ScheduledAt = scheduledAt,
                LocationId = ownedLocation?.Id,
                HostPlayerId = nextHost.PlayerId,
                Status = BoardGamerConstants.GameNightStatus.Planned
            };

            await _gameNightRepository.AddAsync(followUpNight);
        }

        /// <summary>
        /// Wandelt den in der Datenbank gespeicherten ISO-8601-UTC-String (siehe
        /// GameNight.ScheduledAt) zurück in ein lokales DateTime analog zu
        /// EventViewModel.ParseDate, hier aber lokal in diesem Service benötigt, um auf
        /// dem Datum des abgeschlossenen Termins rechnen zu können (+14 Tage).
        /// </summary>
        private static DateTime ParseScheduledAt(string isoString)
        {
            return DateTime.Parse(isoString, null, DateTimeStyles.RoundtripKind)
                            .ToLocalTime();
        }
    }
}