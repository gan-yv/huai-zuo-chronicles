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
    public static class GuanglingArtPilotBuilder
    {
        private const string ScenePath =
            "Assets/Demo02/Scenes/Tests/GuanglingArtPilot.unity";
        private const string MaterialRoot =
            "Assets/Demo02/Art/Materials/GuanglingPilot";
        private const string MeshRoot =
            "Assets/Demo02/Art/Environment/Guangling/Meshes";
        private const string ScreenshotRelativePath =
            "../../docs/screenshots/guangling-art-pilot.png";
        private const float StreetLength = 52f;

        [MenuItem("Demo02/Art/Build Guangling Art Pilot")]
        public static void BuildPilot()
        {
            ThirdPartyArtSetup.Setup();
            GuanglingCharacterArtBuilder.BuildCharacters();
            EnsureFolder(MaterialRoot);
            EnsureFolder(MeshRoot);

            PilotMaterials materials = CreateMaterials();
            Mesh gableMesh = CreateGableEndMesh();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            ConfigureAtmosphere();

            GameObject root = new("Guangling_ArtPilot");
            CreateStreet(root.transform, materials);
            CreateStreetBuildings(root.transform, materials, gableMesh);
            CreateStreetProps(root.transform, materials);
            CreateCharacterInstances(root.transform);
            CreateBackground(root.transform, materials);
            CreateLighting();
            CreateCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Guangling art pilot generated at {ScenePath}. " +
                "The scene is a standalone style test."
            );
        }

        [MenuItem("Demo02/Art/Capture Guangling Art Pilot")]
        public static void CapturePilot()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Guangling art pilot has no Main Camera."
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
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
            {
                throw new InvalidOperationException(
                    "The active render pipeline cannot render the pilot camera."
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
                    ScreenshotRelativePath
                )
            );
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Guangling art pilot captured at {screenshotPath}");
        }

        public static void ValidatePilotForBatch()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Missing Guangling pilot scene: {ScenePath}"
                );
            }

            string[] materialNames =
            {
                "MAT_Guangling_Pilot_Plaster",
                "MAT_Guangling_Pilot_PlasterAlt",
                "MAT_Guangling_Pilot_Wood",
                "MAT_Guangling_Pilot_Roof",
                "MAT_Guangling_Pilot_Stone",
                "MAT_Guangling_Pilot_Trim",
                "MAT_Guangling_Pilot_Accent",
                "MAT_Guangling_Pilot_Lantern",
                "MAT_Guangling_Pilot_Glass"
            };
            foreach (string materialName in materialNames)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    $"{MaterialRoot}/{materialName}.mat"
                );
                if (material == null ||
                    material.shader == null ||
                    material.shader.name != "Universal Render Pipeline/Lit")
                {
                    throw new InvalidOperationException(
                        $"Pilot material is missing or invalid: {materialName}"
                    );
                }
            }

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive
            );
            try
            {
                GameObject root = FindRoot(scene, "Guangling_ArtPilot");
                if (root == null)
                {
                    throw new InvalidOperationException(
                        "Guangling art pilot root is missing."
                    );
                }
                if (root.transform.Find("Buildings") == null ||
                    root.transform.Find("Street") == null ||
                    root.transform.Find("Props") == null ||
                    root.transform.Find("Characters") == null ||
                    root.transform.Find("Backdrop") == null)
                {
                    throw new InvalidOperationException(
                        "Guangling art pilot is missing a required content group."
                    );
                }

                int buildingCount = 0;
                foreach (Transform child in root.transform.Find("Buildings"))
                {
                    if (child.name.StartsWith("Building_", StringComparison.Ordinal))
                    {
                        buildingCount++;
                    }
                }
                if (buildingCount < 10)
                {
                    throw new InvalidOperationException(
                        $"Guangling art pilot expected at least 10 buildings, found {buildingCount}."
                    );
                }

                int characterCount =
                    root.transform.Find("Characters").childCount;
                if (characterCount < 3)
                {
                    throw new InvalidOperationException(
                        $"Guangling art pilot expected at least 3 characters, found {characterCount}."
                    );
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log(
                "Guangling art pilot validation passed: street, roofs, " +
                "materials, props, backdrop, and camera."
            );
        }

        private static PilotMaterials CreateMaterials()
        {
            PilotMaterials result = new()
            {
                Plaster = CopyMaterial(
                    "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_GreyPlaster02.mat",
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Plaster.mat",
                    new Color(0.94f, 0.90f, 0.82f),
                    new Vector2(2f, 2f)
                ),
                PlasterAlt = CopyMaterial(
                    "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_GreyPlaster02.mat",
                    $"{MaterialRoot}/MAT_Guangling_Pilot_PlasterAlt.mat",
                    new Color(0.74f, 0.70f, 0.62f),
                    new Vector2(2f, 2f)
                ),
                Wood = CopyMaterial(
                    "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_OldPlanks02.mat",
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Wood.mat",
                    new Color(0.46f, 0.31f, 0.20f),
                    new Vector2(3f, 1f)
                ),
                Roof = CopyMaterial(
                    "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_GreyRoofTiles02.mat",
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Roof.mat",
                    new Color(0.76f, 0.79f, 0.80f),
                    new Vector2(4f, 2f)
                ),
                Stone = CopyMaterial(
                    "Assets/Demo02/Art/Materials/ThirdPartySamples/MAT_PolyHaven_Cobblestone05.mat",
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Stone.mat",
                    new Color(0.66f, 0.64f, 0.59f),
                    new Vector2(4f, 16f)
                ),
                Trim = CreateColorMaterial(
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Trim.mat",
                    new Color(0.12f, 0.09f, 0.07f),
                    0f,
                    0.12f
                ),
                Accent = CreateColorMaterial(
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Accent.mat",
                    new Color(0.42f, 0.09f, 0.06f),
                    0f,
                    0.22f
                ),
                Lantern = CreateEmissiveMaterial(
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Lantern.mat"
                ),
                Glass = CreateColorMaterial(
                    $"{MaterialRoot}/MAT_Guangling_Pilot_Glass.mat",
                    new Color(0.045f, 0.075f, 0.08f),
                    0.05f,
                    0.72f
                )
            };

            return result;
        }

        private static Material CopyMaterial(
            string sourcePath,
            string targetPath,
            Color tint,
            Vector2 tiling
        )
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Source material was not found: {sourcePath}"
                );
            }

            Material target = LoadOrCreateMaterial(targetPath);
            target.CopyPropertiesFromMaterial(source);
            target.name = Path.GetFileNameWithoutExtension(targetPath);
            target.SetColor("_BaseColor", tint);
            target.SetTextureScale("_BaseMap", tiling);
            target.SetTextureScale("_BumpMap", tiling);
            target.SetTextureScale("_MetallicGlossMap", tiling);
            target.SetTextureScale("_OcclusionMap", tiling);
            target.SetFloat("_Smoothness", 0.28f);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static Material CreateColorMaterial(
            string path,
            Color color,
            float metallic,
            float smoothness
        )
        {
            Material material = LoadOrCreateMaterial(path);
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_BumpMap", null);
            material.SetTexture("_MetallicGlossMap", null);
            material.SetTexture("_OcclusionMap", null);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateEmissiveMaterial(string path)
        {
            Material material = CreateColorMaterial(
                path,
                new Color(0.72f, 0.12f, 0.045f),
                0f,
                0.34f
            );
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1.5f, 0.22f, 0.06f));
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.48f, 0.50f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.42f, 0.49f, 0.54f);
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 92f;
        }

        private static void CreateStreet(
            Transform parent,
            PilotMaterials materials
        )
        {
            GameObject street = new("Street");
            street.transform.SetParent(parent, false);

            CreateBox(
                street.transform,
                "StoneRoad",
                new Vector3(0f, -0.02f, 7f),
                new Vector3(12f, 0.08f, StreetLength),
                materials.Stone
            );
            CreateBox(
                street.transform,
                "OuterGroundLeft",
                new Vector3(-9f, -0.16f, 7f),
                new Vector3(6f, 0.2f, StreetLength),
                materials.Trim
            );
            CreateBox(
                street.transform,
                "OuterGroundRight",
                new Vector3(9f, -0.16f, 7f),
                new Vector3(6f, 0.2f, StreetLength),
                materials.Trim
            );

            foreach (float x in new[] { -5.65f, 5.65f })
            {
                CreateBox(
                    street.transform,
                    $"Curb_{x}",
                    new Vector3(x, 0.12f, 7f),
                    new Vector3(0.28f, 0.2f, StreetLength),
                    materials.Stone
                );
            }
        }

        private static void CreateStreetBuildings(
            Transform parent,
            PilotMaterials materials,
            Mesh gableMesh
        )
        {
            GameObject buildings = new("Buildings");
            buildings.transform.SetParent(parent, false);

            float[] positions = { -18f, -10f, -2f, 6f, 14f, 22f };
            float[] heights = { 5.8f, 6.5f, 5.4f, 6.8f, 5.9f, 6.2f };
            for (int index = 0; index < positions.Length; index++)
            {
                CreateBuilding(
                    buildings.transform,
                    materials,
                    gableMesh,
                    -1,
                    index + 1,
                    positions[index],
                    6.2f,
                    heights[index]
                );

                if (index != 3)
                {
                    CreateBuilding(
                        buildings.transform,
                        materials,
                        gableMesh,
                        1,
                        index + 1,
                        positions[index] + 0.25f,
                        6.2f,
                        heights[(index + 2) % heights.Length]
                    );
                }
            }

            CreateGate(buildings.transform, materials, gableMesh);
        }

        private static void CreateBuilding(
            Transform parent,
            PilotMaterials materials,
            Mesh gableMesh,
            int side,
            int index,
            float z,
            float length,
            float height
        )
        {
            float sideValue = side;
            float centerX = sideValue * 9f;
            float facadeX = sideValue * 6f;
            float inward = -sideValue;
            Material wall = index % 3 == 0
                ? materials.PlasterAlt
                : materials.Plaster;

            GameObject building = new(
                $"Building_{(side < 0 ? "L" : "R")}_{index:00}"
            );
            building.transform.SetParent(parent, false);

            CreateBox(
                building.transform,
                "StoneFoundation",
                new Vector3(centerX, 0.42f, z),
                new Vector3(6f, 0.84f, length + 0.12f),
                materials.Stone
            );
            CreateBox(
                building.transform,
                "PlasterBody",
                new Vector3(centerX, height * 0.5f, z),
                new Vector3(6f, height, length),
                wall
            );
            CreateBox(
                building.transform,
                "WoodApron",
                new Vector3(
                    facadeX + inward * 0.1f,
                    0.64f,
                    z
                ),
                new Vector3(0.15f, 0.7f, length),
                materials.Wood
            );

            CreateFacadeWindow(
                building.transform,
                materials,
                facadeX + inward * 0.17f,
                z - 1.45f,
                1.15f
            );
            CreateFacadeDoor(
                building.transform,
                materials,
                facadeX + inward * 0.18f,
                z + 1.5f,
                1.45f
            );

            foreach (float postZ in new[]
                     {
                         z - length * 0.5f + 0.15f,
                         z,
                         z + length * 0.5f - 0.15f
                     })
            {
                CreateBox(
                    building.transform,
                    $"Post_{postZ:0.0}",
                    new Vector3(facadeX + inward * 0.19f, height * 0.5f, postZ),
                    new Vector3(0.2f, height, 0.24f),
                    materials.Trim
                );
            }

            CreateBox(
                building.transform,
                "HorizontalBeam",
                new Vector3(facadeX + inward * 0.2f, 3.22f, z),
                new Vector3(0.22f, 0.24f, length - 0.35f),
                materials.Trim
            );
            CreateBox(
                building.transform,
                "MidBeam",
                new Vector3(facadeX + inward * 0.19f, 2.55f, z),
                new Vector3(0.16f, 0.16f, length - 0.35f),
                materials.Wood
            );
            CreateBox(
                building.transform,
                "EaveBeam",
                new Vector3(facadeX + inward * 0.3f, height - 0.18f, z),
                new Vector3(0.32f, 0.26f, length + 0.35f),
                materials.Wood
            );
            foreach (float bracketZ in new[]
                     {
                         z - length * 0.34f,
                         z,
                         z + length * 0.34f
                     })
            {
                CreateBox(
                    building.transform,
                    $"EaveBracket_{bracketZ:0.0}",
                    new Vector3(
                        facadeX + inward * 0.4f,
                        height - 0.42f,
                        bracketZ
                    ),
                    new Vector3(0.55f, 0.15f, 0.18f),
                    materials.Trim
                );
            }

            if (index % 2 == 0)
            {
                GameObject awning = CreateBox(
                    building.transform,
                    "Awning",
                    new Vector3(
                        facadeX + inward * 0.58f,
                        3.05f,
                        z - 0.7f
                    ),
                    new Vector3(0.95f, 0.12f, 2.4f),
                    materials.Accent
                );
                awning.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    sideValue * 9f
                );
            }

            CreateSignboard(
                building.transform,
                materials,
                new Vector3(facadeX + inward * 0.22f, 4.05f, z),
                z
            );
            CreateLantern(
                building.transform,
                materials,
                new Vector3(
                    facadeX + inward * 0.48f,
                    2.85f,
                    z - length * 0.34f
                ),
                index < 4
            );

            CreateGableRoof(
                building.transform,
                materials,
                gableMesh,
                centerX,
                z,
                3.9f,
                1.2f,
                height + 0.15f,
                length + 1.1f
            );
        }

        private static void CreateFacadeWindow(
            Transform parent,
            PilotMaterials materials,
            float x,
            float z,
            float width
        )
        {
            CreateBox(
                parent,
                "WindowGlass",
                new Vector3(x, 2.1f, z),
                new Vector3(0.08f, 1.25f, width),
                materials.Glass
            );

            foreach (float offset in new[] { -0.42f, 0f, 0.42f })
            {
                CreateBox(
                    parent,
                    $"WindowBarV_{offset}",
                    new Vector3(x - 0.06f, 2.1f, z + offset * width),
                    new Vector3(0.1f, 1.32f, 0.055f),
                    materials.Trim
                );
            }
            foreach (float offset in new[] { -0.32f, 0.32f })
            {
                CreateBox(
                    parent,
                    $"WindowBarH_{offset}",
                    new Vector3(x - 0.06f, 2.1f + offset, z),
                    new Vector3(0.1f, 0.055f, width),
                    materials.Trim
                );
            }
        }

        private static void CreateFacadeDoor(
            Transform parent,
            PilotMaterials materials,
            float x,
            float z,
            float width
        )
        {
            CreateBox(
                parent,
                "DoorPanel",
                new Vector3(x, 1.45f, z),
                new Vector3(0.12f, 2.9f, width),
                materials.Trim
            );
            CreateBox(
                parent,
                "DoorInset",
                new Vector3(x - 0.05f, 1.5f, z),
                new Vector3(0.07f, 2.45f, width - 0.3f),
                materials.Accent
            );
        }

        private static void CreateSignboard(
            Transform parent,
            PilotMaterials materials,
            Vector3 position,
            float z
        )
        {
            CreateBox(
                parent,
                "SignBack",
                position,
                new Vector3(0.18f, 0.72f, 2.2f),
                materials.Trim
            );
            CreateBox(
                parent,
                "SignFace",
                position + new Vector3(-Mathf.Sign(position.x) * 0.08f, 0f, 0f),
                new Vector3(0.08f, 0.52f, 1.86f),
                materials.Accent
            );
            CreateBox(
                parent,
                "SignRail",
                new Vector3(position.x, position.y + 0.48f, z),
                new Vector3(0.25f, 0.1f, 2.35f),
                materials.Wood
            );
        }

        private static void CreateLantern(
            Transform parent,
            PilotMaterials materials,
            Vector3 position,
            bool addLight
        )
        {
            GameObject lantern = CreateSphere(
                parent,
                "Lantern",
                position,
                Vector3.one * 0.32f,
                materials.Lantern
            );
            CreateCylinder(
                parent,
                "LanternCapTop",
                position + new Vector3(0f, 0.24f, 0f),
                new Vector3(0.24f, 0.08f, 0.24f),
                materials.Trim
            );
            CreateCylinder(
                parent,
                "LanternCapBottom",
                position - new Vector3(0f, 0.24f, 0f),
                new Vector3(0.18f, 0.06f, 0.18f),
                materials.Trim
            );

            if (!addLight)
            {
                return;
            }

            GameObject lightObject = new("LanternGlow");
            lightObject.transform.SetParent(lantern.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.36f, 0.16f);
            light.intensity = 2.2f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;
        }

        private static void CreateGableRoof(
            Transform parent,
            PilotMaterials materials,
            Mesh gableMesh,
            float centerX,
            float z,
            float halfSpan,
            float rise,
            float eaveY,
            float length
        )
        {
            float slopeLength = Mathf.Sqrt(halfSpan * halfSpan + rise * rise);
            float angle = Mathf.Atan2(rise, halfSpan) * Mathf.Rad2Deg;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                GameObject slope = CreateBox(
                    parent,
                    sign < 0 ? "RoofSlopeLeft" : "RoofSlopeRight",
                    new Vector3(
                        centerX + sign * halfSpan * 0.5f,
                        eaveY + rise * 0.5f,
                        z
                    ),
                    new Vector3(slopeLength, 0.16f, length),
                    materials.Roof
                );
                slope.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    -sign * angle
                );
            }

            CreateBox(
                parent,
                "RoofRidge",
                new Vector3(centerX, eaveY + rise + 0.08f, z),
                new Vector3(0.24f, 0.18f, length + 0.3f),
                materials.Trim
            );

            for (int end = -1; end <= 1; end += 2)
            {
                GameObject gable = new(end < 0 ? "GableFront" : "GableBack");
                gable.transform.SetParent(parent, false);
                gable.transform.position = new Vector3(
                    centerX,
                    eaveY,
                    z + end * length * 0.5f
                );
                gable.transform.localScale = new Vector3(halfSpan, rise, 1f);
                if (end > 0)
                {
                    gable.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                MeshFilter filter = gable.AddComponent<MeshFilter>();
                filter.sharedMesh = gableMesh;
                MeshRenderer renderer = gable.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = materials.PlasterAlt;
            }

            for (int tile = 0; tile < 7; tile++)
            {
                float tileZ = z - length * 0.43f + tile * length * 0.86f / 6f;
                CreateCylinder(
                    parent,
                    $"RoofEnd_{tile}",
                    new Vector3(
                        centerX + halfSpan,
                        eaveY + 0.02f,
                        tileZ
                    ),
                    new Vector3(0.12f, 0.22f, 0.12f),
                    materials.Roof
                );
            }
        }

        private static void CreateGate(
            Transform parent,
            PilotMaterials materials,
            Mesh gableMesh
        )
        {
            CreateBox(
                parent,
                "GatePostLeft",
                new Vector3(-4.2f, 2.8f, 30f),
                new Vector3(0.7f, 5.6f, 0.7f),
                materials.Stone
            );
            CreateBox(
                parent,
                "GatePostRight",
                new Vector3(4.2f, 2.8f, 30f),
                new Vector3(0.7f, 5.6f, 0.7f),
                materials.Stone
            );
            CreateBox(
                parent,
                "GateLintel",
                new Vector3(0f, 5.15f, 30f),
                new Vector3(9.1f, 0.55f, 0.65f),
                materials.Wood
            );
            CreateBox(
                parent,
                "GateSign",
                new Vector3(0f, 5.05f, 29.62f),
                new Vector3(3.2f, 0.72f, 0.12f),
                materials.Trim
            );
            CreateBox(
                parent,
                "GateSignFace",
                new Vector3(0f, 5.05f, 29.53f),
                new Vector3(2.85f, 0.48f, 0.06f),
                materials.Accent
            );
            CreateGableRoof(
                parent,
                materials,
                gableMesh,
                0f,
                30f,
                5.3f,
                1.3f,
                5.65f,
                2.5f
            );
        }

        private static void CreateStreetProps(
            Transform parent,
            PilotMaterials materials
        )
        {
            GameObject props = new("Props");
            props.transform.SetParent(parent, false);

            CreateCrate(props.transform, materials, new Vector3(-4.7f, 0f, -4f), 1.1f);
            CreateCrate(props.transform, materials, new Vector3(-4.1f, 0f, 3.7f), 0.9f);
            CreateCrate(props.transform, materials, new Vector3(4.5f, 0f, 10f), 1.2f);
            CreateCrate(props.transform, materials, new Vector3(4.2f, 0f, 20f), 0.85f);
            CreateBarrel(props.transform, materials, new Vector3(4.75f, 0f, -6f));
            CreateBarrel(props.transform, materials, new Vector3(-4.65f, 0f, 17f));

            for (int index = 0; index < 4; index++)
            {
                CreateBanner(
                    props.transform,
                    materials,
                    new Vector3(
                        index % 2 == 0 ? -5.25f : 5.25f,
                        4.35f,
                        -13f + index * 10f
                    )
                );
            }
        }

        private static void CreateCharacterInstances(Transform parent)
        {
            GameObject characters = new("Characters");
            characters.transform.SetParent(parent, false);

            CreateCharacterInstance(
                characters.transform,
                "ART_Hero_Pilot",
                "Hero",
                new Vector3(0f, 0f, -15f),
                Quaternion.identity
            );
            CreateCharacterInstance(
                characters.transform,
                "ART_Guide_Pilot",
                "Guide",
                new Vector3(4.25f, 0f, -5.5f),
                Quaternion.Euler(0f, -90f, 0f)
            );
            CreateCharacterInstance(
                characters.transform,
                "ART_Official_Pilot",
                "Official",
                new Vector3(-4.35f, 0f, 5.2f),
                Quaternion.Euler(0f, 90f, 0f)
            );
        }

        private static void CreateCharacterInstance(
            Transform parent,
            string prefabName,
            string instanceName,
            Vector3 position,
            Quaternion rotation
        )
        {
            string path =
                $"Assets/Demo02/Art/Characters/Pilot/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Character prefab was not found: {path}"
                );
            }

            GameObject character =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            character.name = instanceName;
            character.transform.position = position;
            character.transform.rotation = rotation;
        }

        private static void CreateCrate(
            Transform parent,
            PilotMaterials materials,
            Vector3 groundPosition,
            float size
        )
        {
            Vector3 center = groundPosition + new Vector3(0f, size * 0.5f, 0f);
            CreateBox(
                parent,
                "CrateBody",
                center,
                new Vector3(size, size, size),
                materials.Wood
            );
            foreach (float offset in new[] { -0.35f, 0.35f })
            {
                CreateBox(
                    parent,
                    "CrateBand",
                    center + new Vector3(0f, offset * size, 0f),
                    new Vector3(size + 0.04f, 0.06f, size + 0.04f),
                    materials.Trim
                );
            }
        }

        private static void CreateBarrel(
            Transform parent,
            PilotMaterials materials,
            Vector3 groundPosition
        )
        {
            Vector3 center = groundPosition + new Vector3(0f, 0.52f, 0f);
            CreateCylinder(
                parent,
                "BarrelBody",
                center,
                new Vector3(0.62f, 1.04f, 0.62f),
                materials.Wood
            );
            foreach (float offset in new[] { -0.32f, 0.32f })
            {
                CreateCylinder(
                    parent,
                    "BarrelBand",
                    center + new Vector3(0f, offset, 0f),
                    new Vector3(0.67f, 0.07f, 0.67f),
                    materials.Trim
                );
            }
        }

        private static void CreateBanner(
            Transform parent,
            PilotMaterials materials,
            Vector3 position
        )
        {
            CreateBox(
                parent,
                "BannerPole",
                position + new Vector3(0f, 0.85f, 0f),
                new Vector3(0.08f, 1.7f, 0.08f),
                materials.Trim
            );
            CreateBox(
                parent,
                "BannerCloth",
                position,
                new Vector3(0.06f, 1.2f, 0.52f),
                materials.Accent
            );
        }

        private static void CreateBackground(
            Transform parent,
            PilotMaterials materials
        )
        {
            GameObject backdrop = new("Backdrop");
            backdrop.transform.SetParent(parent, false);

            float[] widths = { 10f, 13f, 9f, 15f };
            for (int index = 0; index < widths.Length; index++)
            {
                float z = 37f + index * 5f;
                float x = index % 2 == 0 ? -8f : 8f;
                float height = 5.5f + index * 0.7f;
                CreateBox(
                    backdrop.transform,
                    $"DistantBuilding_{index}",
                    new Vector3(x, height * 0.5f, z),
                    new Vector3(widths[index], height, 4.5f),
                    materials.PlasterAlt
                );
            }
        }

        private static void CreateLighting()
        {
            GameObject sunObject = new("Sunset Key Light");
            sunObject.transform.rotation = Quaternion.Euler(34f, -36f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.76f, 0.52f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;

            GameObject fillObject = new("Cool Fill Light");
            fillObject.transform.rotation = Quaternion.Euler(28f, 142f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.58f, 0.72f, 1f);
            fill.intensity = 0.85f;
            fill.shadows = LightShadows.None;

            CreateFacadeFill("Left Facade Fill", new Vector3(-1f, -0.35f, 0.2f), 0.62f);
            CreateFacadeFill("Right Facade Fill", new Vector3(1f, -0.35f, 0.2f), 0.72f);
        }

        private static void CreateFacadeFill(
            string name,
            Vector3 direction,
            float intensity
        )
        {
            GameObject fillObject = new(name);
            fillObject.transform.rotation = Quaternion.LookRotation(direction);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.82f, 0.88f, 1f);
            fill.intensity = intensity;
            fill.shadows = LightShadows.None;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.48f, 0.55f, 0.59f);
            cameraObject.transform.position = new Vector3(1.6f, 2.6f, -25f);
            cameraObject.transform.LookAt(new Vector3(0f, 3.5f, 8f));
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material
        )
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            box.isStatic = true;
            return box;
        }

        private static GameObject CreateSphere(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material
        )
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = position;
            sphere.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            return sphere;
        }

        private static GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material
        )
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cylinder;
        }

        private static Mesh CreateGableEndMesh()
        {
            string path = $"{MeshRoot}/SM_Guangling_Pilot_GableEnd.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            Mesh mesh = new()
            {
                name = "SM_Guangling_Pilot_GableEnd"
            };
            mesh.vertices = new[]
            {
                new Vector3(-1f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material LoadOrCreateMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Lit shader was not found."
                );
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
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

        private sealed class PilotMaterials
        {
            public Material Plaster;
            public Material PlasterAlt;
            public Material Wood;
            public Material Roof;
            public Material Stone;
            public Material Trim;
            public Material Accent;
            public Material Lantern;
            public Material Glass;
        }
    }
}
