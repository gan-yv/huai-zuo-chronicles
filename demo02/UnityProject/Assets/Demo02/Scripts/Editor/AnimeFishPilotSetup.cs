using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class AnimeFishPilotSetup
    {
        private const string ArtRoot =
            "Assets/Demo02/Art/Characters/AnimeFishPilot";
        private const string SourceModelPath =
            ArtRoot + "/VIS_WorkFish.glb";
        public const string PrefabPath =
            ArtRoot + "/VIS_AnimeFish_Work.prefab";
        private const string ScenePath =
            "Assets/Demo02/Scenes/Tests/AnimeFishPilotShowcase.unity";
        private const string ScreenshotPath =
            "../../docs/screenshots/anime-fish-pilot.png";

        [MenuItem("Demo02/Art/Build Anime Fish Pilot")]
        public static void BuildAndCapture()
        {
            Build();
            Capture();
        }

        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Anime fish model was not imported: {SourceModelPath}"
                );
            }

            GameObject root = new("VIS_AnimeFish_Work");
            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "VisualReplacementSlot";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds? sourceBounds = GetCombinedBounds(model);
            if (!sourceBounds.HasValue || sourceBounds.Value.size.y <= 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    "Anime fish model has no renderer bounds."
                );
            }

            model.transform.localScale *=
                1.72f / sourceBounds.Value.size.y;
            Physics.SyncTransforms();
            Bounds scaled = GetCombinedBounds(model)!.Value;
            model.transform.localPosition = new Vector3(
                -scaled.center.x,
                -scaled.min.y,
                -scaled.center.z
            );
            ApplyRelaxedPose(model);

            int rendererCount = 0;
            int texturedMaterialCount = 0;
            foreach (Renderer renderer in
                     model.GetComponentsInChildren<Renderer>(true))
            {
                rendererCount++;
                Mesh mesh = renderer is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                string blendShapeInfo = mesh != null
                    && mesh.blendShapeCount > 0
                    ? $", blendShapes={mesh.blendShapeCount}, "
                        + $"firstShape={mesh.GetBlendShapeName(0)}"
                    : string.Empty;
                Debug.Log(
                    "Anime fish renderer: "
                    + $"{renderer.name}, vertices="
                    + $"{(mesh != null ? mesh.vertexCount : 0)}"
                    + $", materialCount={renderer.sharedMaterials.Length}"
                    + blendShapeInfo
                );
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseMap")
                        && material.GetTexture("_BaseMap") != null)
                    {
                        texturedMaterialCount++;
                    }
                    Debug.Log(
                        "Anime fish material: "
                        + $"{material.name}, shader={material.shader.name}"
                    );
                }
            }

            if (rendererCount == 0)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    "Anime fish model imported without renderers."
                );
            }

            Bounds finalBounds = GetCombinedBounds(root)!.Value;
            Debug.Log(
                "Anime fish pilot prepared: "
                + $"renderers={rendererCount}, "
                + $"texturedMaterials={texturedMaterialCount}, "
                + $"size={finalBounds.size}."
            );

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Capture()
        {
            EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Anime fish showcase has no Main Camera."
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
            screenshot.ReadPixels(
                new Rect(0f, 0f, width, height),
                0,
                0
            );
            screenshot.Apply();
            RenderTexture.active = previous;

            string path = Path.GetFullPath(
                Path.Combine(Application.dataPath, ScreenshotPath)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Anime fish pilot captured at {path}");
        }

        private static void CreateShowcaseScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(0.58f, 0.62f, 0.7f);
            RenderSettings.fog = false;

            GameObject floor = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );
            floor.name = "Studio_Floor";
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.15f, 8f);
            SetStudioColor(
                floor,
                new Color(0.22f, 0.25f, 0.3f)
            );

            GameObject backdrop = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );
            backdrop.name = "Studio_Backdrop";
            backdrop.transform.position = new Vector3(0f, 2.8f, -2.5f);
            backdrop.transform.localScale = new Vector3(8f, 6f, 0.12f);
            SetStudioColor(
                backdrop,
                new Color(0.13f, 0.17f, 0.22f)
            );

            CreateLight(
                "Key_Light",
                new Vector3(35f, -35f, 0f),
                new Color(1f, 0.86f, 0.72f),
                1.8f
            );
            CreateLight(
                "Fill_Light",
                new Vector3(12f, 145f, 0f),
                new Color(0.6f, 0.76f, 1f),
                0.8f
            );
            CreateLight(
                "Rim_Light",
                new Vector3(55f, 190f, 0f),
                new Color(1f, 0.54f, 0.36f),
                1.2f
            );

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.08f, 0.11f, 0.16f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.05f;
            cameraObject.transform.position =
                new Vector3(0f, 1.05f, 3.1f);
            cameraObject.transform.LookAt(
                new Vector3(0f, 0.92f, 0f)
            );
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Anime fish prefab was not created: {PrefabPath}"
                );
            }
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "AnimeFish_Work";
            instance.transform.position = Vector3.zero;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save anime fish showcase: {ScenePath}"
                );
            }
        }

        private static void CreateLight(
            string name,
            Vector3 rotation,
            Color color,
            float intensity)
        {
            GameObject lightObject = new(name);
            lightObject.transform.rotation =
                Quaternion.Euler(rotation);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void SetStudioColor(
            GameObject target,
            Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            Material material = new(
                Shader.Find("Universal Render Pipeline/Lit")
            );
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            renderer.sharedMaterial = material;
        }

        private static Bounds? GetCombinedBounds(GameObject target)
        {
            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static void ApplyRelaxedPose(GameObject root)
        {
            LowerArm(
                root.transform,
                FindDescendant(root.transform, "upper_arm.L"),
                FindDescendant(root.transform, "lower_arm.L"),
                -1f
            );
            LowerArm(
                root.transform,
                FindDescendant(root.transform, "upper_arm.R"),
                FindDescendant(root.transform, "lower_arm.R"),
                1f
            );
        }

        private static void LowerArm(
            Transform root,
            Transform upperArm,
            Transform lowerArm,
            float side)
        {
            if (upperArm == null || lowerArm == null)
            {
                return;
            }

            Vector3 current = lowerArm.position - upperArm.position;
            if (current.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 target = (
                -root.up * 0.94f
                + root.right * side * 0.24f
                + root.forward * 0.06f
            ).normalized;
            upperArm.rotation =
                Quaternion.FromToRotation(current.normalized, target)
                * upperArm.rotation;
            Physics.SyncTransforms();
        }

        private static Transform FindDescendant(
            Transform root,
            string name)
        {
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
