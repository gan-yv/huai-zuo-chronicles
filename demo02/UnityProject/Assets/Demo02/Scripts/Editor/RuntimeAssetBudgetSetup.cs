using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class RuntimeAssetBudgetSetup
    {
        private const int EnvironmentColorSize = 1024;
        private const int EnvironmentNormalSize = 1024;
        private const int EnvironmentDataSize = 512;

        private static readonly string[] EnvironmentTextureRoots =
        {
            "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit/Textures",
            "Assets/ThirdParty/PolyHaven"
        };

        private static readonly string[] EnvironmentMeshRoots =
        {
            "Assets/Demo02/Art/Environment/Quaternius/Meshes",
            "Assets/Demo02/Art/Environment/StreetDressing/Meshes"
        };

        [MenuItem("Demo02/Performance/Apply Runtime Asset Budget")]
        public static void BuildAll()
        {
            int changed = 0;
            foreach (string path in FindEnvironmentTextures())
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }
                if (!ApplyToImporter(importer, path))
                {
                    continue;
                }
                importer.SaveAndReimport();
                changed++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Runtime texture budget applied: {changed} texture(s)."
            );
        }

        public static void ValidateForBatch()
        {
            List<string> errors = new();
            foreach (string path in FindEnvironmentTextures())
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }
                TextureBudget budget = Classify(path);
                if (importer.maxTextureSize > budget.MaxSize)
                {
                    errors.Add(
                        $"{path}: max size {importer.maxTextureSize} > " +
                        $"{budget.MaxSize}"
                    );
                }
                if (!importer.streamingMipmaps)
                {
                    errors.Add($"{path}: streaming mipmaps disabled");
                }
            }
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Runtime asset budget validation failed:\n" +
                    string.Join("\n", errors)
                );
            }
            Debug.Log(
                "Runtime texture budget validation passed."
            );
        }

        public static void ApplyMeshBudget()
        {
            int changed = ApplyMeshReadabilityBudget();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Runtime mesh budget applied: {changed} mesh(es) updated."
            );
        }

        public static void ValidateMeshBudget()
        {
            List<string> errors = new();
            ValidateMeshReadability(errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Runtime mesh budget validation failed:\n" +
                    string.Join("\n", errors)
                );
            }
            Debug.Log("Runtime mesh budget validation passed.");
        }

        public static bool ApplyToImporter(
            TextureImporter importer,
            string assetPath)
        {
            if (importer == null)
            {
                return false;
            }
            TextureBudget budget = Classify(assetPath);
            int maxSize = Mathf.Min(
                importer.maxTextureSize,
                budget.MaxSize
            );
            bool dirty =
                importer.maxTextureSize != maxSize
                || importer.textureCompression
                    != TextureImporterCompression.CompressedHQ
                || !importer.streamingMipmaps
                || importer.anisoLevel != 4
                || importer.sRGBTexture != budget.Srgb;
            if (!dirty)
            {
                return false;
            }

            importer.maxTextureSize = maxSize;
            importer.textureCompression =
                TextureImporterCompression.CompressedHQ;
            importer.streamingMipmaps = true;
            importer.anisoLevel = 4;
            importer.sRGBTexture = budget.Srgb;
            return true;
        }

        public static void MakeEnvironmentMeshesReadableForBuild()
        {
            int changed = 0;
            foreach (string path in FindEnvironmentMeshes())
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null || mesh.isReadable)
                {
                    continue;
                }
                SerializedObject serialized = new(mesh);
                SerializedProperty readable =
                    serialized.FindProperty("m_IsReadable");
                if (readable == null)
                {
                    continue;
                }
                readable.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mesh);
                changed++;
            }
            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log(
                $"Environment mesh staging: {changed} mesh(es) made readable."
            );
        }

        private static IEnumerable<string> FindEnvironmentTextures()
        {
            HashSet<string> paths = new(StringComparer.Ordinal);
            foreach (string root in EnvironmentTextureRoots)
            {
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:Texture2D",
                             new[] { root }
                         ))
                {
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            return paths;
        }

        private static int ApplyMeshReadabilityBudget()
        {
            int changed = 0;
            foreach (string path in FindEnvironmentMeshes())
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }
                SerializedObject serialized = new(mesh);
                SerializedProperty readable =
                    serialized.FindProperty("m_IsReadable");
                if (readable == null)
                {
                    continue;
                }
                readable.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mesh);
                changed++;
            }
            return changed;
        }

        private static void ValidateMeshReadability(List<string> errors)
        {
            foreach (string path in FindEnvironmentMeshes())
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh != null && mesh.isReadable)
                {
                    errors.Add($"{path}: CPU-readable mesh is not allowed");
                }
            }
        }

        private static IEnumerable<string> FindEnvironmentMeshes()
        {
            HashSet<string> paths = new(StringComparer.Ordinal);
            foreach (string root in EnvironmentMeshRoots)
            {
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:Mesh",
                             new[] { root }
                         ))
                {
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            return paths;
        }

        private static TextureBudget Classify(string path)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path)
                .ToLowerInvariant();
            bool data = name.Contains("roughness")
                || name.Contains("metallic")
                || name.Contains("orm")
                || name.Contains("_arm")
                || name.Contains("occlusion");
            bool normal = name.Contains("normal") || name.EndsWith("_n");
            int size = data
                ? EnvironmentDataSize
                : normal
                    ? EnvironmentNormalSize
                    : EnvironmentColorSize;
            return new TextureBudget(size, !data && !normal);
        }

        private readonly struct TextureBudget
        {
            public TextureBudget(int maxSize, bool srgb)
            {
                MaxSize = maxSize;
                Srgb = srgb;
            }

            public int MaxSize { get; }
            public bool Srgb { get; }
        }
    }
}
