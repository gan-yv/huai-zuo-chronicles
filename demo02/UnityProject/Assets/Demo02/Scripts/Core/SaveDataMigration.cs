using System;
using System.Collections.Generic;

namespace Demo02.Core
{
    /// <summary>
    /// Applies one-way save upgrades. Every released save format adds a new
    /// sequential step instead of rewriting all historical readers.
    /// </summary>
    public static class SaveDataMigration
    {
        private static readonly Dictionary<int, Action<WorldSaveData>>
            UpgradeSteps = new()
            {
                [1] = UpgradeVersionOne,
                [2] = UpgradeVersionTwo
            };

        private static readonly Dictionary<string, string> StableIdAliases =
            new(StringComparer.Ordinal)
            {
                ["clue-salt-permit"] = "old-street-sign",
                ["clue-ledger"] = "salt-ledger",
                ["echo-coin"] = "salt-token"
            };

        public static int CurrentVersion => WorldSaveData.CurrentVersion;

        public static bool TryUpgrade(
            WorldSaveData data,
            int loadedVersion,
            out string error)
        {
            error = null;
            if (data == null)
            {
                error = "Save payload is empty.";
                return false;
            }

            int version = loadedVersion > 0 ? loadedVersion : 1;
            if (version > CurrentVersion)
            {
                error =
                    $"Save version {version} is newer than supported " +
                    $"version {CurrentVersion}.";
                return false;
            }

            while (version < CurrentVersion)
            {
                if (!UpgradeSteps.TryGetValue(version, out Action<WorldSaveData> upgrade))
                {
                    error = $"No save upgrade path from version {version}.";
                    return false;
                }

                upgrade(data);
                version++;
                data.Version = version;
            }

            NormalizeCollections(data);
            if (string.IsNullOrWhiteSpace(data.SceneName))
            {
                error = "Save has no scene name.";
                return false;
            }

            data.Version = CurrentVersion;
            return true;
        }

        private static void UpgradeVersionOne(WorldSaveData data)
        {
            NormalizeCollections(data);
            MigrateIds(data.ClueIds);
            MigrateIds(data.TimeAnchorIds);
            MigrateIds(data.EchoIds);
            MigrateIds(data.StoryFactIds);
            MigrateIds(data.CollectedPickupIds);
            MigrateIds(data.UnlockedAnchorIds);
            MigrateIds(data.CompletedStreamingRegionIds);
            MigrateIds(data.CompletedEncounterIds);
            MigrateIds(data.CompletedWorldContentIds);
            MigrateIds(data.StartedWorldContentIds);
            foreach (InventoryItemSaveData item in data.InventoryItems)
            {
                item.ItemId = MigrateId(item.ItemId);
            }
        }

        private static void UpgradeVersionTwo(WorldSaveData data)
        {
            NormalizeCollections(data);
            // Version 2 identified combatants by object name. Keep that field
            // as a fallback while new saves write scene-scoped persistent IDs.
            foreach (CombatantSaveData combatant in data.Combatants)
            {
                combatant.PersistentId ??= string.Empty;
            }
        }

        private static void MigrateIds(List<string> ids)
        {
            for (int index = 0; index < ids.Count; index++)
            {
                ids[index] = MigrateId(ids[index]);
            }
            Deduplicate(ids);
        }

        private static string MigrateId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && StableIdAliases.TryGetValue(id, out string replacement)
                ? replacement
                : id;
        }

        private static void NormalizeCollections(WorldSaveData data)
        {
            data.ClueIds ??= new List<string>();
            data.TimeAnchorIds ??= new List<string>();
            data.EchoIds ??= new List<string>();
            data.StoryFactIds ??= new List<string>();
            data.InventoryItems ??= new List<InventoryItemSaveData>();
            data.CollectedPickupIds ??= new List<string>();
            data.UnlockedAnchorIds ??= new List<string>();
            data.CompletedStreamingRegionIds ??= new List<string>();
            data.CompletedEncounterIds ??= new List<string>();
            data.CompletedWorldContentIds ??= new List<string>();
            data.StartedWorldContentIds ??= new List<string>();
            data.Combatants ??= new List<CombatantSaveData>();
            data.InventoryItems.RemoveAll(item => item == null);
            data.Combatants.RemoveAll(item => item == null);
        }

        private static void Deduplicate(List<string> ids)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            ids.RemoveAll(id => string.IsNullOrWhiteSpace(id)
                || !seen.Add(id));
        }
    }
}
