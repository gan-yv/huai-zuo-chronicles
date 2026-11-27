using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Demo02.Editor
{
    internal static class Demo02SaveRegressionSuite
    {
        public static void VerifySaveSlotsForBatch()
        {
            string testDirectory = Path.Combine(
                Application.temporaryCachePath,
                "Demo02SaveSlotVerification"
            );
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }

            Directory.CreateDirectory(testDirectory);
            SaveSystem.SaveDirectoryOverrideForTests = testDirectory;
            try
            {
                string legacyPath = Path.Combine(
                    testDirectory,
                    "demo02-save.json"
                );
                WriteTestSave(
                    legacyPath,
                    null
                );
                File.SetLastWriteTimeUtc(
                    legacyPath,
                    new System.DateTime(
                        2026,
                        11,
                        21,
                        10,
                        0,
                        0,
                        System.DateTimeKind.Utc
                    )
                );
                if (!SaveSystem.HasSave()
                    || !SaveSystem.TryReadSave(
                        SaveSystem.AutoSlot,
                        out WorldSaveData migratedData
                    )
                    || string.IsNullOrWhiteSpace(migratedData.SavedAtUtc)
                    || migratedData.Version
                        != WorldSaveData.CurrentVersion
                    || !migratedData.ClueIds.Contains("old-street-sign")
                    || !migratedData.ClueIds.Contains("salt-ledger")
                    || !migratedData.EchoIds.Contains("salt-token")
                    || File.Exists(legacyPath) == false)
                {
                    throw new System.InvalidOperationException(
                        "Legacy save migration to the automatic slot failed."
                    );
                }

                WriteTestSave(
                    SaveSystem.GetSlotPathForTests(1),
                    "2026-11-21T11:00:00.0000000Z"
                );
                WriteTestSave(
                    SaveSystem.GetSlotPathForTests(2),
                    "2026-11-21T12:00:00.0000000Z"
                );
                WriteTestSave(
                    SaveSystem.GetSlotPathForTests(3),
                    "2026-11-21T13:00:00.0000000Z"
                );

                for (int slot = 1; slot <= SaveSystem.ManualSlotCount; slot++)
                {
                    if (!SaveSystem.TryReadSave(slot, out WorldSaveData data)
                        || data.Version != WorldSaveData.CurrentVersion
                        || data.SceneName != SceneNames.Guangling
                        || data.ClueIds.Count != 2
                        || data.TimeAnchorIds.Count != 1
                        || data.EchoIds.Count != 1
                        || data.StoryFactIds.Count != 1
                        || data.StoryFactIds[0] != "teahouse-cart-testimony"
                        || data.InventoryItems.Count != 1
                        || data.InventoryItems[0].ItemId != "ginger-tea"
                        || data.InventoryItems[0].Count != 2
                        || data.CollectedPickupIds.Count != 1
                        || data.UnlockedAnchorIds.Count != 2
                        || data.CompletedStreamingRegionIds.Count != 1
                        || data.CompletedEncounterIds.Count != 1
                        || data.CompletedWorldContentIds.Count != 1
                        || data.StartedWorldContentIds.Count != 1)
                    {
                        throw new System.InvalidOperationException(
                            $"Manual slot {slot} did not round-trip correctly."
                        );
                    }
                }

                if (!SaveSystem.TryReadLatestSave(
                        out _,
                        out int latestSlot
                    )
                    || latestSlot != 3)
                {
                    throw new System.InvalidOperationException(
                        "Latest-save selection did not choose manual slot 3."
                    );
                }

                string slotOnePath =
                    SaveSystem.GetSlotPathForTests(1);
                if (!SaveSystem.TryReadSave(
                        1,
                        out WorldSaveData currentSave
                    ))
                {
                    throw new System.InvalidOperationException(
                        "Could not prepare current-format save verification."
                    );
                }
                SaveSystem.WriteSaveFileForTests(
                    slotOnePath,
                    currentSave
                );
                string envelopeJson = File.ReadAllText(slotOnePath);
                if (!envelopeJson.Contains(
                        "\"Format\": \"Demo02.Save\""
                    )
                    || !envelopeJson.Contains("\"Checksum\"")
                    || !SaveSystem.TryReadSave(1, out _))
                {
                    throw new System.InvalidOperationException(
                        "Current save format or checksum validation failed."
                    );
                }

                File.WriteAllText(slotOnePath, "{ damaged");
                if (!SaveSystem.TryReadSave(1, out WorldSaveData recovered)
                    || recovered.Version != WorldSaveData.CurrentVersion
                    || !File.ReadAllText(slotOnePath).Contains(
                        "\"Checksum\""
                    ))
                {
                    throw new System.InvalidOperationException(
                        "Damaged save backup recovery or self-heal failed."
                    );
                }

                WriteTestSave(
                    SaveSystem.GetSlotPathForTests(2),
                    "2026-11-21T12:00:00.0000000Z",
                    version: WorldSaveData.CurrentVersion + 1
                );
                if (SaveSystem.TryReadSave(2, out _))
                {
                    throw new System.InvalidOperationException(
                        "A save from a newer version was accepted."
                    );
                }
                WriteTestSave(
                    SaveSystem.GetSlotPathForTests(2),
                    "2026-11-21T12:00:00.0000000Z"
                );

                if (SaveSystem.DeleteSlot(SaveSystem.AutoSlot))
                {
                    throw new System.InvalidOperationException(
                        "The automatic save slot must remain read-only."
                    );
                }

                if (!SaveSystem.DeleteSlot(3)
                    || SaveSystem.TryReadSave(3, out _)
                    || !SaveSystem.TryReadLatestSave(out _, out latestSlot)
                    || latestSlot != 2)
                {
                    throw new System.InvalidOperationException(
                        "Manual slot deletion or latest-save fallback failed."
                    );
                }

                VerifyCombatantPersistence();
            }
            finally
            {
                SaveSystem.SaveDirectoryOverrideForTests = null;
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, recursive: true);
                }
            }

            Debug.Log(
                "Save verification passed: version migration, checksum, "
                + "backup recovery, slots and deletion."
            );
        }

        private static void VerifyCombatantPersistence()
        {
            WorldSaveData legacyData = new()
            {
                Version = 2,
                SceneName = SceneNames.Guangling
            };
            legacyData.Combatants.Add(new CombatantSaveData
            {
                ObjectName = "Enemy_Legacy",
                Health = 35f,
                Active = true
            });
            if (!SaveDataMigration.TryUpgrade(
                    legacyData,
                    2,
                    out string migrationError
                )
                || legacyData.Version != WorldSaveData.CurrentVersion
                || legacyData.Combatants.Count != 1
                || !string.IsNullOrEmpty(
                    legacyData.Combatants[0].PersistentId
                ))
            {
                throw new System.InvalidOperationException(
                    "Combatant version 2 migration failed: "
                    + migrationError
                );
            }

            GameObject encounterA = new("EncounterA");
            GameObject encounterB = new("EncounterB");
            try
            {
                Combatant first = CreateNamedCombatant(
                    encounterA.transform,
                    "Enemy",
                    "encounter-a:Enemy"
                );
                Combatant second = CreateNamedCombatant(
                    encounterB.transform,
                    "Enemy",
                    "encounter-b:Enemy"
                );
                Combatant legacy = CreateNamedCombatant(
                    encounterB.transform,
                    "Enemy_Legacy",
                    null
                );
                first.TakeDamage(25f);
                second.TakeDamage(40f);
                legacy.TakeDamage(65f);

                SaveSystem.RestoreCombatantStatesForTests(
                    new[] { first, second, legacy },
                    new[]
                    {
                        new CombatantSaveData
                        {
                            PersistentId = second.PersistentId,
                            ObjectName = second.gameObject.name,
                            Health = 41f,
                            Active = true
                        },
                        new CombatantSaveData
                        {
                            PersistentId = first.PersistentId,
                            ObjectName = first.gameObject.name,
                            Health = 52f,
                            Active = true
                        },
                        new CombatantSaveData
                        {
                            PersistentId = string.Empty,
                            ObjectName = legacy.gameObject.name,
                            Health = 36f,
                            Active = true
                        }
                    }
                );

                if (first.PersistentId == second.PersistentId
                    || !Mathf.Approximately(first.CurrentHealth, 52f)
                    || !Mathf.Approximately(second.CurrentHealth, 41f)
                    || !Mathf.Approximately(legacy.CurrentHealth, 36f))
                {
                    throw new System.InvalidOperationException(
                        "Combatant IDs or legacy-name restoration collided."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(encounterA);
                Object.DestroyImmediate(encounterB);
            }
        }

        private static Combatant CreateNamedCombatant(
            Transform parent,
            string objectName,
            string persistentId)
        {
            GameObject probe = new(objectName);
            probe.transform.SetParent(parent, false);
            Combatant combatant = probe.AddComponent<Combatant>();
            combatant.Configure(
                CombatTeam.Enemy,
                100f,
                persistentId
            );
            return combatant;
        }

        private static void WriteTestSave(
            string path,
            string savedAtUtc,
            int version = 1)
        {
            WorldSaveData data = new()
            {
                Version = version,
                SavedAtUtc = savedAtUtc,
                SceneName = SceneNames.Guangling,
                AreaId = "official-street",
                AreaName = "官署前街",
                PlayerX = 1.5f,
                PlayerY = 0f,
                PlayerZ = -3f,
                ActivePartyIndex = 1,
                TimeLayer = TimeLayer.Past,
                Stance = WitnessStance.PersonFirst,
                EvidencePlan = EvidencePlan.PrivateProtection,
                ChapterStage = ChapterStage.Investigate,
                ClueIds = new List<string>
                {
                    "clue-salt-permit",
                    "clue-ledger"
                },
                TimeAnchorIds = new List<string> { "past-02" },
                EchoIds = new List<string> { "echo-coin" },
                StoryFactIds = new List<string>
                {
                    "teahouse-cart-testimony"
                },
                InventoryItems = new List<InventoryItemSaveData>
                {
                    new()
                    {
                        ItemId = "ginger-tea",
                        Count = 2
                    }
                },
                CollectedPickupIds = new List<string>
                {
                    "pickup-ginger-tea"
                },
                UnlockedAnchorIds = new List<string>
                {
                    "guangling_start",
                    "dongguan_start"
                },
                CompletedStreamingRegionIds = new List<string>
                {
                    "salt-street-support"
                },
                CompletedEncounterIds = new List<string>
                {
                    "dongguan-patrol"
                },
                CompletedWorldContentIds = new List<string>
                {
                    "dongguan-apprentice-ledger"
                },
                StartedWorldContentIds = new List<string>
                {
                    "caiyi-apprentice-ledger-page"
                }
            };
            File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
        }

    }
}
