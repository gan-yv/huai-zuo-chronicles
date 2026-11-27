using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demo02.Combat;
using Demo02.WorldState;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class WorldStreamingRegionSeeder
    {
        public const string DefinitionFolder =
            "Assets/Demo02/Data/WorldStreaming";
        public const string SceneFolder =
            "Assets/Demo02/Scenes/Guangling/Streaming";
        private const string LegacyDefinitionFolder =
            "Assets/Demo02/Data/WorldState";
        private static readonly string[] LegacyAssetNames =
        {
            "STREAM_SaltStreetSupport",
            "STREAM_DongguanPatrol",
            "STREAM_CaiyiLookout"
        };

        public static void BuildAll()
        {
            EnsureFolder(DefinitionFolder);
            MigrateLegacyDefinitions();
            Configure(
                "STREAM_SaltStreetSupport",
                "salt-street-support",
                "SaltStreetSupport",
                "盐仓横街增援",
                0,
                new Vector3(1.4f, 0.9f, 20f),
                new Vector3(1.4f, 0.9f, 24f),
                20f,
                38f,
                46f,
                false,
                "Streaming_SaltStreetSupport",
                "Encounter_SaltStreetSupport",
                "Enemy_Support_01",
                EncounterDefinitionSeeder.LoadSupport(),
                null
            );
            Configure(
                "STREAM_DongguanPatrol",
                "dongguan-patrol",
                "DongguanPatrol",
                "东关街巡哨",
                10,
                new Vector3(60f, 0.9f, 44f),
                new Vector3(60f, 0.9f, 44f),
                18f,
                34f,
                44f,
                false,
                "Streaming_DongguanPatrol",
                "Encounter_DongguanPatrol",
                "Enemy_DongguanPatrol",
                EncounterDefinitionSeeder.LoadDongguanPatrol(),
                "dongguan-apprentice-ledger"
            );
            Configure(
                "STREAM_CaiyiLookout",
                "caiyi-lookout",
                "CaiyiLookout",
                "彩衣街望风",
                20,
                new Vector3(60f, 0.9f, 220f),
                new Vector3(60f, 0.9f, 220f),
                18f,
                34f,
                44f,
                false,
                "Streaming_CaiyiLookout",
                "Encounter_CaiyiLookout",
                "Enemy_CaiyiLookout",
                EncounterDefinitionSeeder.LoadCaiyiLookout(),
                "caiyi-apprentice-ledger-page"
            );

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Streaming region definitions rebuilt: " +
                $"{LoadAll().Count} region(s)."
            );
        }

        private static void MigrateLegacyDefinitions()
        {
            foreach (string assetName in LegacyAssetNames)
            {
                string legacyPath =
                    $"{LegacyDefinitionFolder}/{assetName}.asset";
                string targetPath = $"{DefinitionFolder}/{assetName}.asset";
                WorldStreamingRegionDefinition legacy =
                    AssetDatabase.LoadAssetAtPath<
                        WorldStreamingRegionDefinition
                    >(legacyPath);
                if (legacy == null)
                {
                    continue;
                }
                if (AssetDatabase.LoadAssetAtPath<
                        WorldStreamingRegionDefinition
                    >(targetPath) != null)
                {
                    throw new InvalidOperationException(
                        $"Both legacy and target streaming definitions " +
                        $"exist: {legacyPath} and {targetPath}"
                    );
                }
                string error = AssetDatabase.MoveAsset(
                    legacyPath,
                    targetPath
                );
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new InvalidOperationException(
                        $"Could not migrate streaming definition " +
                        $"{legacyPath}: {error}"
                    );
                }
            }
            AssetDatabase.Refresh();
            if (AssetDatabase.IsValidFolder(LegacyDefinitionFolder)
                && AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { LegacyDefinitionFolder }
                ).Length == 0)
            {
                AssetDatabase.DeleteAsset(LegacyDefinitionFolder);
                AssetDatabase.Refresh();
            }
        }

        public static IReadOnlyList<WorldStreamingRegionDefinition> LoadAll()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:WorldStreamingRegionDefinition",
                new[] { DefinitionFolder }
            );
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path =>
                    AssetDatabase.LoadAssetAtPath<
                        WorldStreamingRegionDefinition
                    >(path)
                )
                .Where(definition => definition != null)
                .OrderBy(definition => definition.BuildOrder)
                .ThenBy(definition => definition.RegionId)
                .ToArray();
        }

        public static WorldStreamingRegionDefinition Load(string regionId)
        {
            WorldStreamingRegionDefinition definition = LoadAll()
                .FirstOrDefault(item => item.RegionId == regionId);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Streaming region definition is missing: {regionId}"
                );
            }
            return definition;
        }

        public static string GetScenePath(
            WorldStreamingRegionDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            return $"{SceneFolder}/{definition.SceneName}.unity";
        }

        public static void ValidateForBatch()
        {
            IReadOnlyList<WorldStreamingRegionDefinition> definitions =
                LoadAll();
            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "No streaming region definitions were found."
                );
            }

            List<string> errors = new();
            HashSet<string> regionIds = new(StringComparer.Ordinal);
            HashSet<string> sceneNames = new(StringComparer.Ordinal);
            foreach (WorldStreamingRegionDefinition definition in definitions)
            {
                errors.AddRange(definition.Validate());
                if (!regionIds.Add(definition.RegionId))
                {
                    errors.Add(
                        $"Duplicate streaming region ID: " +
                        $"{definition.RegionId}"
                    );
                }
                if (!sceneNames.Add(definition.SceneName))
                {
                    errors.Add(
                        $"Duplicate streaming scene name: " +
                        $"{definition.SceneName}"
                    );
                }

                string scenePath = GetScenePath(definition);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)
                    == null)
                {
                    errors.Add(
                        $"Streaming scene is missing: {scenePath}"
                    );
                }
            }
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Streaming region validation failed:\n" +
                    string.Join(
                        "\n",
                        errors.Select(error => $"- {error}")
                    )
                );
            }

            Debug.Log(
                $"Streaming region validation passed: " +
                $"{definitions.Count} data-driven region(s)."
            );
        }

        private static void Configure(
            string assetName,
            string regionId,
            string sceneName,
            string displayName,
            int buildOrder,
            Vector3 worldPosition,
            Vector3 enemyPosition,
            float loadRadius,
            float preloadRadius,
            float unloadRadius,
            bool preloadOnStart,
            string streamingRootName,
            string encounterObjectName,
            string enemyObjectName,
            WorldEncounterDefinition encounterContent,
            string contentEventId)
        {
            string path = $"{DefinitionFolder}/{assetName}.asset";
            WorldStreamingRegionDefinition definition =
                AssetDatabase.LoadAssetAtPath<
                    WorldStreamingRegionDefinition
                >(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    WorldStreamingRegionDefinition
                >();
                definition.name = assetName;
                AssetDatabase.CreateAsset(definition, path);
            }
            definition.ConfigureForEditor(
                regionId,
                sceneName,
                displayName,
                buildOrder,
                worldPosition,
                enemyPosition,
                loadRadius,
                preloadRadius,
                unloadRadius,
                preloadOnStart,
                streamingRootName,
                encounterObjectName,
                enemyObjectName,
                encounterContent,
                contentEventId
            );
            EditorUtility.SetDirty(definition);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent)
                || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(
                    $"Invalid folder: {path}"
                );
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
