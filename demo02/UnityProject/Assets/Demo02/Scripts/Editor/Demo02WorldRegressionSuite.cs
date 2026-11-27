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
    internal static class Demo02WorldRegressionSuite
    {
        private const string ScenePath =
            "Assets/Demo02/Scenes/Guangling/Greybox.unity";
        private const string RootName = "Greybox";

        public static void VerifyOpenWorldFrameworkForBatch()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }
            try
            {
                GameObject root = scene
                    .GetRootGameObjects()
                    .FirstOrDefault(item => item.name == RootName);
                if (root == null
                    || root.transform.Find("Chapter01_OpenWorldFramework") == null
                    || root.transform.Find("Chapter01_AbilityFramework") == null)
                {
                    throw new System.InvalidOperationException(
                        "Open-world framework marker is missing."
                    );
                }

                PlayerPartyController party = root
                    .GetComponentsInChildren<PlayerPartyController>(true)
                    .FirstOrDefault();
                if (party == null
                    || party.PartySlotCount != PlayerPartyController.MaxPartySlots
                    || party.GetCharacter(0) == null
                    || party.GetCharacter(1) == null
                    || party.GetCharacter(2) != null
                    || party.GetCharacter(3) != null)
                {
                    throw new System.InvalidOperationException(
                        "Party framework must expose four slots with two current members."
                    );
                }

                CharacterRuntime repairer = party.GetCharacter(0)
                    .GetComponent<CharacterRuntime>();
                CharacterRuntime guide = party.GetCharacter(1)
                    .GetComponent<CharacterRuntime>();
                if (repairer?.Definition == null
                    || guide?.Definition == null
                    || repairer.Definition.ResonanceMark
                        != ResonanceMark.Continuation
                    || guide.Definition.ResonanceMark != ResonanceMark.Path)
                {
                    throw new System.InvalidOperationException(
                        "Playable party members are missing their character definitions."
                    );
                }

                foreach (Transform member in new[]
                         {
                             party.GetCharacter(0),
                             party.GetCharacter(1)
                         })
                {
                    if (member.Find("ExplorationGlider") == null)
                    {
                        throw new System.InvalidOperationException(
                            $"{member.name} is missing its glider visual."
                        );
                    }
                    if (member.Find("AbilityPulse") == null
                        || member.GetComponent<CharacterAbilityController>()
                            == null)
                    {
                        throw new System.InvalidOperationException(
                            $"{member.name} is missing its ability runtime."
                        );
                    }
                }

                if (root.GetComponentsInChildren<AbilityHud>(true).Length == 0
                    || root.GetComponentInChildren<QuickHealController>(
                        true
                    ) == null
                    || root.GetComponentsInChildren<Combatant>(true)
                        .FirstOrDefault(enemy =>
                            enemy.Team == CombatTeam.Enemy
                            && enemy.GetComponent<ResonanceStatus>() != null)
                        == null)
                {
                    throw new System.InvalidOperationException(
                        "Ability HUD or resonance status framework is missing."
                    );
                }
                HashSet<EnemyArchetype> sceneArchetypes = root
                    .GetComponentsInChildren<EnemyAI>(true)
                    .Select(enemy => enemy.Archetype)
                    .ToHashSet();
                if (!sceneArchetypes.Contains(EnemyArchetype.Chaser)
                    || !sceneArchetypes.Contains(EnemyArchetype.Shield)
                    || !sceneArchetypes.Contains(EnemyArchetype.Ranged))
                {
                    throw new System.InvalidOperationException(
                        "Main scene does not contain the mixed enemy roster."
                    );
                }

                WorldContentTracker contentTracker = root
                    .GetComponentInChildren<WorldContentTracker>(true);
                if (contentTracker == null
                    || contentTracker.Entries.Count != 4)
                {
                    throw new System.InvalidOperationException(
                        "World content tracker is incomplete."
                    );
                }
                if (root.GetComponentInChildren<ContinuationLoadout>(true) == null)
                {
                    throw new System.InvalidOperationException(
                        "Continuation loadout is missing."
                    );
                }
                Transform archiveClerk =
                    root.transform.Find("NPC_ArchiveClerk");
                Transform aheController =
                    root.transform.Find("ConditionalNPC_AHe");
                WorldNpcAvailability aheAvailability = aheController != null
                    ? aheController.GetComponent<WorldNpcAvailability>()
                    : null;
                Transform luChongController =
                    root.transform.Find("ConditionalNPC_LuChong");
                WorldNpcAvailability luChongAvailability =
                    luChongController != null
                        ? luChongController.GetComponent<
                            WorldNpcAvailability
                        >()
                        : null;
                if (archiveClerk == null
                    || archiveClerk.GetComponent<
                        NpcDialogueInteractable
                    >() == null
                    || aheAvailability == null
                    || aheAvailability.Target == null
                    || aheAvailability.Target.activeSelf
                    || luChongAvailability == null
                    || luChongAvailability.Target == null
                    || luChongAvailability.Target.activeSelf)
                {
                    throw new System.InvalidOperationException(
                        "Conditional story NPCs are incomplete."
                    );
                }
                if (root.transform.Find(
                        "DongguanStreet/Challenge_DongguanWindCourse"
                    ) == null)
                {
                    throw new System.InvalidOperationException(
                        "Dongguan wind course is missing."
                    );
                }
                PaperKiteStoryController paperKiteStory = root
                    .GetComponentInChildren<PaperKiteStoryController>(
                        true
                    );
                if (paperKiteStory == null
                    || paperKiteStory.StorytellerTarget == null
                    || paperKiteStory.KnotTarget == null
                    || root.GetComponentsInChildren<
                        PaperKiteStoryInteractable>(true).Length != 4)
                {
                    throw new System.InvalidOperationException(
                        "Dongguan paper kite story is incomplete."
                    );
                }

                WorldEncounterZone[] encounterZones = root
                    .GetComponentsInChildren<WorldEncounterZone>(true);
                WorldEncounterDirector encounterDirector = root
                    .GetComponentsInChildren<WorldEncounterDirector>(true)
                    .FirstOrDefault();
                WorldAdditiveSceneRegion[] streamingRegions = root
                    .GetComponentsInChildren<WorldAdditiveSceneRegion>(true);
                IReadOnlyList<WorldStreamingRegionDefinition>
                    streamingDefinitions =
                        WorldStreamingRegionSeeder.LoadAll();
                WorldStreamingDirector streamingDirector = scene
                    .GetRootGameObjects()
                    .SelectMany(item =>
                        item.GetComponentsInChildren<WorldStreamingDirector>(
                            true
                        ))
                    .FirstOrDefault();
                if (encounterDirector == null
                    || streamingDirector == null
                    || encounterZones.Length != 1
                    || encounterZones.FirstOrDefault(zone =>
                        zone.Definition != null
                        && zone.Definition.EncounterId == "official-blockade"
                        && zone.Definition.SupportEncounterIds.Contains(
                            "salt-street-support"
                        )) == null
                    || streamingRegions.Length
                        != streamingDefinitions.Count
                    || streamingDefinitions.Any(definition =>
                        !streamingRegions.Any(region =>
                            region.RegionId == definition.RegionId
                            && region.SceneName == definition.SceneName
                            && Mathf.Approximately(
                                region.LoadRadius,
                                definition.LoadRadius
                            )
                            && Mathf.Approximately(
                                region.PreloadRadius,
                                definition.PreloadRadius
                            )
                            && Mathf.Approximately(
                                region.UnloadRadius,
                                definition.UnloadRadius
                            )))
                    || streamingRegions.Any(region => region.PreloadRadius
                        <= region.LoadRadius))
                {
                    throw new System.InvalidOperationException(
                        "Encounter director or streaming support region is missing."
                    );
                }

                WorldSpawnPoint[] anchors = root
                    .GetComponentsInChildren<WorldSpawnPoint>(true)
                    .Where(spawnPoint => spawnPoint.IsFastTravelAnchor)
                    .ToArray();
                if (anchors.Length != 3
                    || !anchors.Any(anchor =>
                        anchor.SpawnId == "guangling_start"
                        && anchor.StartsUnlocked)
                    || !anchors.Any(anchor =>
                        anchor.SpawnId == "dongguan_start"
                        && !anchor.StartsUnlocked)
                    || !anchors.Any(anchor =>
                        anchor.SpawnId == "caiyi_start"
                        && !anchor.StartsUnlocked))
                {
                    throw new System.InvalidOperationException(
                        "World anchor network is incomplete."
                    );
                }
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Debug.Log(
                "Open-world framework validation passed: four party slots, "
                + "character definitions, abilities, encounters, anchors "
                + "and glider visuals."
            );
        }

        public static void VerifyStreamingSceneForBatch()
        {
            IReadOnlyList<WorldStreamingRegionDefinition> definitions =
                WorldStreamingRegionSeeder.LoadAll();
            foreach (WorldStreamingRegionDefinition definition in
                     definitions)
            {
                string path =
                    WorldStreamingRegionSeeder.GetScenePath(definition);
                string encounterId =
                    definition.EncounterContent.EncounterId;
                string enemyName = definition.EnemyObjectName;
                string contentId = definition.ContentEventId;
                Scene scene = SceneManager.GetSceneByPath(path);
                bool openedHere = !scene.IsValid() || !scene.isLoaded;
                if (openedHere)
                {
                    scene = EditorSceneManager.OpenScene(
                        path,
                        OpenSceneMode.Additive
                    );
                }
                try
                {
                    WorldEncounterZone zone = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<WorldEncounterZone>(
                                true
                            ))
                        .FirstOrDefault();
                    Combatant enemy = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<Combatant>(true))
                        .FirstOrDefault(combatant =>
                            combatant.Team == CombatTeam.Enemy);
                    WorldContentEvent contentEvent = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<WorldContentEvent>(
                                true
                            ))
                        .FirstOrDefault();
                    EliteEnemyController elite = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<
                                EliteEnemyController
                            >(true))
                        .FirstOrDefault();
                    EliteArenaBoundary eliteArena = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<
                                EliteArenaBoundary
                            >(true))
                        .FirstOrDefault();
                    Transform visualSlot = scene
                        .GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<Transform>(true))
                        .FirstOrDefault(item =>
                            item.name == "VisualReplacementSlot");
                    bool hasStoryNpc =
                        !string.IsNullOrWhiteSpace(contentId)
                        && scene
                            .GetRootGameObjects()
                            .SelectMany(root =>
                                root.GetComponentsInChildren<
                                    WorldStoryNpcInteractable
                                >(true))
                            .Any();
                    if (zone == null
                        || zone.Definition == null
                        || zone.Definition.EncounterId != encounterId
                        || enemy == null
                        || enemy.gameObject.name != enemyName
                        || (!string.IsNullOrWhiteSpace(contentId)
                            && (contentEvent == null
                                || contentEvent.ContentId != contentId
                                || !hasStoryNpc))
                        || (encounterId == "caiyi-lookout"
                            && (elite == null
                                || enemy.MaxHealth < 120f
                                || eliteArena == null
                                || visualSlot == null)))
                    {
                        throw new System.InvalidOperationException(
                            $"Streaming scene is incomplete: {path}"
                        );
                    }
                }
                finally
                {
                    if (openedHere)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            Debug.Log(
                $"Streaming scene validation passed: " +
                $"{definitions.Count} data-driven encounter layers are isolated."
            );
        }

        public static void VerifyStreamingRetryStateForBatch()
        {
            GameObject probe = new("StreamingRetryProbe");
            try
            {
                WorldAdditiveSceneRegion region =
                    probe.AddComponent<WorldAdditiveSceneRegion>();
                region.Configure(
                    "retry-probe",
                    "MissingRetryScene",
                    "加载失败测试区域",
                    10f,
                    16f,
                    20f,
                    false
                );
                region.RecordLoadFailureForTests(
                    "Synthetic streaming failure."
                );
                if (region.IsCompleted
                    || region.IsLoaded
                    || !region.IsWaitingForRetry
                    || region.RetryAttempt != 1
                    || string.IsNullOrWhiteSpace(region.LastLoadError))
                {
                    throw new System.InvalidOperationException(
                        "A streaming load failure disabled retries."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
            Debug.Log(
                "Streaming retry validation passed: failure remains retryable."
            );
        }

        public static void VerifyMapReadabilityForBatch()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }
            try
            {
                GameObject root = scene
                    .GetRootGameObjects()
                    .FirstOrDefault(item => item.name == RootName);
                if (root == null)
                {
                    throw new System.InvalidOperationException(
                        "Map readability validation could not find Greybox."
                    );
                }
                if (root.transform.Find("Chapter01_MapReadability") == null)
                {
                    throw new System.InvalidOperationException(
                        "Map readability marker is missing."
                    );
                }
                if (root.transform.Find("Chapter01_PublicPropDressing") == null)
                {
                    throw new System.InvalidOperationException(
                        "Public prop dressing marker is missing."
                    );
                }

                foreach (string path in new[]
                         {
                             "MapReadabilityDressing/OfficialStreet_Curb_Left",
                             "MapReadabilityDressing/DongguanStreet_Curb_Right",
                             "MapReadabilityDressing/CaiyiStreet_Curb_Left",
                             "MapReadabilityDressing/CourtyardGate_Beam",
                             "MapReadabilityDressing/OfficialStreet_EndGate_Beam",
                             "PublicPropDressing/TeaHouse_Screen",
                             "PublicPropDressing/TeaHouse_Table",
                             "PublicPropDressing/TeaHouse_Chair_Left",
                             "PublicPropDressing/TeaHouse_Chair_Right",
                             "PublicPropDressing/Warehouse_ChineseCabinet"
                         })
                {
                    if (root.transform.Find(path) == null)
                    {
                        throw new System.InvalidOperationException(
                            $"Map readability object is missing: {path}"
                        );
                    }
                }

                foreach (string clueName in new[]
                         {
                             "Clue_OldStreetSign",
                             "Clue_SaltLedger",
                             "Clue_OfficialSeal"
                         })
                {
                    Transform clue = root.transform.Find(clueName);
                    if (clue == null || Mathf.Abs(clue.position.x) < 3.8f)
                    {
                        throw new System.InvalidOperationException(
                            $"Map clue still obstructs the street center: {clueName}"
                        );
                    }
                }
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Debug.Log(
                "Map readability validation passed: street edges, side branch, "
                + "end gate and interaction clearance."
            );
        }

    }
}
