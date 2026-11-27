using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demo02.Combat;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class GameContentSeeder
    {
        public const string CatalogPath =
            "Assets/Demo02/Resources/Data/Demo02ContentCatalog.asset";
        private const string LegacyCatalogPath =
            "Assets/Demo02/Resources/Demo02ContentCatalog.asset";
        public const string Chapter01Path =
            "Assets/Demo02/Data/Chapters/Chapter01.asset";

        [MenuItem("Demo02/Content/Rebuild Game Content Catalog")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Demo02/Data/Chapters");
            EnsureFolder("Assets/Demo02/Resources/Data");
            MigrateLegacyCatalog();
            CaseDefinition caseContent =
                CaseDefinitionSeeder.LoadOrCreateChapter01();

            ChapterDefinition chapter =
                AssetDatabase.LoadAssetAtPath<ChapterDefinition>(
                    Chapter01Path
                );
            if (chapter == null)
            {
                chapter =
                    ScriptableObject.CreateInstance<ChapterDefinition>();
                chapter.name = "Chapter01";
                AssetDatabase.CreateAsset(chapter, Chapter01Path);
            }
            chapter.ConfigureForEditor(
                "chapter-01",
                "消失的盐引",
                "Greybox",
                "guangling_start",
                "official-street",
                "官署前街",
                caseContent
            );
            EditorUtility.SetDirty(chapter);

            GameContentCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                    CatalogPath
                );
            if (catalog == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(CatalogPath) != null)
                {
                    AssetDatabase.DeleteAsset(CatalogPath);
                }
                catalog =
                    ScriptableObject.CreateInstance<GameContentCatalog>();
                catalog.name = "Demo02ContentCatalog";
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.ConfigureForEditor(
                chapter.ChapterId,
                new List<ChapterDefinition> { chapter },
                new List<CharacterDefinition>
                {
                    CharacterDefinitionSeeder.LoadRepairer(),
                    CharacterDefinitionSeeder.LoadPathGuide()
                },
                new List<WorldEncounterDefinition>
                {
                    EncounterDefinitionSeeder.LoadBlockade(),
                    EncounterDefinitionSeeder.LoadSupport(),
                    EncounterDefinitionSeeder.LoadDongguanPatrol(),
                    EncounterDefinitionSeeder.LoadCaiyiLookout()
                },
                new List<WorldStreamingRegionDefinition>(
                    WorldStreamingRegionSeeder.LoadAll()
                )
            );
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Game content catalog rebuilt: {catalog.Chapters.Count} " +
                $"chapter(s), {catalog.Characters.Count} character(s), " +
                $"{catalog.Encounters.Count} encounter(s), " +
                $"{catalog.StreamingRegions.Count} streaming region(s), " +
                $"default={catalog.DefaultChapterId}."
            );
        }

        public static ChapterDefinition LoadChapter01()
        {
            ChapterDefinition chapter =
                AssetDatabase.LoadAssetAtPath<ChapterDefinition>(
                    Chapter01Path
                );
            if (chapter == null)
            {
                throw new System.InvalidOperationException(
                    $"Chapter definition is missing: {Chapter01Path}"
                );
            }
            return chapter;
        }

        public static void ValidateForBatch()
        {
            GameContentCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                    CatalogPath
                );
            if (catalog == null)
            {
                throw new System.InvalidOperationException(
                    $"Game content catalog is missing: {CatalogPath}"
                );
            }

            List<string> errors = catalog.Validate();
            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(
                    "Game content catalog validation failed:\n" +
                    string.Join(
                        "\n",
                        errors.Select(error => $"- {error}")
                    )
                );
            }

            foreach (ChapterDefinition chapter in catalog.Chapters)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        FindScenePath(chapter.SceneName)
                    ) == null)
                {
                    throw new System.InvalidOperationException(
                        $"Chapter scene is missing: " +
                        $"{chapter.SceneName}"
                    );
                }
            }
            GameContentCatalog runtimeCatalog =
                Resources.Load<GameContentCatalog>(
                    GameContentRegistry.CatalogResourcePath
                );
            if (runtimeCatalog == null
                || runtimeCatalog.DefaultChapter == null
                || runtimeCatalog.DefaultChapter.ChapterId
                    != catalog.DefaultChapterId
                || runtimeCatalog.Characters.Count
                    != catalog.Characters.Count
                || runtimeCatalog.Encounters.Count
                    != catalog.Encounters.Count
                || runtimeCatalog.StreamingRegions.Count
                    != catalog.StreamingRegions.Count)
            {
                throw new System.InvalidOperationException(
                    "Game content catalog cannot be loaded from Resources."
                );
            }
            if (runtimeCatalog.FindCharacter("repairer") == null
                || runtimeCatalog.FindCharacter("path-guide") == null
                || runtimeCatalog.FindEncounter("official-blockade") == null
                || runtimeCatalog.FindStreamingRegion(
                    "salt-street-support"
                ) == null)
            {
                throw new System.InvalidOperationException(
                    "Runtime content catalog cannot resolve required IDs."
                );
            }
            if (runtimeCatalog.FindCharacter("repairer") == null
                || runtimeCatalog.FindCharacter("path-guide") == null
                || runtimeCatalog.FindEncounter("official-blockade") == null
                || runtimeCatalog.FindStreamingRegion(
                    "salt-street-support"
                ) == null)
            {
                throw new System.InvalidOperationException(
                    "Runtime content catalog cannot resolve required IDs."
                );
            }
            Debug.Log(
                $"Game content validation passed: " +
                $"{catalog.Chapters.Count} chapter(s), " +
                $"{catalog.Characters.Count} character(s), " +
                $"{catalog.Encounters.Count} encounter(s), " +
                $"{catalog.StreamingRegions.Count} streaming region(s), " +
                $"default={catalog.DefaultChapterId}."
            );
        }

        private static string FindScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets(
                $"{sceneName} t:Scene"
            );
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    return path;
                }
            }
            return string.Empty;
        }

        private static void MigrateLegacyCatalog()
        {
            GameContentCatalog legacy =
                AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                    LegacyCatalogPath
                );
            if (legacy == null)
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                    CatalogPath
                ) != null)
            {
                throw new System.InvalidOperationException(
                    $"Both legacy and target content catalogs exist: " +
                    $"{LegacyCatalogPath} and {CatalogPath}"
                );
            }
            string error = AssetDatabase.MoveAsset(
                LegacyCatalogPath,
                CatalogPath
            );
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new System.InvalidOperationException(
                    $"Could not migrate content catalog: {error}"
                );
            }
            AssetDatabase.Refresh();
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
                throw new System.InvalidOperationException(
                    $"Invalid folder: {path}"
                );
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
