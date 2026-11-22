using System;
using System.Linq;
using Demo02.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class FantasyTownEnvironmentSetup
    {
        private const string PrefabPath =
            "Assets/Demo02/Art/Environment/Guangling/Prefabs/ART_Replacement_Building_8x6.prefab";

        [MenuItem("Demo02/Art/Refresh Fantasy Town Building")]
        public static void BuildAll()
        {
            ThirdPartyArtSetup.Setup();
        }

        public static void ApplyToGreybox(Transform greyboxRoot)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                return;
            }

            Transform[] buildings = greyboxRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(IsGreyboxBuilding)
                .ToArray();
            for (int index = 0; index < buildings.Length; index++)
            {
                Transform building = buildings[index];
                string visualName = $"FantasyVisual_{building.name}";
                if (building.parent.Find(visualName) != null)
                {
                    continue;
                }

                GameObject visual =
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab, building.parent);
                visual.name = visualName;
                visual.transform.position = new Vector3(
                    building.position.x,
                    0f,
                    building.position.z
                );
                visual.transform.rotation =
                    building.position.x > 0f
                        ? Quaternion.Euler(0f, 180f, 0f)
                        : Quaternion.identity;

                Color[] palette =
                {
                    new Color(1f, 0.96f, 0.88f),
                    new Color(0.88f, 0.94f, 1f),
                    new Color(0.96f, 0.9f, 1f),
                    new Color(1f, 0.9f, 0.84f)
                };
                visual.AddComponent<ColorTint>().Configure(
                    palette[index % palette.Length],
                    0.2f
                );

                MeshRenderer greyboxRenderer =
                    building.GetComponent<MeshRenderer>();
                if (greyboxRenderer != null)
                {
                    greyboxRenderer.enabled = false;
                }

                foreach (Transform sibling in building.parent)
                {
                    if (sibling == visual.transform ||
                        !sibling.name.StartsWith(
                            building.name + "_",
                            StringComparison.Ordinal
                        ))
                    {
                        continue;
                    }

                    foreach (Renderer renderer in
                             sibling.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        public static void ValidateForBatch()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Missing Fantasy Town replacement building: {PrefabPath}"
                );
            }

            if (prefab.transform.Find("Visuals") == null ||
                prefab.transform.Find("Colliders") == null ||
                prefab.transform.Find("Gameplay") == null)
            {
                throw new InvalidOperationException(
                    "Fantasy Town replacement building is missing content groups."
                );
            }

            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length < 20)
            {
                throw new InvalidOperationException(
                    "Fantasy Town replacement building does not contain a complete shell."
                );
            }

            Debug.Log(
                $"Fantasy Town environment validation passed: {renderers.Length} " +
                "ready-made visual renderers."
            );
        }

        [MenuItem("Demo02/Art/Capture Fantasy Town Greybox")]
        public static void CaptureGreybox()
        {
            EditorSceneManager.OpenScene(
                "Assets/Demo02/Scenes/Guangling/Greybox.unity",
                OpenSceneMode.Single
            );
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Greybox scene has no Main Camera."
                );
            }

            const int width = 1280;
            const int height = 720;
            RenderTexture target = new(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32
            );
            UniversalRenderPipeline.SingleCameraRequest request = new()
            {
                destination = target
            };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D screenshot = new(
                width,
                height,
                TextureFormat.RGB24,
                false
            );
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            RenderTexture.active = previous;

            string directory = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    Application.dataPath,
                    "../../docs/screenshots"
                )
            );
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(
                directory,
                "current-game-fantasy-town.png"
            );
            System.IO.File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Fantasy Town greybox captured at {path}");
        }

        private static bool IsGreyboxBuilding(Transform item)
        {
            string name = item.name;
            return name.StartsWith("LeftBuilding_", StringComparison.Ordinal) ||
                   name.StartsWith("RightBuilding_", StringComparison.Ordinal) ||
                   name.StartsWith("Dongguan_LeftShop_", StringComparison.Ordinal) ||
                   name.StartsWith("Dongguan_RightShop_", StringComparison.Ordinal) ||
                   name.StartsWith("Caiyi_LeftShop_", StringComparison.Ordinal) ||
                   name.StartsWith("Caiyi_RightShop_", StringComparison.Ordinal);
        }
    }
}
