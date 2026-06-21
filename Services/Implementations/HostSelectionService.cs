using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;

namespace BoardGamerApp.Services.Implementations
{
    public class HostSelectionService : IHostSelectionService
    {/*
        private readonly Random _random = new Random();

        public GroupMember SelectNextHost(List<GroupMember> members)
        {
            if (members == null || members.Count == 0)
                return null;

            var lastHost = members
                .OrderByDescending(m => m.LastHostedDate)
                .FirstOrDefault();

            var allHosted = members.All(m => m.HostedFlag);

            List<GroupMember> candidates;

            if (allHosted && lastHost != null)
            {
                candidates = members
                    .Where(m => m != lastHost)
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

            var selected = candidates[_random.Next(candidates.Count)];

            System.Diagnostics.Debug.WriteLine(
                $"Selected: {selected.DisplayName}");

            foreach (var member in members)
            {
                member.IsNextHost = false;
            }

            selected.IsNextHost = true;

            return selected;
        }*/
    }
}