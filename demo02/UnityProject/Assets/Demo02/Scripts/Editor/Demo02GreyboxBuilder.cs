using System.Collections.Generic;
using System.Linq;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class Demo02GreyboxBuilder
    {
        private const string ScenePath = "Assets/Demo02/Scenes/Guangling/Greybox.unity";
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
        }

        public static void RebuildForBatch()
        {
            BuildScene(forceRebuild: true);
        }

        private static void BuildIfNeeded()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorApplication.delayCall += BuildIfNeeded;
                return;
            }

            BuildScene(forceRebuild: false);
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
                    || existingRoot.transform.Find("GameSession") == null);

            if (existingRoot == null || forceRebuild || needsUpgrade)
            {
                if (existingRoot != null)
                {
                    Object.DestroyImmediate(existingRoot);
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

            GameObject ground = CreateBlock(root.transform, "Ground", new Vector3(0f, -0.5f, 5f), new Vector3(16f, 1f, 54f));
            ground.isStatic = true;

            float[] buildingZPositions = { -14f, -4f, 6f, 16f, 26f };
            foreach (float z in buildingZPositions)
            {
                GameObject left = CreateBlock(root.transform, $"LeftBuilding_{z:0}", new Vector3(-7f, 3f, z), new Vector3(6f, 6f, 8f));
                GameObject right = CreateBlock(root.transform, $"RightBuilding_{z:0}", new Vector3(7f, 3f, z), new Vector3(6f, 6f, 8f));
                left.isStatic = true;
                right.isStatic = true;
            }

            GameObject endWall = CreateBlock(root.transform, "EndWall", new Vector3(0f, 2f, 33f), new Vector3(14f, 4f, 2f));
            endWall.isStatic = true;

            CreateBlock(root.transform, "Crate_A", new Vector3(2.5f, 0.75f, -2f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateBlock(root.transform, "Crate_B", new Vector3(-2.5f, 1f, 14f), new Vector3(2f, 2f, 2f));

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PlayerPlaceholder";
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0f, 1f, -10f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.AddComponent<CharacterController>();
            player.AddComponent<ThirdPersonController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerCombat>();
            player.AddComponent<Combatant>().Configure(CombatTeam.Player, 100f);

            GameObject partner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            partner.name = "PartnerPlaceholder";
            partner.transform.SetParent(root.transform);
            partner.transform.position = new Vector3(-1.6f, 1f, -11.5f);
            Object.DestroyImmediate(partner.GetComponent<CapsuleCollider>());
            partner.AddComponent<CharacterController>();
            ThirdPersonController partnerController = partner.AddComponent<ThirdPersonController>();
            partnerController.enabled = false;
            PlayerCombat partnerCombat = partner.AddComponent<PlayerCombat>();
            partnerCombat.enabled = false;
            PlayerInteractor partnerInteractor = partner.AddComponent<PlayerInteractor>();
            partnerInteractor.enabled = false;
            partner.AddComponent<Combatant>().Configure(CombatTeam.Player, 100f);

            GameObject clue = CreateBlock(
                root.transform,
                "Clue_OldStreetSign",
                new Vector3(2.7f, 1.2f, -6f),
                new Vector3(2.2f, 1.8f, 0.25f)
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
                new Vector3(1.6f, 1.1f, 0.25f)
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
                new Vector3(1.4f, 1.8f, 0.25f)
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
                new Vector3(2f, 3f, 0.6f)
            );
            repairSite.AddComponent<RepairSite>();

            List<Combatant> enemies = new()
            {
                CreateEnemy(root.transform, "Enemy_Patrol_01", new Vector3(-1.2f, 0.9f, 12f)),
                CreateEnemy(root.transform, "Enemy_Patrol_02", new Vector3(1.4f, 0.9f, 24f))
            };

            GameObject preserveOutcome = CreateBlock(
                root.transform,
                "Outcome_OldStreetPreserved",
                new Vector3(0f, 0.15f, 30.5f),
                new Vector3(9f, 0.3f, 1.4f)
            );
            preserveOutcome.SetActive(false);

            GameObject sealOutcome = CreateBlock(
                root.transform,
                "Outcome_OldStreetSealed",
                new Vector3(0f, 2f, 31f),
                new Vector3(14f, 4f, 0.5f)
            );
            sealOutcome.SetActive(false);

            GameObject preserveChoice = CreateBlock(
                root.transform,
                "Choice_Preserve",
                new Vector3(-3f, 1.1f, 26f),
                new Vector3(1.3f, 2.2f, 0.3f)
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
                new Vector3(1.3f, 2.2f, 0.3f)
            );
            sealChoice.AddComponent<EndingChoiceInteractable>().Configure(
                ConclusionChoice.Seal,
                preserveOutcome,
                sealOutcome
            );

            GameObject gameSession = new GameObject("GameSession");
            gameSession.transform.SetParent(root.transform);
            gameSession.AddComponent<InvestigationState>();
            EncounterState encounterState = gameSession.AddComponent<EncounterState>();
            encounterState.Configure(enemies);
            gameSession.AddComponent<ObjectiveDisplay>();
            PlayerPartyController party = gameSession.AddComponent<PlayerPartyController>();
            party.Configure(player.transform, partner.transform);
            gameSession.AddComponent<CombatHud>();
            gameSession.AddComponent<GameFlowController>();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(root.transform);
            spawn.transform.position = new Vector3(0f, 0f, -10f);

            Camera sceneCamera = scene
                .GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Camera>(includeInactive: true))
                .FirstOrDefault();

            if (sceneCamera != null)
            {
                sceneCamera.transform.position = new Vector3(0f, 5f, -16f);
                sceneCamera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            }
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            return block;
        }

        private static Combatant CreateEnemy(Transform parent, string name, Vector3 position)
        {
            GameObject enemy = CreateBlock(parent, name, position, new Vector3(0.9f, 1.8f, 0.9f));
            Object.DestroyImmediate(enemy.GetComponent<BoxCollider>());
            enemy.AddComponent<CharacterController>();
            Combatant combatant = enemy.AddComponent<Combatant>();
            combatant.Configure(CombatTeam.Enemy, 60f);
            enemy.AddComponent<EnemyAI>();
            return combatant;
        }
    }
}
