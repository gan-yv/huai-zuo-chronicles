using Demo02.Combat;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class EncounterDefinitionSeeder
    {
        public const string BlockadePath =
            "Assets/Demo02/Data/Encounters/ENC_OfficialBlockade.asset";
        public const string SupportPath =
            "Assets/Demo02/Data/Encounters/ENC_SaltStreetSupport.asset";
        public const string DongguanPath =
            "Assets/Demo02/Data/Encounters/ENC_DongguanPatrol.asset";
        public const string CaiyiPath =
            "Assets/Demo02/Data/Encounters/ENC_CaiyiLookout.asset";

        [MenuItem("Demo02/Content/Rebuild Encounter Definitions")]
        public static void BuildAll()
        {
            LoadOrConfigure(
                BlockadePath,
                "official-blockade",
                "官署前街封锁",
                18f,
                60f,
                10f,
                2.2f,
                12f,
                false,
                new[] { "salt-street-support" }
            );
            LoadOrConfigure(
                SupportPath,
                "salt-street-support",
                "盐仓横街增援",
                14f,
                60f,
                10f,
                2.2f,
                12f,
                false,
                null
            );
            LoadOrConfigure(
                DongguanPath,
                "dongguan-patrol",
                "东关街巡哨",
                17f,
                60f,
                10f,
                2.2f,
                12f,
                false,
                null
            );
            LoadOrConfigure(
                CaiyiPath,
                "caiyi-lookout",
                "彩衣街望风",
                15f,
                60f,
                10f,
                2.2f,
                12f,
                false,
                null
            );
            AssetDatabase.SaveAssets();
        }

        public static WorldEncounterDefinition LoadBlockade()
        {
            return LoadRequired(BlockadePath);
        }

        public static WorldEncounterDefinition LoadSupport()
        {
            return LoadRequired(SupportPath);
        }

        public static WorldEncounterDefinition LoadDongguanPatrol()
        {
            return LoadRequired(DongguanPath);
        }

        public static WorldEncounterDefinition LoadCaiyiLookout()
        {
            return LoadRequired(CaiyiPath);
        }

        public static void ValidateOrThrow()
        {
            foreach (string path in new[]
                     {
                         BlockadePath,
                         SupportPath,
                         DongguanPath,
                         CaiyiPath
                     })
            {
                WorldEncounterDefinition definition =
                    AssetDatabase.LoadAssetAtPath<WorldEncounterDefinition>(
                        path
                    );
                if (definition == null)
                {
                    throw new System.InvalidOperationException(
                        $"Encounter definition is missing: {path}"
                    );
                }
                if (!definition.IsValid(out string error))
                {
                    throw new System.InvalidOperationException(
                        $"Encounter definition validation failed: "
                        + $"{path} {error}"
                    );
                }
            }

            Debug.Log(
                "Encounter definitions validated: blockade, support and two "
                + "street patrols."
            );
        }

        private static WorldEncounterDefinition LoadRequired(string path)
        {
            WorldEncounterDefinition definition =
                AssetDatabase.LoadAssetAtPath<WorldEncounterDefinition>(path);
            if (definition == null)
            {
                throw new System.InvalidOperationException(
                    $"Encounter definition is missing: {path}"
                );
            }
            return definition;
        }

        private static void LoadOrConfigure(
            string path,
            string id,
            string displayName,
            float activationRadius,
            float health,
            float damage,
            float moveSpeed,
            float aggroDistance,
            bool startsActive,
            string[] supportIds)
        {
            WorldEncounterDefinition definition =
                AssetDatabase.LoadAssetAtPath<WorldEncounterDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    WorldEncounterDefinition
                >();
                definition.name =
                    System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(definition, path);
                definition.ConfigureForEditor(
                    id,
                    displayName,
                    activationRadius,
                    health,
                    damage,
                    moveSpeed,
                    aggroDistance,
                    startsActive,
                    supportIds
                );
                EditorUtility.SetDirty(definition);
            }
        }
    }
}
