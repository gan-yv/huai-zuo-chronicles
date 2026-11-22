using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demo02.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class QuaterniusEnvironmentSetup
    {
        private const string SourceRoot =
            "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit";
        private const string ModelRoot = SourceRoot + "/Models";
        private const string TextureRoot = SourceRoot + "/Textures";
        private const string ArtRoot =
            "Assets/Demo02/Art/Environment/Quaternius";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string MeshRoot = ArtRoot + "/Meshes";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string PilotScenePath =
            "Assets/Demo02/Scenes/Tests/QuaterniusVillagePilot.unity";
        private const string ScreenshotPath =
            "demo02/docs/screenshots/quaternius-village-pilot.png";
        private const string PilotBuildDirectory =
            "../Builds/QuaterniusPilot";

        private static readonly string[] HouseNames =
        {
            "ART_Quaternius_PlasterHouse_6x8",
            "ART_Quaternius_BrickHouse_6x8",
            "ART_Quaternius_TimberHouse_6x8"
        };

        private static readonly Dictionary<string, Material> MaterialLibrary =
            new(StringComparer.Ordinal);

        [MenuItem("Demo02/Art/Quaternius/Rebuild Environment Kit")]
        public static void BuildAll()
        {
            BuildAllInternal();
        }

        public static void BuildPilotForBatch()
        {
            BuildAllInternal();
            CreatePilotScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Quaternius pilot is ready: " +
                $"{PilotScenePath} with {HouseNames.Length} instanced house shells."
            );
        }

        [MenuItem("Demo02/Art/Quaternius/Capture Pilot Screenshot")]
        public static void CapturePilot()
        {
            CaptureScene(PilotScenePath, ScreenshotPath);
        }

        [MenuItem("Demo02/Art/Quaternius/Capture Main Street Screenshot")]
        public static void CaptureAppliedGreybox()
        {
            CaptureScene(
                "Assets/Demo02/Scenes/Guangling/Greybox.unity",
                "demo02/docs/screenshots/quaternius-main-street.png"
            );
        }

        public static void AuditVisibleGreyboxForBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Demo02/Scenes/Guangling/Greybox.unity",
                OpenSceneMode.Single
            );
            GameObject root = scene
                .GetRootGameObjects()
                .FirstOrDefault(item => item.name == "Greybox");
            if (root == null)
            {
                throw new InvalidOperationException(
                    "Greybox root was not found for visual audit."
                );
            }

            string[] ignoredPrefixes =
            {
                "QuaterniusBuildings_",
                "QuaterniusFillLight",
                "OfficialStreetPaving_Quaternius",
                "DongguanPaving_Quaternius",
                "CaiyiPaving_Quaternius"
            };
            MeshRenderer[] renderers =
                root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers
                         .Where(item => item.enabled)
                         .OrderBy(item => item.transform.position.z)
                         .ThenBy(item => item.transform.position.x))
            {
                if (ignoredPrefixes.Any(prefix =>
                        renderer.name.StartsWith(
                            prefix,
                            StringComparison.Ordinal
                        )))
                {
                    continue;
                }

                Transform area = renderer.transform;
                while (area.parent != null && area.parent != root.transform)
                {
                    area = area.parent;
                }
                Bounds bounds = renderer.bounds;
                Debug.Log(
                    $"GREYBOX_VISUAL area={area.name} name={renderer.name} " +
                    $"position={renderer.transform.position} " +
                    $"size={bounds.size} material={renderer.sharedMaterial?.name}"
                );
            }
        }

        private static void CaptureScene(
            string scenePath,
            string screenshotPath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Quaternius scene is missing: {scenePath}"
                );
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 85f;
            RenderSettings.fogColor = new Color(0.56f, 0.62f, 0.62f);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Quaternius pilot scene has no Main Camera."
                );
            }

            const int width = 1600;
            const int height = 900;
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
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
            {
                throw new InvalidOperationException(
                    "The active render pipeline cannot render the pilot camera."
                );
            }

            for (int frame = 0; frame < 4; frame++)
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
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

            string fullPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../..", screenshotPath)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, screenshot.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Quaternius screenshot captured at {fullPath}");
        }

        public static void BuildPilotWindowsForBatch()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PilotScenePath) == null)
            {
                BuildPilotForBatch();
            }

            ConfigurePlayerForPilot();
            string outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, PilotBuildDirectory)
            );
            Directory.CreateDirectory(outputDirectory);
            string executablePath = Path.Combine(
                outputDirectory,
                "QuaterniusPilot.exe"
            );

            BuildPlayerOptions options = new()
            {
                scenes = new[] { PilotScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Quaternius pilot Windows build failed: " +
                    report.summary.result
                );
            }

            File.WriteAllText(
                Path.Combine(outputDirectory, "BuildInfo.txt"),
                $"Product: Quaternius Village Pilot{Environment.NewLine}" +
                $"Built: {DateTime.Now:O}{Environment.NewLine}" +
                $"Result: {report.summary.result}{Environment.NewLine}" +
                $"Output: {executablePath}{Environment.NewLine}" +
                $"Size: {report.summary.totalSize} bytes{Environment.NewLine}" +
                $"Duration: {report.summary.totalTime}{Environment.NewLine}"
            );
            Debug.Log(
                $"Quaternius pilot Windows build completed at {executablePath}"
            );
        }

        public static void ValidateForBatch()
        {
            List<string> failures = new();
            foreach (string houseName in HouseNames)
            {
                string prefabPath = $"{PrefabRoot}/{houseName}.prefab";
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    failures.Add($"Missing house prefab: {prefabPath}");
                    continue;
                }

                MeshRenderer renderer =
                    prefab.GetComponentInChildren<MeshRenderer>(true);
                MeshFilter filter =
                    prefab.GetComponentInChildren<MeshFilter>(true);
                if (renderer == null || filter == null || filter.sharedMesh == null)
                {
                    failures.Add($"House has no combined mesh: {prefabPath}");
                    continue;
                }

                int subMeshCount = filter.sharedMesh.subMeshCount;
                if (subMeshCount < 3 ||
                    subMeshCount > 12 ||
                    renderer.sharedMaterials.Length != subMeshCount)
                {
                    failures.Add(
                        $"Unexpected material batching for {houseName}: " +
                        $"{subMeshCount} submeshes and " +
                        $"{renderer.sharedMaterials.Length} materials."
                    );
                }

                if (renderer.sharedMaterials.Any(
                        material =>
                            material == null ||
                            material.shader == null ||
                            (!material.name.Contains(
                                "WindowGlass",
                                StringComparison.Ordinal
                            ) &&
                             material.GetTexture("_BaseMap") == null)
                    ))
                {
                    failures.Add(
                        $"House contains an invalid or untracked material: {houseName}"
                    );
                }

                Bounds bounds = renderer.bounds;
                if (bounds.size.x < 5.2f ||
                    bounds.size.x > 7.2f ||
                    bounds.size.z < 7.1f ||
                    bounds.size.z > 10.4f ||
                    bounds.size.y < 6.3f ||
                    bounds.size.y > 8.8f)
                {
                    failures.Add(
                        $"House has an unexpected footprint: {houseName}, " +
                        $"size={bounds.size}"
                    );
                }

                Collider[] colliders =
                    prefab.GetComponentsInChildren<Collider>(true);
                if (colliders.Length > 0)
                {
                    failures.Add(
                        $"Visual house prefab must not own gameplay colliders: " +
                        $"{houseName}"
                    );
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PilotScenePath) == null)
            {
                failures.Add($"Missing pilot scene: {PilotScenePath}");
            }
            ValidateAppliedScene(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Quaternius environment validation failed:\n" +
                    string.Join("\n", failures)
                );
            }

            Debug.Log(
                "Quaternius validation passed: 3 batched ready-made house " +
                "shells, URP materials, street batching, and pilot scene."
            );
        }

        private static void ValidateAppliedScene(List<string> failures)
        {
            const string scenePath =
                "Assets/Demo02/Scenes/Guangling/Greybox.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive
                );
            }

            try
            {
                GameObject root = scene
                    .GetRootGameObjects()
                    .FirstOrDefault(item => item.name == "Greybox");
                if (root == null)
                {
                    failures.Add("Applied Quaternius scene has no Greybox root.");
                    return;
                }

                foreach (string groupName in new[]
                         {
                             "OfficialStreet",
                             "DongguanStreet",
                             "CaiyiStreet"
                         })
                {
                    Transform aggregate = root.transform.Find(
                        $"QuaterniusBuildings_{groupName}"
                    );
                    MeshRenderer renderer = aggregate != null
                        ? aggregate.GetComponent<MeshRenderer>()
                        : null;
                    MeshFilter filter = aggregate != null
                        ? aggregate.GetComponent<MeshFilter>()
                        : null;
                    if (renderer == null ||
                        filter == null ||
                        filter.sharedMesh == null ||
                        renderer.sharedMaterials.Length > 10 ||
                        filter.sharedMesh.subMeshCount !=
                        renderer.sharedMaterials.Length)
                    {
                        failures.Add(
                            $"Applied street batch is invalid: {groupName}"
                        );
                    }
                }

                Transform building = root.transform.Find("LeftBuilding_-14");
                BoxCollider collider = building != null
                    ? building.GetComponent<BoxCollider>()
                    : null;
                if (collider == null || !collider.enabled)
                {
                    failures.Add(
                        "Quaternius swap did not preserve the gameplay building collider."
                    );
                }
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        public static void ApplyToGreybox(Transform greyboxRoot)
        {
            Transform[] buildings = greyboxRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(IsGreyboxBuilding)
                .ToArray();
            Dictionary<string, List<GameObject>> groupedVisuals = new()
            {
                { "OfficialStreet", new List<GameObject>() },
                { "DongguanStreet", new List<GameObject>() },
                { "CaiyiStreet", new List<GameObject>() }
            };
            for (int index = 0; index < buildings.Length; index++)
            {
                Transform building = buildings[index];
                string visualName = $"QuaterniusVisual_{building.name}";
                if (building.parent.Find(visualName) != null)
                {
                    continue;
                }

                int visualIndex = PositiveHash(building.name) % HouseNames.Length;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabRoot}/{HouseNames[visualIndex]}.prefab"
                );
                if (prefab == null)
                {
                    continue;
                }

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(
                    prefab,
                    building.parent
                );
                visual.name = visualName;
                visual.transform.position = new Vector3(
                    building.position.x,
                    0f,
                    building.position.z
                );
                bool isRightSide =
                    building.name.Contains("Right", StringComparison.Ordinal);
                visual.transform.rotation = Quaternion.Euler(
                    0f,
                    isRightSide ? 90f : -90f,
                    0f
                );
                visual.transform.localScale = new Vector3(
                    1.32f,
                    0.86f,
                    0.75f
                );
                foreach (Transform child in
                         visual.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.isStatic = true;
                }
                groupedVisuals[GetAreaGroup(building.name)].Add(visual);

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

            foreach ((string groupName, List<GameObject> visuals) in groupedVisuals)
            {
                if (visuals.Count > 0)
                {
                    CombineVisualGroup(
                        greyboxRoot,
                        groupName,
                        visuals
                    );
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor =
                new Color(0.57f, 0.63f, 0.67f);
            RenderSettings.ambientEquatorColor =
                new Color(0.38f, 0.36f, 0.32f);
            RenderSettings.ambientGroundColor =
                new Color(0.16f, 0.15f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 132f;
            RenderSettings.fogColor = new Color(0.59f, 0.64f, 0.64f);
            ApplyPavingMaterials(greyboxRoot);
            EnsureFillLight(greyboxRoot);
            ApplyWallRuns(greyboxRoot);
        }

        private static string GetAreaGroup(string buildingName)
        {
            if (buildingName.StartsWith("Caiyi_", StringComparison.Ordinal))
            {
                return "CaiyiStreet";
            }
            if (buildingName.StartsWith("Dongguan_", StringComparison.Ordinal))
            {
                return "DongguanStreet";
            }
            return "OfficialStreet";
        }

        private static void CombineVisualGroup(
            Transform greyboxRoot,
            string groupName,
            List<GameObject> visuals)
        {
            Dictionary<Material, List<CombineInstance>> batches = new();
            foreach (GameObject visual in visuals)
            {
                foreach (MeshFilter filter in
                         visual.GetComponentsInChildren<MeshFilter>(true))
                {
                    MeshRenderer renderer =
                        filter.GetComponent<MeshRenderer>();
                    Mesh sourceMesh = filter.sharedMesh;
                    if (renderer == null ||
                        sourceMesh == null ||
                        !renderer.enabled)
                    {
                        continue;
                    }
                    if (!sourceMesh.isReadable)
                    {
                        throw new InvalidOperationException(
                            $"Quaternius visual is not CPU-readable: {filter.name}"
                        );
                    }

                    Matrix4x4 matrix =
                        greyboxRoot.worldToLocalMatrix *
                        filter.transform.localToWorldMatrix;
                    for (int subMesh = 0;
                         subMesh < sourceMesh.subMeshCount;
                         subMesh++)
                    {
                        Material material = renderer.sharedMaterials[
                            Mathf.Min(
                                subMesh,
                                renderer.sharedMaterials.Length - 1
                            )
                        ];
                        if (material == null)
                        {
                            continue;
                        }
                        if (!batches.TryGetValue(material, out var batch))
                        {
                            batch = new List<CombineInstance>();
                            batches.Add(material, batch);
                        }
                        batch.Add(new CombineInstance
                        {
                            mesh = sourceMesh,
                            subMeshIndex = subMesh,
                            transform = matrix
                        });
                    }
                }
            }

            List<CombineInstance> materialMeshes = new();
            List<Mesh> temporaryMeshes = new();
            try
            {
                foreach ((Material material, List<CombineInstance> batch) in
                         batches.OrderBy(pair => pair.Key.name))
                {
                    Mesh layer = new()
                    {
                        name = $"{groupName}_{material.name}",
                        indexFormat = IndexFormat.UInt32
                    };
                    layer.CombineMeshes(
                        batch.ToArray(),
                        mergeSubMeshes: true,
                        useMatrices: true,
                        hasLightmapData: false
                    );
                    layer.UploadMeshData(markNoLongerReadable: false);
                    temporaryMeshes.Add(layer);
                    materialMeshes.Add(new CombineInstance
                    {
                        mesh = layer,
                        subMeshIndex = 0,
                        transform = Matrix4x4.identity
                    });
                }

                Mesh combined = new()
                {
                    name = $"SM_Quaternius_{groupName}",
                    indexFormat = IndexFormat.UInt32
                };
                combined.CombineMeshes(
                    materialMeshes.ToArray(),
                    mergeSubMeshes: false,
                    useMatrices: true,
                    hasLightmapData: false
                );
                combined.RecalculateBounds();

                string meshPath =
                    $"{MeshRoot}/SM_Quaternius_{groupName}.asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(combined, existing);
                    combined = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(combined, meshPath);
                }
                EditorUtility.SetDirty(combined);

                GameObject aggregate = new(
                    $"QuaterniusBuildings_{groupName}"
                );
                aggregate.transform.SetParent(greyboxRoot, false);
                MeshFilter filter = aggregate.AddComponent<MeshFilter>();
                MeshRenderer renderer = aggregate.AddComponent<MeshRenderer>();
                filter.sharedMesh = combined;
                renderer.sharedMaterials = batches.Keys
                    .OrderBy(material => material.name)
                    .ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                aggregate.isStatic = true;
            }
            finally
            {
                foreach (Mesh temporary in temporaryMeshes)
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }

            foreach (GameObject visual in visuals)
            {
                UnityEngine.Object.DestroyImmediate(visual);
            }
            Debug.Log(
                $"Quaternius {groupName} combined: {visuals.Count} buildings, " +
                $"{batches.Count} material batches."
            );
        }

        private static void ApplyPavingMaterials(Transform greyboxRoot)
        {
            Material paving = CreatePavingMaterial(
                "MAT_Quaternius_Paving",
                "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_Cobblestone05.mat",
                8f,
                12f
            );
            Material curb = CreatePavingMaterial(
                "MAT_Quaternius_Curb",
                "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_GreyPlaster02.mat",
                3f,
                2f
            );

            string[] pavingGroups =
            {
                "OfficialStreetPaving",
                "DongguanPaving",
                "CaiyiPaving"
            };
            MeshRenderer[] renderers =
                greyboxRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (string groupName in pavingGroups)
            {
                List<MeshRenderer> groupRenderers = renderers
                    .Where(renderer =>
                        renderer.name.StartsWith(
                            groupName,
                            StringComparison.Ordinal
                        ))
                    .ToList();
                if (groupRenderers.Count == 0)
                {
                    continue;
                }

                Bounds bounds = groupRenderers[0].bounds;
                for (int index = 1; index < groupRenderers.Count; index++)
                {
                    bounds.Encapsulate(groupRenderers[index].bounds);
                }

                foreach (MeshRenderer renderer in groupRenderers)
                {
                    renderer.enabled = false;
                }

                GameObject surface = GameObject.CreatePrimitive(
                    PrimitiveType.Plane
                );
                surface.name = $"{groupName}_QuaterniusSurface";
                surface.transform.SetParent(greyboxRoot, true);
                surface.transform.position = new Vector3(
                    bounds.center.x,
                    0.018f,
                    bounds.center.z
                );
                surface.transform.localScale = new Vector3(
                    bounds.size.x / 10f,
                    1f,
                    bounds.size.z / 10f
                );
                MeshRenderer surfaceRenderer =
                    surface.GetComponent<MeshRenderer>();
                surfaceRenderer.sharedMaterial = paving;
                surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
                surfaceRenderer.receiveShadows = true;
                MaterialPropertyBlock block = new();
                block.SetVector(
                    "_BaseMap_ST",
                    new Vector4(
                        bounds.size.x / 4f,
                        bounds.size.z / 4f,
                        0f,
                        0f
                    )
                );
                surfaceRenderer.SetPropertyBlock(block);
                UnityEngine.Object.DestroyImmediate(
                    surface.GetComponent<MeshCollider>()
                );

                CreateCurbVisual(
                    greyboxRoot,
                    groupName,
                    "Left",
                    bounds.center.x - bounds.extents.x + 0.22f,
                    bounds,
                    curb
                );
                CreateCurbVisual(
                    greyboxRoot,
                    groupName,
                    "Right",
                    bounds.center.x + bounds.extents.x - 0.22f,
                    bounds,
                    curb
                );
            }
        }

        private static void CreateCurbVisual(
            Transform parent,
            string groupName,
            string side,
            float x,
            Bounds streetBounds,
            Material material)
        {
            GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = $"{groupName}_QuaterniusCurb{side}";
            curb.transform.SetParent(parent, true);
            curb.transform.position = new Vector3(
                x,
                0.1f,
                streetBounds.center.z
            );
            curb.transform.localScale = new Vector3(
                0.38f,
                0.2f,
                streetBounds.size.z
            );
            MeshRenderer renderer = curb.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(curb.GetComponent<BoxCollider>());
        }

        private static void ApplyWallRuns(Transform greyboxRoot)
        {
            string[] wallNames =
            {
                "Courtyard_NorthWall",
                "Courtyard_SouthWall",
                "Courtyard_EndWall",
                "Warehouse_SouthWall",
                "Warehouse_EastWall",
                "Warehouse_NorthWall",
                "Warehouse_WestWall_South",
                "Warehouse_WestWall_North",
                "EndWall",
                "Dongguan_EndWall",
                "Caiyi_EndWall"
            };
            Transform[] transforms =
                greyboxRoot.GetComponentsInChildren<Transform>(true);
            foreach (string wallName in wallNames)
            {
                Transform target = transforms.FirstOrDefault(
                    item => item.name == wallName
                );
                MeshRenderer targetRenderer =
                    target != null ? target.GetComponent<MeshRenderer>() : null;
                if (targetRenderer == null ||
                    target.parent.Find(wallName + "_QuaterniusRun") != null)
                {
                    continue;
                }

                Bounds desired = targetRenderer.bounds;
                bool runsAlongZ = desired.size.z > desired.size.x;
                float length = runsAlongZ ? desired.size.z : desired.size.x;
                float height = desired.size.y;
                int segmentCount = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
                float segmentWidth = length / segmentCount;
                string moduleName = wallName.StartsWith(
                    "Warehouse_",
                    StringComparison.Ordinal
                )
                    ? "Wall_UnevenBrick_Straight"
                    : "Wall_Plaster_Straight";

                GameObject assembly = new($"{wallName}_Assembly");
                activeAssembly = assembly;
                try
                {
                    assembly.transform.position = desired.center;
                    assembly.transform.rotation =
                        target.rotation *
                        (runsAlongZ
                            ? Quaternion.Euler(0f, 90f, 0f)
                            : Quaternion.identity);
                    Vector3 segmentScale = new(
                        segmentWidth / 2f,
                        height / 3.12f,
                        1f
                    );
                    for (int index = 0; index < segmentCount; index++)
                    {
                        float x = -length * 0.5f +
                            segmentWidth * (index + 0.5f);
                        AddModule(
                            moduleName,
                            new Vector3(x, -height * 0.5f, 0f),
                            Quaternion.identity,
                            segmentScale
                        );
                        AddModule(
                            "Wall_BottomCover",
                            new Vector3(x, -height * 0.5f + 0.08f, 0f),
                            Quaternion.identity,
                            new Vector3(segmentWidth / 2f, 1f, 1f)
                        );
                    }

                    Mesh mesh = CombineAssembly(
                        assembly.transform,
                        $"{MeshRoot}/SM_Quaternius_WallRun_{wallName}.asset"
                    );
                    GameObject visual = new($"{wallName}_QuaterniusRun");
                    visual.transform.SetParent(target.parent, false);
                    visual.transform.position = assembly.transform.position;
                    visual.transform.rotation = assembly.transform.rotation;
                    visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer =
                        visual.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = GetCombinedMaterials(
                        assembly.transform
                    );
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    visual.isStatic = true;
                    targetRenderer.enabled = false;
                }
                finally
                {
                    activeAssembly = null;
                    UnityEngine.Object.DestroyImmediate(assembly);
                }
            }
        }

        private static void EnsureFillLight(Transform greyboxRoot)
        {
            if (greyboxRoot.Find("QuaterniusFillLight") != null)
            {
                return;
            }

            GameObject fillObject = new("QuaterniusFillLight");
            fillObject.transform.SetParent(greyboxRoot, false);
            fillObject.transform.rotation = Quaternion.Euler(28f, 142f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.58f, 0.72f, 1f);
            fill.intensity = 0.48f;
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForcePixel;
        }

        private static void BuildAllInternal()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(MeshRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextures();
            BuildMaterialLibrary();

            CreateHousePrefab(
                HouseNames[0],
                _ => BuildPlasterHouse()
            );
            CreateHousePrefab(
                HouseNames[1],
                _ => BuildBrickHouse()
            );
            CreateHousePrefab(
                HouseNames[2],
                _ => BuildTimberHouse()
            );
            CreateModulePrefab(
                "ART_Quaternius_Prop_Crate",
                "Prop_Crate"
            );
            CreateModulePrefab(
                "ART_Quaternius_RoofAwning",
                "Roof_Wooden_2x1"
            );
            CreateModulePrefab(
                "ART_Quaternius_GateArch",
                "Wall_Arch"
            );
            CreateModulePrefab(
                "ART_Quaternius_WoodSign",
                "Door_1_Flat"
            );
            CreateModulePrefab(
                "ART_Quaternius_Frame",
                "DoorFrame_Flat_Brick"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Quaternius environment kit rebuilt: {HouseNames.Length} " +
                "offline-batched ready-made building prefabs."
            );
        }

        private static void ConfigureTextures()
        {
            string[] textures = Directory.GetFiles(
                Path.GetFullPath(TextureRoot),
                "*.png"
            );
            foreach (string texturePath in textures)
            {
                string assetPath = ToAssetPath(texturePath);
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(texturePath);
                importer.textureType = name.Contains(
                    "Normal",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = !name.Contains(
                    "Normal",
                    StringComparison.OrdinalIgnoreCase
                ) && !name.Contains(
                    "Roughness",
                    StringComparison.OrdinalIgnoreCase
                ) && !name.Contains(
                    "ORM",
                    StringComparison.OrdinalIgnoreCase
                );
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static void BuildMaterialLibrary()
        {
            MaterialLibrary.Clear();
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Lit shader was not found."
                );
            }

            foreach (string modelPath in Directory.GetFiles(
                         Path.GetFullPath(ModelRoot),
                         "*.gltf"
                     ))
            {
                string assetPath = ToAssetPath(modelPath);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    assetPath
                );
                if (model == null)
                {
                    throw new InvalidOperationException(
                        $"Quaternius model was not imported: {assetPath}"
                    );
                }

                foreach (Renderer renderer in
                         model.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material source in renderer.sharedMaterials)
                    {
                        if (source == null)
                        {
                            continue;
                        }
                        string key = MaterialKey(source);
                        if (MaterialLibrary.ContainsKey(key))
                        {
                            continue;
                        }

                        string materialPath = $"{MaterialRoot}/MAT_Quaternius_{key}.mat";
                        Material material =
                            AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        Material template = CreateNormalizedMaterial(key, lit);
                        if (material == null)
                        {
                            material = template;
                            AssetDatabase.CreateAsset(material, materialPath);
                        }
                        else
                        {
                            EditorUtility.CopySerialized(template, material);
                            UnityEngine.Object.DestroyImmediate(template);
                        }
                        EditorUtility.SetDirty(material);
                        MaterialLibrary.Add(key, material);
                    }
                }
            }

            if (MaterialLibrary.Count < 5)
            {
                throw new InvalidOperationException(
                    "Quaternius material import did not expose the expected " +
                    $"shared texture set. Found {MaterialLibrary.Count} materials."
                );
            }
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Quaternius material library ready: {MaterialLibrary.Count} " +
                "normalized URP materials."
            );
        }

        private static Material CreateNormalizedMaterial(string key, Shader lit)
        {
            Material material = new(lit)
            {
                name = $"MAT_Quaternius_{key}",
                enableInstancing = true,
                doubleSidedGI = true
            };
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.28f);
            material.SetFloat("_Glossiness", 0.28f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);

            string texturePrefix = key switch
            {
                "Vine" => "T_VineLeaf_png",
                "WoodTrimWear" => "T_WoodTrim",
                _ => $"T_{key}"
            };
            string baseTextureName = texturePrefix.EndsWith(
                "_png",
                StringComparison.Ordinal
            )
                ? $"{texturePrefix}.png"
                : $"{texturePrefix}_BaseColor.png";
            Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{TextureRoot}/{baseTextureName}"
            );
            if (baseTexture != null)
            {
                material.SetTexture("_BaseMap", baseTexture);
                material.SetTexture("_MainTex", baseTexture);
            }

            Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{TextureRoot}/{texturePrefix}_Normal.png"
            );
            if (normalTexture != null)
            {
                material.SetTexture("_BumpMap", normalTexture);
                material.EnableKeyword("_NORMALMAP");
            }

            if (key == "WindowGlass")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(0.11f, 0.20f, 0.22f, 1f)
                );
                material.SetFloat("_Metallic", 0.08f);
                material.SetFloat("_Smoothness", 0.62f);
            }
            else if (key == "MetalOrnaments")
            {
                material.SetFloat("_Metallic", 0.55f);
                material.SetFloat("_Smoothness", 0.48f);
            }
            else if (key == "Plaster")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(1f, 0.88f, 0.72f, 1f)
                );
                material.SetFloat("_Smoothness", 0.18f);
            }
            else if (key == "Brick")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(0.92f, 0.77f, 0.66f, 1f)
                );
            }
            else if (key == "UnevenBrick")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(0.91f, 0.87f, 0.80f, 1f)
                );
            }
            else if (key == "RoundTiles")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(0.97f, 0.79f, 0.64f, 1f)
                );
                material.SetFloat("_Smoothness", 0.22f);
            }
            else if (key == "Vine")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(0.08f, 0.18f, 0.045f, 1f)
                );
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.34f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = 2450;
            }

            return material;
        }

        private static void CreateHousePrefab(
            string prefabName,
            Action<Transform> buildShell)
        {
            GameObject assembly = new($"{prefabName}_Assembly");
            activeAssembly = assembly;
            try
            {
                buildShell(assembly.transform);
                Mesh mesh = CombineAssembly(
                    assembly.transform,
                    $"{MeshRoot}/SM_{prefabName}.asset"
                );
                Material[] materials = mesh.subMeshCount > 0
                    ? GetCombinedMaterials(assembly.transform)
                    : Array.Empty<Material>();
                if (materials.Length != mesh.subMeshCount)
                {
                    throw new InvalidOperationException(
                        $"Combined material mismatch for {prefabName}."
                    );
                }

                GameObject prefabRoot = new(prefabName);
                try
                {
                    GameObject visuals = new("Visuals");
                    visuals.transform.SetParent(prefabRoot.transform, false);
                    GameObject shell = new("Shell");
                    shell.transform.SetParent(visuals.transform, false);
                    MeshFilter filter = shell.AddComponent<MeshFilter>();
                    MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                    renderer.reflectionProbeUsage =
                        ReflectionProbeUsage.BlendProbes;

                    new GameObject("Colliders").transform.SetParent(
                        prefabRoot.transform,
                        false
                    );
                    new GameObject("Gameplay").transform.SetParent(
                        prefabRoot.transform,
                        false
                    );
                    new GameObject("Metadata").transform.SetParent(
                        prefabRoot.transform,
                        false
                    );

                    string prefabPath = $"{PrefabRoot}/{prefabName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log(
                        $"Quaternius prefab ready: {prefabPath}, " +
                        $"{mesh.vertexCount:N0} vertices, " +
                        $"{mesh.subMeshCount} batched material groups."
                    );
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
            finally
            {
                activeAssembly = null;
                UnityEngine.Object.DestroyImmediate(assembly);
            }
        }

        private static void CreateModulePrefab(
            string prefabName,
            string moduleName)
        {
            GameObject assembly = new($"{prefabName}_Assembly");
            activeAssembly = assembly;
            try
            {
                AddModule(moduleName, Vector3.zero);
                Mesh mesh = CombineAssembly(
                    assembly.transform,
                    $"{MeshRoot}/SM_{prefabName}.asset"
                );
                GameObject prefabRoot = new(prefabName);
                try
                {
                    GameObject shell = new("Shell");
                    shell.transform.SetParent(prefabRoot.transform, false);
                    MeshFilter filter = shell.AddComponent<MeshFilter>();
                    MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterials = GetCombinedMaterials(
                        assembly.transform
                    );
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    PrefabUtility.SaveAsPrefabAsset(
                        prefabRoot,
                        $"{PrefabRoot}/{prefabName}.prefab"
                    );
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
            finally
            {
                activeAssembly = null;
                UnityEngine.Object.DestroyImmediate(assembly);
            }
        }

        private static void BuildPlasterHouse()
        {
            BuildWallBox(
                "Wall_Plaster_Door_Flat",
                "Wall_Plaster_Window_Wide_Flat",
                "Wall_Plaster_Straight",
                "Wall_Plaster_WoodGrid"
            );
            AddModule("DoorFrame_Flat_Brick", new Vector3(0f, 0f, -4.30f));
            AddModule("Door_1_Flat", new Vector3(0f, 0f, -4.42f));
            AddFrontWindow(-2f, -4.38f, true);
            AddFrontWindow(2f, -4.38f, true);
            AddCornerSet("Corner_ExteriorWide_Brick");
            AddRoof("Roof_RoundTiles_4x6", 0f);
            AddModule(
                "Prop_Chimney",
                new Vector3(1.45f, 3.06f, 1.52f),
                Quaternion.Euler(0f, 18f, 0f)
            );
        }

        private static void BuildBrickHouse()
        {
            BuildWallBox(
                "Wall_UnevenBrick_Door_Flat",
                "Wall_UnevenBrick_Window_Wide_Flat",
                "Wall_UnevenBrick_Straight",
                "Wall_UnevenBrick_Straight"
            );
            AddModule("DoorFrame_Flat_Brick", new Vector3(0f, 0f, -4.30f));
            AddModule("Door_2_Flat", new Vector3(0f, 0f, -4.42f));
            AddFrontWindow(-2f, -4.38f, true);
            AddFrontWindow(2f, -4.38f, true);
            AddCornerSet("Corner_ExteriorWide_Wood");
            AddRoof("Roof_RoundTiles_4x6", 0f);
            AddModule(
                "Roof_Dormer_RoundTile",
                new Vector3(0f, 4.70f, -1.72f)
            );
            AddModule(
                "Prop_Chimney",
                new Vector3(-1.42f, 3.02f, 1.35f),
                Quaternion.Euler(0f, -12f, 0f)
            );
        }

        private static void BuildTimberHouse()
        {
            BuildWallBox(
                "Wall_Plaster_Door_Flat",
                "Wall_Plaster_Window_Wide_Round",
                "Wall_Plaster_Straight_Base",
                "Wall_Plaster_WoodGrid"
            );
            AddModule("DoorFrame_Flat_Brick", new Vector3(0f, 0f, -4.30f));
            AddModule("Door_1_Flat", new Vector3(0f, 0f, -4.42f));
            AddModule(
                "Overhang_Plaster_Short",
                new Vector3(-1.02f, 0f, -4.00f)
            );
            AddFrontWindow(-2f, -4.38f, false);
            AddFrontWindow(2f, -4.38f, true);
            AddModule(
                "Window_Wide_Flat1",
                new Vector3(-1.02f, 0.10f, -5.22f)
            );
            AddModule(
                "WindowShutters_Wide_Flat_Open",
                new Vector3(-1.02f, 0.10f, -5.36f)
            );
            AddCornerSet("Corner_ExteriorWide_Brick");
            AddRoof("Roof_RoundTiles_4x6", 0f);
            AddModule(
                "Prop_Chimney",
                new Vector3(1.25f, 3.02f, 1.48f),
                Quaternion.Euler(0f, 11f, 0f)
            );
        }

        private static void BuildWallBox(
            string frontDoor,
            string frontWindow,
            string sideWall,
            string sideAccent)
        {
            AddModule(frontDoor, new Vector3(0f, 0f, -4f));
            AddModule(frontWindow, new Vector3(-2f, 0f, -4f));
            AddModule(frontWindow, new Vector3(2f, 0f, -4f));

            for (int index = 0; index < 4; index++)
            {
                float z = -3f + index * 2f;
                AddModule(
                    index == 1 ? sideAccent : sideWall,
                    new Vector3(-3f, 0f, z),
                    Quaternion.Euler(0f, -90f, 0f)
                );
                AddModule(
                    index == 2 ? sideAccent : sideWall,
                    new Vector3(3f, 0f, z),
                    Quaternion.Euler(0f, 90f, 0f)
                );
            }

            AddModule(
                sideWall,
                new Vector3(-2f, 0f, 4f),
                Quaternion.Euler(0f, 180f, 0f)
            );
            AddModule(
                sideWall,
                new Vector3(0f, 0f, 4f),
                Quaternion.Euler(0f, 180f, 0f)
            );
            AddModule(
                sideWall,
                new Vector3(2f, 0f, 4f),
                Quaternion.Euler(0f, 180f, 0f)
            );

            AddModule("Wall_BottomCover", new Vector3(-2f, 0.08f, -4.13f));
            AddModule("Wall_BottomCover", new Vector3(2f, 0.08f, -4.13f));
            AddModule(
                "Wall_BottomCover",
                new Vector3(-3.13f, 0.08f, 0f),
                Quaternion.Euler(0f, -90f, 0f)
            );
            AddModule(
                "Wall_BottomCover",
                new Vector3(3.13f, 0.08f, 0f),
                Quaternion.Euler(0f, 90f, 0f)
            );
        }

        private static void AddFrontWindow(
            float x,
            float z,
            bool withShutters)
        {
            AddModule(
                "Window_Wide_Flat1",
                new Vector3(x, 0.14f, z)
            );
            if (withShutters)
            {
                AddModule(
                    "WindowShutters_Wide_Flat_Open",
                    new Vector3(x, 0.14f, z - 0.14f)
                );
            }
        }

        private static void AddCornerSet(string module)
        {
            AddModule(
                module,
                new Vector3(-2.83f, 0f, -3.83f),
                Quaternion.Euler(0f, 0f, 0f)
            );
            AddModule(
                module,
                new Vector3(2.83f, 0f, -3.83f),
                Quaternion.Euler(0f, 90f, 0f)
            );
            AddModule(
                module,
                new Vector3(-2.83f, 0f, 3.83f),
                Quaternion.Euler(0f, -90f, 0f)
            );
            AddModule(
                module,
                new Vector3(2.83f, 0f, 3.83f),
                Quaternion.Euler(0f, 180f, 0f)
            );
        }

        private static void AddRoof(string roof, float yaw)
        {
            AddModule(
                roof,
                new Vector3(0f, 3.43f, 0f),
                Quaternion.Euler(0f, yaw, 0f)
            );
        }

        private static void AddModule(
            string moduleName,
            Vector3 localPosition,
            Quaternion? localRotation = null,
            Vector3? localScale = null)
        {
            if (activeAssembly == null)
            {
                throw new InvalidOperationException(
                    "Quaternius assembly root is unavailable."
                );
            }
            AddModule(
                activeAssembly.transform,
                moduleName,
                localPosition,
                localRotation ?? Quaternion.identity,
                localScale ?? Vector3.one
            );
        }

        private static GameObject activeAssembly;

        private static void AddModule(
            Transform parent,
            string moduleName,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            string assetPath = $"{ModelRoot}/{moduleName}.gltf";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                assetPath
            );
            if (model == null)
            {
                throw new InvalidOperationException(
                    $"Missing Quaternius module: {assetPath}"
                );
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                model,
                parent
            );
            instance.name = moduleName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
        }

        private static Mesh CombineAssembly(Transform root, string meshPath)
        {
            Dictionary<Material, List<CombineInstance>> batches = new();
            foreach (MeshFilter filter in
                     root.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                Mesh sourceMesh = filter.sharedMesh;
                if (renderer == null ||
                    sourceMesh == null ||
                    !renderer.enabled)
                {
                    continue;
                }
                if (!sourceMesh.isReadable)
                {
                    throw new InvalidOperationException(
                        $"Quaternius mesh is not CPU-readable: {filter.name}"
                    );
                }

                Matrix4x4 matrix =
                    root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                int subMeshCount = sourceMesh.subMeshCount;
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    Material material = renderer.sharedMaterials[
                        Mathf.Min(subMesh, renderer.sharedMaterials.Length - 1)
                    ];
                    if (material == null)
                    {
                        continue;
                    }
                    Material normalized = NormalizeMaterial(material);
                    if (!batches.TryGetValue(normalized, out var list))
                    {
                        list = new List<CombineInstance>();
                        batches.Add(normalized, list);
                    }
                    list.Add(new CombineInstance
                    {
                        mesh = sourceMesh,
                        subMeshIndex = subMesh,
                        transform = matrix
                    });
                }
            }

            if (batches.Count == 0)
            {
                throw new InvalidOperationException(
                    "Quaternius assembly produced no renderable geometry."
                );
            }

            List<CombineInstance> materialMeshes = new();
            List<Mesh> temporaryMeshes = new();
            try
            {
                foreach ((Material material, List<CombineInstance> batch) in
                         batches.OrderBy(pair => pair.Key.name))
                {
                    Mesh layer = new()
                    {
                        name = $"{material.name}_Batch",
                        indexFormat = IndexFormat.UInt32
                    };
                    layer.CombineMeshes(
                        batch.ToArray(),
                        mergeSubMeshes: true,
                        useMatrices: true,
                        hasLightmapData: false
                    );
                    layer.UploadMeshData(markNoLongerReadable: false);
                    temporaryMeshes.Add(layer);
                    materialMeshes.Add(new CombineInstance
                    {
                        mesh = layer,
                        subMeshIndex = 0,
                        transform = Matrix4x4.identity
                    });
                }

                Mesh combined = new()
                {
                    name = Path.GetFileNameWithoutExtension(meshPath),
                    indexFormat = IndexFormat.UInt32
                };
                combined.CombineMeshes(
                    materialMeshes.ToArray(),
                    mergeSubMeshes: false,
                    useMatrices: true,
                    hasLightmapData: false
                );
                combined.RecalculateBounds();

                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(combined, existing);
                    combined = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(combined, meshPath);
                }
                EditorUtility.SetDirty(combined);
                return combined;
            }
            finally
            {
                foreach (Mesh temporary in temporaryMeshes)
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }
        }

        private static Material[] GetCombinedMaterials(Transform root)
        {
            return root
                .GetComponentsInChildren<MeshRenderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Select(NormalizeMaterial)
                .Distinct()
                .OrderBy(material => material.name)
                .ToArray();
        }

        private static Material NormalizeMaterial(Material source)
        {
            string key = MaterialKey(source);
            if (!MaterialLibrary.TryGetValue(key, out Material material))
            {
                throw new InvalidOperationException(
                    $"Quaternius material was not normalized: {key}"
                );
            }
            return material;
        }

        private static string MaterialKey(Material material)
        {
            Texture texture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture != null)
            {
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    string fileName = Path.GetFileNameWithoutExtension(texturePath);
                    const string suffix = "_BaseColor";
                    if (fileName.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        fileName = fileName.Substring(
                            0,
                            fileName.Length - suffix.Length
                        );
                    }
                    return Sanitize(fileName.Replace("T_", string.Empty));
                }
            }

            return Sanitize(material.name.Replace("MI_", string.Empty));
        }

        private static string Sanitize(string value)
        {
            return new string(
                value
                    .Where(character => char.IsLetterOrDigit(character))
                    .ToArray()
            );
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            int assetsIndex = normalized.IndexOf(
                "/Assets/",
                StringComparison.OrdinalIgnoreCase
            );
            if (assetsIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Asset path is outside the Unity project: {absolutePath}"
                );
            }
            return normalized.Substring(assetsIndex + 1);
        }

        private static void CreatePilotScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.73f, 0.79f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.44f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 32f;
            RenderSettings.fogEndDistance = 92f;
            RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.69f);

            CreatePilotGround();
            CreatePilotBuildings();

            GameObject streetProps = new("Street Props");
            AddWorldModule(
                streetProps.transform,
                "Prop_Wagon",
                new Vector3(3.9f, 0f, -8.8f),
                Quaternion.Euler(0f, 90f, 0f)
            );
            AddWorldModule(
                streetProps.transform,
                "Prop_Crate",
                new Vector3(-4.55f, 0f, -2.3f),
                Quaternion.Euler(0f, 18f, 0f)
            );
            AddWorldModule(
                streetProps.transform,
                "Prop_Crate",
                new Vector3(-4.22f, 0f, -1.12f),
                Quaternion.Euler(0f, -14f, 0f)
            );
            AddWorldModule(
                streetProps.transform,
                "Prop_WoodenFence_Single",
                new Vector3(4.9f, 0f, 8.8f),
                Quaternion.Euler(0f, 90f, 0f)
            );

            AddCharacter(
                "VIS_Stylized_Hero",
                new Vector3(-0.55f, 0f, -6.7f),
                Quaternion.Euler(0f, 12f, 0f)
            );
            AddCharacter(
                "VIS_Stylized_Guide",
                new Vector3(1.35f, 0f, -5.35f),
                Quaternion.Euler(0f, -18f, 0f)
            );
            AddCharacter(
                "VIS_Stylized_NPC_A",
                new Vector3(-1.8f, 0f, 4.8f),
                Quaternion.Euler(0f, 155f, 0f)
            );

            CreatePilotCamera();
            CreatePilotLighting();
            CreatePilotAtmosphere();

            GameObject runtime = new("Runtime Diagnostics");
            runtime.AddComponent<RuntimeDiagnostics>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PilotScenePath);
        }

        private static void CreatePilotGround()
        {
            Material paving = CreatePavingMaterial(
                "MAT_Quaternius_Paving",
                "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_Cobblestone05.mat",
                8f,
                12f
            );
            Material curb = CreatePavingMaterial(
                "MAT_Quaternius_Curb",
                "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_GreyPlaster02.mat",
                3f,
                2f
            );

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Cobblestone Street";
            ground.transform.position = new Vector3(0f, -0.12f, 5f);
            ground.transform.localScale = new Vector3(75f, 0.24f, 62f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = paving;
            ground.isStatic = true;

            GameObject leftCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftCurb.name = "Left Curb";
            leftCurb.transform.position = new Vector3(-6.12f, 0.05f, 5f);
            leftCurb.transform.localScale = new Vector3(0.32f, 0.18f, 58f);
            leftCurb.GetComponent<MeshRenderer>().sharedMaterial = curb;
            leftCurb.isStatic = true;

            GameObject rightCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightCurb.name = "Right Curb";
            rightCurb.transform.position = new Vector3(6.12f, 0.05f, 5f);
            rightCurb.transform.localScale = new Vector3(0.32f, 0.18f, 58f);
            rightCurb.GetComponent<MeshRenderer>().sharedMaterial = curb;
            rightCurb.isStatic = true;
        }

        private static Material CreatePavingMaterial(
            string name,
            string sourcePath,
            float tileX,
            float tileY)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Paving source material is missing: {sourcePath}"
                );
            }

            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, material);
            }
            material.name = name;
            material.enableInstancing = true;
            material.SetTextureScale("_BaseMap", new Vector2(tileX, tileY));
            material.SetTextureScale("_MainTex", new Vector2(tileX, tileY));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePilotBuildings()
        {
            float[] zPositions = { -16f, -6f, 4f, 14f };
            for (int index = 0; index < zPositions.Length; index++)
            {
                AddHousePrefab(
                    HouseNames[index % HouseNames.Length],
                    new Vector3(-10f, 0f, zPositions[index]),
                    -90f,
                    $"LeftHouse_{index + 1}"
                );
                if (index != 2)
                {
                    AddHousePrefab(
                        HouseNames[(index + 1) % HouseNames.Length],
                        new Vector3(10f, 0f, zPositions[index]),
                        90f,
                        $"RightHouse_{index + 1}"
                    );
                }
            }
        }

        private static void AddHousePrefab(
            string prefabName,
            Vector3 position,
            float yaw,
            string instanceName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabRoot}/{prefabName}.prefab"
            );
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Quaternius house prefab is missing: {prefabName}"
                );
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab
            );
            instance.name = instanceName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (Transform child in
                     instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.isStatic = true;
            }
        }

        private static void AddWorldModule(
            Transform parent,
            string moduleName,
            Vector3 position,
            Quaternion rotation)
        {
            AddModule(
                parent,
                moduleName,
                position,
                rotation,
                Vector3.one
            );
            foreach (MeshRenderer renderer in
                     parent.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] materials = renderer.sharedMaterials
                    .Select(NormalizeMaterial)
                    .ToArray();
                renderer.sharedMaterials = materials;
            }
        }

        private static void AddCharacter(
            string prefabName,
            Vector3 position,
            Quaternion rotation)
        {
            string path =
                $"Assets/Demo02/Art/Characters/Stylized/Prefabs/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Stylized showcase character is missing: {path}"
                );
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab
            );
            instance.name = prefabName;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }

        private static void CreatePilotCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 53f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 160f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.67f, 0.75f, 0.78f);
            cameraObject.transform.position = new Vector3(0f, 5.3f, -24f);
            cameraObject.transform.LookAt(new Vector3(0f, 2.45f, 5.6f));
        }

        private static void CreatePilotLighting()
        {
            GameObject sunObject = new("Sun");
            sunObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.86f, 0.68f);
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;

            GameObject fillObject = new("Sky Fill");
            fillObject.transform.rotation = Quaternion.Euler(27f, 142f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.62f, 0.75f, 1f);
            fill.intensity = 0.55f;
            fill.shadows = LightShadows.None;
        }

        private static void CreatePilotAtmosphere()
        {
            GameObject volumeObject = new("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "QuaterniusPilotProfile";
            string profilePath = $"{ArtRoot}/QuaterniusPilotVolume.asset";
            VolumeProfile existing =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else
            {
                profile = existing;
            }

            Bloom bloom = profile.Has<Bloom>()
                ? profile.components.First(component => component is Bloom) as Bloom
                : profile.Add<Bloom>();
            bloom.intensity.value = 0.16f;
            bloom.threshold.value = 1.15f;
            bloom.scatter.value = 0.56f;

            ColorAdjustments color = profile.Has<ColorAdjustments>()
                ? profile.components
                    .First(component => component is ColorAdjustments)
                    as ColorAdjustments
                : profile.Add<ColorAdjustments>();
            color.postExposure.value = 0.08f;
            color.contrast.value = 8f;
            color.saturation.value = 4f;

            volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
        }

        private static void ConfigurePlayerForPilot()
        {
            PlayerSettings.companyName = "gan-yv";
            PlayerSettings.productName = "Quaternius Village Pilot";
            PlayerSettings.bundleVersion = "1.22.0-pilot";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
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

        private static bool IsGreyboxBuilding(Transform item)
        {
            if (item.GetComponent<BoxCollider>() == null)
            {
                return false;
            }

            string name = item.name;
            int separatorCount = name.Count(character => character == '_');
            if ((name.StartsWith(
                     "LeftBuilding_",
                     StringComparison.Ordinal
                 ) ||
                 name.StartsWith(
                     "RightBuilding_",
                     StringComparison.Ordinal
                 )) &&
                separatorCount == 1)
            {
                return true;
            }

            return (name.StartsWith(
                        "Dongguan_LeftShop_",
                        StringComparison.Ordinal
                    ) ||
                    name.StartsWith(
                        "Dongguan_RightShop_",
                        StringComparison.Ordinal
                    ) ||
                    name.StartsWith(
                        "Caiyi_LeftShop_",
                        StringComparison.Ordinal
                    ) ||
                    name.StartsWith(
                        "Caiyi_RightShop_",
                        StringComparison.Ordinal
                    )) &&
                separatorCount == 2;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }
    }
}
