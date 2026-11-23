using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Combat
{
    [DefaultExecutionOrder(-10)]
    public sealed class WorldEncounterDirector : MonoBehaviour
    {
        private readonly Dictionary<string, WorldEncounterZone> zones = new();
        private readonly HashSet<string> pendingSupportIds = new();

        public static WorldEncounterDirector Instance { get; private set; }
        public int RegisteredZoneCount => zones.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(WorldEncounterZone zone)
        {
            if (zone?.Definition == null
                || string.IsNullOrWhiteSpace(zone.Definition.EncounterId))
            {
                return;
            }

            zones[zone.Definition.EncounterId] = zone;
            if (pendingSupportIds.Remove(zone.Definition.EncounterId))
            {
                zone.Activate();
            }
        }

        public void Unregister(WorldEncounterZone zone)
        {
            if (zone?.Definition != null
                && zones.TryGetValue(
                    zone.Definition.EncounterId,
                    out WorldEncounterZone current
                )
                && current == zone)
            {
                zones.Remove(zone.Definition.EncounterId);
            }
        }

        public void NotifyActivated(WorldEncounterZone zone)
        {
            if (zone?.Definition == null)
            {
                return;
            }

            foreach (string supportId in zone.Definition.SupportEncounterIds)
            {
                if (string.IsNullOrWhiteSpace(supportId)
                    || supportId == zone.Definition.EncounterId)
                {
                    continue;
                }

                if (zones.TryGetValue(
                        supportId,
                        out WorldEncounterZone support
                    ))
                {
                    support.Activate();
                }
                else
                {
                    pendingSupportIds.Add(supportId);
                }
            }
        }
    }
}
