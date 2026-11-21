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
        private const string RootName = "Greybox";

        [InitializeOnLoadMethod]
        private static void QueueInitialBuild()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Demo02/Rebuild Greybox Scene")]
        public static void RebuildFromMenu()
        {
            BuildScene(forceRebuild: true);
            BuildBootScene();
            ConfigureBuildSettings();
        }

        public static void RebuildForBatch()
        {
            BuildScene(forceRebuild: true);
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
            VerifyCombatDamageForBatch();
            VerifyTimeLayerForBatch();
            VerifyChapterFlowForBatch();
            VerifySaveSlotsForBatch();
            Debug.Log(
                "All automated checks passed: dialogue, combat, time layer, "
                + "chapter and saves."
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
            if (teahouseRumor == null
                || teahouseRumor.Id != "teahouse-rumor-seal")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The teahouse event did not react to the official seal."
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
                || crossExamine.Id != "shenbo-cross-examine")
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
                        || data.EchoIds.Count != 1)
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
                EchoIds = new List<string> { "echo-coin" }
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
                2
            );
            if (state.Stage != ChapterStage.Resolved
                || state.SelectedConclusion != ConclusionChoice.Seal
                || state.Stance != WitnessStance.PersonFirst
                || state.Plan != EvidencePlan.PrivateProtection
                || !state.TestimonyContradictionSolved
                || state.ContradictionAttempts != 2)
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
            if (bootScene == null || greyboxScene == null)
            {
                Debug.LogWarning("World scenes are not ready for Build Settings yet.");
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
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
                new Vector3(16f, 1f, 54f),
                new Color(0.55f, 0.52f, 0.46f)
            );
            ground.isStatic = true;
            CreateStonePaving(
                root.transform,
                "OfficialStreetPaving",
                new Vector3(0f, 0f, 5f),
                15f,
                52f,
                new Color(0.46f, 0.43f, 0.38f)
            );

            float[] buildingZPositions = { -14f, -4f, 6f, 16f, 26f };
            foreach (float z in buildingZPositions)
            {
                GameObject left = CreateBlock(
                    root.transform,
                    $"LeftBuilding_{z:0}",
                    new Vector3(-7f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    new Color(0.23f, 0.28f, 0.34f)
                );
                GameObject right = null;
                if (!Mathf.Approximately(z, 6f))
                {
                    right = CreateBlock(
                        root.transform,
                        $"RightBuilding_{z:0}",
                        new Vector3(7f, 3f, z),
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
                new Vector3(14f, 4f, 2f),
                new Color(0.36f, 0.3f, 0.28f)
            );
            endWall.isStatic = true;

            CreateBlock(
                root.transform,
                "Crate_A",
                new Vector3(2.5f, 0.75f, -2f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.45f, 0.3f, 0.17f)
            );

            BuildWarehouseCourtyard(root.transform);
            CreateBlock(
                root.transform,
                "Crate_B",
                new Vector3(-2.5f, 1f, 14f),
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
                new Vector3(2.7f, 1.2f, -6f),
                new Vector3(2.2f, 1.8f, 0.25f),
                new Color(0.95f, 0.72f, 0.25f)
            );
            clue.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
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
                new Vector3(-2.7f, 1.1f, 2f),
                new Vector3(1.6f, 1.1f, 0.25f),
                new Color(0.82f, 0.56f, 0.25f)
            );
            saltLedger.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
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
                new Vector3(2.7f, 1.2f, 18f),
                new Vector3(1.4f, 1.8f, 0.25f),
                new Color(0.82f, 0.28f, 0.2f)
            );
            officialSeal.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
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
                CreateEnemy(root.transform, "Enemy_Patrol_01", new Vector3(-1.2f, 0.9f, 12f)),
                CreateEnemy(root.transform, "Enemy_Patrol_02", new Vector3(1.4f, 0.9f, 24f))
            };

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
            gameSession.AddComponent<ScreenFade>();
            gameSession.AddComponent<AreaBanner>();
            PlayerPartyController party = gameSession.AddComponent<PlayerPartyController>();
            party.Configure(player.transform, partner.transform);
            gameSession.AddComponent<CombatHud>();
            gameSession.AddComponent<GameFlowController>();
            gameSession.AddComponent<SaveSystem>();
            gameSession.AddComponent<GameAudio>();
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

            GameObject worldRuntimeObject = new GameObject("WorldRuntime");
            SceneManager.MoveGameObjectToScene(worldRuntimeObject, scene);
            WorldRuntime worldRuntime = worldRuntimeObject.AddComponent<WorldRuntime>();
            worldRuntime.SetCurrentArea("official-street", "官署前街");

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(root.transform);
            spawn.transform.position = new Vector3(0f, 0f, -10f);
            spawn.AddComponent<WorldSpawnPoint>().Configure("guangling_start");

            CreateWorldArea(
                root.transform,
                "Area_OfficialStreet",
                new Vector3(0f, 1f, -8f),
                new Vector3(9f, 3f, 16f),
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
                CapsuleCollider collider = character.AddComponent<CapsuleCollider>();
                collider.center = Vector3.zero;
                collider.height = 2f;
                collider.radius = 0.48f;
                character.AddComponent<NpcFacingController>();
            }

            character.AddComponent<ProceduralCharacterAnimator>().Configure(addNpcCollider);

            return character;
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
            GameObject enemy = CreateBlock(
                parent,
                name,
                position,
                new Vector3(0.9f, 1.8f, 0.9f),
                new Color(0.68f, 0.16f, 0.16f)
            );
            Object.DestroyImmediate(enemy.GetComponent<BoxCollider>());
            enemy.AddComponent<CharacterController>();
            Combatant combatant = enemy.AddComponent<Combatant>();
            combatant.Configure(CombatTeam.Enemy, 60f);
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
            dongguanSpawn.AddComponent<WorldSpawnPoint>().Configure("dongguan_start");

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
            NpcDialogueInteractable storytellerDialogue =
                storyteller.AddComponent<NpcDialogueInteractable>();
            storytellerDialogue.Configure(
                "说书人",
                "E  与说书人交谈",
                new DialogueLine(
                    "说书人",
                    "二十年前有场盐仓大火，第二天整条盐引巷就从官图上没了。老辈人都说，那不是火，是有人拿纸盖住了火。"
                ),
                new DialogueLine(
                    "说书人",
                    "城里的故事总要有人讲。你若真找到那七页账，别让它只留在官府手里。"
                )
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
            caiyiSpawn.AddComponent<WorldSpawnPoint>().Configure("caiyi_start");

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
