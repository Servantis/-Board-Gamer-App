using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using System.Diagnostics;

namespace BoardGamerApp.Services.Implementations
{
    public class HostSelectionService : IHostSelectionService
    {
        private readonly Random _random = new Random();

        // Liefert den nächsten Host eines Spieletermins
        public GroupMember SelectNextHost(List<GroupMember> members,
                                            string? lastHostPlayerId,
                                            bool cycleCompleted)
        {

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
            }
            else
            {
                candidates = members
                    .Where(m => !m.HostedFlag)
                    .ToList();
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

            return selected;
        }
    }
}