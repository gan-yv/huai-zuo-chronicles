using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class Demo02GreyboxBuilder
    {
        private const string ScenePath = "Assets/Demo02/Scenes/Guangling/Greybox.unity";
        private const string BootScenePath = "Assets/Demo02/Scenes/Boot/Boot.unity";
        private const string StreamingScenePath =
            "Assets/Demo02/Scenes/Guangling/Streaming/SaltStreetSupport.unity";
        private const string DongguanStreamingScenePath =
            "Assets/Demo02/Scenes/Guangling/Streaming/DongguanPatrol.unity";
        private const string CaiyiStreamingScenePath =
            "Assets/Demo02/Scenes/Guangling/Streaming/CaiyiLookout.unity";
        private const string RootName = "Greybox";

        [InitializeOnLoadMethod]
        private static void QueueInitialBuild()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Demo02/Rebuild Greybox Scene")]
        public static void RebuildFromMenu()
        {
            CharacterDefinitionSeeder.BuildAll();
            EncounterDefinitionSeeder.BuildAll();
            StylizedCharacterSetup.BuildAll();
            QuaterniusAnimationLibrarySetup.BuildAll();
            QuaterniusEnvironmentSetup.BuildAll();
            ReadyMadeStreetDressingSetup.BuildAll();
            ReadyMadeAudioSetup.BuildAll();
            BuildScene(forceRebuild: true);
            BuildStreamingEncounterScene();
            BuildBootScene();
            ConfigureBuildSettings();
        }

        public static void RebuildForBatch()
        {
            CharacterDefinitionSeeder.BuildAll();
            EncounterDefinitionSeeder.BuildAll();
            StylizedCharacterSetup.BuildAll();
            QuaterniusAnimationLibrarySetup.BuildAll();
            QuaterniusEnvironmentSetup.BuildAll();
            ReadyMadeStreetDressingSetup.BuildAll();
            ReadyMadeAudioSetup.BuildAll();
            BuildScene(forceRebuild: true);
            BuildStreamingEncounterScene();
            BuildBootScene();
            ConfigureBuildSettings();
        }

        public static void VerifyCombatDamageForBatch()
        {
            GameObject probe = new GameObject("CombatantDamageProbe");
            Combatant combatant = probe.AddComponent<Combatant>();
            combatant.Configure(CombatTeam.Enemy, 60f);

            combatant.TakeDamage(30f);
            if (Mathf.Approximately(combatant.CurrentHealth, 30f) == false
                || combatant.IsAlive == false)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("First hit did not reduce enemy health to 30.");
            }

            combatant.TakeDamage(30f);
            if (Mathf.Approximately(combatant.CurrentHealth, 0f) == false
                || combatant.IsAlive)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Second hit did not kill the enemy.");
            }

            Object.DestroyImmediate(probe);
            Debug.Log("Combat damage verification passed: 60 HP defeated in two 30 damage hits.");
        }

        public static void VerifyAllForBatch()
        {
            CaseDefinitionSeeder.ValidateOrThrow();
            VerifyDialogueContentForBatch();
            VerifyInventoryForBatch();
            VerifyCombatDamageForBatch();
            VerifyTimeLayerForBatch();
            VerifyChapterFlowForBatch();
            VerifySaveSlotsForBatch();
            CharacterDefinitionSeeder.ValidateOrThrow();
            EncounterDefinitionSeeder.ValidateOrThrow();
            VerifyCharacterFrameworkForBatch();
            VerifyOpenWorldFrameworkForBatch();
            VerifyStreamingSceneForBatch();
            StylizedCharacterSetup.ValidateForBatch();
            QuaterniusAnimationLibrarySetup.ValidateForBatch();
            QuaterniusEnvironmentSetup.ValidateForBatch();
            ReadyMadeStreetDressingSetup.ValidateForBatch();
            ReadyMadeAudioSetup.ValidateForBatch();
            PublicPropDressingSetup.ValidateForBatch();
            VerifyMapReadabilityForBatch();
            Debug.Log(
                "All automated checks passed: dialogue, combat, time layer, "
                + "inventory, chapter and saves."
            );
        }

        private static void VerifyCharacterFrameworkForBatch()
        {
            CharacterDefinition definition =
                CharacterDefinitionSeeder.LoadRepairer();
            GameObject probe = new("CharacterRuntimeProbe");
            CharacterRuntime runtime = probe.AddComponent<CharacterRuntime>();
            runtime.Configure(definition);
            float initialStamina = runtime.CurrentStamina;
            bool drained = runtime.DrainStamina(18f);
            runtime.GainEnergy(12f);
            bool spent = runtime.TrySpendEnergy(10f);
            bool valid = Mathf.Approximately(initialStamina, definition.MaxStamina)
                && drained
                && Mathf.Approximately(
                    runtime.CurrentStamina,
                    definition.MaxStamina - 18f
                )
                && Mathf.Approximately(runtime.CurrentEnergy, 2f)
                && spent;
            runtime.SetContinuationBonuses(10f, 1.25f);
            runtime.RestoreEnergy(0f);
            runtime.GainEnergy(20f);
            valid &= Mathf.Approximately(
                runtime.MaxStamina,
                definition.MaxStamina + 10f
            ) && Mathf.Approximately(runtime.CurrentEnergy, 25f);
            Object.DestroyImmediate(probe);

            if (!valid)
            {
                throw new System.InvalidOperationException(
                    "Character stamina or energy framework validation failed."
                );
            }
            Debug.Log(
                "Character framework validation passed: stamina drain and "
                + "energy gain/spend."
            );
        }

        private static void VerifyOpenWorldFrameworkForBatch()
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
                    || streamingRegions.Length != 3
                    || !streamingRegions.Any(region =>
                        region.RegionId == "salt-street-support"
                        && region.SceneName == "SaltStreetSupport")
                    || !streamingRegions.Any(region =>
                        region.RegionId == "dongguan-patrol"
                        && region.SceneName == "DongguanPatrol")
                    || !streamingRegions.Any(region =>
                        region.RegionId == "caiyi-lookout"
                        && region.SceneName == "CaiyiLookout")
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

        private static void VerifyStreamingSceneForBatch()
        {
            foreach ((string path, string encounterId, string enemyName,
                         string contentId)
                     in new[]
                     {
                         (
                             StreamingScenePath,
                             "salt-street-support",
                             "Enemy_Support_01",
                             (string)null
                         ),
                         (
                             DongguanStreamingScenePath,
                             "dongguan-patrol",
                             "Enemy_DongguanPatrol",
                             "dongguan-apprentice-ledger"
                         ),
                         (
                             CaiyiStreamingScenePath,
                             "caiyi-lookout",
                             "Enemy_CaiyiLookout",
                             "caiyi-apprentice-ledger-page"
                         )
                     })
            {
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
                    bool hasStoryNpc = contentId != null
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
                        || (contentId != null
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
                "Streaming scene validation passed: three encounter layers "
                + "are isolated."
            );
        }

        private static void VerifyMapReadabilityForBatch()
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

        public static void VerifyInventoryForBatch()
        {
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            GameObject probe = new("InventoryProbe");
            InventoryState inventory = probe.AddComponent<InventoryState>();
            inventory.Configure(content);
            InvestigationState state = probe.AddComponent<InvestigationState>();
            state.ConfigureContent(content);
            Combatant health = probe.AddComponent<Combatant>();
            health.Configure(CombatTeam.Player, 100f);

            if (!inventory.AddItem("salt-token-charm", 1)
                || inventory.GetCount("salt-token-charm") != 1
                || !inventory.AddItem("ginger-tea", 7)
                || inventory.GetCount("ginger-tea") != 5
                || !inventory.AddItem("blank-ledger-page", 1)
                || !inventory.MarkPickupCollected("pickup-ginger-tea")
                || !inventory.IsPickupCollected("pickup-ginger-tea"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory stacking or pickup state verification failed."
                );
            }

            state.SetFact("teahouse-cart-testimony");
            if (!inventory.TryUseItem(
                    "blank-ledger-page",
                    null,
                    state,
                    out _
                )
                || !state.HasFact("teahouse-written-record")
                || inventory.GetCount("blank-ledger-page") != 0
                || !inventory.TryUseItem(
                    "salt-token-charm",
                    null,
                    state,
                    out _
                )
                || !state.HasFact("salt-token-identified")
                || inventory.GetCount("salt-token-charm") != 1)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory story-item effects verification failed."
                );
            }

            health.TakeDamage(40f);
            if (!inventory.TryUseItem("ginger-tea", health, state, out _)
                || !Mathf.Approximately(health.CurrentHealth, 85f)
                || inventory.GetCount("ginger-tea") != 4)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory healing item verification failed."
                );
            }

            List<InventoryItemSaveData> snapshot = inventory.ExportItems();
            string[] pickupIds = inventory.ExportCollectedPickupIds();
            Object.DestroyImmediate(probe);

            GameObject restoredProbe = new("InventoryRestoreProbe");
            InventoryState restored =
                restoredProbe.AddComponent<InventoryState>();
            restored.Configure(content);
            restored.Restore(snapshot, pickupIds);
            if (restored.GetCount("salt-token-charm") != 1
                || restored.GetCount("ginger-tea") != 4
                || !restored.IsPickupCollected("pickup-ginger-tea"))
            {
                Object.DestroyImmediate(restoredProbe);
                throw new System.InvalidOperationException(
                    "Inventory snapshot restore verification failed."
                );
            }

            Object.DestroyImmediate(restoredProbe);
            Debug.Log(
                "Inventory verification passed: stacking, story use, "
                + "healing, pickup and restore."
            );
        }

        public static void VerifyDialogueContentForBatch()
        {
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            GameObject probe = new("DialogueContentProbe");
            InvestigationState state = probe.AddComponent<InvestigationState>();
            state.ConfigureContent(content);

            DialogueScriptDefinition arrival = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (arrival == null
                || arrival.Id != "shenbo-arrival"
                || arrival.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Arrival dialogue did not resolve from recorded data."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                arrival.Choices[0].Effects,
                state
            );
            DialogueContentRuntime.ApplyEffects(
                arrival.CompletionEffects,
                state
            );
            DialogueScriptDefinition reminder = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (state.Stage != ChapterStage.Investigate
                || state.Stance != WitnessStance.EvidenceFirst
                || reminder == null
                || reminder.Id != "shenbo-investigate-evidence")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Dialogue effects did not select the stance-specific reminder."
                );
            }
            if (OptionalObjectiveRuntime.FindActive(content, state) != null)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Optional tea objective activated before the seal was found."
                );
            }

            DialogueScriptDefinition teahouseEarly =
                DialogueContentRuntime.Find(content, "teahouse", state);
            if (teahouseEarly == null
                || teahouseEarly.Id != "teahouse-early")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The data-configured teahouse event did not match early game."
                );
            }

            state.CollectClue("old-street-sign");
            state.CollectClue("salt-ledger");
            state.CollectClue("official-seal");
            DialogueScriptDefinition teahouseRumor =
                DialogueContentRuntime.Find(content, "teahouse", state);
            OptionalObjectiveDefinition teaObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            if (teahouseRumor == null
                || teahouseRumor.Id != "teahouse-rumor-seal"
                || teaObjective == null
                || teaObjective.Id != "optional-teahouse-cart-rumor")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The teahouse event did not react to the official seal."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                teahouseRumor.Choices[0].Effects,
                state
            );
            DialogueScriptDefinition teahouseAfterChoice =
                DialogueContentRuntime.Find(content, "teahouse", state);
            OptionalObjectiveDefinition recordObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            if (!state.HasFact("teahouse-cart-testimony")
                || !state.HasFact("teahouse-event-completed")
                || teahouseAfterChoice == null
                || teahouseAfterChoice.Id != "teahouse-after-cart-testimony"
                || recordObjective == null
                || recordObjective.Id
                    != "optional-record-teahouse-testimony")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The teahouse choice did not persist its story fact."
                );
            }
            state.SetFact("teahouse-written-record");
            if (OptionalObjectiveRuntime.FindActive(content, state) != null)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Completed optional objectives were still active."
                );
            }

            state.SetEvidencePlan(EvidencePlan.PublicRecord);
            DialogueScriptDefinition review = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (review == null || review.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Evidence review dialogue did not resolve from data."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                review.CompletionEffects,
                state
            );
            DialogueScriptDefinition crossExamine = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (state.Stage != ChapterStage.CrossExamineTestimony
                || crossExamine == null
                || crossExamine.Id != "shenbo-cross-examine-teahouse")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Dialogue completion effects did not advance the chapter."
                );
            }

            Object.DestroyImmediate(probe);
            Debug.Log(
                "Dialogue data verification passed: conditions, choices, "
                + "effects and stage-specific scripts."
            );
        }

        public static void VerifyTimeLayerForBatch()
        {
            GameObject probe = new GameObject("TimeLayerProbe");
            TimeLayerController controller = probe.AddComponent<TimeLayerController>();

            controller.ToggleLayer();
            if (controller.CurrentLayer != TimeLayer.Past)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Time layer did not switch to Past.");
            }

            controller.ToggleLayer();
            if (controller.CurrentLayer != TimeLayer.Present)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Time layer did not switch back to Present.");
            }

            Object.DestroyImmediate(probe);
            Debug.Log("Time layer verification passed: Present <-> Past.");
        }

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
                "Save verification passed: migration, 3 manual slots, "
                + "latest selection, read-only auto slot and deletion."
            );
        }

        private static void WriteTestSave(string path, string savedAtUtc)
        {
            WorldSaveData data = new()
            {
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

        public static void VerifyChapterFlowForBatch()
        {
            GameObject probe = new("ChapterFlowProbe");
            InvestigationState state = probe.AddComponent<InvestigationState>();
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            state.ConfigureContent(content);

            if (state.CanInvestigateClues
                || state.CollectClue("old-street-sign"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Clues were available before Shen Bo briefed the player."
                );
            }

            if (!state.CompleteWitnessBriefing()
                || state.Stage != ChapterStage.Investigate)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The chapter did not enter the investigation stage."
                );
            }
            state.SetWitnessStance(WitnessStance.PersonFirst);

            if (!state.CollectClue("old-street-sign")
                || !state.CollectClue("salt-ledger")
                || !state.CollectClue("official-seal")
                || state.Stage != ChapterStage.ReviewEvidence)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Collecting three clues did not require evidence review."
                );
            }

            if (state.TryStabilizeTimeAnchor("past-02", out _)
                != TimeAnchorResult.NotReady)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Time anchors unlocked before the evidence review."
                );
            }

            state.SetEvidencePlan(EvidencePlan.PrivateProtection);
            if (!state.CompleteEvidenceReview()
                || state.Stage != ChapterStage.CrossExamineTestimony
                || state.TryStabilizeTimeAnchor("past-02", out _)
                    != TimeAnchorResult.NotReady)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Evidence review did not open testimony cross-examination."
                );
            }
            if (state.SubmitContradiction(
                    content.Testimonies[1].Id,
                    content.Contradiction.EvidenceId
                ) != ContradictionResult.Incorrect
                || state.ContradictionAttempts != 1
                || state.Stage != ChapterStage.CrossExamineTestimony
                || state.SubmitContradiction(
                    content.Contradiction.StatementId,
                    content.Contradiction.EvidenceId
                ) != ContradictionResult.Correct
                || state.ContradictionAttempts != 2
                || !state.TestimonyContradictionSolved
                || state.Stage != ChapterStage.StabilizeAnchors)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Testimony contradiction did not validate and unlock anchors."
                );
            }
            if (state.TryStabilizeTimeAnchor("present-01", out _)
                    != TimeAnchorResult.WrongOrder
                || state.StabilizedTimeAnchorCount != 0
                || state.TryStabilizeTimeAnchor("past-02", out _)
                    != TimeAnchorResult.Stabilized
                || state.TryStabilizeTimeAnchor("present-01", out _)
                    != TimeAnchorResult.Stabilized
                || state.TryStabilizeTimeAnchor("present-03", out _)
                    != TimeAnchorResult.SequenceComplete
                || state.Stage != ChapterStage.RepairRift)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Three stable anchors did not unlock the repair step."
                );
            }

            if (!state.CompleteRepair()
                || state.Stage != ChapterStage.FinalDecision
                || !state.SelectConclusion(ConclusionChoice.Preserve)
                || state.Stage != ChapterStage.Resolved)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The chapter did not reach a resolved conclusion."
                );
            }

            state.RestoreProgress(
                new[] { "old-street-sign", "salt-ledger", "official-seal" },
                System.Array.Empty<string>(),
                false,
                ConclusionChoice.None,
                ChapterStage.StabilizeAnchors,
                null,
                WitnessStance.EvidenceFirst,
                EvidencePlan.PublicRecord
            );
            if (state.Stage != ChapterStage.StabilizeAnchors
                || !state.TestimonyContradictionSolved)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "An older anchor-stage save did not infer the completed deduction."
                );
            }

            state.RestoreProgress(
                new[] { "old-street-sign", "salt-ledger", "official-seal" },
                new[] { "past-02", "present-01", "present-03" },
                true,
                ConclusionChoice.Seal,
                ChapterStage.Resolved,
                null,
                WitnessStance.PersonFirst,
                EvidencePlan.PrivateProtection,
                true,
                2,
                new[] { "teahouse-cart-testimony" }
            );
            if (state.Stage != ChapterStage.Resolved
                || state.SelectedConclusion != ConclusionChoice.Seal
                || state.Stance != WitnessStance.PersonFirst
                || state.Plan != EvidencePlan.PrivateProtection
                || !state.TestimonyContradictionSolved
                || state.ContradictionAttempts != 2
                || !state.HasFact("teahouse-cart-testimony"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Chapter progress did not restore correctly from a save."
                );
            }

            Object.DestroyImmediate(probe);
            Debug.Log(
                "Chapter verification passed: brief -> clues -> review -> "
                + "contradiction -> anchors -> repair -> ending."
            );
        }

        private static void BuildIfNeeded()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorApplication.delayCall += BuildIfNeeded;
                return;
            }

            BuildScene(forceRebuild: false);
            BuildBootScene();
            ConfigureBuildSettings();
        }

        private static void BuildStreamingEncounterScene()
        {
            if (!AssetDatabase.IsValidFolder(
                    "Assets/Demo02/Scenes/Guangling/Streaming"
                ))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Demo02/Scenes/Guangling",
                    "Streaming"
                );
            }

            BuildStreamingScene(
                StreamingScenePath,
                "Streaming_SaltStreetSupport",
                "Encounter_SaltStreetSupport",
                "Enemy_Support_01",
                new Vector3(1.4f, 0.9f, 24f),
                EncounterDefinitionSeeder.LoadSupport()
            );
            BuildStreamingScene(
                DongguanStreamingScenePath,
                "Streaming_DongguanPatrol",
                "Encounter_DongguanPatrol",
                "Enemy_DongguanPatrol",
                new Vector3(60f, 0.9f, 44f),
                EncounterDefinitionSeeder.LoadDongguanPatrol()
            );
            BuildStreamingScene(
                CaiyiStreamingScenePath,
                "Streaming_CaiyiLookout",
                "Encounter_CaiyiLookout",
                "Enemy_CaiyiLookout",
                new Vector3(60f, 0.9f, 220f),
                EncounterDefinitionSeeder.LoadCaiyiLookout()
            );
        }

        private static void BuildStreamingScene(
            string scenePath,
            string rootName,
            string zoneName,
            string enemyName,
            Vector3 enemyPosition,
            WorldEncounterDefinition definition)
        {
            Scene streamingScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive
            );
            GameObject root = new(rootName);
            SceneManager.MoveGameObjectToScene(root, streamingScene);

            Combatant enemy = CreateEnemy(
                root.transform,
                enemyName,
                enemyPosition
            );
            EliteEnemyController eliteController = null;
            if (definition.EncounterId == "caiyi-lookout")
            {
                enemy.Configure(CombatTeam.Enemy, 120f);
                eliteController =
                    enemy.gameObject.AddComponent<EliteEnemyController>();
                eliteController.Configure("彩衣街望风 · 精英");
                enemy.gameObject.AddComponent<EliteChargeAttack>();
                PrepareEliteVisualSlot(enemy.transform);
            }
            GameObject zoneObject = new(zoneName);
            zoneObject.transform.SetParent(root.transform);
            zoneObject.transform.position = enemyPosition;
            zoneObject.AddComponent<WorldEncounterZone>().Configure(
                definition,
                new[] { enemy }
            );
            if (eliteController != null)
            {
                zoneObject
                    .AddComponent<EliteArenaBoundary>()
                    .Configure(eliteController, 10.5f);
            }
            AddStreetStory(
                root.transform,
                zoneObject.GetComponent<WorldEncounterZone>(),
                definition.EncounterId
            );

            EditorSceneManager.MarkSceneDirty(streamingScene);
            if (!EditorSceneManager.SaveScene(
                    streamingScene,
                    scenePath
                ))
            {
                throw new System.InvalidOperationException(
                    $"Could not save streaming scene: {scenePath}"
                );
            }
            EditorSceneManager.CloseScene(streamingScene, true);
            AssetDatabase.Refresh();
            Debug.Log(
                $"Streaming encounter scene generated at {scenePath}"
            );
        }

        private static void AddStreetStory(
            Transform root,
            WorldEncounterZone zone,
            string encounterId)
        {
            string storyId;
            string npcName;
            string rewardItemId;
            Vector3 npcPosition;
            Color outfit;
            Color accent;
            DialogueLine[] intro;
            DialogueLine[] ready;
            DialogueLine[] completed;

            if (encounterId == "dongguan-patrol")
            {
                storyId = "dongguan-apprentice-ledger";
                npcName = "新来的脚夫";
                rewardItemId = "ginger-tea";
                npcPosition = new Vector3(57.5f, 1f, 40.5f);
                outfit = new Color(0.34f, 0.28f, 0.2f);
                accent = new Color(0.74f, 0.52f, 0.25f);
                intro = new[]
                {
                    new DialogueLine(
                        npcName,
                        "那队巡哨把一只旧账袋拖进盐仓，说里面只剩废纸。可我看见封签上盖的是官印。"
                    ),
                    new DialogueLine(
                        "主角",
                        "巡逻若真是查案，就不会把封签藏起来。"
                    )
                };
                ready = new[]
                {
                    new DialogueLine(
                        npcName,
                        "队伍散了。账袋还在供桌后，我没敢碰。"
                    ),
                    new DialogueLine(
                        npcName,
                        "这壶热姜汤你先拿着，潮气重，别在旧仓里病倒。"
                    )
                };
                completed = new[]
                {
                    new DialogueLine(
                        npcName,
                        "街口今天安静多了，我也该回去搬货了。"
                    )
                };
            }
            else if (encounterId == "caiyi-lookout")
            {
                storyId = "caiyi-apprentice-ledger-page";
                npcName = "裁衣学徒";
                rewardItemId = "blank-ledger-page";
                npcPosition = new Vector3(62.5f, 1f, 216f);
                outfit = new Color(0.38f, 0.22f, 0.28f);
                accent = new Color(0.78f, 0.44f, 0.28f);
                intro = new[]
                {
                    new DialogueLine(
                        npcName,
                        "每晚都有人替望风的人改衣，袖口藏着纸条。师父说别问，可我不信他们只是在巡夜。"
                    ),
                    new DialogueLine(
                        "主角",
                        "纸条既然能缝进衣服，就也能从这里带出去。"
                    )
                };
                ready = new[]
                {
                    new DialogueLine(
                        npcName,
                        "望风的人走了。我在裁衣案板下找到一张空白账页。"
                    ),
                    new DialogueLine(
                        npcName,
                        "你比我更需要它。若真能把街名写回来，记得别写错。"
                    )
                };
                completed = new[]
                {
                    new DialogueLine(
                        npcName,
                        "针脚已经拆干净了。希望下次来，这里只剩正常做买卖的人。"
                    )
                };
            }
            else
            {
                return;
            }

            GameObject npc = CreateCharacter(
                root,
                $"NPC_Story_{encounterId}",
                npcPosition,
                outfit,
                accent,
                addNpcCollider: true
            );
            WorldContentEvent contentEvent =
                root.gameObject.AddComponent<WorldContentEvent>();
            contentEvent.Configure(
                storyId,
                npcName,
                zone,
                rewardItemId,
                1,
                "巡哨线索已交付并领取街区奖励。"
            );
            npc.AddComponent<WorldStoryNpcInteractable>().Configure(
                npcName,
                contentEvent,
                intro,
                ready,
                completed
            );
        }

        private static void PrepareEliteVisualSlot(Transform enemy)
        {
            GameObject slot = new("VisualReplacementSlot");
            slot.transform.SetParent(enemy, false);
            foreach (string visualName in new[]
                     {
                         "BlockyVisual",
                         "GreyboxVisual"
                     })
            {
                Transform visual = enemy.Find(visualName);
                if (visual != null)
                {
                    visual.SetParent(slot.transform, true);
                }
            }
        }

        private static void BuildBootScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            bool hasUnsavedStartupScene = string.IsNullOrWhiteSpace(previousActiveScene.path);
            Scene bootScene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                hasUnsavedStartupScene ? NewSceneMode.Single : NewSceneMode.Additive
            );
            SceneManager.SetActiveScene(bootScene);

            GameObject bootRoot = new GameObject("Boot");
            SceneManager.MoveGameObjectToScene(bootRoot, bootScene);
            bootRoot.AddComponent<BootController>();

            GameObject worldRuntimeObject = new GameObject("WorldRuntime");
            SceneManager.MoveGameObjectToScene(worldRuntimeObject, bootScene);
            worldRuntimeObject.AddComponent<WorldRuntime>();

            EditorSceneManager.SaveScene(bootScene, BootScenePath);
            if (!hasUnsavedStartupScene)
            {
                EditorSceneManager.CloseScene(bootScene, removeScene: true);
            }

            if (!hasUnsavedStartupScene
                && previousActiveScene.IsValid()
                && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            Debug.Log("Demo02 boot scene generated.");
        }

        private static void ConfigureBuildSettings()
        {
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            SceneAsset greyboxScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            SceneAsset streamingScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(StreamingScenePath);
            SceneAsset dongguanStreamingScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    DongguanStreamingScenePath
                );
            SceneAsset caiyiStreamingScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    CaiyiStreamingScenePath
                );
            if (bootScene == null
                || greyboxScene == null
                || streamingScene == null
                || dongguanStreamingScene == null
                || caiyiStreamingScene == null)
            {
                Debug.LogWarning("World scenes are not ready for Build Settings yet.");
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(StreamingScenePath, true),
                new EditorBuildSettingsScene(
                    DongguanStreamingScenePath,
                    true
                ),
                new EditorBuildSettingsScene(CaiyiStreamingScenePath, true)
            };
        }

        private static void BuildScene(bool forceRebuild)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene greyboxScene = SceneManager.GetSceneByPath(ScenePath);
            bool wasAlreadyLoaded = greyboxScene.IsValid() && greyboxScene.isLoaded;
            Scene previousActiveScene = SceneManager.GetActiveScene();

            if (!wasAlreadyLoaded)
            {
                greyboxScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject existingRoot = greyboxScene
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == RootName);
            bool needsUpgrade = existingRoot != null
                && (existingRoot.transform.Find("Clue_OldStreetSign") == null
                    || existingRoot.transform.Find("Clue_SaltLedger") == null
                    || existingRoot.transform.Find("Clue_OfficialSeal") == null
                    || existingRoot.transform.Find("RepairSite_TimeRift") == null
                    || existingRoot.transform.Find("PartnerPlaceholder") == null
                    || existingRoot.transform.Find("Enemy_Patrol_01") == null
                    || existingRoot.transform.Find("Choice_Preserve") == null
                    || existingRoot.transform.Find("PlayerPlaceholder/MeleeArc") == null
                    || existingRoot.transform.Find("TimeAnchor_Present_01") == null
                    || existingRoot.transform.Find("Area_OfficialStreet") == null
                    || existingRoot.transform.Find("Area_WarehouseCourtyard") == null
                    || existingRoot.transform.Find("Area_WarehouseInterior") == null
                    || existingRoot.transform.Find("DongguanStreet") == null
                    || existingRoot.transform.Find("CaiyiStreet") == null
                    || existingRoot.transform.Find("NPC_WarehouseWorker") == null
                    || existingRoot.transform.Find("NPC_ShenBo") == null
                    || existingRoot.transform.Find("Chapter01_StorySetup") == null
                    || existingRoot.transform.Find("Chapter01_ArtPass") == null
                    || existingRoot.transform.Find("Chapter01_VisionSystem") == null
                    || existingRoot.transform.Find("Chapter01_ArchitecturePolish") == null
                    || existingRoot.transform.Find("Chapter01_ChoiceConsequences") == null
                    || existingRoot.transform.Find("Chapter01_ProceduralAnimation") == null
                    || existingRoot.transform.Find("Chapter01_TimeAnchorPuzzle") == null
                    || existingRoot.transform.Find("Chapter01_SettingsSystem") == null
                    || existingRoot.transform.Find("Chapter01_DataDrivenContent") == null
                    || existingRoot.transform.Find("Chapter01_StoryFacts") == null
                    || existingRoot.transform.Find("Chapter01_InventorySystem") == null
                    || existingRoot.transform.Find("Chapter01_OnboardingGuidance") == null
                    || existingRoot.transform.Find("Chapter01_RuntimeDiagnostics") == null
                    || existingRoot.transform.Find("Chapter01_MapReadability") == null
                    || existingRoot.transform.Find("Chapter01_PublicPropDressing") == null
                    || existingRoot.transform.Find("Chapter01_OpenWorldFramework") == null
                    || existingRoot.transform.Find("Chapter01_AbilityFramework") == null
                    || existingRoot.transform.Find("Chapter01_EncounterDirector") == null
                    || existingRoot.transform.Find("Stream_SaltStreetSupport") == null
                    || existingRoot.transform.Find(
                        "DongguanStreet/Stream_DongguanPatrol"
                    ) == null
                    || existingRoot.transform.Find(
                        "CaiyiStreet/Stream_CaiyiLookout"
                    ) == null
                    || existingRoot.transform.Find(
                        "DongguanStreet/Challenge_DongguanWindCourse"
                    ) == null
                    || existingRoot.transform.Find("GameSession") == null);

            if (existingRoot == null || forceRebuild || needsUpgrade)
            {
                GameObject existingRuntime = greyboxScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root => root.name == "WorldRuntime");
                if (existingRoot != null)
                {
                    Object.DestroyImmediate(existingRoot);
                }
                if (existingRuntime != null)
                {
                    Object.DestroyImmediate(existingRuntime);
                }

                SceneManager.SetActiveScene(greyboxScene);
                CaseDefinition chapterContent =
                    CaseDefinitionSeeder.LoadOrCreateChapter01();
                BuildGeometry(greyboxScene, chapterContent);
                GameObject generatedRoot = greyboxScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root => root.name == RootName);
                if (generatedRoot == null)
                {
                    throw new System.InvalidOperationException(
                        "Greybox geometry root was not generated."
                    );
                }
                QuaterniusEnvironmentSetup.ApplyToGreybox(
                    generatedRoot.transform
                );
                ReadyMadeStreetDressingSetup.ApplyToGreybox(
                    generatedRoot.transform
                );
                CreateMapReadabilityDressing(generatedRoot.transform);
                PublicPropDressingSetup.ApplyToGreybox(
                    generatedRoot.transform
                );
                EditorSceneManager.MarkSceneDirty(greyboxScene);
                EditorSceneManager.SaveScene(greyboxScene);
                Debug.Log("Demo02 greybox scene generated.");
            }

            if (!wasAlreadyLoaded)
            {
                EditorSceneManager.CloseScene(greyboxScene, removeScene: true);
            }

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static void BuildGeometry(
            Scene scene,
            CaseDefinition chapterContent)
        {
            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject ground = CreateBlock(
                root.transform,
                "Ground",
                new Vector3(0f, -0.5f, 5f),
                new Vector3(20f, 1f, 54f),
                new Color(0.55f, 0.52f, 0.46f)
            );
            ground.isStatic = true;
            CreateStonePaving(
                root.transform,
                "OfficialStreetPaving",
                new Vector3(0f, 0f, 5f),
                19f,
                52f,
                new Color(0.46f, 0.43f, 0.38f)
            );

            float[] buildingZPositions = { -14f, -4f, 6f, 16f, 26f };
            foreach (float z in buildingZPositions)
            {
                GameObject left = CreateBlock(
                    root.transform,
                    $"LeftBuilding_{z:0}",
                    new Vector3(-9f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    new Color(0.23f, 0.28f, 0.34f)
                );
                GameObject right = null;
                if (!Mathf.Approximately(z, 6f))
                {
                    right = CreateBlock(
                        root.transform,
                        $"RightBuilding_{z:0}",
                        new Vector3(9f, 3f, z),
                        new Vector3(6f, 6f, 8f),
                        new Color(0.3f, 0.27f, 0.32f)
                    );
                }
                left.isStatic = true;
                if (right != null)
                {
                    right.isStatic = true;
                }

                int styleIndex = Mathf.RoundToInt((z + 14f) / 10f);
                CreateShopFacadeDetails(
                    root.transform,
                    $"LeftBuilding_{z:0}",
                    left.transform.position,
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: 1f,
                    styleIndex: styleIndex,
                    includeAwning: z > 10f
                );
                if (right != null)
                {
                    CreateShopFacadeDetails(
                        root.transform,
                        $"RightBuilding_{z:0}",
                        right.transform.position,
                        new Vector3(6f, 6f, 8f),
                        facadeDirection: -1f,
                        styleIndex: styleIndex + 1,
                        includeAwning: z > 10f
                    );
                }
            }

            GameObject endWall = CreateBlock(
                root.transform,
                "EndWall",
                new Vector3(0f, 2f, 33f),
                new Vector3(18f, 4f, 2f),
                new Color(0.36f, 0.3f, 0.28f)
            );
            endWall.isStatic = true;

            CreateBlock(
                root.transform,
                "Crate_A",
                new Vector3(4.3f, 0.75f, -2f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.45f, 0.3f, 0.17f)
            );

            BuildWarehouseCourtyard(root.transform);
            CreateBlock(
                root.transform,
                "Crate_B",
                new Vector3(-4.3f, 1f, 14f),
                new Vector3(2f, 2f, 2f),
                new Color(0.45f, 0.3f, 0.17f)
            );

            GameObject player = CreateCharacter(
                root.transform,
                "PlayerPlaceholder",
                new Vector3(0f, 1f, -10f),
                new Color(0.2f, 0.43f, 0.68f),
                new Color(0.82f, 0.65f, 0.3f),
                addNpcCollider: false
            );
            player.AddComponent<CharacterController>();
            player.AddComponent<ThirdPersonController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerCombat>();
            player.AddComponent<Combatant>().Configure(CombatTeam.Player, 100f);
            player.AddComponent<CharacterRuntime>().Configure(
                CharacterDefinitionSeeder.LoadRepairer()
            );
            player.AddComponent<CharacterAbilityController>();
            CreateGliderVisual(
                player.transform,
                new Color(0.78f, 0.67f, 0.43f),
                new Color(0.93f, 0.76f, 0.36f)
            );
            CreateAbilityPulse(
                player.transform,
                new Color(0.93f, 0.76f, 0.36f)
            );

            GameObject meleeArc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meleeArc.name = "MeleeArc";
            meleeArc.transform.SetParent(player.transform, worldPositionStays: false);
            meleeArc.transform.localPosition = new Vector3(0f, 0.15f, 1.1f);
            meleeArc.transform.localScale = new Vector3(0.9f, 0.18f, 2f);
            Object.DestroyImmediate(meleeArc.GetComponent<BoxCollider>());
            meleeArc.SetActive(false);

            GameObject partner = CreateCharacter(
                root.transform,
                "PartnerPlaceholder",
                new Vector3(-1.6f, 1f, -11.5f),
                new Color(0.24f, 0.56f, 0.42f),
                new Color(0.76f, 0.58f, 0.24f),
                addNpcCollider: false
            );
            partner.AddComponent<CharacterController>();
            ThirdPersonController partnerController = partner.AddComponent<ThirdPersonController>();
            partnerController.enabled = false;
            PlayerCombat partnerCombat = partner.AddComponent<PlayerCombat>();
            partnerCombat.enabled = false;
            PlayerInteractor partnerInteractor = partner.AddComponent<PlayerInteractor>();
            partnerInteractor.enabled = false;
            partner.AddComponent<Combatant>().Configure(CombatTeam.Player, 100f);
            partner.AddComponent<CharacterRuntime>().Configure(
                CharacterDefinitionSeeder.LoadPathGuide()
            );
            CharacterAbilityController partnerAbilities =
                partner.AddComponent<CharacterAbilityController>();
            partnerAbilities.enabled = false;
            CreateGliderVisual(
                partner.transform,
                new Color(0.28f, 0.55f, 0.5f),
                new Color(0.31f, 0.78f, 0.72f)
            );
            CreateAbilityPulse(
                partner.transform,
                new Color(0.31f, 0.78f, 0.72f)
            );

            GameObject worker = CreateCharacter(
                root.transform,
                "NPC_WarehouseWorker",
                new Vector3(-4.7f, 1f, 1f),
                new Color(0.62f, 0.37f, 0.18f),
                new Color(0.85f, 0.58f, 0.24f),
                addNpcCollider: true
            );
            NpcDialogueInteractable workerDialogue = worker.AddComponent<NpcDialogueInteractable>();
            workerDialogue.Configure(
                "盐仓搬运工",
                "E  与盐仓搬运工交谈",
                new DialogueLine(
                    "盐仓搬运工",
                    "沈砚昨夜问我，盐仓里到底少了几车盐。我还没来得及数，他就不见了。"
                ),
                new DialogueLine(
                    "盐仓搬运工",
                    "今早伙计们都说从没这个人。可墙上还挂着他的斗笠，谁都拿不下来。"
                )
            );

            GameObject clue = CreateBlock(
                root.transform,
                "Clue_OldStreetSign",
                new Vector3(4.35f, 1.35f, -6f),
                new Vector3(1.6f, 1.5f, 0.22f),
                new Color(0.95f, 0.72f, 0.25f)
            );
            clue.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            EvidenceDefinition oldStreetEvidence = RequireEvidence(
                chapterContent,
                "old-street-sign"
            );
            ClueInteractable oldStreetSign = clue.AddComponent<ClueInteractable>();
            oldStreetSign.Configure(
                oldStreetEvidence.Id,
                oldStreetEvidence.InteractionPrompt,
                oldStreetEvidence.DiscoveredText
            );

            GameObject saltLedger = CreateBlock(
                root.transform,
                "Clue_SaltLedger",
                new Vector3(-4.35f, 1.1f, 2f),
                new Vector3(1.2f, 0.9f, 0.22f),
                new Color(0.82f, 0.56f, 0.25f)
            );
            saltLedger.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            EvidenceDefinition ledgerEvidence = RequireEvidence(
                chapterContent,
                "salt-ledger"
            );
            ClueInteractable ledger = saltLedger.AddComponent<ClueInteractable>();
            ledger.Configure(
                ledgerEvidence.Id,
                ledgerEvidence.InteractionPrompt,
                ledgerEvidence.DiscoveredText
            );

            GameObject officialSeal = CreateBlock(
                root.transform,
                "Clue_OfficialSeal",
                new Vector3(4.35f, 1.3f, 18f),
                new Vector3(1.15f, 1.45f, 0.22f),
                new Color(0.82f, 0.28f, 0.2f)
            );
            officialSeal.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            EvidenceDefinition sealEvidence = RequireEvidence(
                chapterContent,
                "official-seal"
            );
            ClueInteractable seal = officialSeal.AddComponent<ClueInteractable>();
            seal.Configure(
                sealEvidence.Id,
                sealEvidence.InteractionPrompt,
                sealEvidence.DiscoveredText
            );

            CreateInventoryPickup(
                root.transform,
                "Pickup_SaltToken",
                new Vector3(-1.8f, 0.55f, -4.8f),
                new Vector3(0.35f, 0.12f, 0.35f),
                new Color(0.92f, 0.68f, 0.2f),
                "pickup-salt-token",
                "salt-token-charm",
                1
            );
            CreateInventoryPickup(
                root.transform,
                "Pickup_BlankLedgerPage",
                new Vector3(-4.2f, 0.6f, 4.5f),
                new Vector3(0.7f, 0.08f, 0.9f),
                new Color(0.82f, 0.78f, 0.62f),
                "pickup-blank-ledger-page",
                "blank-ledger-page",
                2
            );

            CreateMemoryEcho(
                root.transform,
                "Echo_SaltToken",
                new Vector3(3.8f, 0.55f, -2.5f),
                "salt-token",
                "盐引铜钱",
                "铜钱边缘刻着沈砚的名字。茶肆掌柜说，这是沈砚失踪前留在桌上的最后一件事物。"
            );
            CreateMemoryEcho(
                root.transform,
                "Echo_FireAsh",
                new Vector3(13.8f, 0.55f, 12f),
                "warehouse-ash",
                "烧焦的账角",
                "灰烬里残留着二十年前的盐仓火印。那场火没有被记录，却一直留在旧仓的墙里。"
            );
            CreateMemoryEcho(
                root.transform,
                "Echo_SevenPages",
                new Vector3(0f, 0.62f, 25f),
                "seven-pages",
                "被撕走的七页",
                "七道纸影从街尾一直延伸到时缝。沈砚没有逃跑，他把账页藏进了每个人都能看见、却没人肯相信的地方。"
            );

            GameObject repairSite = CreateBlock(
                root.transform,
                "RepairSite_TimeRift",
                new Vector3(0f, 1.5f, 29f),
                new Vector3(2f, 3f, 0.6f),
                new Color(0.58f, 0.22f, 0.82f)
            );
            repairSite.AddComponent<RepairSite>();

            CreateTimeAnchor(
                root.transform,
                "TimeAnchor_Present_01",
                new Vector3(2.7f, 1.2f, 5f),
                "present-01",
                TimeLayer.Present,
                "东验锚点"
            );
            CreateTimeAnchor(
                root.transform,
                "TimeAnchor_Past_02",
                new Vector3(-2.7f, 1.2f, 14f),
                "past-02",
                TimeLayer.Past,
                "西仓锚点"
            );
            CreateTimeAnchor(
                root.transform,
                "TimeAnchor_Present_03",
                new Vector3(0f, 1.2f, 22f),
                "present-03",
                TimeLayer.Present,
                "中册锚点"
            );

            List<Combatant> enemies = new()
            {
                CreateEnemy(
                    root.transform,
                    "Enemy_Patrol_01",
                    new Vector3(-1.2f, 0.9f, 12f)
                )
            };
            GameObject blockadeZoneObject =
                new("Encounter_OfficialBlockade");
            blockadeZoneObject.transform.SetParent(root.transform);
            blockadeZoneObject.transform.position =
                new Vector3(0f, 0.9f, 12f);
            blockadeZoneObject.AddComponent<WorldEncounterZone>().Configure(
                EncounterDefinitionSeeder.LoadBlockade(),
                new[] { enemies[0] }
            );
            GameObject streamingRegionObject =
                new("Stream_SaltStreetSupport");
            streamingRegionObject.transform.SetParent(root.transform);
            streamingRegionObject.transform.position =
                new Vector3(1.4f, 0.9f, 20f);
            streamingRegionObject
                .AddComponent<WorldAdditiveSceneRegion>()
                .Configure(
                    "salt-street-support",
                    "SaltStreetSupport",
                    "盐仓横街增援",
                    20f,
                    38f,
                    46f,
                    false
                );

            GameObject preserveOutcome = CreateBlock(
                root.transform,
                "Outcome_OldStreetPreserved",
                new Vector3(0f, 0.15f, 30.5f),
                new Vector3(9f, 0.3f, 1.4f),
                new Color(0.28f, 0.7f, 0.42f)
            );
            preserveOutcome.SetActive(false);

            GameObject sealOutcome = CreateBlock(
                root.transform,
                "Outcome_OldStreetSealed",
                new Vector3(0f, 2f, 31f),
                new Vector3(14f, 4f, 0.5f),
                new Color(0.6f, 0.18f, 0.18f)
            );
            sealOutcome.SetActive(false);

            GameObject preserveChoice = CreateBlock(
                root.transform,
                "Choice_Preserve",
                new Vector3(-3f, 1.1f, 26f),
                new Vector3(1.3f, 2.2f, 0.3f),
                new Color(0.86f, 0.72f, 0.3f)
            );
            preserveChoice.AddComponent<EndingChoiceInteractable>().Configure(
                ConclusionChoice.Preserve,
                preserveOutcome,
                sealOutcome
            );

            GameObject sealChoice = CreateBlock(
                root.transform,
                "Choice_Seal",
                new Vector3(3f, 1.1f, 26f),
                new Vector3(1.3f, 2.2f, 0.3f),
                new Color(0.55f, 0.58f, 0.62f)
            );
            sealChoice.AddComponent<EndingChoiceInteractable>().Configure(
                ConclusionChoice.Seal,
                preserveOutcome,
                sealOutcome
            );

            GameObject publicRecordVisual = CreateBlock(
                root.transform,
                "Consequence_PublicRecord",
                new Vector3(-4.4f, 1.25f, 7.5f),
                new Vector3(1.8f, 2.15f, 0.22f),
                new Color(0.88f, 0.68f, 0.28f)
            );
            publicRecordVisual.AddComponent<NpcDialogueInteractable>().Configure(
                "官署抄录告示",
                "E  查看已经送交官署的记录",
                new DialogueLine(
                    "官署抄录告示",
                    "盐引巷七车盐账已抄录三份。沈砚的名字重新出现在官署门外的告示上，来往行人都能看见。"
                )
            );
            publicRecordVisual.SetActive(false);

            GameObject privateRecordVisual = CreateBlock(
                root.transform,
                "Consequence_ProtectedEvidence",
                new Vector3(14.5f, 0.7f, 10.5f),
                new Vector3(1.4f, 1.4f, 1.4f),
                new Color(0.3f, 0.19f, 0.1f)
            );
            privateRecordVisual.AddComponent<NpcDialogueInteractable>().Configure(
                "封存证物箱",
                "E  查看封存的账页副本",
                new DialogueLine(
                    "封存证物箱",
                    "账页副本藏在旧仓最暗的角落。只有沈伯和你知道这里，但真相也因此暂时无法被官署销毁。"
                )
            );
            privateRecordVisual.SetActive(false);

            GameObject gameSession = new GameObject("GameSession");
            gameSession.transform.SetParent(root.transform);
            gameSession.AddComponent<TimeLayerController>();
            InvestigationState investigationState =
                gameSession.AddComponent<InvestigationState>();
            investigationState.ConfigureContent(chapterContent);
            InventoryState inventoryState = gameSession.AddComponent<InventoryState>();
            inventoryState.Configure(chapterContent);
            EncounterState encounterState = gameSession.AddComponent<EncounterState>();
            encounterState.Configure(enemies);
            gameSession.AddComponent<DialogueController>();
            gameSession.AddComponent<OpeningSequence>();
            gameSession.AddComponent<ObjectiveDisplay>();
            gameSession.AddComponent<QuestNavigationHud>();
            gameSession.AddComponent<WorldMapController>();
            gameSession.AddComponent<InvestigationJournalController>();
            gameSession.AddComponent<InvestigationVisionController>();
            gameSession.AddComponent<TestimonyBoardController>();
            gameSession.AddComponent<SettingsController>();
            gameSession.AddComponent<SaveSlotController>();
            gameSession.AddComponent<StoryFactNotification>();
            gameSession.AddComponent<InventoryController>();
            gameSession.AddComponent<ContextHintController>();
            gameSession.AddComponent<RuntimeDiagnostics>();
            gameSession.AddComponent<ScreenFade>();
            gameSession.AddComponent<AreaBanner>();
            PlayerPartyController party = gameSession.AddComponent<PlayerPartyController>();
            party.Configure(player.transform, partner.transform);
            gameSession.AddComponent<ContinuationLoadout>();
            gameSession.AddComponent<PartyHud>();
            gameSession.AddComponent<AbilityHud>();
            gameSession.AddComponent<EliteEnemyHud>();
            gameSession.AddComponent<EliteBattleResultHud>();
            gameSession.AddComponent<CombatFeedbackHud>();
            gameSession.AddComponent<WorldContentToast>();
            gameSession.AddComponent<WorldContentTracker>().Configure(
                new[]
                {
                    new WorldContentEntry
                    {
                        ContentId = PaperKiteStoryController.ContentId,
                        Title = "东关纸鸢旧信",
                        AreaId = "dongguan-street",
                        AreaName = "东关街",
                        Hint = "与东关街说书人交谈",
                        EncounterId = PaperKiteStoryController.ReadyId,
                        MapX = 0.53f,
                        MapY = 0.48f
                    },
                    new WorldContentEntry
                    {
                        ContentId = "dongguan-apprentice-ledger",
                        Title = "脚夫看见的封条",
                        AreaId = "dongguan-street",
                        AreaName = "东关街",
                        Hint = "找到新来的脚夫，清除巡哨后回去交付",
                        EncounterId = "dongguan-patrol",
                        MapX = 0.45f,
                        MapY = 0.62f
                    },
                    new WorldContentEntry
                    {
                        ContentId = "caiyi-apprentice-ledger-page",
                        Title = "缝进袖口的纸",
                        AreaId = "caiyi-street",
                        AreaName = "彩衣街",
                        Hint = "询问裁衣学徒，解除望风后领取账页",
                        EncounterId = "caiyi-lookout",
                        MapX = 0.84f,
                        MapY = 0.3f
                    },
                    new WorldContentEntry
                    {
                        ContentId = "dongguan-rooftop-glide",
                        Title = "东关风廊",
                        AreaId = "dongguan-street",
                        AreaName = "东关街",
                        Hint = "在街口摊位旁进入风场，依次穿过屋顶航点",
                        MapX = 0.59f,
                        MapY = 0.57f
                    }
                }
            );
            gameSession.AddComponent<WorldEncounterDirector>();
            gameSession.AddComponent<CombatHud>();
            gameSession.AddComponent<GameFlowController>();
            gameSession.AddComponent<SaveSystem>();
            GameAudio gameAudio = gameSession.AddComponent<GameAudio>();
            ReadyMadeAudioSetup.ApplyTo(gameAudio);
            gameSession.AddComponent<UiAudioFeedback>();
            gameSession
                .AddComponent<EvidencePlanDisplay>()
                .Configure(publicRecordVisual, privateRecordVisual);

            GameObject chapterMarker = new GameObject("Chapter01_StorySetup");
            chapterMarker.transform.SetParent(root.transform);

            GameObject artMarker = new GameObject("Chapter01_ArtPass");
            artMarker.transform.SetParent(root.transform);

            GameObject visionMarker = new GameObject("Chapter01_VisionSystem");
            visionMarker.transform.SetParent(root.transform);

            GameObject polishMarker = new GameObject("Chapter01_ArchitecturePolish");
            polishMarker.transform.SetParent(root.transform);

            GameObject consequenceMarker = new GameObject("Chapter01_ChoiceConsequences");
            consequenceMarker.transform.SetParent(root.transform);

            GameObject animationMarker = new GameObject("Chapter01_ProceduralAnimation");
            animationMarker.transform.SetParent(root.transform);

            GameObject puzzleMarker = new GameObject("Chapter01_TimeAnchorPuzzle");
            puzzleMarker.transform.SetParent(root.transform);

            GameObject settingsMarker = new GameObject("Chapter01_SettingsSystem");
            settingsMarker.transform.SetParent(root.transform);

            GameObject saveSlotsMarker = new GameObject("Chapter01_MultiSlotSaves");
            saveSlotsMarker.transform.SetParent(root.transform);

            GameObject testimonyMarker = new GameObject("Chapter01_TestimonyBoard");
            testimonyMarker.transform.SetParent(root.transform);

            GameObject contentMarker = new GameObject("Chapter01_DataDrivenContent");
            contentMarker.transform.SetParent(root.transform);

            GameObject storyFactMarker = new GameObject("Chapter01_StoryFacts");
            storyFactMarker.transform.SetParent(root.transform);

            GameObject inventoryMarker = new GameObject("Chapter01_InventorySystem");
            inventoryMarker.transform.SetParent(root.transform);

            GameObject onboardingMarker = new GameObject("Chapter01_OnboardingGuidance");
            onboardingMarker.transform.SetParent(root.transform);

            GameObject diagnosticsMarker = new GameObject("Chapter01_RuntimeDiagnostics");
            diagnosticsMarker.transform.SetParent(root.transform);

            GameObject mapPolishMarker = new GameObject("Chapter01_MapReadability");
            mapPolishMarker.transform.SetParent(root.transform);

            GameObject publicPropMarker =
                new("Chapter01_PublicPropDressing");
            publicPropMarker.transform.SetParent(root.transform);

            GameObject frameworkMarker =
                new("Chapter01_OpenWorldFramework");
            frameworkMarker.transform.SetParent(root.transform);

            GameObject abilityMarker =
                new("Chapter01_AbilityFramework");
            abilityMarker.transform.SetParent(root.transform);

            GameObject encounterMarker =
                new("Chapter01_EncounterDirector");
            encounterMarker.transform.SetParent(root.transform);

            GameObject worldRuntimeObject = new GameObject("WorldRuntime");
            SceneManager.MoveGameObjectToScene(worldRuntimeObject, scene);
            WorldRuntime worldRuntime = worldRuntimeObject.AddComponent<WorldRuntime>();
            worldRuntime.SetCurrentArea("official-street", "官署前街");

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(root.transform);
            spawn.transform.position = new Vector3(0f, 0f, -10f);
            WorldSpawnPoint officialSpawn =
                spawn.AddComponent<WorldSpawnPoint>();
            officialSpawn.Configure("guangling_start");
            officialSpawn.ConfigureAsAnchor("官署前街", true);
            CreateWorldAnchor(
                root.transform,
                "Anchor_OfficialStreet",
                officialSpawn,
                new Color(0.93f, 0.76f, 0.36f)
            );

            CreateWorldArea(
                root.transform,
                "Area_OfficialStreet",
                new Vector3(0f, 1f, -8f),
                new Vector3(13f, 3f, 16f),
                "official-street",
                "官署前街"
            );
            CreateWorldArea(
                root.transform,
                "Area_SaltStreet",
                new Vector3(0f, 1f, 6f),
                new Vector3(9f, 3f, 12f),
                "salt-street",
                "盐仓横街"
            );
            CreateWorldArea(
                root.transform,
                "Area_TideAlley",
                new Vector3(0f, 1f, 22f),
                new Vector3(9f, 3f, 20f),
                "tide-alley",
                "回潮巷"
            );

            BuildDongguanStreet(root.transform, spawn.transform);

            Camera sceneCamera = scene
                .GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Camera>(includeInactive: true))
                .FirstOrDefault();

            if (sceneCamera != null)
            {
                sceneCamera.transform.position = new Vector3(0f, 5f, -16f);
                sceneCamera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
                sceneCamera.farClipPlane = 220f;
            }

            Light sun = scene
                .GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Light>(includeInactive: true))
                .FirstOrDefault(light => light.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.86f, 0.68f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.74f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.68f);
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 165f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.46f, 0.52f);
        }

        private static void CreateMapReadabilityDressing(Transform root)
        {
            Transform group = new GameObject("MapReadabilityDressing").transform;
            group.SetParent(root, false);

            Color roadEdge = new(0.30f, 0.29f, 0.26f);
            Color walkway = new(0.59f, 0.55f, 0.47f);
            CreateStreetEdgeRun(
                group,
                "OfficialStreet",
                centerX: 0f,
                centerZ: 5f,
                halfWidth: 5.8f,
                length: 52f,
                roadEdge,
                walkway
            );
            CreateStreetEdgeRun(
                group,
                "DongguanStreet",
                centerX: 60f,
                centerZ: 58f,
                halfWidth: 5.05f,
                length: 154f,
                roadEdge,
                walkway
            );
            CreateStreetEdgeRun(
                group,
                "CaiyiStreet",
                centerX: 60f,
                centerZ: 225f,
                halfWidth: 4.95f,
                length: 128f,
                roadEdge,
                new Color(0.54f, 0.54f, 0.5f)
            );

            CreateBlock(
                group,
                "OfficialStreet_PlazaInlay",
                new Vector3(0f, 0.025f, 6f),
                new Vector3(6.6f, 0.035f, 5.2f),
                new Color(0.52f, 0.49f, 0.42f)
            );

            CreateBlock(
                group,
                "CourtyardGate_Left",
                new Vector3(6.15f, 2.2f, 3f),
                new Vector3(0.45f, 4.4f, 0.45f),
                new Color(0.34f, 0.24f, 0.15f)
            );
            CreateBlock(
                group,
                "CourtyardGate_Right",
                new Vector3(6.15f, 2.2f, 9f),
                new Vector3(0.45f, 4.4f, 0.45f),
                new Color(0.34f, 0.24f, 0.15f)
            );
            CreateBlock(
                group,
                "CourtyardGate_Beam",
                new Vector3(6.15f, 4.55f, 6f),
                new Vector3(0.5f, 0.65f, 6.45f),
                new Color(0.42f, 0.3f, 0.18f)
            );

            CreateBlock(
                group,
                "OfficialStreet_EndGate_Left",
                new Vector3(-5.1f, 2.45f, 31.3f),
                new Vector3(0.55f, 4.9f, 0.65f),
                new Color(0.32f, 0.26f, 0.2f)
            );
            CreateBlock(
                group,
                "OfficialStreet_EndGate_Right",
                new Vector3(5.1f, 2.45f, 31.3f),
                new Vector3(0.55f, 4.9f, 0.65f),
                new Color(0.32f, 0.26f, 0.2f)
            );
            CreateBlock(
                group,
                "OfficialStreet_EndGate_Beam",
                new Vector3(0f, 4.8f, 31.3f),
                new Vector3(10.75f, 0.7f, 0.72f),
                new Color(0.44f, 0.33f, 0.2f)
            );
            CreateBlock(
                group,
                "OfficialStreet_EndGate_Plaque",
                new Vector3(0f, 5.28f, 31.2f),
                new Vector3(3.8f, 0.55f, 0.24f),
                new Color(0.78f, 0.59f, 0.25f)
            );
        }

        private static void CreateStreetEdgeRun(
            Transform parent,
            string streetName,
            float centerX,
            float centerZ,
            float halfWidth,
            float length,
            Color roadEdge,
            Color walkway)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float edgeX = centerX + side * halfWidth;
                float walkX = centerX + side * (halfWidth + 0.22f);
                CreateBlock(
                    parent,
                    $"{streetName}_Curb_{(side < 0 ? "Left" : "Right")}",
                    new Vector3(edgeX, 0.055f, centerZ),
                    new Vector3(0.14f, 0.11f, length),
                    roadEdge
                );
                CreateBlock(
                    parent,
                    $"{streetName}_Walk_{(side < 0 ? "Left" : "Right")}",
                    new Vector3(walkX, 0.025f, centerZ),
                    new Vector3(0.34f, 0.05f, length),
                    walkway
                );
            }
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color? tint = null)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            if (tint.HasValue)
            {
                block.AddComponent<ColorTint>().Configure(tint.Value, 0.12f);
            }

            return block;
        }

        private static EvidenceDefinition RequireEvidence(
            CaseDefinition content,
            string evidenceId)
        {
            EvidenceDefinition evidence = content.FindEvidence(evidenceId);
            if (evidence == null)
            {
                throw new System.InvalidOperationException(
                    $"Case content is missing evidence '{evidenceId}'."
                );
            }

            return evidence;
        }

        private static void CreateInventoryPickup(
            Transform parent,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Color tint,
            string pickupId,
            string itemId,
            int amount)
        {
            GameObject pickup = CreateBlock(
                parent,
                objectName,
                position,
                scale,
                tint
            );
            pickup.AddComponent<InventoryItemPickup>().Configure(
                pickupId,
                itemId,
                amount
            );
        }

        private static GameObject CreateCharacter(
            Transform parent,
            string name,
            Vector3 position,
            Color outfit,
            Color accent,
            bool addNpcCollider)
        {
            GameObject character = new(name);
            character.transform.SetParent(parent);
            character.transform.position = position;

            if (TryAttachBlockyCharacter(character, name))
            {
                if (addNpcCollider)
                {
                    ConfigureNpcCollider(character);
                }
                return character;
            }

            Color skin = new(0.82f, 0.66f, 0.52f);
            Color hair = new(0.13f, 0.09f, 0.075f);
            Color trousers = Color.Lerp(outfit, Color.black, 0.38f);

            CreateAvatarPart(
                character.transform,
                "Body",
                PrimitiveType.Capsule,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(0.72f, 0.64f, 0.72f),
                outfit
            );
            CreateAvatarPart(
                character.transform,
                "Sash",
                PrimitiveType.Cube,
                new Vector3(0f, -0.08f, -0.37f),
                new Vector3(0.18f, 0.58f, 0.05f),
                accent
            );
            CreateAvatarPart(
                character.transform,
                "Belt",
                PrimitiveType.Cube,
                new Vector3(0f, -0.24f, 0f),
                new Vector3(0.74f, 0.1f, 0.74f),
                Color.Lerp(accent, Color.black, 0.28f)
            );
            CreateAvatarPart(
                character.transform,
                "Head",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.67f, 0f),
                new Vector3(0.58f, 0.62f, 0.58f),
                skin
            );
            CreateAvatarPart(
                character.transform,
                "Hair",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.88f, -0.03f),
                new Vector3(0.6f, 0.25f, 0.6f),
                hair
            );

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject arm = CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftArm" : "RightArm",
                    PrimitiveType.Capsule,
                    new Vector3(0.43f * side, -0.03f, 0f),
                    new Vector3(0.17f, 0.39f, 0.17f),
                    outfit
                );
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, -13f * side);

                CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftLeg" : "RightLeg",
                    PrimitiveType.Capsule,
                    new Vector3(0.2f * side, -0.63f, 0f),
                    new Vector3(0.2f, 0.38f, 0.2f),
                    trousers
                );
                CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftShoe" : "RightShoe",
                    PrimitiveType.Cube,
                    new Vector3(0.2f * side, -0.89f, 0.08f),
                    new Vector3(0.3f, 0.13f, 0.45f),
                    new Color(0.1f, 0.08f, 0.065f)
                );
            }

            if (name.Contains("Player"))
            {
                CreateAvatarPart(
                    character.transform,
                    "RepairTool",
                    PrimitiveType.Cylinder,
                    new Vector3(0.47f, -0.22f, -0.08f),
                    new Vector3(0.13f, 0.34f, 0.13f),
                    new Color(0.52f, 0.4f, 0.18f)
                );
                CreateAvatarPart(
                    character.transform,
                    "RecordSatchel",
                    PrimitiveType.Cube,
                    new Vector3(-0.42f, -0.2f, -0.08f),
                    new Vector3(0.3f, 0.34f, 0.18f),
                    new Color(0.2f, 0.14f, 0.09f)
                );
            }
            else if (name.Contains("Partner"))
            {
                CreateAvatarPart(
                    character.transform,
                    "TravelBag",
                    PrimitiveType.Cube,
                    new Vector3(-0.43f, -0.2f, -0.08f),
                    new Vector3(0.28f, 0.3f, 0.16f),
                    new Color(0.4f, 0.25f, 0.12f)
                );
            }

            if (addNpcCollider)
            {
                ConfigureNpcCollider(character);
            }

            character.AddComponent<ProceduralCharacterAnimator>().Configure(addNpcCollider);

            return character;
        }

        private static bool TryAttachBlockyCharacter(
            GameObject character,
            string name)
        {
            string prefabPath =
                StylizedCharacterSetup.ResolveCharacterPrefab(name);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                prefabPath = BlockyCharacterSetup.ResolveCharacterPrefab(name);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return false;
                }
            }

            GameObject visual =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = "BlockyVisual";
            visual.transform.SetParent(character.transform, false);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            return true;
        }

        private static void ConfigureNpcCollider(GameObject character)
        {
            CapsuleCollider collider = character.AddComponent<CapsuleCollider>();
            collider.center = Vector3.zero;
            collider.height = 2f;
            collider.radius = 0.48f;
            character.AddComponent<NpcFacingController>();
        }

        private static GameObject CreateAvatarPart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Color tint)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.AddComponent<ColorTint>().Configure(tint, 0.32f);
            return part;
        }

        private static void CreateGliderVisual(
            Transform character,
            Color canopyColor,
            Color frameColor)
        {
            GameObject root = new("ExplorationGlider");
            root.transform.SetParent(character, false);
            root.transform.localPosition = new Vector3(0f, 0.82f, -0.12f);

            GameObject leftWing = CreateAvatarPart(
                root.transform,
                "Canopy_Left",
                PrimitiveType.Cube,
                new Vector3(-0.61f, 0.08f, 0f),
                new Vector3(1.16f, 0.055f, 0.72f),
                canopyColor
            );
            leftWing.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            GameObject rightWing = CreateAvatarPart(
                root.transform,
                "Canopy_Right",
                PrimitiveType.Cube,
                new Vector3(0.61f, 0.08f, 0f),
                new Vector3(1.16f, 0.055f, 0.72f),
                canopyColor
            );
            rightWing.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            CreateAvatarPart(
                root.transform,
                "Canopy_Ridge",
                PrimitiveType.Cube,
                new Vector3(0f, 0.11f, 0f),
                new Vector3(0.08f, 0.09f, 0.76f),
                frameColor
            );
            CreateAvatarPart(
                root.transform,
                "Glider_Crossbar",
                PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(2.36f, 0.045f, 0.055f),
                frameColor
            );
            root.SetActive(false);
        }

        private static void CreateAbilityPulse(
            Transform character,
            Color color)
        {
            GameObject pulse = CreateAvatarPart(
                character,
                "AbilityPulse",
                PrimitiveType.Cylinder,
                new Vector3(0f, -0.88f, 0f),
                new Vector3(0.3f, 0.035f, 0.3f),
                color
            );
            pulse.AddComponent<AbilityPulseVisual>();
            pulse.SetActive(false);
        }

        private static void CreateWorldAnchor(
            Transform parent,
            string name,
            WorldSpawnPoint spawnPoint,
            Color color)
        {
            GameObject anchor = new(name);
            anchor.transform.SetParent(parent);
            anchor.transform.position = spawnPoint.transform.position;
            SphereCollider trigger = anchor.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.45f;
            CreateAvatarPart(
                anchor.transform,
                "Anchor_Core",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.28f, 0f),
                new Vector3(0.34f, 0.24f, 0.34f),
                color
            );
            anchor.AddComponent<WorldAnchorInteractable>()
                .Configure(spawnPoint);
        }

        private static void CreateShopFacadeDetails(
            Transform parent,
            string namePrefix,
            Vector3 center,
            Vector3 size,
            float facadeDirection,
            int styleIndex,
            bool includeAwning = true)
        {
            Color roofColor = new(0.11f, 0.12f, 0.125f);
            Color woodColor = new(0.25f, 0.16f, 0.1f);
            Color doorColor = new(0.16f, 0.105f, 0.075f);
            Color lanternColor = new(0.72f, 0.105f, 0.075f);
            Color lanternCap = new(0.82f, 0.58f, 0.18f);
            Color[] bannerColors =
            {
                new Color(0.64f, 0.16f, 0.12f),
                new Color(0.16f, 0.42f, 0.43f),
                new Color(0.68f, 0.46f, 0.16f)
            };
            Color bannerColor = bannerColors[styleIndex % bannerColors.Length];
            float frontX = center.x + facadeDirection * size.x * 0.5f;
            float roofY = center.y + size.y * 0.5f + 0.28f;

            GameObject leftRoof = CreateDecorBlock(
                parent,
                $"{namePrefix}_RoofLeft",
                new Vector3(center.x - size.x * 0.24f, roofY + 0.22f, center.z),
                new Vector3(size.x * 0.64f, 0.18f, size.z + 1.25f),
                roofColor
            );
            leftRoof.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            GameObject rightRoof = CreateDecorBlock(
                parent,
                $"{namePrefix}_RoofRight",
                new Vector3(center.x + size.x * 0.24f, roofY + 0.22f, center.z),
                new Vector3(size.x * 0.64f, 0.18f, size.z + 1.25f),
                roofColor
            );
            rightRoof.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            CreateDecorBlock(
                parent,
                $"{namePrefix}_RoofRidge",
                new Vector3(center.x, roofY + 0.58f, center.z),
                new Vector3(0.28f, 0.22f, size.z + 1.3f),
                new Color(0.18f, 0.19f, 0.19f)
            );
            CreateDecorBlock(
                parent,
                $"{namePrefix}_Eave",
                new Vector3(frontX + facadeDirection * 0.18f, roofY - 0.08f, center.z),
                new Vector3(0.28f, 0.18f, size.z + 1.05f),
                woodColor
            );
            if (includeAwning)
            {
                GameObject awning = CreateDecorBlock(
                    parent,
                    $"{namePrefix}_Awning",
                    new Vector3(
                        frontX + facadeDirection * 0.72f,
                        3.15f,
                        center.z
                    ),
                    new Vector3(1.05f, 0.13f, size.z * 0.44f),
                    bannerColor
                );
                awning.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    -8f * facadeDirection
                );
                CreateDecorBlock(
                    parent,
                    $"{namePrefix}_AwningValance",
                    new Vector3(
                        frontX + facadeDirection * 1.16f,
                        2.92f,
                        center.z
                    ),
                    new Vector3(0.07f, 0.34f, size.z * 0.44f),
                    Color.Lerp(bannerColor, Color.black, 0.2f)
                );
            }
            CreateDecorBlock(
                parent,
                $"{namePrefix}_Door",
                new Vector3(frontX + facadeDirection * 0.04f, 1.35f, center.z),
                new Vector3(0.14f, 2.5f, Mathf.Min(2f, size.z * 0.3f)),
                doorColor
            );
            CreateDecorBlock(
                parent,
                $"{namePrefix}_DoorStep",
                new Vector3(frontX + facadeDirection * 0.34f, 0.1f, center.z),
                new Vector3(0.75f, 0.2f, Mathf.Min(2.5f, size.z * 0.34f)),
                new Color(0.27f, 0.27f, 0.25f)
            );
            CreateDecorBlock(
                parent,
                $"{namePrefix}_SignBoard",
                new Vector3(frontX + facadeDirection * 0.15f, 3.62f, center.z),
                new Vector3(0.12f, 0.72f, 1.45f),
                new Color(0.13f, 0.09f, 0.06f)
            );
            CreateDecorBlock(
                parent,
                $"{namePrefix}_SignFace",
                new Vector3(frontX + facadeDirection * 0.22f, 3.62f, center.z),
                new Vector3(0.06f, 0.56f, 1.18f),
                bannerColor
            );

            foreach (float windowOffset in new[]
            {
                -size.z * 0.24f,
                size.z * 0.24f
            })
            {
                CreateDecorBlock(
                    parent,
                    $"{namePrefix}_Window_{windowOffset:0}",
                    new Vector3(
                        frontX + facadeDirection * 0.08f,
                        1.85f,
                        center.z + windowOffset
                    ),
                    new Vector3(0.08f, 0.92f, 1.05f),
                    new Color(0.12f, 0.16f, 0.16f)
                );
            }
            CreateDecorBlock(
                parent,
                $"{namePrefix}_Banner",
                new Vector3(
                    frontX + facadeDirection * 0.09f,
                    3.95f,
                    center.z + size.z * 0.31f
                ),
                new Vector3(0.08f, 1.45f, 0.72f),
                bannerColor
            );

            foreach (float zOffset in new[] { -size.z * 0.34f, size.z * 0.34f })
            {
                CreateDecorBlock(
                    parent,
                    $"{namePrefix}_Column_{zOffset:0}",
                    new Vector3(frontX + facadeDirection * 0.12f, 1.55f, center.z + zOffset),
                    new Vector3(0.16f, 3.1f, 0.16f),
                    woodColor
                );
            }

            float lanternZ = center.z - size.z * 0.35f;
            CreateDecorBlock(
                parent,
                $"{namePrefix}_Lantern",
                new Vector3(frontX + facadeDirection * 0.34f, 2.72f, lanternZ),
                new Vector3(0.34f, 0.48f, 0.34f),
                lanternColor,
                PrimitiveType.Sphere
            );
            CreateDecorBlock(
                parent,
                $"{namePrefix}_LanternCap",
                new Vector3(frontX + facadeDirection * 0.34f, 3f, lanternZ),
                new Vector3(0.24f, 0.1f, 0.24f),
                lanternCap
            );
        }

        private static GameObject CreateDecorBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color tint,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.position = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.AddComponent<ColorTint>().Configure(tint, 0.22f);
            return part;
        }

        private static void CreateMemoryEcho(
            Transform parent,
            string name,
            Vector3 position,
            string echoId,
            string title,
            string text)
        {
            GameObject echo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            echo.name = name;
            echo.transform.SetParent(parent);
            echo.transform.position = position;
            echo.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
            echo.AddComponent<ColorTint>().Configure(new Color(0.98f, 0.76f, 0.3f), 0.55f);

            SphereCollider trigger = echo.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            echo.AddComponent<MemoryEchoInteractable>().Configure(echoId, title, text);
        }

        private static void CreateStonePaving(
            Transform parent,
            string name,
            Vector3 center,
            float width,
            float length,
            Color baseColor)
        {
            int laneCount = Mathf.Max(3, Mathf.RoundToInt(width / 2.4f));
            float laneWidth = width / laneCount;
            for (int lane = 0; lane < laneCount; lane++)
            {
                float x = center.x - width * 0.5f + laneWidth * (lane + 0.5f);
                Color stoneColor = lane % 2 == 0
                    ? baseColor
                    : Color.Lerp(baseColor, new Color(0.23f, 0.24f, 0.23f), 0.16f);
                CreateDecorBlock(
                    parent,
                    $"{name}_Lane_{lane + 1}",
                    new Vector3(x, center.y + 0.012f, center.z),
                    new Vector3(laneWidth * 0.94f, 0.024f, length),
                    stoneColor
                );
            }

            Color seamColor = Color.Lerp(baseColor, Color.black, 0.34f);
            for (float z = center.z - length * 0.5f + 3f;
                z < center.z + length * 0.5f;
                z += 4.5f)
            {
                CreateDecorBlock(
                    parent,
                    $"{name}_Seam_{z:0}",
                    new Vector3(center.x, center.y + 0.03f, z),
                    new Vector3(width * 0.96f, 0.02f, 0.055f),
                    seamColor
                );
            }

            CreateDecorBlock(
                parent,
                $"{name}_CurbLeft",
                new Vector3(center.x - width * 0.5f + 0.2f, center.y + 0.12f, center.z),
                new Vector3(0.38f, 0.24f, length),
                new Color(0.3f, 0.3f, 0.28f)
            );
            CreateDecorBlock(
                parent,
                $"{name}_CurbRight",
                new Vector3(center.x + width * 0.5f - 0.2f, center.y + 0.12f, center.z),
                new Vector3(0.38f, 0.24f, length),
                new Color(0.3f, 0.3f, 0.28f)
            );
        }

        private static Combatant CreateEnemy(Transform parent, string name, Vector3 position)
        {
            GameObject enemy = new(name);
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;

            GameObject blockVisual =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockVisual.name = "GreyboxVisual";
            blockVisual.transform.SetParent(enemy.transform, false);
            blockVisual.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
            Object.DestroyImmediate(blockVisual.GetComponent<BoxCollider>());

            GameObject blockyPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StylizedCharacterSetup.EnemyPrefabPath
                );
            if (blockyPrefab == null)
            {
                blockyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    BlockyCharacterSetup.EnemyPrefabPath
                );
            }
            if (blockyPrefab != null)
            {
                GameObject visual =
                    (GameObject)PrefabUtility.InstantiatePrefab(blockyPrefab);
                visual.name = "BlockyVisual";
                visual.transform.SetParent(enemy.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.9f, 0f);
                blockVisual.GetComponent<MeshRenderer>().enabled = false;
            }

            enemy.AddComponent<ColorTint>().Configure(
                new Color(0.68f, 0.16f, 0.16f)
            );
            enemy.AddComponent<CharacterController>();
            Combatant combatant = enemy.AddComponent<Combatant>();
            combatant.Configure(CombatTeam.Enemy, 60f);
            enemy.AddComponent<ResonanceStatus>();
            enemy.AddComponent<EnemyBreakStatus>();
            enemy.AddComponent<EnemyAI>();
            return combatant;
        }

        private static void CreateWorldArea(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            string areaId,
            string displayName)
        {
            GameObject area = new GameObject(name);
            area.transform.SetParent(parent);
            area.transform.position = position;

            BoxCollider trigger = area.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;

            Rigidbody body = area.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            area.AddComponent<WorldArea>().Configure(areaId, displayName);
        }

        private static void BuildWarehouseCourtyard(Transform root)
        {
            CreateBlock(
                root,
                "CourtyardGround",
                new Vector3(18f, -0.55f, 6f),
                new Vector3(24f, 1f, 20f),
                new Color(0.48f, 0.44f, 0.36f)
            );
            CreateBlock(
                root,
                "Courtyard_NorthWall",
                new Vector3(18f, 2f, -4f),
                new Vector3(24f, 4f, 2f),
                new Color(0.24f, 0.28f, 0.3f)
            );
            CreateBlock(
                root,
                "Courtyard_SouthWall",
                new Vector3(18f, 2f, 16f),
                new Vector3(24f, 4f, 2f),
                new Color(0.24f, 0.28f, 0.3f)
            );
            CreateBlock(
                root,
                "Courtyard_EndWall",
                new Vector3(30f, 2f, 6f),
                new Vector3(2f, 4f, 20f),
                new Color(0.3f, 0.27f, 0.25f)
            );
            BuildWarehouseInterior(root);
            CreateBlock(
                root,
                "Courtyard_Crate_A",
                new Vector3(12f, 0.75f, 2f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.5f, 0.32f, 0.17f)
            );
            CreateBlock(
                root,
                "Courtyard_Crate_B",
                new Vector3(14f, 1f, 10f),
                new Vector3(2f, 2f, 2f),
                new Color(0.5f, 0.32f, 0.17f)
            );

            CreateWorldArea(
                root,
                "Area_WarehouseCourtyard",
                new Vector3(12f, 1f, 6f),
                new Vector3(10f, 3f, 18f),
                "warehouse-courtyard",
                "旧仓内院"
            );

            GameObject keeper = CreateCharacter(
                root,
                "NPC_ShenBo",
                new Vector3(16.5f, 1f, 8f),
                new Color(0.48f, 0.38f, 0.22f),
                new Color(0.76f, 0.6f, 0.32f),
                addNpcCollider: true
            );
            keeper.AddComponent<ShenBoInteractable>();

            GameObject notice = CreateBlock(
                root,
                "Courtyard_NoticeBoard",
                new Vector3(8.8f, 1.25f, 11f),
                new Vector3(1.8f, 2.2f, 0.25f),
                new Color(0.9f, 0.7f, 0.3f)
            );
            notice.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            NpcDialogueInteractable noticeDialogue = notice.AddComponent<NpcDialogueInteractable>();
            noticeDialogue.Configure(
                "旧仓告示",
                "E  查看旧仓告示",
                new DialogueLine(
                    "旧仓告示",
                    "“仓中货物，只记其来处，不记其去处。”告示右下角还有一行被刮掉的小字。"
                )
            );
        }

        private static void BuildWarehouseInterior(Transform root)
        {
            Color wallColor = new(0.28f, 0.34f, 0.36f);
            Color woodColor = new(0.45f, 0.3f, 0.18f);

            CreateBlock(
                root,
                "Warehouse_Floor",
                new Vector3(23f, 0.03f, 9f),
                new Vector3(10f, 0.12f, 7f),
                new Color(0.38f, 0.31f, 0.23f)
            );
            CreateBlock(
                root,
                "Warehouse_NorthWall",
                new Vector3(23f, 2.5f, 12.5f),
                new Vector3(10f, 5f, 0.4f),
                wallColor
            );
            CreateBlock(
                root,
                "Warehouse_SouthWall",
                new Vector3(23f, 2.5f, 5.5f),
                new Vector3(10f, 5f, 0.4f),
                wallColor
            );
            CreateBlock(
                root,
                "Warehouse_EastWall",
                new Vector3(28f, 2.5f, 9f),
                new Vector3(0.4f, 5f, 7f),
                wallColor
            );
            CreateBlock(
                root,
                "Warehouse_WestWall_North",
                new Vector3(18f, 2.5f, 11.5f),
                new Vector3(0.4f, 5f, 2.4f),
                wallColor
            );
            CreateBlock(
                root,
                "Warehouse_WestWall_South",
                new Vector3(18f, 2.5f, 6.2f),
                new Vector3(0.4f, 5f, 1.6f),
                wallColor
            );
            CreateBlock(
                root,
                "Warehouse_DoorBeam",
                new Vector3(18f, 4.4f, 8.8f),
                new Vector3(0.5f, 0.7f, 3f),
                woodColor
            );
            CreateBlock(
                root,
                "Warehouse_Shelf_A",
                new Vector3(25.5f, 1.3f, 10.8f),
                new Vector3(3.5f, 2.6f, 0.8f),
                woodColor
            );
            CreateBlock(
                root,
                "Warehouse_Shelf_B",
                new Vector3(21f, 1.1f, 7f),
                new Vector3(2.8f, 2.2f, 0.8f),
                woodColor
            );
            CreateBlock(
                root,
                "Warehouse_Table",
                new Vector3(23f, 0.6f, 9f),
                new Vector3(2.4f, 0.8f, 1.4f),
                new Color(0.5f, 0.34f, 0.2f)
            );

            CreateWorldArea(
                root,
                "Area_WarehouseInterior",
                new Vector3(23f, 1f, 9f),
                new Vector3(8f, 3f, 5.5f),
                "warehouse-interior",
                "旧仓室内"
            );

            GameObject ledger = CreateBlock(
                root,
                "Warehouse_MissingLedger",
                new Vector3(23f, 1.12f, 9f),
                new Vector3(1.3f, 0.16f, 0.8f),
                new Color(0.92f, 0.7f, 0.24f)
            );
            NpcDialogueInteractable ledgerDialogue = ledger.AddComponent<NpcDialogueInteractable>();
            ledgerDialogue.Configure(
                "缺页账簿",
                "E  调查缺页账簿",
                new DialogueLine(
                    "缺页账簿",
                    "账簿中间少了整整七页。残留的装订线上，还能看出被人匆忙撕走的痕迹。"
                ),
                new DialogueLine(
                    "主角",
                    "这些页不是自然损坏。有人先改写了这里，再让整条旧街从记录里消失。"
                )
            );
        }

        private static void BuildDongguanStreet(Transform root, Transform guanglingSpawn)
        {
            Transform areaRoot = new GameObject("DongguanStreet").transform;
            areaRoot.SetParent(root);

            CreateBlock(
                areaRoot,
                "Dongguan_Ground",
                new Vector3(60f, -0.55f, 58f),
                new Vector3(20f, 1f, 160f),
                new Color(0.52f, 0.48f, 0.4f)
            );
            CreateStonePaving(
                areaRoot,
                "DongguanPaving",
                new Vector3(60f, 0f, 58f),
                19f,
                158f,
                new Color(0.43f, 0.4f, 0.35f)
            );

            float[] shopPositions =
            {
                -12f, 0f, 12f, 24f, 36f, 48f,
                60f, 72f, 84f, 96f, 108f, 120f
            };
            for (int index = 0; index < shopPositions.Length; index++)
            {
                float z = shopPositions[index];
                Color leftColor = index % 2 == 0
                    ? new Color(0.38f, 0.25f, 0.2f)
                    : new Color(0.42f, 0.31f, 0.22f);
                Color rightColor = index % 2 == 0
                    ? new Color(0.29f, 0.3f, 0.27f)
                    : new Color(0.36f, 0.24f, 0.24f);

                CreateBlock(
                    areaRoot,
                    $"Dongguan_LeftShop_{index + 1}",
                    new Vector3(52f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    leftColor
                );
                CreateBlock(
                    areaRoot,
                    $"Dongguan_RightShop_{index + 1}",
                    new Vector3(68f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    rightColor
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Dongguan_LeftShop_{index + 1}",
                    new Vector3(52f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: 1f,
                    styleIndex: index
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Dongguan_RightShop_{index + 1}",
                    new Vector3(68f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: -1f,
                    styleIndex: index + 1
                );
            }

            CreateBlock(
                areaRoot,
                "Dongguan_EndWall",
                new Vector3(60f, 2f, 135f),
                new Vector3(18f, 4f, 2f),
                new Color(0.36f, 0.27f, 0.22f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Arch_Left",
                new Vector3(56.5f, 2f, -20f),
                new Vector3(0.8f, 4f, 0.8f),
                new Color(0.5f, 0.32f, 0.18f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Arch_Right",
                new Vector3(63.5f, 2f, -20f),
                new Vector3(0.8f, 4f, 0.8f),
                new Color(0.5f, 0.32f, 0.18f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Arch_Beam",
                new Vector3(60f, 4.1f, -20f),
                new Vector3(7.8f, 0.8f, 0.8f),
                new Color(0.62f, 0.42f, 0.2f)
            );

            CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Left",
                new Vector3(56.5f, 2f, 58f),
                new Vector3(0.7f, 4f, 0.7f),
                new Color(0.62f, 0.4f, 0.2f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Right",
                new Vector3(63.5f, 2f, 58f),
                new Vector3(0.7f, 4f, 0.7f),
                new Color(0.62f, 0.4f, 0.2f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Beam",
                new Vector3(60f, 4f, 58f),
                new Vector3(7.8f, 0.7f, 0.7f),
                new Color(0.72f, 0.5f, 0.24f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Well",
                new Vector3(56.9f, 0.55f, 59f),
                new Vector3(1.8f, 1.1f, 1.8f),
                new Color(0.34f, 0.4f, 0.42f)
            );

            CreateBlock(
                areaRoot,
                "Dongguan_Stall_A",
                new Vector3(57f, 0.7f, 8f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.7f, 0.35f, 0.22f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Stall_B",
                new Vector3(63f, 0.7f, 38f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.25f, 0.5f, 0.48f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Stall_C",
                new Vector3(57f, 0.7f, 84f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.4f, 0.54f, 0.3f)
            );
            CreateBlock(
                areaRoot,
                "Dongguan_Stall_D",
                new Vector3(63f, 0.7f, 112f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.66f, 0.38f, 0.18f)
            );

            GameObject dongguanSpawn = new("DongguanSpawn");
            dongguanSpawn.transform.SetParent(areaRoot);
            dongguanSpawn.transform.position = new Vector3(60f, 0f, -16f);
            WorldSpawnPoint dongguanAnchor =
                dongguanSpawn.AddComponent<WorldSpawnPoint>();
            dongguanAnchor.Configure("dongguan_start");
            dongguanAnchor.ConfigureAsAnchor("东关街", false);
            CreateWorldAnchor(
                areaRoot,
                "Anchor_DongguanStreet",
                dongguanAnchor,
                new Color(0.31f, 0.78f, 0.72f)
            );
            GameObject dongguanStreamObject =
                new("Stream_DongguanPatrol");
            dongguanStreamObject.transform.SetParent(areaRoot);
            dongguanStreamObject.transform.position =
                new Vector3(60f, 0.9f, 44f);
            dongguanStreamObject
                .AddComponent<WorldAdditiveSceneRegion>()
                .Configure(
                    "dongguan-patrol",
                    "DongguanPatrol",
                    "东关街巡哨",
                    18f,
                    34f,
                    44f,
                    false
                );

            GameObject glideChallengeObject =
                new("Challenge_DongguanWindCourse");
            glideChallengeObject.transform.SetParent(areaRoot);
            glideChallengeObject.transform.position =
                new Vector3(57f, 0f, 8f);
            GlideChallengeController glideChallenge =
                glideChallengeObject.AddComponent<GlideChallengeController>();
            glideChallenge.Configure(
                "dongguan-rooftop-glide",
                new[]
                {
                    new Vector3(58f, 5f, 12f),
                    new Vector3(60f, 7f, 20f),
                    new Vector3(60f, 6f, 30f),
                    new Vector3(60f, 4f, 42f),
                    new Vector3(60f, 2.5f, 54f)
                },
                40f
            );
            BoxCollider windTrigger =
                glideChallengeObject.AddComponent<BoxCollider>();
            windTrigger.isTrigger = true;
            windTrigger.center = new Vector3(0f, 2.5f, 0f);
            windTrigger.size = new Vector3(4f, 6f, 4f);
            Rigidbody windBody =
                glideChallengeObject.AddComponent<Rigidbody>();
            windBody.isKinematic = true;
            windBody.useGravity = false;
            glideChallengeObject
                .AddComponent<WindLiftZone>()
                .Configure(glideChallenge, 8f);

            CreateWorldArea(
                areaRoot,
                "Area_DongguanStreet",
                new Vector3(60f, 1f, 56f),
                new Vector3(10f, 3f, 154f),
                "dongguan-street",
                "东关街"
            );

            GameObject gateway = CreateBlock(
                root,
                "Gate_ToDongguan",
                new Vector3(-4.7f, 1.2f, -14f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.86f, 0.62f, 0.22f)
            );
            gateway.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            gateway.AddComponent<LocalWorldPortal>().Configure(
                dongguanSpawn.transform,
                "dongguan-street",
                "东关街",
                "E  前往东关街"
            );

            GameObject returnGate = CreateBlock(
                areaRoot,
                "Gate_ReturnGuangling",
                new Vector3(60f, 1.2f, -21f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.8f, 0.55f, 0.2f)
            );
            returnGate.AddComponent<LocalWorldPortal>().Configure(
                guanglingSpawn,
                "official-street",
                "官署前街",
                "E  返回官署前街"
            );

            GameObject storyteller = CreateCharacter(
                areaRoot,
                "NPC_DongguanStoryteller",
                new Vector3(56f, 1f, -4f),
                new Color(0.2f, 0.4f, 0.58f),
                new Color(0.72f, 0.58f, 0.3f),
                addNpcCollider: true
            );

            GameObject paperKiteRoot =
                new("Story_DongguanPaperKite");
            paperKiteRoot.transform.SetParent(areaRoot);
            paperKiteRoot.transform.position =
                new Vector3(60f, 0f, 28f);

            GameObject presentTrace = CreateBlock(
                paperKiteRoot.transform,
                "Trace_PaperKiteCord_Present",
                new Vector3(56.8f, 1.15f, 16f),
                new Vector3(0.22f, 1.6f, 0.22f),
                new Color(0.3f, 0.68f, 0.64f)
            );

            GameObject pastTrace = CreateBlock(
                paperKiteRoot.transform,
                "Trace_PaperKiteBell_Past",
                new Vector3(63.2f, 1.05f, 27f),
                new Vector3(0.28f, 1.35f, 0.28f),
                new Color(0.78f, 0.64f, 0.32f)
            );
            pastTrace
                .AddComponent<TimeLayerVisibility>()
                .Configure(TimeLayer.Past);

            GameObject windlineKnot = CreateBlock(
                paperKiteRoot.transform,
                "Anchor_OldWindlineKnot",
                new Vector3(60f, 1.05f, 54f),
                new Vector3(0.3f, 1.55f, 0.3f),
                new Color(0.3f, 0.72f, 0.76f)
            );

            GameObject publicConsequence =
                new("Consequence_PaperKitePublicName");
            publicConsequence.transform.SetParent(
                paperKiteRoot.transform
            );
            CreateBlock(
                publicConsequence.transform,
                "PublicNoticePost",
                new Vector3(62.6f, 0.62f, 35f),
                new Vector3(0.14f, 1.24f, 0.14f),
                new Color(0.28f, 0.18f, 0.1f)
            );
            CreateBlock(
                publicConsequence.transform,
                "PublicNoticeBoard",
                new Vector3(62.6f, 1.42f, 35f),
                new Vector3(0.92f, 0.58f, 0.1f),
                new Color(0.68f, 0.46f, 0.2f)
            );
            publicConsequence.SetActive(false);

            GameObject privateConsequence =
                new("Consequence_PaperKitePrivateMemory");
            privateConsequence.transform.SetParent(
                paperKiteRoot.transform
            );
            CreateBlock(
                privateConsequence.transform,
                "PrivateMemoryPost",
                new Vector3(57.4f, 0.72f, 35f),
                new Vector3(0.13f, 1.44f, 0.13f),
                new Color(0.25f, 0.2f, 0.14f)
            );
            CreateBlock(
                privateConsequence.transform,
                "PrivateMemorySilk",
                new Vector3(57.4f, 1.18f, 35.18f),
                new Vector3(0.08f, 0.7f, 0.38f),
                new Color(0.28f, 0.62f, 0.72f)
            );
            privateConsequence.SetActive(false);

            PaperKiteStoryController paperKiteStory =
                paperKiteRoot.AddComponent<PaperKiteStoryController>();
            paperKiteStory.Configure(
                storyteller.transform,
                presentTrace.transform,
                pastTrace.transform,
                windlineKnot.transform,
                glideChallenge,
                publicConsequence,
                privateConsequence
            );

            storyteller
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.Storyteller
                );
            presentTrace
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.PresentTrace
                );
            pastTrace
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.PastTrace
                );
            windlineKnot
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.WindlineKnot
                );

            GameObject clothMerchant = CreateCharacter(
                areaRoot,
                "NPC_ClothMerchant",
                new Vector3(63.5f, 1f, 64f),
                new Color(0.48f, 0.22f, 0.32f),
                new Color(0.85f, 0.55f, 0.28f),
                addNpcCollider: true
            );
            NpcDialogueInteractable merchantDialogue =
                clothMerchant.AddComponent<NpcDialogueInteractable>();
            merchantDialogue.Configure(
                "绸缎商",
                "E  与绸缎商交谈",
                new DialogueLine(
                    "绸缎商",
                    "沈砚前日来扯过一尺青布，说要给父亲缝个新账袋。钱是我亲手收的，账上却找不到这笔买卖。"
                ),
                new DialogueLine(
                    "绸缎商",
                    "若连买卖都能被改，东关街还算得上是谁的街？去旧仓看看吧，他父亲一直守在那里。"
                )
            );

            GameObject teaMaster = CreateCharacter(
                areaRoot,
                "NPC_TeaMaster",
                new Vector3(56.5f, 1f, 112f),
                new Color(0.24f, 0.46f, 0.27f),
                new Color(0.78f, 0.62f, 0.32f),
                addNpcCollider: true
            );
            NpcDialogueInteractable teaDialogue =
                teaMaster.AddComponent<NpcDialogueInteractable>();
            teaDialogue.ConfigureData("teahouse");
            CreateInventoryPickup(
                areaRoot,
                "Pickup_GingerTea",
                new Vector3(58f, 0.62f, 111.5f),
                new Vector3(0.42f, 0.3f, 0.42f),
                new Color(0.55f, 0.26f, 0.12f),
                "pickup-ginger-tea",
                "ginger-tea",
                2
            );

            BuildCaiyiStreet(root, dongguanSpawn.transform);
        }

        private static void BuildCaiyiStreet(Transform root, Transform dongguanSpawn)
        {
            Transform areaRoot = new GameObject("CaiyiStreet").transform;
            areaRoot.SetParent(root);

            CreateBlock(
                areaRoot,
                "Caiyi_Ground",
                new Vector3(60f, -0.56f, 225f),
                new Vector3(18f, 1f, 130f),
                new Color(0.4f, 0.42f, 0.43f)
            );
            CreateStonePaving(
                areaRoot,
                "CaiyiPaving",
                new Vector3(60f, 0.005f, 225f),
                17f,
                128f,
                new Color(0.35f, 0.36f, 0.36f)
            );

            float[] shopPositions =
            {
                170f, 181f, 192f, 203f, 214f, 225f,
                236f, 247f, 258f, 269f, 280f
            };

            for (int index = 0; index < shopPositions.Length; index++)
            {
                float z = shopPositions[index];
                float height = 4.5f + (index % 3) * 0.8f;
                Color leftWall = index % 2 == 0
                    ? new Color(0.58f, 0.55f, 0.5f)
                    : new Color(0.5f, 0.45f, 0.4f);
                Color rightWall = index % 2 == 0
                    ? new Color(0.5f, 0.42f, 0.38f)
                    : new Color(0.57f, 0.52f, 0.46f);

                CreateBlock(
                    areaRoot,
                    $"Caiyi_LeftShop_{index + 1}",
                    new Vector3(52.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    leftWall
                );
                CreateBlock(
                    areaRoot,
                    $"Caiyi_RightShop_{index + 1}",
                    new Vector3(67.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    rightWall
                );

                CreateBlock(
                    areaRoot,
                    $"Caiyi_LeftAwning_{index + 1}",
                    new Vector3(55.8f, 2.9f, z),
                    new Vector3(2.2f, 0.18f, 3.2f),
                    index % 2 == 0
                        ? new Color(0.72f, 0.22f, 0.18f)
                        : new Color(0.22f, 0.48f, 0.5f)
                );
                CreateBlock(
                    areaRoot,
                    $"Caiyi_RightAwning_{index + 1}",
                    new Vector3(64.2f, 2.9f, z),
                    new Vector3(2.2f, 0.18f, 3.2f),
                    index % 2 == 0
                        ? new Color(0.68f, 0.46f, 0.16f)
                        : new Color(0.56f, 0.22f, 0.32f)
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Caiyi_LeftShop_{index + 1}",
                    new Vector3(52.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    facadeDirection: 1f,
                    styleIndex: index + 1
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Caiyi_RightShop_{index + 1}",
                    new Vector3(67.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    facadeDirection: -1f,
                    styleIndex: index + 2
                );
            }

            for (int index = 0; index < 8; index++)
            {
                float z = 174f + index * 15f;
                CreateCaiyiTree(
                    areaRoot,
                    $"Caiyi_Tree_{index + 1}",
                    new Vector3(index % 2 == 0 ? 55.2f : 64.8f, 0f, z)
                );
            }

            for (int index = 0; index < 6; index++)
            {
                CreateBlock(
                    areaRoot,
                    $"Caiyi_CrosswalkStart_{index + 1}",
                    new Vector3(56.2f + index * 1.5f, 0.02f, 170f),
                    new Vector3(0.8f, 0.04f, 4.2f),
                    new Color(0.85f, 0.85f, 0.78f)
                );
            }

            CreateBlock(
                areaRoot,
                "Caiyi_EndWall",
                new Vector3(60f, 2f, 290f),
                new Vector3(18f, 4f, 2f),
                new Color(0.32f, 0.29f, 0.26f)
            );

            GameObject caiyiSpawn = new("CaiyiSpawn");
            caiyiSpawn.transform.SetParent(areaRoot);
            caiyiSpawn.transform.position = new Vector3(60f, 0f, 166f);
            WorldSpawnPoint caiyiAnchor =
                caiyiSpawn.AddComponent<WorldSpawnPoint>();
            caiyiAnchor.Configure("caiyi_start");
            caiyiAnchor.ConfigureAsAnchor("彩衣街", false);
            CreateWorldAnchor(
                areaRoot,
                "Anchor_CaiyiStreet",
                caiyiAnchor,
                new Color(0.78f, 0.38f, 0.24f)
            );
            GameObject caiyiStreamObject =
                new("Stream_CaiyiLookout");
            caiyiStreamObject.transform.SetParent(areaRoot);
            caiyiStreamObject.transform.position =
                new Vector3(60f, 0.9f, 220f);
            caiyiStreamObject
                .AddComponent<WorldAdditiveSceneRegion>()
                .Configure(
                    "caiyi-lookout",
                    "CaiyiLookout",
                    "彩衣街望风",
                    18f,
                    34f,
                    44f,
                    false
                );

            CreateWorldArea(
                areaRoot,
                "Area_CaiyiStreet",
                new Vector3(60f, 1f, 230f),
                new Vector3(10f, 3f, 120f),
                "caiyi-street",
                "彩衣街"
            );

            GameObject caiyiGate = CreateBlock(
                areaRoot,
                "Gate_ReturnDongguan",
                new Vector3(60f, 1.2f, 162f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.55f, 0.32f, 0.18f)
            );
            caiyiGate.AddComponent<LocalWorldPortal>().Configure(
                dongguanSpawn,
                "dongguan-street",
                "东关街",
                "E  返回东关街"
            );

            GameObject dongguanToCaiyi = CreateBlock(
                root,
                "Gate_DongguanToCaiyi",
                new Vector3(64.5f, 1.2f, -17.5f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.85f, 0.32f, 0.2f)
            );
            dongguanToCaiyi.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            dongguanToCaiyi.AddComponent<LocalWorldPortal>().Configure(
                caiyiSpawn.transform,
                "caiyi-street",
                "彩衣街",
                "E  前往彩衣街"
            );

            GameObject tailor = CreateCharacter(
                areaRoot,
                "NPC_CaiyiTailor",
                new Vector3(56f, 1f, 190f),
                new Color(0.55f, 0.24f, 0.26f),
                new Color(0.8f, 0.58f, 0.3f),
                addNpcCollider: true
            );
            NpcDialogueInteractable tailorDialogue =
                tailor.AddComponent<NpcDialogueInteractable>();
            tailorDialogue.Configure(
                "裁衣铺掌柜",
                "E  与裁衣铺掌柜交谈",
                new DialogueLine(
                    "裁衣铺掌柜",
                    "盐引巷从前就在旧仓后面。那里的媳妇常来我这儿挑青布，说盐灰落上去也不显脏。"
                ),
                new DialogueLine(
                    "裁衣铺掌柜",
                    "街和衣裳一样。少了一段，旁人未必看得出来，住在这里的人却知道。"
                )
            );

            GameObject snackVendor = CreateCharacter(
                areaRoot,
                "NPC_CaiyiSnackVendor",
                new Vector3(64f, 1f, 245f),
                new Color(0.66f, 0.36f, 0.13f),
                new Color(0.88f, 0.65f, 0.3f),
                addNpcCollider: true
            );
            NpcDialogueInteractable vendorDialogue =
                snackVendor.AddComponent<NpcDialogueInteractable>();
            vendorDialogue.Configure(
                "小吃摊主",
                "E  与小吃摊主交谈",
                new DialogueLine(
                    "小吃摊主",
                    "以前盐船靠岸，搬运工会来我这儿买热汤。最近少了盐引巷的客人，生意都冷清了。"
                )
            );
        }

        private static void CreateCaiyiTree(Transform parent, string name, Vector3 position)
        {
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"{name}_Trunk";
            trunk.transform.SetParent(parent);
            trunk.transform.position = position + Vector3.up * 1.4f;
            trunk.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
            Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());
            trunk.AddComponent<ColorTint>().Configure(new Color(0.3f, 0.2f, 0.12f));

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = $"{name}_Canopy";
            canopy.transform.SetParent(parent);
            canopy.transform.position = position + Vector3.up * 3.5f;
            canopy.transform.localScale = new Vector3(3.4f, 1.8f, 3.4f);
            Object.DestroyImmediate(canopy.GetComponent<SphereCollider>());
            canopy.AddComponent<ColorTint>().Configure(new Color(0.18f, 0.42f, 0.2f));
        }

        private static void CreateTimeAnchor(
            Transform parent,
            string name,
            Vector3 position,
            string anchorId,
            TimeLayer layer,
            string displayName)
        {
            GameObject anchor = CreateBlock(
                parent,
                name,
                position,
                new Vector3(1.1f, 2.4f, 0.35f),
                layer == TimeLayer.Present
                    ? new Color(0.2f, 0.78f, 0.88f)
                    : new Color(0.78f, 0.32f, 0.92f)
            );
            anchor.AddComponent<TimeLayerVisibility>().Configure(layer);
            anchor.AddComponent<TimeAnchor>().Configure(anchorId, displayName);
        }
    }
}
