using System.Collections.Generic;
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
                BuildGeometry(greyboxScene);
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

        private static void BuildGeometry(Scene scene)
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

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PlayerPlaceholder";
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0f, 1f, -10f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.AddComponent<ColorTint>().Configure(new Color(0.25f, 0.55f, 0.9f));
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

            GameObject partner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            partner.name = "PartnerPlaceholder";
            partner.transform.SetParent(root.transform);
            partner.transform.position = new Vector3(-1.6f, 1f, -11.5f);
            Object.DestroyImmediate(partner.GetComponent<CapsuleCollider>());
            partner.AddComponent<ColorTint>().Configure(new Color(0.3f, 0.75f, 0.55f));
            partner.AddComponent<CharacterController>();
            ThirdPersonController partnerController = partner.AddComponent<ThirdPersonController>();
            partnerController.enabled = false;
            PlayerCombat partnerCombat = partner.AddComponent<PlayerCombat>();
            partnerCombat.enabled = false;
            PlayerInteractor partnerInteractor = partner.AddComponent<PlayerInteractor>();
            partnerInteractor.enabled = false;
            partner.AddComponent<Combatant>().Configure(CombatTeam.Player, 100f);

            GameObject worker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            worker.name = "NPC_WarehouseWorker";
            worker.transform.SetParent(root.transform);
            worker.transform.position = new Vector3(-4.7f, 1f, 1f);
            worker.AddComponent<ColorTint>().Configure(new Color(0.85f, 0.5f, 0.18f));
            NpcDialogueInteractable workerDialogue = worker.AddComponent<NpcDialogueInteractable>();
            workerDialogue.Configure(
                "盐仓搬运工",
                "E  与盐仓搬运工交谈",
                new DialogueLine(
                    "盐仓搬运工",
                    "旧街的货昨夜还在仓库里。今早一到，账上的入库记录却全没了。"
                ),
                new DialogueLine(
                    "盐仓搬运工",
                    "我只是个搬货的。可要是整条街都能被从纸上抹掉，下一个会不会轮到我们？"
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
            ClueInteractable oldStreetSign = clue.AddComponent<ClueInteractable>();
            oldStreetSign.Configure(
                "old-street-sign",
                "E  调查旧街招牌",
                "招牌背面留着被刮掉的旧街名。有人不希望这条街继续存在。"
            );

            GameObject saltLedger = CreateBlock(
                root.transform,
                "Clue_SaltLedger",
                new Vector3(-2.7f, 1.1f, 2f),
                new Vector3(1.6f, 1.1f, 0.25f),
                new Color(0.82f, 0.56f, 0.25f)
            );
            saltLedger.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            ClueInteractable ledger = saltLedger.AddComponent<ClueInteractable>();
            ledger.Configure(
                "salt-ledger",
                "E  调查盐仓账牌",
                "旧街的货物昨夜仍从盐仓运出，现存记录却写着它们从未入仓。"
            );

            GameObject officialSeal = CreateBlock(
                root.transform,
                "Clue_OfficialSeal",
                new Vector3(2.7f, 1.2f, 18f),
                new Vector3(1.4f, 1.8f, 0.25f),
                new Color(0.82f, 0.28f, 0.2f)
            );
            officialSeal.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
            ClueInteractable seal = officialSeal.AddComponent<ClueInteractable>();
            seal.Configure(
                "official-seal",
                "E  调查官府封条",
                "封条日期早于旧街重新出现的时间。官府在事件发生前就已经知道这片街区。"
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
                TimeLayer.Present
            );
            CreateTimeAnchor(
                root.transform,
                "TimeAnchor_Past_02",
                new Vector3(-2.7f, 1.2f, 14f),
                "past-02",
                TimeLayer.Past
            );
            CreateTimeAnchor(
                root.transform,
                "TimeAnchor_Present_03",
                new Vector3(0f, 1.2f, 22f),
                "present-03",
                TimeLayer.Present
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

            GameObject gameSession = new GameObject("GameSession");
            gameSession.transform.SetParent(root.transform);
            gameSession.AddComponent<TimeLayerController>();
            gameSession.AddComponent<InvestigationState>();
            EncounterState encounterState = gameSession.AddComponent<EncounterState>();
            encounterState.Configure(enemies);
            gameSession.AddComponent<DialogueController>();
            gameSession.AddComponent<OpeningSequence>();
            gameSession.AddComponent<ObjectiveDisplay>();
            gameSession.AddComponent<QuestNavigationHud>();
            gameSession.AddComponent<WorldMapController>();
            gameSession.AddComponent<ScreenFade>();
            gameSession.AddComponent<AreaBanner>();
            PlayerPartyController party = gameSession.AddComponent<PlayerPartyController>();
            party.Configure(player.transform, partner.transform);
            gameSession.AddComponent<CombatHud>();
            gameSession.AddComponent<GameFlowController>();
            gameSession.AddComponent<SaveSystem>();

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
                block.AddComponent<ColorTint>().Configure(tint.Value);
            }

            return block;
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

            GameObject keeper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            keeper.name = "NPC_OldStorekeeper";
            keeper.transform.SetParent(root);
            keeper.transform.position = new Vector3(16.5f, 1f, 8f);
            keeper.AddComponent<ColorTint>().Configure(new Color(0.76f, 0.6f, 0.32f));
            NpcDialogueInteractable keeperDialogue = keeper.AddComponent<NpcDialogueInteractable>();
            keeperDialogue.Configure(
                "守仓老人",
                "E  与守仓老人交谈",
                new DialogueLine(
                    "守仓老人",
                    "这道院墙后头，原本还有一整排旧铺。地图没画，账上也没写，可我还记得它们的位置。"
                ),
                new DialogueLine(
                    "守仓老人",
                    "年轻人，别只看纸上留下了什么。也看看纸外被拿走了什么。"
                )
            );

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

            GameObject storyteller = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            storyteller.name = "NPC_DongguanStoryteller";
            storyteller.transform.SetParent(areaRoot);
            storyteller.transform.position = new Vector3(56f, 1f, -4f);
            storyteller.AddComponent<ColorTint>().Configure(new Color(0.3f, 0.56f, 0.72f));
            NpcDialogueInteractable storytellerDialogue =
                storyteller.AddComponent<NpcDialogueInteractable>();
            storytellerDialogue.Configure(
                "说书人",
                "E  与说书人交谈",
                new DialogueLine(
                    "说书人",
                    "东关街的铺子一日比一日多，可老住户却一日比一日少。你说是生意变了，还是人变了？"
                ),
                new DialogueLine(
                    "说书人",
                    "城里的故事总要有人讲。没人讲的，久了就像从来没发生过。"
                )
            );

            GameObject clothMerchant = CreateBlock(
                areaRoot,
                "NPC_ClothMerchant",
                new Vector3(63.5f, 1f, 64f),
                new Vector3(1.1f, 2f, 1.1f),
                new Color(0.55f, 0.28f, 0.38f)
            );
            NpcDialogueInteractable merchantDialogue =
                clothMerchant.AddComponent<NpcDialogueInteractable>();
            merchantDialogue.Configure(
                "绸缎商",
                "E  与绸缎商交谈",
                new DialogueLine(
                    "绸缎商",
                    "这几日布料上的花色总在变。昨日的客人说，他明明买过一样的料子，账上却查不到。"
                ),
                new DialogueLine(
                    "绸缎商",
                    "若连买卖都能被改，东关街还算得上是谁的街？"
                )
            );

            GameObject teaMaster = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teaMaster.name = "NPC_TeaMaster";
            teaMaster.transform.SetParent(areaRoot);
            teaMaster.transform.position = new Vector3(56.5f, 1f, 112f);
            teaMaster.AddComponent<ColorTint>().Configure(new Color(0.35f, 0.62f, 0.35f));
            NpcDialogueInteractable teaDialogue =
                teaMaster.AddComponent<NpcDialogueInteractable>();
            teaDialogue.Configure(
                "茶肆掌柜",
                "E  与茶肆掌柜交谈",
                new DialogueLine(
                    "茶肆掌柜",
                    "东关街最怕的不是客人少，是昨天来坐过的人，今天没人记得。"
                ),
                new DialogueLine(
                    "茶肆掌柜",
                    "茶凉了还能续，人会从纸上消失，可就续不回来了。"
                )
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

            GameObject tailor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tailor.name = "NPC_CaiyiTailor";
            tailor.transform.SetParent(areaRoot);
            tailor.transform.position = new Vector3(56f, 1f, 190f);
            tailor.AddComponent<ColorTint>().Configure(new Color(0.65f, 0.3f, 0.32f));
            NpcDialogueInteractable tailorDialogue =
                tailor.AddComponent<NpcDialogueInteractable>();
            tailorDialogue.Configure(
                "裁衣铺掌柜",
                "E  与裁衣铺掌柜交谈",
                new DialogueLine(
                    "裁衣铺掌柜",
                    "彩衣街的衣裳讲究配色，一段布接错了，整件衣裳都不是原来的样子。"
                ),
                new DialogueLine(
                    "裁衣铺掌柜",
                    "街和衣裳也一样。少了一段，旁人未必看得出来，住在这里的人却知道。"
                )
            );

            GameObject snackVendor = CreateBlock(
                areaRoot,
                "NPC_CaiyiSnackVendor",
                new Vector3(64f, 1f, 245f),
                new Vector3(1.1f, 2f, 1.1f),
                new Color(0.78f, 0.45f, 0.18f)
            );
            NpcDialogueInteractable vendorDialogue =
                snackVendor.AddComponent<NpcDialogueInteractable>();
            vendorDialogue.Configure(
                "小吃摊主",
                "E  与小吃摊主交谈",
                new DialogueLine(
                    "小吃摊主",
                    "街口那棵树我从小看到大。可前些天有人指着这里说，这条路从前根本没有树。"
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
            TimeLayer layer)
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
            anchor.AddComponent<TimeAnchor>().Configure(anchorId);
        }
    }
}
