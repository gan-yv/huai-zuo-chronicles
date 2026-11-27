using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Player;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    /// <summary>
    /// Builds controllable actors, combat encounters, traversal and time mechanics.
    /// </summary>
    internal static class Demo02GreyboxGameplayAssembler
    {
        public static void BuildPlayerRig(
            Transform root,
            out GameObject player,
            out GameObject partner)
        {
            player = Demo02GreyboxNpcAssembler.CreateCharacter(
                root,
                "PlayerPlaceholder",
                new Vector3(0f, 1f, -10f),
                new Color(0.2f, 0.43f, 0.68f),
                new Color(0.82f, 0.65f, 0.3f),
                addNpcCollider: false
            );
            player.AddComponent<CharacterController>();
            player.AddComponent<ThirdPersonController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerCombat>().ConfigureMeleeProfile(
                28f,
                1.8f,
                1.2f,
                0.28f,
                0.72f
            );
            player
                .AddComponent<Combatant>()
                .Configure(
                    CombatTeam.Player,
                    100f,
                    "character:repairer"
                );
            player.AddComponent<CharacterRuntime>().Configure(
                CharacterDefinitionSeeder.LoadRepairer()
            );
            player.AddComponent<CharacterAbilityController>();
            Demo02GreyboxNpcAssembler.CreateExplorationVisuals(player);

            GameObject meleeArc =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            meleeArc.name = "MeleeArc";
            meleeArc.transform.SetParent(
                player.transform,
                worldPositionStays: false
            );
            meleeArc.transform.localPosition =
                new Vector3(0f, 0.15f, 1.1f);
            meleeArc.transform.localScale =
                new Vector3(0.9f, 0.18f, 2f);
            Object.DestroyImmediate(meleeArc.GetComponent<BoxCollider>());
            meleeArc.SetActive(false);

            partner = Demo02GreyboxNpcAssembler.CreateCharacter(
                root,
                "PartnerPlaceholder",
                new Vector3(-1.6f, 1f, -11.5f),
                new Color(0.24f, 0.56f, 0.42f),
                new Color(0.76f, 0.58f, 0.24f),
                addNpcCollider: false
            );
            partner.AddComponent<CharacterController>();
            ThirdPersonController partnerController =
                partner.AddComponent<ThirdPersonController>();
            partnerController.enabled = false;
            PlayerCombat partnerCombat =
                partner.AddComponent<PlayerCombat>();
            partnerCombat.ConfigureMeleeProfile(
                28f,
                1.8f,
                1.2f,
                0.28f,
                0.72f
            );
            partnerCombat.enabled = false;
            PlayerInteractor partnerInteractor =
                partner.AddComponent<PlayerInteractor>();
            partnerInteractor.enabled = false;
            partner
                .AddComponent<Combatant>()
                .Configure(
                    CombatTeam.Player,
                    100f,
                    "character:path-guide"
                );
            partner.AddComponent<CharacterRuntime>().Configure(
                CharacterDefinitionSeeder.LoadPathGuide()
            );
            CharacterAbilityController partnerAbilities =
                partner.AddComponent<CharacterAbilityController>();
            partnerAbilities.enabled = false;
            Demo02GreyboxNpcAssembler.CreateExplorationVisuals(partner);
            Demo02GreyboxNpcAssembler.CreateWorkFishCompanion(
                player.transform,
                root
            );
        }

        public static List<Combatant> BuildOfficialStreetEncounter(
            Transform root)
        {
            WorldEncounterDefinition definition =
                EncounterDefinitionSeeder.LoadBlockade();
            List<Combatant> enemies = new()
            {
                CreateEnemy(
                    root,
                    "Enemy_Patrol_01",
                    new Vector3(-1.2f, 0.9f, 12f),
                    definition,
                    EnemyArchetype.Chaser
                ),
                CreateEnemy(
                    root,
                    "Enemy_Shield_01",
                    new Vector3(-3.1f, 0.9f, 13f),
                    definition,
                    EnemyArchetype.Shield
                ),
                CreateEnemy(
                    root,
                    "Enemy_Archer_01",
                    new Vector3(2.8f, 0.9f, 16f),
                    definition,
                    EnemyArchetype.Ranged
                )
            };

            GameObject blockadeZoneObject =
                new("Encounter_OfficialBlockade");
            blockadeZoneObject.transform.SetParent(root);
            blockadeZoneObject.transform.position =
                new Vector3(0f, 0.9f, 12f);
            blockadeZoneObject
                .AddComponent<WorldEncounterZone>()
                .Configure(
                    definition,
                    enemies
                );
            CreateStreamingRegion(root, "salt-street-support");
            return enemies;
        }

        public static void BuildTimeAnchors(Transform root)
        {
            CreateTimeAnchor(
                root,
                "TimeAnchor_Present_01",
                new Vector3(3.35f, 1.2f, 5f),
                "present-01",
                TimeLayer.Present,
                "东验锚点",
                90f
            );
            CreateTimeAnchor(
                root,
                "TimeAnchor_Past_02",
                new Vector3(-3.35f, 1.2f, 14f),
                "past-02",
                TimeLayer.Past,
                "西仓锚点",
                90f
            );
            CreateTimeAnchor(
                root,
                "TimeAnchor_Present_03",
                new Vector3(3.35f, 1.2f, 22f),
                "present-03",
                TimeLayer.Present,
                "中册锚点",
                90f
            );
        }

        public static GlideChallengeController BuildDongguanChallenge(
            Transform areaRoot)
        {
            GameObject glideChallengeObject =
                new("Challenge_DongguanWindCourse");
            glideChallengeObject.transform.SetParent(areaRoot);
            glideChallengeObject.transform.position =
                new Vector3(57f, 0f, 8f);
            GlideChallengeController glideChallenge =
                glideChallengeObject.AddComponent<
                    GlideChallengeController
                >();
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
            return glideChallenge;
        }

        public static void BuildStreamingEncounterScenes()
        {
            if (!AssetDatabase.IsValidFolder(
                    WorldStreamingRegionSeeder.SceneFolder
                ))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Demo02/Scenes/Guangling",
                    "Streaming"
                );
            }

            foreach (WorldStreamingRegionDefinition region in
                     WorldStreamingRegionSeeder.LoadAll())
            {
                BuildStreamingScene(
                    WorldStreamingRegionSeeder.GetScenePath(region),
                    region.StreamingRootName,
                    region.EncounterObjectName,
                    region.EnemyObjectName,
                    region.EnemyPosition,
                    region.EncounterContent
                );
            }
        }

        public static void CreateStreamingRegion(
            Transform parent,
            string regionId)
        {
            WorldStreamingRegionDefinition definition =
                WorldStreamingRegionSeeder.Load(regionId);
            GameObject regionObject =
                new($"Stream_{definition.SceneName}");
            regionObject.transform.SetParent(parent);
            regionObject.transform.position = definition.WorldPosition;
            regionObject
                .AddComponent<WorldAdditiveSceneRegion>()
                .Configure(
                    definition.RegionId,
                    definition.SceneName,
                    definition.DisplayName,
                    definition.LoadRadius,
                    definition.PreloadRadius,
                    definition.UnloadRadius,
                    definition.PreloadOnStart
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
                enemyPosition,
                definition,
                definition.EnemyArchetype
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
            WorldEncounterZone zone =
                zoneObject.AddComponent<WorldEncounterZone>();
            zone.Configure(definition, new[] { enemy });
            if (eliteController != null)
            {
                zoneObject
                    .AddComponent<EliteArenaBoundary>()
                    .Configure(eliteController, 10.5f);
            }
            Demo02GreyboxTaskAssembler.BuildStreetStory(
                root.transform,
                zone,
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

        internal static Combatant CreateEnemy(
            Transform parent,
            string name,
            Vector3 position,
            WorldEncounterDefinition definition,
            EnemyArchetype archetype)
        {
            GameObject enemy = new(name);
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;

            GameObject blockVisual =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockVisual.name = "GreyboxVisual";
            blockVisual.transform.SetParent(enemy.transform, false);
            blockVisual.transform.localScale =
                new Vector3(0.9f, 1.8f, 0.9f);
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
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        blockyPrefab
                    );
                visual.name = "BlockyVisual";
                visual.transform.SetParent(enemy.transform, false);
                visual.transform.localPosition =
                    new Vector3(0f, -0.9f, 0f);
                blockVisual.GetComponent<MeshRenderer>().enabled = false;
            }

            AddArchetypeVisual(enemy.transform, archetype);

            enemy.AddComponent<ColorTint>().Configure(
                archetype switch
                {
                    EnemyArchetype.Shield =>
                        new Color(0.24f, 0.46f, 0.72f),
                    EnemyArchetype.Ranged =>
                        new Color(0.9f, 0.4f, 0.12f),
                    _ => new Color(0.68f, 0.16f, 0.16f)
                }
            );
            enemy.AddComponent<CharacterController>();
            Combatant combatant = enemy.AddComponent<Combatant>();
            combatant.Configure(
                CombatTeam.Enemy,
                definition != null ? definition.EnemyHealth : 60f,
                $"{definition?.EncounterId ?? "unscoped"}:{name}"
            );
            enemy.AddComponent<ResonanceStatus>();
            enemy.AddComponent<EnemyBreakStatus>();
            EnemyAI enemyAi = enemy.AddComponent<EnemyAI>();
            if (definition != null)
            {
                enemyAi.ConfigureCombatProfile(
                    definition.EnemyMoveSpeed,
                    definition.EnemyDamage,
                    1.2f,
                    0.4f
                );
            }
            enemyAi.ConfigureArchetype(archetype);
            return combatant;
        }

        private static void AddArchetypeVisual(
            Transform enemy,
            EnemyArchetype archetype)
        {
            if (archetype == EnemyArchetype.Shield)
            {
                GameObject shield = Demo02SceneObjectFactory
                    .CreateDecorBlock(
                        enemy,
                        "Archetype_Shield",
                        enemy.position + enemy.forward * 0.58f
                            + Vector3.up * 0.96f,
                        new Vector3(0.16f, 1.35f, 0.92f),
                        new Color(0.46f, 0.62f, 0.78f)
                    );
                shield.transform.SetParent(enemy, true);
                return;
            }

            if (archetype == EnemyArchetype.Ranged)
            {
                GameObject weapon = Demo02SceneObjectFactory
                    .CreateDecorBlock(
                        enemy,
                        "Archetype_Bow",
                        enemy.position + enemy.right * 0.55f
                            + Vector3.up,
                        new Vector3(0.09f, 1.05f, 0.09f),
                        new Color(0.45f, 0.25f, 0.1f),
                        PrimitiveType.Cylinder
                    );
                weapon.transform.SetParent(enemy, true);
                weapon.transform.localRotation =
                    Quaternion.Euler(0f, 0f, 22f);
            }
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

        private static void CreateTimeAnchor(
            Transform parent,
            string name,
            Vector3 position,
            string anchorId,
            TimeLayer layer,
            string displayName,
            float rotationY)
        {
            GameObject anchor = Demo02SceneObjectFactory.CreateBlock(
                parent,
                name,
                position,
                new Vector3(1.1f, 2.4f, 0.35f),
                layer == TimeLayer.Present
                    ? new Color(0.2f, 0.78f, 0.88f)
                    : new Color(0.78f, 0.32f, 0.92f)
            );
            anchor.transform.rotation =
                Quaternion.Euler(0f, rotationY, 0f);
            anchor
                .AddComponent<TimeLayerVisibility>()
                .Configure(layer);
            anchor
                .AddComponent<TimeAnchor>()
                .Configure(anchorId, displayName);
        }
    }
}
