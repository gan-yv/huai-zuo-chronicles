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
    public static class GuanglingCharacterArtBuilder
    {
        private const string MaterialRoot =
            "Assets/Demo02/Art/Materials/GuanglingCharacters";
        private const string PrefabRoot =
            "Assets/Demo02/Art/Characters/Pilot";
        private const string ScenePath =
            "Assets/Demo02/Scenes/Tests/GuanglingCharacterShowcase.unity";
        private const string ScreenshotRelativePath =
            "../../docs/screenshots/guangling-character-showcase.png";

        [MenuItem("Demo02/Art/Build Character Pilot")]
        public static void BuildCharacters()
        {
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);
            CharacterMaterials materials = CreateMaterials();

            CreateCharacterPrefab(
                "ART_Hero_Pilot",
                AvatarStyle.Hero,
                materials,
                1f,
                $"{PrefabRoot}/ART_Hero_Pilot.prefab"
            );
            CreateCharacterPrefab(
                "ART_Guide_Pilot",
                AvatarStyle.Guide,
                materials,
                0.94f,
                $"{PrefabRoot}/ART_Guide_Pilot.prefab"
            );
            CreateCharacterPrefab(
                "ART_Official_Pilot",
                AvatarStyle.Official,
                materials,
                1.04f,
                $"{PrefabRoot}/ART_Official_Pilot.prefab"
            );
            CreateShowcaseScene(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Guangling character pilot generated at {PrefabRoot}. " +
                "The showcase is a silhouette and proportion test."
            );
        }

        [MenuItem("Demo02/Art/Capture Character Pilot")]
        public static void CaptureCharacters()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Character showcase has no Main Camera."
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
                    "The active render pipeline cannot render the character camera."
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
            Debug.Log($"Guangling character showcase captured at {screenshotPath}");
        }

        public static void ValidateCharactersForBatch()
        {
            string[] prefabPaths =
            {
                $"{PrefabRoot}/ART_Hero_Pilot.prefab",
                $"{PrefabRoot}/ART_Guide_Pilot.prefab",
                $"{PrefabRoot}/ART_Official_Pilot.prefab"
            };
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing character pilot prefab: {path}"
                    );
                }

                foreach (string childName in new[]
                         {
                             "Visuals",
                             "Colliders",
                             "Gameplay",
                             "Metadata"
                         })
                {
                    if (prefab.transform.Find(childName) == null)
                    {
                        throw new InvalidOperationException(
                            $"Character {prefab.name} is missing {childName}."
                        );
                    }
                }

                CapsuleCollider collider =
                    prefab.GetComponentInChildren<CapsuleCollider>(true);
                if (collider == null ||
                    collider.height < 1.6f ||
                    collider.height > 2f)
                {
                    throw new InvalidOperationException(
                        $"Character {prefab.name} has an invalid scale collider."
                    );
                }

                int rendererCount =
                    prefab.GetComponentsInChildren<MeshRenderer>(true).Length;
                if (rendererCount < 12)
                {
                    throw new InvalidOperationException(
                        $"Character {prefab.name} has too few silhouette parts."
                    );
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Missing character showcase scene: {ScenePath}"
                );
            }

            Debug.Log(
                "Guangling character pilot validation passed: three " +
                "silhouettes, scale colliders, content groups and showcase."
            );
        }

        private static void CreateCharacterPrefab(
            string rootName,
            AvatarStyle style,
            CharacterMaterials materials,
            float heightScale,
            string path
        )
        {
            GameObject root = new(rootName);
            GameObject visuals = new("Visuals");
            visuals.transform.SetParent(root.transform, false);
            GameObject colliders = new("Colliders");
            colliders.transform.SetParent(root.transform, false);
            GameObject gameplay = new("Gameplay");
            gameplay.transform.SetParent(root.transform, false);
            GameObject metadata = new("Metadata");
            metadata.transform.SetParent(root.transform, false);

            CreateAvatar(visuals.transform, style, materials, heightScale);
            CapsuleCollider collider = colliders.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.88f * heightScale, 0f);
            collider.height = 1.76f * heightScale;
            collider.radius = 0.32f;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateAvatar(
            Transform parent,
            AvatarStyle style,
            CharacterMaterials materials,
            float heightScale
        )
        {
            GameObject avatar = new(style.ToString());
            avatar.transform.SetParent(parent, false);
            avatar.transform.localScale = Vector3.one * heightScale;

            Material robe = style switch
            {
                AvatarStyle.Hero => materials.HeroRobe,
                AvatarStyle.Guide => materials.GuideJacket,
                _ => materials.OfficialRobe
            };
            Material accent = style switch
            {
                AvatarStyle.Hero => materials.Sash,
                AvatarStyle.Guide => materials.GuideAccent,
                _ => materials.OfficialGold
            };
            Material trousers = style == AvatarStyle.Guide
                ? materials.GuideTrousers
                : materials.DarkCloth;

            CreatePart(
                avatar.transform,
                "Robe",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.72f, 0f),
                new Vector3(0.46f, 0.55f, 0.36f),
                robe
            );
            CreatePart(
                avatar.transform,
                "Torso",
                PrimitiveType.Capsule,
                new Vector3(0f, 1.18f, 0f),
                new Vector3(0.44f, 0.42f, 0.29f),
                robe
            );

            foreach (float x in new[] { -0.18f, 0.18f })
            {
                CreatePart(
                    avatar.transform,
                    "Leg",
                    PrimitiveType.Capsule,
                    new Vector3(x, 0.39f, 0f),
                    new Vector3(0.13f, 0.38f, 0.13f),
                    trousers
                );
                CreatePart(
                    avatar.transform,
                    "Boot",
                    PrimitiveType.Cube,
                    new Vector3(x, 0.08f, 0.06f),
                    new Vector3(0.22f, 0.16f, 0.36f),
                    materials.Leather
                );
            }

            CreatePart(
                avatar.transform,
                "Belt",
                PrimitiveType.Cube,
                new Vector3(0f, 0.9f, 0f),
                new Vector3(0.5f, 0.1f, 0.34f),
                materials.Leather
            );
            CreatePart(
                avatar.transform,
                "Sash",
                PrimitiveType.Cube,
                new Vector3(0f, 0.87f, 0.18f),
                new Vector3(0.2f, 0.12f, 0.06f),
                accent
            );
            CreatePart(
                avatar.transform,
                "SashTail",
                PrimitiveType.Cube,
                new Vector3(0.17f, 0.66f, 0.18f),
                new Vector3(0.1f, 0.38f, 0.05f),
                accent
            );

            foreach (float x in new[] { -0.32f, 0.32f })
            {
                GameObject upperArm = CreatePart(
                    avatar.transform,
                    "UpperArm",
                    PrimitiveType.Capsule,
                    new Vector3(x, 1.08f, 0f),
                    new Vector3(0.13f, 0.28f, 0.13f),
                    robe
                );
                upperArm.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    x < 0f ? -13f : 13f
                );
                CreatePart(
                    avatar.transform,
                    "Hand",
                    PrimitiveType.Sphere,
                    new Vector3(x * 1.05f, 0.77f, 0.02f),
                    Vector3.one * 0.13f,
                    materials.Skin
                );
            }

            CreatePart(
                avatar.transform,
                "ShoulderMantle",
                PrimitiveType.Capsule,
                new Vector3(0f, 1.4f, 0f),
                new Vector3(0.38f, 0.12f, 0.25f),
                accent
            );
            CreatePart(
                avatar.transform,
                "Neck",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.47f, 0f),
                new Vector3(0.1f, 0.08f, 0.1f),
                materials.Skin
            );
            CreatePart(
                avatar.transform,
                "Head",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.59f, 0.02f),
                new Vector3(0.36f, 0.4f, 0.35f),
                materials.Skin
            );
            CreatePart(
                avatar.transform,
                "HairBack",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.63f, -0.09f),
                new Vector3(0.37f, 0.35f, 0.3f),
                materials.Hair
            );
            CreatePart(
                avatar.transform,
                "HairTop",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.76f, -0.02f),
                new Vector3(0.28f, 0.15f, 0.28f),
                materials.Hair
            );

            foreach (float x in new[] { -0.075f, 0.075f })
            {
                CreatePart(
                    avatar.transform,
                    "Eye",
                    PrimitiveType.Sphere,
                    new Vector3(x, 1.62f, 0.19f),
                    new Vector3(0.035f, 0.035f, 0.025f),
                    materials.Hair
                );
            }

            switch (style)
            {
                case AvatarStyle.Hero:
                    CreateHeroDetails(avatar.transform, materials);
                    break;
                case AvatarStyle.Guide:
                    CreateGuideDetails(avatar.transform, materials);
                    break;
                case AvatarStyle.Official:
                    CreateOfficialDetails(avatar.transform, materials);
                    break;
            }
        }

        private static void CreateHeroDetails(
            Transform parent,
            CharacterMaterials materials
        )
        {
            CreatePart(
                parent,
                "HeadBun",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.87f, -0.06f),
                new Vector3(0.15f, 0.17f, 0.15f),
                materials.Hair
            );
            CreatePart(
                parent,
                "RecordBag",
                PrimitiveType.Cube,
                new Vector3(-0.31f, 0.86f, -0.14f),
                new Vector3(0.22f, 0.3f, 0.14f),
                materials.Leather
            );
            GameObject tool = CreatePart(
                parent,
                "RepairTool",
                PrimitiveType.Cube,
                new Vector3(0.24f, 1.22f, -0.22f),
                new Vector3(0.08f, 0.78f, 0.08f),
                materials.Metal
            );
            tool.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            CreatePart(
                parent,
                "ToolHandle",
                PrimitiveType.Cylinder,
                new Vector3(0.35f, 1.55f, -0.22f),
                new Vector3(0.08f, 0.16f, 0.08f),
                materials.Sash
            );
        }

        private static void CreateGuideDetails(
            Transform parent,
            CharacterMaterials materials
        )
        {
            CreatePart(
                parent,
                "ShortCoat",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.91f, 0f),
                new Vector3(0.43f, 0.3f, 0.34f),
                materials.GuideJacket
            );
            CreatePart(
                parent,
                "Cap",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.79f, 0f),
                new Vector3(0.25f, 0.055f, 0.25f),
                materials.GuideAccent
            );
            CreatePart(
                parent,
                "MapTube",
                PrimitiveType.Cylinder,
                new Vector3(-0.3f, 0.92f, -0.16f),
                new Vector3(0.08f, 0.42f, 0.08f),
                materials.Leather
            ).transform.localRotation = Quaternion.Euler(15f, 0f, 28f);
            CreatePart(
                parent,
                "CrossBodyStrap",
                PrimitiveType.Cube,
                new Vector3(0f, 1.12f, 0.17f),
                new Vector3(0.07f, 0.65f, 0.04f),
                materials.Leather
            ).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        }

        private static void CreateOfficialDetails(
            Transform parent,
            CharacterMaterials materials
        )
        {
            CreatePart(
                parent,
                "OfficialHat",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.83f, 0f),
                new Vector3(0.31f, 0.07f, 0.31f),
                materials.Hair
            );
            CreatePart(
                parent,
                "HatCrown",
                PrimitiveType.Cube,
                new Vector3(0f, 1.92f, -0.02f),
                new Vector3(0.42f, 0.16f, 0.3f),
                materials.Hair
            );
            CreatePart(
                parent,
                "Seal",
                PrimitiveType.Cube,
                new Vector3(0.24f, 0.84f, 0.18f),
                new Vector3(0.14f, 0.18f, 0.05f),
                materials.OfficialGold
            );
            CreatePart(
                parent,
                "LongSleeve",
                PrimitiveType.Capsule,
                new Vector3(-0.32f, 1.02f, 0f),
                new Vector3(0.18f, 0.32f, 0.18f),
                materials.OfficialRobe
            );
            CreatePart(
                parent,
                "LongSleeve",
                PrimitiveType.Capsule,
                new Vector3(0.32f, 1.02f, 0f),
                new Vector3(0.18f, 0.32f, 0.18f),
                materials.OfficialRobe
            );
        }

        private static CharacterMaterials CreateMaterials()
        {
            return new CharacterMaterials
            {
                HeroRobe = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_HeroRobe.mat",
                    new Color(0.11f, 0.29f, 0.48f),
                    0.16f
                ),
                Sash = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_Sash.mat",
                    new Color(0.9f, 0.55f, 0.12f),
                    0.2f
                ),
                GuideJacket = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_GuideJacket.mat",
                    new Color(0.2f, 0.43f, 0.25f),
                    0.16f
                ),
                GuideTrousers = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_GuideTrousers.mat",
                    new Color(0.2f, 0.17f, 0.12f),
                    0.12f
                ),
                GuideAccent = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_GuideAccent.mat",
                    new Color(0.75f, 0.52f, 0.18f),
                    0.18f
                ),
                OfficialRobe = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_OfficialRobe.mat",
                    new Color(0.5f, 0.12f, 0.1f),
                    0.17f
                ),
                OfficialGold = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_OfficialGold.mat",
                    new Color(0.95f, 0.72f, 0.2f),
                    0.35f
                ),
                DarkCloth = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_DarkCloth.mat",
                    new Color(0.1f, 0.09f, 0.08f),
                    0.08f
                ),
                Leather = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_Leather.mat",
                    new Color(0.24f, 0.14f, 0.08f),
                    0.16f
                ),
                Metal = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_Metal.mat",
                    new Color(0.39f, 0.4f, 0.38f),
                    0.48f,
                    0.5f
                ),
                Skin = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_Skin.mat",
                    new Color(0.9f, 0.68f, 0.52f),
                    0.1f
                ),
                Hair = CreateMaterial(
                    $"{MaterialRoot}/MAT_Character_Hair.mat",
                    new Color(0.055f, 0.042f, 0.032f),
                    0.08f
                )
            };
        }

        private static Material CreateMaterial(
            string path,
            Color color,
            float smoothness,
            float metallic = 0f
        )
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

            material.SetTexture("_BaseMap", null);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material
        )
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static void CreateShowcaseScene(CharacterMaterials materials)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.68f, 0.7f, 0.72f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "CharacterFloor";
            floor.transform.localScale = new Vector3(1.5f, 1f, 0.8f);
            Material floorMaterial = CreateMaterial(
                $"{MaterialRoot}/MAT_Character_Backdrop.mat",
                new Color(0.25f, 0.27f, 0.28f),
                0.16f
            );
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            CreateShowcaseCharacter(
                "ART_Hero_Pilot",
                new Vector3(-3f, 0f, 0f)
            );
            CreateShowcaseCharacter(
                "ART_Guide_Pilot",
                new Vector3(0f, 0f, 0f)
            );
            CreateShowcaseCharacter(
                "ART_Official_Pilot",
                new Vector3(3f, 0f, 0f)
            );

            CreateWorldLabel("HERO", new Vector3(-3f, 2.25f, 0f));
            CreateWorldLabel("GUIDE", new Vector3(0f, 2.25f, 0f));
            CreateWorldLabel("OFFICIAL", new Vector3(3f, 2.25f, 0f));

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.47f, 0.54f, 0.58f);
            cameraObject.transform.position = new Vector3(0f, 1.25f, -7.8f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.05f, 0f));

            GameObject sunObject = new("Key Light");
            sunObject.transform.rotation = Quaternion.Euler(32f, -25f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.white;
            sun.intensity = 1.55f;

            GameObject fillObject = new("Fill Light");
            fillObject.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.74f, 0.82f, 1f);
            fill.intensity = 0.95f;

            GameObject frontObject = new("Front Fill Light");
            frontObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Light front = frontObject.AddComponent<Light>();
            front.type = LightType.Directional;
            front.color = Color.white;
            front.intensity = 0.8f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateShowcaseCharacter(
            string prefabName,
            Vector3 position
        )
        {
            string path = $"{PrefabRoot}/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Character prefab was not found: {path}"
                );
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefabName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static void CreateWorldLabel(string text, Vector3 position)
        {
            GameObject labelObject = new(text);
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.055f;
            textMesh.color = new Color(0.9f, 0.84f, 0.7f);
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

        private enum AvatarStyle
        {
            Hero,
            Guide,
            Official
        }

        private sealed class CharacterMaterials
        {
            public Material HeroRobe;
            public Material Sash;
            public Material GuideJacket;
            public Material GuideTrousers;
            public Material GuideAccent;
            public Material OfficialRobe;
            public Material OfficialGold;
            public Material DarkCloth;
            public Material Leather;
            public Material Metal;
            public Material Skin;
            public Material Hair;
        }
    }
}
