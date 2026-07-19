using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;

namespace BoardGamerApp.Services.Implementations
{
    public class HostSelectionService : IHostSelectionService
    {
        private readonly Random _random = new Random();

        public GroupMember SelectNextHost(List<GroupMember> members,
                                            string? lastHostPlayerId,
                                            bool cycleCompleted)
        {
            /*
            Debug.WriteLine(
                $"[SELECT] LastHostPlayerId => " +
                $"{lastHostPlayerId}");
            */
            if (members == null || members.Count == 0)
                return null;

            // Mitgliederliste
            List<GroupMember> candidates;

            // Verhindere, dass der letzte Host nach Reset direkt neu gewählt wird

            if (cycleCompleted &&
                !string.IsNullOrWhiteSpace(lastHostPlayerId))
            {
                candidates = members
                    .Where(m => m.PlayerId != lastHostPlayerId)
                    .ToList();
                /*
                Debug.WriteLine(
                    "[SELECT] Neuer Zyklus erkannt.");

                Debug.WriteLine(
                    $"[SELECT] Letzter Host ausgeschlossen => " +
                    $"{lastHostPlayerId}");
                */
            }
            else
            {
                candidates = members
                    .Where(m => !m.HostedFlag)
                    .ToList();
            }


            foreach (var candidate in candidates)
            {
                /*
                Debug.WriteLine(
                    $"[SELECT] Kandidat => " +
                    $"{candidate.PlayerId}");
                */
            }


            if (!candidates.Any())
                return null;

            // lege zufällig den nächsten Host fest
            var selected = candidates[_random.Next(candidates.Count)];

            foreach (var member in members)
            {
                member.IsNextHost = false;
            }

            selected.IsNextHost = true;
           // Debug.WriteLine($"[HOST] Neuer Host: {selected.PlayerId}");

            return selected;
        }
    }
}