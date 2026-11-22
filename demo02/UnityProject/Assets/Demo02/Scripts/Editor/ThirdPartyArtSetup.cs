using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class ThirdPartyArtSetup
    {
        private const string FantasyTownRoot =
            "Assets/ThirdParty/Kenney/FantasyTownKit";
        private const string PolyHavenRoot =
            "Assets/ThirdParty/PolyHaven";
        private const string MaterialRoot =
            "Assets/Demo02/Art/Materials/ThirdPartySamples";
        private const string PrefabRoot =
            "Assets/Demo02/Art/Environment/Guangling/Prefabs";
        private const string PreviewScenePath =
            "Assets/Demo02/Scenes/Tests/ArtImportShowcase.unity";

        private static readonly string[] ModelFiles =
        {
            "wall-wood-door.fbx",
            "wall-wood-window-small.fbx",
            "wall-wood-window-shutters.fbx",
            "wall-wood-arch.fbx",
            "roof.fbx",
            "roof-gable.fbx",
            "roof-gable-end.fbx",
            "pillar-wood.fbx",
            "lantern.fbx",
            "stall.fbx",
            "cart.fbx",
            "tree-high.fbx"
        };

        [MenuItem("Demo02/Art/Setup Third-Party Samples")]
        public static void Setup()
        {
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureThirdPartyTextures();
            Material kenneyMaterial = CreateKenneyMaterial();
            Material plaster = CreatePolyHavenMaterial(
                "grey_plaster_02",
                "GreyPlaster02"
            );
            Material planks = CreatePolyHavenMaterial(
                "old_planks_02",
                "OldPlanks02"
            );
            Material roof = CreatePolyHavenMaterial(
                "clay_roof_tiles_02",
                "ClayRoofTiles02"
            );
            Material previewFloor = CreatePreviewFloorMaterial();
            Material pedestal = CreatePreviewPedestalMaterial();
            GameObject samplePrefab = CreateSamplePrefab(kenneyMaterial);
            CreateTemplatePrefab();
            CreatePreviewScene(
                samplePrefab,
                previewFloor,
                plaster,
                planks,
                roof,
                pedestal
            );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Third-party art samples are ready. " +
                $"Open {PreviewScenePath} to inspect them."
            );
        }

        [MenuItem("Demo02/Art/Capture Import Preview")]
        public static void CapturePreview()
        {
            EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Preview scene has no Main Camera.");
            }

            const int width = 1280;
            const int height = 720;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            UniversalRenderPipeline.SingleCameraRequest request = new()
            {
                destination = target
            };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
            {
                throw new InvalidOperationException(
                    "The active render pipeline cannot render the preview camera."
                );
            }

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

            string screenshotPath = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../../docs/screenshots/art-import-showcase.png"
                )
            );
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Art import preview captured at {screenshotPath}");
        }

        [MenuItem("Demo02/Art/Log Third-Party Model Bounds")]
        public static void LogModelBounds()
        {
            foreach (string modelFile in ModelFiles)
            {
                string modelPath = $"{FantasyTownRoot}/Models/{modelFile}";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    Debug.LogWarning($"Model was not found: {modelPath}");
                    continue;
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(model);
                Bounds? combinedBounds = GetCombinedBounds(instance);
                if (combinedBounds.HasValue)
                {
                    Bounds bounds = combinedBounds.Value;
                    Debug.Log(
                        $"MODEL_BOUNDS {modelFile} " +
                        $"center={bounds.center} size={bounds.size} " +
                        $"scale={instance.transform.localScale}"
                    );
                }

                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [MenuItem("Demo02/Art/Validate Third-Party Samples")]
        public static void ValidateSamples()
        {
            List<string> failures = new();
            foreach (string modelFile in ModelFiles)
            {
                string modelPath = $"{FantasyTownRoot}/Models/{modelFile}";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
                {
                    failures.Add($"Missing model: {modelPath}");
                }
            }

            ValidateMaterial(
                $"{MaterialRoot}/MAT_Kenney_FantasyTownAtlas.mat",
                true,
                false,
                failures
            );
            ValidateMaterial(
                $"{MaterialRoot}/MAT_PolyHaven_GreyPlaster02.mat",
                true,
                true,
                failures
            );
            ValidateMaterial(
                $"{MaterialRoot}/MAT_PolyHaven_OldPlanks02.mat",
                true,
                true,
                failures
            );
            ValidateMaterial(
                $"{MaterialRoot}/MAT_PolyHaven_ClayRoofTiles02.mat",
                true,
                true,
                failures
            );
            ValidateMaterial(
                $"{MaterialRoot}/MAT_Preview_Floor.mat",
                false,
                false,
                failures
            );
            ValidateMaterial(
                $"{MaterialRoot}/MAT_Preview_Pedestal.mat",
                false,
                false,
                failures
            );

            string prefabPath = $"{PrefabRoot}/ART_Sample_FantasyTownKit.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing sample prefab: {prefabPath}");
            }
            else
            {
                Transform visuals = prefab.transform.Find("Visuals");
                if (visuals == null)
                {
                    failures.Add($"Sample prefab has no Visuals node: {prefabPath}");
                }
                else if (visuals.childCount != ModelFiles.Length)
                {
                    failures.Add(
                        "Sample prefab expected " +
                        $"{ModelFiles.Length} visual modules but found " +
                        $"{visuals.childCount}."
                    );
                }

                foreach (Renderer renderer in
                         prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterials.Length == 0 ||
                        Array.Exists(
                            renderer.sharedMaterials,
                            material => material == null || material.shader == null
                        ))
                    {
                        failures.Add(
                            $"Renderer has a missing material: " +
                            $"{renderer.name} in {prefabPath}"
                        );
                    }
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath) == null)
            {
                failures.Add($"Missing preview scene: {PreviewScenePath}");
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Third-party art sample validation failed:\n" +
                    string.Join("\n", failures)
                );
            }

            Debug.Log(
                "Third-party art sample validation passed: " +
                $"{ModelFiles.Length} models, URP materials, prefab, and preview scene."
            );
        }

        private static void ValidateMaterial(
            string path,
            bool requireBaseMap,
            bool requirePbrMaps,
            List<string> failures
        )
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                failures.Add($"Missing material: {path}");
                return;
            }

            if (material.shader == null ||
                !material.shader.name.Contains(
                    "Universal Render Pipeline",
                    StringComparison.Ordinal
                ))
            {
                failures.Add($"Material does not use URP Lit: {path}");
            }

            if (requireBaseMap && material.GetTexture("_BaseMap") == null)
            {
                failures.Add($"Material has no base map: {path}");
            }

            if (!requirePbrMaps)
            {
                return;
            }

            foreach (string property in new[]
                     {
                         "_BumpMap",
                         "_MetallicGlossMap",
                         "_OcclusionMap"
                     })
            {
                if (material.GetTexture(property) == null)
                {
                    failures.Add($"Material has no {property}: {path}");
                }
            }
        }

        private static void ConfigureThirdPartyTextures()
        {
            ConfigureTexture(
                $"{FantasyTownRoot}/Textures/colormap.png",
                false
            );

            foreach (string assetId in new[]
                     {
                         "grey_plaster_02",
                         "old_planks_02",
                         "clay_roof_tiles_02"
                     })
            {
                string root = $"{PolyHavenRoot}/{assetId}";
                ConfigureTexture(
                    $"{root}/{assetId}_diffuse_2k.jpg",
                    false
                );
                ConfigureTexture(
                    $"{root}/{assetId}_normal_2k.jpg",
                    true
                );
                ConfigureTexture(
                    $"{root}/{assetId}_rough_2k.jpg",
                    false,
                    false
                );
                ConfigureTexture(
                    $"{root}/{assetId}_arm_2k.jpg",
                    false,
                    false,
                    true
                );
            }
        }

        private static void ConfigureTexture(
            string path,
            bool normalMap,
            bool sRgb = true,
            bool readable = false
        )
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new InvalidOperationException($"Texture was not found: {path}");
            }

            importer.textureType = normalMap
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap && sRgb;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static Material CreateKenneyMaterial()
        {
            Material material = LoadOrCreateMaterial(
                $"{MaterialRoot}/MAT_Kenney_FantasyTownAtlas.mat"
            );
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{FantasyTownRoot}/Textures/colormap.png"
            );
            material.SetTexture("_BaseMap", atlas);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePolyHavenMaterial(
            string assetId,
            string materialSuffix
        )
        {
            string textureRoot = $"{PolyHavenRoot}/{assetId}";
            string diffusePath = $"{textureRoot}/{assetId}_diffuse_2k.jpg";
            string normalPath = $"{textureRoot}/{assetId}_normal_2k.jpg";
            string armPath = $"{textureRoot}/{assetId}_arm_2k.jpg";
            string generatedRoot = $"{MaterialRoot}/Generated";
            EnsureFolder(generatedRoot);

            CreateUrpMasks(
                armPath,
                $"{generatedRoot}/T_{materialSuffix}_URP_AO.png",
                $"{generatedRoot}/T_{materialSuffix}_URP_MetallicSmoothness.png"
            );

            Material material = LoadOrCreateMaterial(
                $"{MaterialRoot}/MAT_PolyHaven_{materialSuffix}.mat"
            );
            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D ao = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{generatedRoot}/T_{materialSuffix}_URP_AO.png"
            );
            Texture2D metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"{generatedRoot}/T_{materialSuffix}_URP_MetallicSmoothness.png"
                );

            material.SetTexture("_BaseMap", diffuse);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_OcclusionMap", ao);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_OcclusionStrength", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateUrpMasks(
            string armPath,
            string aoPath,
            string metallicSmoothnessPath
        )
        {
            Texture2D arm = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath);
            if (arm == null)
            {
                throw new InvalidOperationException($"ARM texture was not found: {armPath}");
            }

            int width = arm.width;
            int height = arm.height;
            Color32[] source = arm.GetPixels32();
            int reduction = Mathf.Max(1, width / 1024);
            int outputWidth = width / reduction;
            int outputHeight = height / reduction;
            Color32[] aoPixels = new Color32[outputWidth * outputHeight];
            Color32[] maskPixels = new Color32[aoPixels.Length];

            for (int y = 0; y < outputHeight; y++)
            {
                for (int x = 0; x < outputWidth; x++)
                {
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    for (int offsetY = 0; offsetY < reduction; offsetY++)
                    {
                        for (int offsetX = 0; offsetX < reduction; offsetX++)
                        {
                            Color32 value =
                                source[
                                    (y * reduction + offsetY) * width +
                                    x * reduction +
                                    offsetX
                                ];
                            red += value.r;
                            green += value.g;
                            blue += value.b;
                        }
                    }

                    int samples = reduction * reduction;
                    int outputIndex = y * outputWidth + x;
                    byte ao = (byte)(red / samples);
                    byte roughness = (byte)(green / samples);
                    byte metallic = (byte)(blue / samples);
                    aoPixels[outputIndex] = new Color32(ao, ao, ao, 255);
                    maskPixels[outputIndex] = new Color32(
                        metallic,
                        0,
                        0,
                        (byte)(255 - roughness)
                    );
                }
            }

            WriteMask(aoPath, outputWidth, outputHeight, aoPixels);
            WriteMask(
                metallicSmoothnessPath,
                outputWidth,
                outputHeight,
                maskPixels
            );
        }

        private static void WriteMask(
            string path,
            int width,
            int height,
            Color32[] pixels
        )
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, true, true);
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureTexture(path, false, false);
        }

        private static Material CreatePreviewFloorMaterial()
        {
            Material floor = LoadOrCreateMaterial(
                $"{MaterialRoot}/MAT_Preview_Floor.mat"
            );
            floor.SetColor("_BaseColor", new Color(0.32f, 0.34f, 0.33f));
            floor.SetFloat("_Metallic", 0f);
            floor.SetFloat("_Smoothness", 0.16f);
            EditorUtility.SetDirty(floor);
            return floor;
        }

        private static Material CreatePreviewPedestalMaterial()
        {
            Material pedestal = LoadOrCreateMaterial(
                $"{MaterialRoot}/MAT_Preview_Pedestal.mat"
            );
            pedestal.SetColor("_BaseColor", new Color(0.14f, 0.12f, 0.09f));
            pedestal.SetFloat("_Metallic", 0f);
            pedestal.SetFloat("_Smoothness", 0.22f);
            EditorUtility.SetDirty(pedestal);
            return pedestal;
        }

        private static GameObject CreateSamplePrefab(Material kenneyMaterial)
        {
            GameObject root = new("ART_Sample_FantasyTownKit");
            GameObject visuals = new("Visuals");
            visuals.transform.SetParent(root.transform, false);

            Vector3[] positions =
            {
                new(-6f, 0f, 0f),
                new(-2f, 0f, 0f),
                new(2f, 0f, 0f),
                new(6f, 0f, 0f),
                new(-2f, 0f, 4f),
                new(2f, 0f, 4f),
                new(6f, 0f, 4f),
                new(-7f, 0f, 4f),
                new(-4f, 0f, 11f),
                new(0f, 0f, 11f),
                new(4f, 0f, 11f),
                new(9f, 0f, 11f)
            };

            for (int i = 0; i < ModelFiles.Length; i++)
            {
                string modelPath = $"{FantasyTownRoot}/Models/{ModelFiles[i]}";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    throw new InvalidOperationException($"Model was not found: {modelPath}");
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = Path.GetFileNameWithoutExtension(ModelFiles[i]);
                NormalizeModelScale(instance, ModelFiles[i]);
                Vector3 pivotOffset = GetPivotOffset(instance);
                instance.transform.SetParent(visuals.transform, false);
                instance.transform.localPosition = positions[i] + pivotOffset;
                ApplyMaterial(instance, kenneyMaterial);
            }

            string prefabPath = $"{PrefabRoot}/ART_Sample_FantasyTownKit.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void NormalizeModelScale(
            GameObject model,
            string modelFile
        )
        {
            Bounds? bounds = GetCombinedBounds(model);
            if (!bounds.HasValue)
            {
                throw new InvalidOperationException(
                    $"Model has no render bounds: {modelFile}"
                );
            }

            Vector3 size = bounds.Value.size;
            float sourceSize;
            float targetSize;
            if (modelFile.StartsWith("wall-wood", StringComparison.Ordinal))
            {
                sourceSize = Mathf.Max(size.y, size.z, 0.001f);
                targetSize = 4f;
            }
            else if (modelFile.StartsWith("roof", StringComparison.Ordinal))
            {
                sourceSize = Mathf.Max(size.x, size.z, 0.001f);
                targetSize = 4f;
            }
            else if (modelFile.StartsWith("pillar", StringComparison.Ordinal) ||
                     modelFile.StartsWith("lantern", StringComparison.Ordinal))
            {
                sourceSize = Mathf.Max(size.y, 0.001f);
                targetSize = modelFile.StartsWith("lantern", StringComparison.Ordinal)
                    ? 2f
                    : 4f;
            }
            else if (modelFile.StartsWith("tree", StringComparison.Ordinal))
            {
                sourceSize = Mathf.Max(size.y, 0.001f);
                targetSize = 6f;
            }
            else
            {
                sourceSize = Mathf.Max(size.x, size.z, 0.001f);
                targetSize = 3f;
            }

            float scale = targetSize / sourceSize;
            model.transform.localScale *= scale;
        }

        private static Vector3 GetPivotOffset(GameObject model)
        {
            Bounds? bounds = GetCombinedBounds(model);
            if (!bounds.HasValue)
            {
                return Vector3.zero;
            }

            Bounds value = bounds.Value;
            return new Vector3(
                -value.center.x,
                -value.min.y,
                -value.center.z
            );
        }

        private static Bounds? GetCombinedBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void CreateTemplatePrefab()
        {
            GameObject root = new("ART_Template_Environment");
            new GameObject("Visuals").transform.SetParent(root.transform, false);
            new GameObject("Colliders").transform.SetParent(root.transform, false);
            new GameObject("Gameplay").transform.SetParent(root.transform, false);
            new GameObject("Metadata").transform.SetParent(root.transform, false);

            string prefabPath = $"{PrefabRoot}/ART_Template_Environment.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreatePreviewScene(
            GameObject samplePrefab,
            Material floorMaterial,
            Material plaster,
            Material planks,
            Material roof,
            Material pedestal
        )
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.34f, 0.31f);

            GameObject sample =
                (GameObject)PrefabUtility.InstantiatePrefab(samplePrefab);
            sample.name = "ThirdPartyArtSample";

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PreviewFloor";
            floor.transform.position = new Vector3(1f, -0.02f, 2f);
            floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            CreateMaterialSwatch(
                new Vector3(-5f, 0f, 7f),
                plaster,
                pedestal,
                "GREY PLASTER"
            );
            CreateMaterialSwatch(
                new Vector3(0f, 0f, 7f),
                planks,
                pedestal,
                "OLD PLANKS"
            );
            CreateMaterialSwatch(
                new Vector3(5f, 0f, 7f),
                roof,
                pedestal,
                "CLAY ROOF"
            );

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 58f;
            cameraObject.transform.position = new Vector3(1f, 6.5f, -11.5f);
            cameraObject.transform.LookAt(new Vector3(1f, 2.1f, 5f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.52f, 0.62f, 0.68f);

            GameObject lightObject = new("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.88f, 0.72f);

            GameObject fillObject = new("Fill Light");
            fillObject.transform.rotation = Quaternion.Euler(28f, 145f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.7f;
            fill.color = new Color(0.72f, 0.82f, 1f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        private static void CreateMaterialSwatch(
            Vector3 position,
            Material material,
            Material pedestalMaterial,
            string label
        )
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = $"{label} Pedestal";
            pedestal.transform.position = position + new Vector3(0f, 0.45f, 0f);
            pedestal.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = pedestalMaterial;

            GameObject swatch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            swatch.name = $"{label} Swatch";
            swatch.transform.position = position + new Vector3(0f, 1.55f, 0f);
            swatch.transform.localScale = Vector3.one * 1.25f;
            swatch.GetComponent<MeshRenderer>().sharedMaterial = material;

            CreateWorldLabel(
                label,
                position + new Vector3(0f, 2.55f, -0.45f),
                0.055f
            );
        }

        private static void CreateWorldLabel(
            string text,
            Vector3 position,
            float characterSize
        )
        {
            GameObject labelObject = new(text);
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = new Color(0.12f, 0.09f, 0.06f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                textMesh.font = font;
                labelObject.GetComponent<MeshRenderer>().sharedMaterial =
                    font.material;
            }

            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.identity;
        }

        private static void ApplyMaterial(
            GameObject gameObject,
            Material material
        )
        {
            foreach (Renderer renderer in
                     gameObject.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Material LoadOrCreateMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "Universal Render Pipeline/Lit shader was not found."
                    );
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
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
                throw new InvalidOperationException($"Invalid asset folder: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
