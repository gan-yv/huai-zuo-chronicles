using System;
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
    public static class GameContentAudit
    {
        private const string DataFolder = "Assets/Demo02/Data";
        private const string ManifestRelativePath =
            "docs/generated/content-manifest.json";

        public static void Generate()
        {
            List<string> errors = CollectErrors();
            if (errors.Count > 0)
            {
                ThrowValidation(errors);
            }

            GameContentCatalog catalog = LoadCatalog();
            ContentManifest manifest = BuildManifest(catalog);
            string outputPath = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    ManifestRelativePath
                )
            );
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(
                outputPath,
                JsonUtility.ToJson(manifest, prettyPrint: true)
            );
            Debug.Log($"Content manifest generated: {outputPath}");
        }

        public static void Validate()
        {
            List<string> errors = CollectErrors();
            if (errors.Count > 0)
            {
                ThrowValidation(errors);
            }
            Debug.Log(
                $"Content data audit passed: " +
                $"{LoadCatalog().Chapters.Count} chapter(s)."
            );
        }

        private static List<string> CollectErrors()
        {
            List<string> errors = new();
            GameContentCatalog catalog = LoadCatalog();
            if (catalog == null)
            {
                errors.Add(
                    $"Content catalog is missing: " +
                    $"{GameContentSeeder.CatalogPath}"
                );
                return errors;
            }

            HashSet<UnityEngine.Object> registered = new();
            foreach (ChapterDefinition chapter in catalog.Chapters)
            {
                registered.Add(chapter);
                if (chapter != null && chapter.CaseContent != null)
                {
                    registered.Add(chapter.CaseContent);
                }
            }
            foreach (CharacterDefinition character in catalog.Characters)
            {
                registered.Add(character);
            }
            foreach (WorldEncounterDefinition encounter in catalog.Encounters)
            {
                registered.Add(encounter);
            }
            foreach (WorldStreamingRegionDefinition region in
                     catalog.StreamingRegions)
            {
                registered.Add(region);
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { DataFolder }
            );
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset =
                    AssetDatabase.LoadMainAssetAtPath(path);
                if (asset is ScriptableObject
                    && !registered.Contains(asset))
                {
                    errors.Add($"Unregistered data asset: {path}");
                }
            }
            return errors;
        }

        private static GameContentCatalog LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                GameContentSeeder.CatalogPath
            );
        }

        private static ContentManifest BuildManifest(
            GameContentCatalog catalog)
        {
            ContentManifest manifest = new()
            {
                Version = Application.version,
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                CatalogPath = GameContentSeeder.CatalogPath
            };

            foreach (ChapterDefinition chapter in catalog.Chapters)
            {
                if (chapter == null)
                {
                    continue;
                }
                manifest.Chapters.Add(new ChapterRecord
                {
                    Id = chapter.ChapterId,
                    DisplayName = chapter.DisplayName,
                    AssetPath = AssetDatabase.GetAssetPath(chapter),
                    SceneName = chapter.SceneName,
                    EntrySpawnId = chapter.EntrySpawnId,
                    EntryAreaId = chapter.EntryAreaId,
                    CaseId = chapter.CaseContent != null
                        ? chapter.CaseContent.CaseId
                        : string.Empty,
                    CasePath = chapter.CaseContent != null
                        ? AssetDatabase.GetAssetPath(chapter.CaseContent)
                        : string.Empty
                });
            }
            foreach (CharacterDefinition character in catalog.Characters)
            {
                if (character == null)
                {
                    continue;
                }
                manifest.Characters.Add(new CharacterRecord
                {
                    Id = character.CharacterId,
                    DisplayName = character.DisplayName,
                    AssetPath = AssetDatabase.GetAssetPath(character),
                    ResonanceMark = character.ResonanceMark.ToString()
                });
            }
            foreach (WorldEncounterDefinition encounter in
                     catalog.Encounters)
            {
                if (encounter == null)
                {
                    continue;
                }
                manifest.Encounters.Add(new EncounterRecord
                {
                    Id = encounter.EncounterId,
                    DisplayName = encounter.DisplayName,
                    AssetPath = AssetDatabase.GetAssetPath(encounter),
                    SupportEncounterIds = encounter.SupportEncounterIds.ToList()
                });
            }
            foreach (WorldStreamingRegionDefinition region in
                     catalog.StreamingRegions)
            {
                if (region == null)
                {
                    continue;
                }
                manifest.StreamingRegions.Add(new StreamingRegionRecord
                {
                    Id = region.RegionId,
                    DisplayName = region.DisplayName,
                    AssetPath = AssetDatabase.GetAssetPath(region),
                    SceneName = region.SceneName,
                    EncounterId = region.EncounterContent != null
                        ? region.EncounterContent.EncounterId
                        : string.Empty
                });
            }
            return manifest;
        }

        private static void ThrowValidation(List<string> errors)
        {
            throw new InvalidOperationException(
                "Content data audit failed:\n" +
                string.Join("\n", errors.Select(error => $"- {error}"))
            );
        }

        [Serializable]
        private sealed class ContentManifest
        {
            public string Version;
            public string GeneratedAtUtc;
            public string CatalogPath;
            public List<ChapterRecord> Chapters = new();
            public List<CharacterRecord> Characters = new();
            public List<EncounterRecord> Encounters = new();
            public List<StreamingRegionRecord> StreamingRegions = new();
        }

        [Serializable]
        private sealed class ChapterRecord
        {
            public string Id;
            public string DisplayName;
            public string AssetPath;
            public string SceneName;
            public string EntrySpawnId;
            public string EntryAreaId;
            public string CaseId;
            public string CasePath;
        }

        [Serializable]
        private sealed class CharacterRecord
        {
            public string Id;
            public string DisplayName;
            public string AssetPath;
            public string ResonanceMark;
        }

        [Serializable]
        private sealed class EncounterRecord
        {
            public string Id;
            public string DisplayName;
            public string AssetPath;
            public List<string> SupportEncounterIds = new();
        }

        [Serializable]
        private sealed class StreamingRegionRecord
        {
            public string Id;
            public string DisplayName;
            public string AssetPath;
            public string SceneName;
            public string EncounterId;
        }
    }
}
