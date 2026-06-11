using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Implementations
{
    public class HostSelectionService : IHostSelectionService
    {
        private readonly Random _random = new Random();

        public GroupMember SelectNextHost(List<GroupMember> members)
        {
            if (members == null || members.Count == 0)
                return null;

            // Letzter Host aus letzten Zyklus ermitteln
            var lastHost = members
                .OrderByDescending(m => m.LastHostedDate)
                .FirstOrDefault();

            var allHosted = members.All(m => m.HostedFlag);
            List<GroupMember> candidates;

            if (allHosted && lastHost != null)
            {
                // letzter Host wird ausgeschlossen
                candidates = members
                    .Where(m => m != lastHost)
                    .ToList();
            }
            else
            {
                // normaler Zyklus
                candidates = members
                    .Where(m => !m.HostedFlag)
                    .ToList();
            }

            if (!candidates.Any())
                return null;

            // zufällige Auswahl
            var selected = candidates[_random.Next(candidates.Count)];
            System.Diagnostics.Debug.WriteLine($"Selected: {selected?.DisplayName}");


            // Flags zurücksetzen / setzen
            foreach (var member in members)
            {
                member.IsNextHost = false;
            }

            selected.IsNextHost = true;

      /*      System.Diagnostics.Debug.WriteLine($"Hosted Flag: {selected?.HostedFlag}");
            System.Diagnostics.Debug.WriteLine($"IsNextHost Flag: {selected?.IsNextHost}"); */
            return selected;
        }
    }
}
