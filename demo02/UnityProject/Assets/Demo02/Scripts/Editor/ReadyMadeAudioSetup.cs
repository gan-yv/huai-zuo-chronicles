using System;
using System.IO;
using Demo02.Core;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class ReadyMadeAudioSetup
    {
        private const string LibraryPath =
            "Assets/Demo02/Audio/GameAudioLibrary.asset";
        private const string InterfaceRoot =
            "Assets/ThirdParty/Kenney/AudioPacks/InterfaceSounds";
        private const string ImpactRoot =
            "Assets/ThirdParty/Kenney/AudioPacks/ImpactSounds";
        private const string RpgRoot =
            "Assets/ThirdParty/Kenney/AudioPacks/RpgAudio";

        [MenuItem("Demo02/Art/Rebuild Ready-Made Audio Library")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            GameAudioLibrary library =
                AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath);
            if (library == null)
            {
                string parent = Path.GetDirectoryName(LibraryPath)
                    ?.Replace('\\', '/');
                EnsureFolder(parent!);
                library = ScriptableObject.CreateInstance<GameAudioLibrary>();
                library.name = "GameAudioLibrary";
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            AudioClip[] footsteps =
            {
                LoadClip(ImpactRoot, "footstep_concrete_000"),
                LoadClip(ImpactRoot, "footstep_concrete_001"),
                LoadClip(ImpactRoot, "footstep_concrete_002"),
                LoadClip(ImpactRoot, "footstep_concrete_003"),
                LoadClip(ImpactRoot, "footstep_concrete_004")
            };
            library.Configure(
                null,
                LoadClip(InterfaceRoot, "question_001"),
                LoadClip(InterfaceRoot, "confirmation_001"),
                LoadClip(InterfaceRoot, "open_001"),
                LoadClip(InterfaceRoot, "close_001"),
                LoadClip(InterfaceRoot, "click_001"),
                LoadClip(RpgRoot, "knife_slice"),
                LoadClip(ImpactRoot, "impact_metal_medium_000"),
                LoadClip(ImpactRoot, "impact_plank_medium_000"),
                LoadClip(RpgRoot, "book_open"),
                LoadClip(RpgRoot, "book_close"),
                LoadClip(RpgRoot, "book_flip_1"),
                footsteps
            );
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Ready-made audio library is ready: interface, footsteps, " +
                "impact and RPG feedback."
            );
        }

        public static GameAudioLibrary LoadLibrary()
        {
            return AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath);
        }

        public static void ApplyTo(GameAudio audio)
        {
            GameAudioLibrary library = LoadLibrary();
            if (library == null)
            {
                throw new InvalidOperationException(
                    $"Ready-made audio library is missing: {LibraryPath}"
                );
            }
            audio.Configure(library);
        }

        public static void ValidateForBatch()
        {
            GameAudioLibrary library = LoadLibrary();
            if (library == null ||
                library.Interaction == null ||
                library.Confirmation == null ||
                library.MenuOpen == null ||
                library.MenuClose == null ||
                library.Click == null ||
                library.AttackSwing == null ||
                library.Hit == null ||
                library.Hurt == null ||
                library.BookOpen == null ||
                library.BookClose == null ||
                library.PageTurn == null ||
                library.Footsteps == null ||
                library.Footsteps.Length < 5)
            {
                throw new InvalidOperationException(
                    "Ready-made audio library is incomplete."
                );
            }
            Debug.Log(
                "Ready-made audio validation passed: 12 one-shot clips and " +
                "five footstep variants."
            );
        }

        private static AudioClip LoadClip(string root, string fileName)
        {
            string path = $"{root}/{fileName}";
            string[] matches = AssetDatabase.FindAssets(
                fileName,
                new[] { root }
            );
            foreach (string guid in matches)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(assetPath),
                        fileName,
                        StringComparison.Ordinal
                    ))
                {
                    AudioClip clip =
                        AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }
            throw new InvalidOperationException(
                $"Ready-made audio clip is missing: {path}"
            );
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Invalid folder: {path}");
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
