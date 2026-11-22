using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class CityKitEnvironmentSetup
    {
        private const string SourceRoot =
            "Assets/ThirdParty/Kenney/CityKitCommercial";
        private const string ArtRoot =
            "Assets/Demo02/Art/Environment/CityKit";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string MaterialPath = ArtRoot + "/MAT_CityKit_Atlas.mat";

        private static readonly string[] Models =
        {
            "building-a",
            "building-b",
            "building-c",
            "building-d",
            "building-e",
            "building-f",
            "building-g",
            "building-h",
            "low-detail-building-a",
            "low-detail-building-b",
            "low-detail-building-c",
            "low-detail-building-d"
        };

        [MenuItem("Demo02/Art/Build City Kit Environment Assets")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTexture($"{SourceRoot}/Textures/colormap.png");
            foreach (string model in Models)
            {
                ConfigureModel($"{SourceRoot}/Models/{model}.fbx");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Material material = CreateMaterial();
            for (int index = 0; index < Models.Length; index++)
            {
                CreateVisualPrefab(index, Models[index], material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "City Kit environment assets are ready: " +
                $"{Models.Length} ready-made building prefabs."
            );
        }

        public static void ApplyToGreybox(Transform greyboxRoot)
        {
            Transform[] buildings = greyboxRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(IsGreyboxBuilding)
                .ToArray();
            for (int index = 0; index < buildings.Length; index++)
            {
                Transform building = buildings[index];
                string visualName = $"CityVisual_{building.name}";
                if (building.parent.Find(visualName) != null)
                {
                    continue;
                }

                int visualIndex = PositiveHash(building.name) % Models.Length;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VisualPrefabPath(visualIndex)
                );
                if (prefab == null)
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
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null || material.GetTexture("_BaseMap") == null)
            {
                throw new InvalidOperationException(
                    $"City Kit material is missing or has no atlas: {MaterialPath}"
                );
            }

            for (int index = 0; index < Models.Length; index++)
            {
                string path = VisualPrefabPath(index);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing City Kit visual prefab: {path}"
                    );
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Physics.SyncTransforms();
                Bounds? bounds = GetCombinedBounds(instance);
                if (!bounds.HasValue ||
                    bounds.Value.size.y < 2.5f ||
                    bounds.Value.size.y > 5.8f ||
                    bounds.Value.size.x > 5.6f ||
                    bounds.Value.size.z > 7.5f)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    throw new InvalidOperationException(
                        $"City Kit prefab has an unexpected footprint: {path}, " +
                        $"size={(bounds.HasValue ? bounds.Value.size.ToString() : "none")}"
                    );
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log(
                "City Kit validation passed: shared atlas material and " +
                $"{Models.Length} normalized building prefabs."
            );
        }

        [MenuItem("Demo02/Art/Capture City Kit Greybox")]
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

            string directory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../docs/screenshots")
            );
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "current-game-citykit.png");
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"City Kit greybox captured at {path}");
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

        private static int PositiveHash(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value)
                {
                    hash = hash * 31 + character;
                }
                return hash & int.MaxValue;
            }
        }

        private static void ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new InvalidOperationException(
                    $"City Kit texture was not imported: {path}"
                );
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        private static void ConfigureModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                throw new InvalidOperationException(
                    $"City Kit model was not imported: {path}"
                );
            }

            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Lit shader was not found."
                );
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{SourceRoot}/Textures/colormap.png"
            );
            material.SetTexture("_BaseMap", atlas);
            material.SetTexture("_MainTex", atlas);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.24f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateVisualPrefab(
            int index,
            string modelName,
            Material material
        )
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{SourceRoot}/Models/{modelName}.fbx"
            );
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"City Kit source model was not imported: {modelName}"
                );
            }

            GameObject root = new($"VIS_CityBuilding_{index + 1:00}");
            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = modelName;
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds? bounds = GetCombinedBounds(model);
            if (!bounds.HasValue)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"City Kit model has no bounds: {modelName}"
                );
            }

            Vector3 size = bounds.Value.size;
            float scale = Mathf.Min(
                5.2f / Mathf.Max(0.01f, size.y),
                5.0f / Mathf.Max(0.01f, size.x),
                7.1f / Mathf.Max(0.01f, size.z)
            );
            model.transform.localScale *= scale;
            Physics.SyncTransforms();
            Bounds scaled = GetCombinedBounds(model)!.Value;
            model.transform.localPosition = new Vector3(
                -scaled.center.x,
                -scaled.min.y,
                -scaled.center.z
            );

            foreach (Renderer renderer in
                     model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    materials[materialIndex] = material;
                }
                renderer.sharedMaterials = materials;
            }

            PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath(index));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static string VisualPrefabPath(int index) =>
            $"{PrefabRoot}/VIS_CityBuilding_{index + 1:00}.prefab";

        private static Bounds? GetCombinedBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
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
