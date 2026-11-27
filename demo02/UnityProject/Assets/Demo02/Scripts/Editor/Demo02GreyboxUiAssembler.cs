using System.Collections.Generic;
using System.Linq;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    /// <summary>
    /// Owns runtime UI controllers, HUD wiring, chapter markers and presentation.
    /// </summary>
    internal static class Demo02GreyboxUiAssembler
    {
        public static void BuildGameSession(
            Transform root,
            CaseDefinition chapterContent,
            GameObject player,
            GameObject partner,
            IReadOnlyList<Combatant> enemies,
            GameObject publicRecordVisual,
            GameObject privateRecordVisual)
        {
            GameObject gameSession = new("GameSession");
            gameSession.transform.SetParent(root);
            gameSession.AddComponent<TimeLayerController>();
            InvestigationState investigationState =
                gameSession.AddComponent<InvestigationState>();
            investigationState.ConfigureContent(chapterContent);
            InventoryState inventoryState =
                gameSession.AddComponent<InventoryState>();
            inventoryState.Configure(chapterContent);
            EncounterState encounterState =
                gameSession.AddComponent<EncounterState>();
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
            gameSession.AddComponent<QuickHealController>();
            gameSession.AddComponent<ContextHintController>();
            gameSession.AddComponent<RuntimeDiagnostics>();
            gameSession.AddComponent<ScreenFade>();
            gameSession.AddComponent<AreaBanner>();
            PlayerPartyController party =
                gameSession.AddComponent<PlayerPartyController>();
            party.Configure(player.transform, partner.transform);
            gameSession.AddComponent<ContinuationLoadout>();
            gameSession.AddComponent<PartyHud>();
            gameSession.AddComponent<AbilityHud>();
            gameSession.AddComponent<EliteEnemyHud>();
            gameSession.AddComponent<EliteBattleResultHud>();
            gameSession.AddComponent<CombatFeedbackHud>();
            gameSession.AddComponent<WorldContentToast>();
            BuildWorldContentTracker(gameSession);
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
        }

        public static void CreateChapterMarkers(Transform root)
        {
            foreach (string markerName in new[]
                     {
                         "Chapter01_StorySetup",
                         "Chapter01_ArtPass",
                         "Chapter01_VisionSystem",
                         "Chapter01_ArchitecturePolish",
                         "Chapter01_ChoiceConsequences",
                         "Chapter01_ProceduralAnimation",
                         "Chapter01_TimeAnchorPuzzle",
                         "Chapter01_SettingsSystem",
                         "Chapter01_MultiSlotSaves",
                         "Chapter01_TestimonyBoard",
                         "Chapter01_DataDrivenContent",
                         "Chapter01_StoryFacts",
                         "Chapter01_InventorySystem",
                         "Chapter01_OnboardingGuidance",
                         "Chapter01_RuntimeDiagnostics",
                         "Chapter01_MapReadability",
                         "Chapter01_PublicPropDressing",
                         "Chapter01_OpenWorldFramework",
                         "Chapter01_AbilityFramework",
                         "Chapter01_EncounterDirector"
                     })
            {
                GameObject marker = new(markerName);
                marker.transform.SetParent(root);
            }
        }

        public static void ApplyWorldPresentation(Scene scene)
        {
            Camera sceneCamera = scene
                .GetRootGameObjects()
                .SelectMany(item =>
                    item.GetComponentsInChildren<Camera>(
                        includeInactive: true
                    ))
                .FirstOrDefault();
            if (sceneCamera != null)
            {
                sceneCamera.transform.position =
                    new Vector3(0f, 5f, -16f);
                sceneCamera.transform.rotation =
                    Quaternion.Euler(20f, 0f, 0f);
                sceneCamera.farClipPlane = 220f;
            }

            Light sun = scene
                .GetRootGameObjects()
                .SelectMany(item =>
                    item.GetComponentsInChildren<Light>(
                        includeInactive: true
                    ))
                .FirstOrDefault(light =>
                    light.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.86f, 0.68f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.74f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor =
                new Color(0.55f, 0.62f, 0.68f);
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 165f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(0.42f, 0.46f, 0.52f);
        }

        private static void BuildWorldContentTracker(GameObject gameSession)
        {
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
        }
    }
}
