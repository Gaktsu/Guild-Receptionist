#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildReceptionist.GameDesign.Domain
{
    public sealed class AdventurerRoster
    {
        private readonly Dictionary<string, AdventurerState> _byId = new(StringComparer.Ordinal);

        public IReadOnlyList<AdventurerState> GetAll()
        {
            return _byId.Values.ToList();
        }

        public void Add(AdventurerState adventurer)
        {
            if (adventurer is null)
            {
                throw new ArgumentNullException(nameof(adventurer));
            }

            if (_byId.ContainsKey(adventurer.AdventurerId))
            {
                throw new InvalidOperationException($"Adventurer already exists: {adventurer.AdventurerId}");
            }

            _byId[adventurer.AdventurerId] = adventurer;
        }

        public bool Remove(string adventurerId)
        {
            return _byId.Remove(adventurerId);
        }

        public AdventurerState? FindById(string adventurerId)
        {
            if (string.IsNullOrWhiteSpace(adventurerId))
            {
                return null;
            }

            _byId.TryGetValue(adventurerId, out var adventurer);
            return adventurer;
        }

        public IReadOnlyList<AdventurerState> GetIdleAdventurers()
        {
            return _byId.Values
                .Where(a => a.Availability == AdventurerAvailability.Idle)
                .ToList();
        }
    }
}
