using System.Collections.Generic;
using System.Linq;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    /// <summary>
    /// Coordinates category assemblers without owning their content details.
    /// </summary>
    internal static class Demo02GreyboxSceneAssembler
    {
        public static void BuildScene(bool forceRebuild)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene greyboxScene =
                SceneManager.GetSceneByPath(
                    Demo02GreyboxScenePaths.ScenePath
                );
            bool wasAlreadyLoaded =
                greyboxScene.IsValid() && greyboxScene.isLoaded;
            Scene previousActiveScene = SceneManager.GetActiveScene();

            if (!wasAlreadyLoaded)
            {
                greyboxScene = EditorSceneManager.OpenScene(
                    Demo02GreyboxScenePaths.ScenePath,
                    OpenSceneMode.Additive
                );
            }

            GameObject existingRoot = greyboxScene
                .GetRootGameObjects()
                .FirstOrDefault(root =>
                    root.name == Demo02GreyboxScenePaths.RootName);
            HashSet<string> existingStreamingRegionIds =
                existingRoot != null
                    ? existingRoot
                        .GetComponentsInChildren<
                            WorldAdditiveSceneRegion
                        >(true)
                        .Select(region => region.RegionId)
                        .ToHashSet()
                    : new HashSet<string>();
            IReadOnlyList<WorldStreamingRegionDefinition>
                streamingDefinitions =
                    WorldStreamingRegionSeeder.LoadAll();
            bool needsUpgrade = NeedsUpgrade(
                existingRoot,
                existingStreamingRegionIds,
                streamingDefinitions
            );

            if (existingRoot == null || forceRebuild || needsUpgrade)
            {
                GameObject existingRuntime = greyboxScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root =>
                        root.name == "WorldRuntime");
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
                AssembleGeometry(greyboxScene, chapterContent);
                GameObject generatedRoot = greyboxScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root =>
                        root.name ==
                        Demo02GreyboxScenePaths.RootName);
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
                Demo02GreyboxRegionAssembler
                    .CreateMapReadabilityDressing(
                        generatedRoot.transform
                    );
                PublicPropDressingSetup.ApplyToGreybox(
                    generatedRoot.transform
                );
                EditorSceneManager.MarkSceneDirty(greyboxScene);
                EditorSceneManager.SaveScene(greyboxScene);
                Debug.Log("Demo02 greybox scene generated.");
            }

            if (!wasAlreadyLoaded)
            {
                EditorSceneManager.CloseScene(
                    greyboxScene,
                    removeScene: true
                );
            }
            if (previousActiveScene.IsValid()
                && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static void AssembleGeometry(
            Scene scene,
            CaseDefinition chapterContent)
        {
            GameObject root =
                new(Demo02GreyboxScenePaths.RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject runtimeObject = new("WorldRuntime");
            SceneManager.MoveGameObjectToScene(runtimeObject, scene);
            WorldRuntime runtime =
                runtimeObject.AddComponent<WorldRuntime>();
            runtime.SetCurrentArea("official-street", "官署前街");

            Demo02GreyboxRegionAssembler.BuildOfficialStreet(
                root.transform
            );

            GameObject playerSpawn = new("PlayerSpawn");
            playerSpawn.transform.SetParent(root.transform);
            playerSpawn.transform.position =
                new Vector3(0f, 0f, -10f);
            WorldSpawnPoint officialSpawn =
                playerSpawn.AddComponent<WorldSpawnPoint>();
            officialSpawn.Configure("guangling_start");
            officialSpawn.ConfigureAsAnchor("官署前街", true);
            Demo02SceneObjectFactory.CreateWorldAnchor(
                root.transform,
                "Anchor_OfficialStreet",
                officialSpawn,
                new Color(0.93f, 0.76f, 0.36f)
            );

            Demo02GreyboxGameplayAssembler.BuildPlayerRig(
                root.transform,
                out GameObject player,
                out GameObject partner
            );
            Demo02GreyboxNpcAssembler.BuildOfficialStreet(
                root.transform,
                out _,
                out _,
                out _,
                out _
            );
            Demo02GreyboxNpcAssembler.BuildWarehouse(root.transform);
            Demo02GreyboxTaskAssembler.BuildWarehouse(root.transform);
            Demo02GreyboxTaskAssembler.BuildOfficialStreet(
                root.transform,
                chapterContent,
                out GameObject publicRecordVisual,
                out GameObject privateRecordVisual
            );
            Demo02GreyboxGameplayAssembler.BuildTimeAnchors(
                root.transform
            );
            IReadOnlyList<Combatant> enemies =
                Demo02GreyboxGameplayAssembler
                    .BuildOfficialStreetEncounter(root.transform);

            Demo02GreyboxRegionAssembler.BuildCottageCourtyard(
                root.transform,
                playerSpawn.transform
            );

            Demo02GreyboxRegionAssembler.BuildDongguanStreet(
                root.transform,
                playerSpawn.transform,
                out Transform dongguanArea,
                out Transform dongguanSpawn
            );
            Demo02GreyboxNpcAssembler.BuildDongguan(
                dongguanArea,
                out GameObject storyteller,
                out _,
                out _
            );
            GlideChallengeController glideChallenge =
                Demo02GreyboxGameplayAssembler
                    .BuildDongguanChallenge(dongguanArea);
            Demo02GreyboxTaskAssembler.BuildDongguanStory(
                dongguanArea,
                storyteller,
                glideChallenge
            );
            Demo02GreyboxTaskAssembler.BuildDongguanRewards(
                dongguanArea
            );

            Demo02GreyboxRegionAssembler.BuildCaiyiStreet(
                root.transform,
                dongguanSpawn,
                out Transform caiyiArea,
                out _
            );
            Demo02GreyboxNpcAssembler.BuildCaiyi(
                caiyiArea,
                out _,
                out _
            );

            Demo02GreyboxUiAssembler.CreateChapterMarkers(
                root.transform
            );
            Demo02GreyboxUiAssembler.BuildGameSession(
                root.transform,
                chapterContent,
                player,
                partner,
                enemies,
                publicRecordVisual,
                privateRecordVisual
            );
            Demo02GreyboxUiAssembler.ApplyWorldPresentation(scene);
        }

        private static bool NeedsUpgrade(
            GameObject existingRoot,
            ISet<string> existingStreamingRegionIds,
            IReadOnlyList<WorldStreamingRegionDefinition>
                streamingDefinitions)
        {
            if (existingRoot == null)
            {
                return false;
            }
            Transform root = existingRoot.transform;
            return root.Find("Clue_OldStreetSign") == null
                || root.Find("Clue_SaltLedger") == null
                || root.Find("Clue_OfficialSeal") == null
                || root.Find("RepairSite_TimeRift") == null
                || root.Find("PartnerPlaceholder") == null
                || root.Find("Enemy_Patrol_01") == null
                || root.Find("Choice_Preserve") == null
                || root.Find("PlayerPlaceholder/MeleeArc") == null
                || root.Find("TimeAnchor_Present_01") == null
                || root.Find("Area_OfficialStreet") == null
                || root.Find("Area_WarehouseCourtyard") == null
                || root.Find("Area_WarehouseInterior") == null
                || root.Find("DongguanStreet") == null
                || root.Find("CaiyiStreet") == null
                || root.Find("NPC_WarehouseWorker") == null
                || root.Find("NPC_ShenBo") == null
                || root.Find("NPC_ArchiveClerk") == null
                || root.Find("ConditionalNPC_AHe") == null
                || root.Find("ConditionalNPC_LuChong") == null
                || root.Find("Chapter01_StorySetup") == null
                || root.Find("Chapter01_ArtPass") == null
                || root.Find("Chapter01_VisionSystem") == null
                || root.Find("Chapter01_ArchitecturePolish") == null
                || root.Find("Chapter01_ChoiceConsequences") == null
                || root.Find("Chapter01_ProceduralAnimation") == null
                || root.Find("Chapter01_TimeAnchorPuzzle") == null
                || root.Find("Chapter01_SettingsSystem") == null
                || root.Find("Chapter01_DataDrivenContent") == null
                || root.Find("Chapter01_StoryFacts") == null
                || root.Find("Chapter01_InventorySystem") == null
                || root.Find("Chapter01_OnboardingGuidance") == null
                || root.Find("Chapter01_RuntimeDiagnostics") == null
                || root.Find("Chapter01_MapReadability") == null
                || root.Find("Chapter01_PublicPropDressing") == null
                || root.Find("Chapter01_OpenWorldFramework") == null
                || root.Find("Chapter01_AbilityFramework") == null
                || root.Find("Chapter01_EncounterDirector") == null
                || streamingDefinitions.Any(definition =>
                    !existingStreamingRegionIds.Contains(
                        definition.RegionId
                    ))
                || root.Find(
                    "DongguanStreet/Challenge_DongguanWindCourse"
                ) == null
                || root.Find(
                    "DongguanStreet/ART_Dongguan_PavilionLandmark"
                ) == null
                || root.Find(
                    "DongguanStreet/ART_Dongguan_DownloadedTreeLine"
                ) == null
                || existingRoot.GetComponentInChildren<
                    QuickHealController
                >(true) == null
                || root.Find("RepairSite_TimeRift")
                    ?.GetComponent<WorldObjectId>() == null
                || root.Find("Choice_Preserve")
                    ?.GetComponent<WorldObjectId>() == null
                || root.Find("NPC_ShenBo")
                    ?.GetComponent<WorldObjectId>() == null
                || root.Find("NPC_WarehouseWorker")
                    ?.GetComponent<WorldObjectId>() == null
                || root.Find("GameSession") == null;
        }
    }
}
