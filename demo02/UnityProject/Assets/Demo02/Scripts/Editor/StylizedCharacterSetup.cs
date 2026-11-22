using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demo02.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class StylizedCharacterSetup
    {
        private const string SourceRoot =
            "Assets/ThirdParty/Kenney/AnimatedCharactersProtagonists";
        private const string ArtRoot =
            "Assets/Demo02/Art/Characters/Stylized";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string ControllerPath =
            ArtRoot + "/AC_StylizedCharacter.controller";
        private const string ShowcaseScenePath =
            "Assets/Demo02/Scenes/Tests/StylizedCharacterShowcase.unity";
        private const string ShowcaseScreenshotPath =
            "../../docs/screenshots/stylized-character-showcase.png";

        private static readonly CharacterSpec[] Characters =
        {
            new("Hero", "skaterMaleA", Color.white),
            new("Guide", "skaterFemaleA", Color.white),
            new("Official", "criminalMaleA", Color.white),
            new("NPC_A", "skaterMaleA", new Color(0.82f, 0.9f, 1f)),
            new("NPC_B", "skaterFemaleA", new Color(1f, 0.92f, 0.82f)),
            new("Enemy", "cyborgFemaleA", new Color(1f, 0.72f, 0.72f))
        };

        [MenuItem("Demo02/Art/Build Stylized Character Assets")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            foreach (string skin in new[]
                     {
                         "skaterMaleA",
                         "skaterFemaleA",
                         "criminalMaleA",
                         "cyborgFemaleA"
                     })
            {
                ConfigureTexture($"{SourceRoot}/Skins/{skin}.png");
            }
            ConfigureModel($"{SourceRoot}/Model/characterMedium.fbx", false);
            foreach (string animation in new[] { "idle", "run", "jump" })
            {
                ConfigureModel(
                    $"{SourceRoot}/Animations/{animation}.fbx",
                    true
                );
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AnimatorController controller = CreateAnimatorController();
            foreach (CharacterSpec spec in Characters)
            {
                Material material = CreateMaterial(spec);
                CreateVisualPrefab(spec, material, controller);
            }
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Stylized character assets are ready: six visual prefabs, " +
                "four skins and one shared Generic Animator Controller."
            );
        }

        public static string ResolveCharacterPrefab(string characterName)
        {
            string visualName = characterName switch
            {
                _ when characterName.Contains("Player") => "VIS_Stylized_Hero",
                _ when characterName.Contains("Partner") => "VIS_Stylized_Guide",
                _ when characterName.Contains("Worker") => "VIS_Stylized_NPC_A",
                _ when characterName.Contains("Keeper") => "VIS_Stylized_NPC_B",
                _ when characterName.Contains("Storyteller") => "VIS_Stylized_Official",
                _ when characterName.Contains("Cloth") => "VIS_Stylized_NPC_A",
                _ when characterName.Contains("Tea") => "VIS_Stylized_NPC_B",
                _ when characterName.Contains("Tailor") => "VIS_Stylized_Official",
                _ when characterName.Contains("Snack") => "VIS_Stylized_Guide",
                _ => "VIS_Stylized_NPC_A"
            };
            return $"{PrefabRoot}/{visualName}.prefab";
        }

        public static string EnemyPrefabPath =>
            $"{PrefabRoot}/VIS_Stylized_Enemy.prefab";

        [MenuItem("Demo02/Art/Capture Stylized Character Showcase")]
        public static void CaptureShowcase()
        {
            EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stylized character showcase has no Main Camera."
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

            string path = Path.GetFullPath(
                Path.Combine(Application.dataPath, ShowcaseScreenshotPath)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Stylized character showcase captured at {path}");
        }

        public static void ValidateForBatch()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Missing Stylized Animator Controller: {ControllerPath}"
                );
            }

            foreach (string parameter in new[] { "Speed", "Jump" })
            {
                if (!controller.parameters.Any(item => item.name == parameter))
                {
                    throw new InvalidOperationException(
                        $"Stylized controller is missing '{parameter}'."
                    );
                }
            }

            foreach (CharacterSpec spec in Characters)
            {
                string prefabPath = PrefabPath(spec);
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing stylized prefab: {prefabPath}"
                    );
                }
                if (prefab.GetComponent<ReadyMadeCharacterAnimator>() == null)
                {
                    throw new InvalidOperationException(
                        $"Stylized prefab has no animation driver: {prefabPath}"
                    );
                }

                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null ||
                    animator.runtimeAnimatorController != controller)
                {
                    throw new InvalidOperationException(
                        $"Stylized prefab has an invalid Animator: {prefabPath}"
                    );
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Physics.SyncTransforms();
                Bounds? bounds = GetCombinedBounds(instance);
                if (!bounds.HasValue ||
                    bounds.Value.size.y < 1.6f ||
                    bounds.Value.size.y > 1.95f)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    throw new InvalidOperationException(
                        $"Stylized prefab height is outside 1.6-1.95m: {prefabPath}"
                    );
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Missing stylized character showcase: {ShowcaseScenePath}"
                );
            }

            GameObject samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabRoot}/VIS_Stylized_Hero.prefab"
            );
            GameObject sample =
                (GameObject)PrefabUtility.InstantiatePrefab(samplePrefab);
            Animator sampleAnimator =
                sample.GetComponentInChildren<Animator>(true);
            try
            {
                foreach (string animationName in new[] { "idle", "run", "jump" })
                {
                    AnimationClip clip = LoadClip(animationName);
                    EditorCurveBinding[] bindings =
                        AnimationUtility.GetCurveBindings(clip);
                    if (bindings.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"Animation '{animationName}' has no curve bindings."
                        );
                    }

                    int missing = bindings.Count(binding =>
                        sampleAnimator.transform.Find(binding.path) == null
                    );
                    if (missing > 0)
                    {
                        throw new InvalidOperationException(
                            $"Animation '{animationName}' has {missing} " +
                            "skeleton paths missing from the stylized model."
                        );
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }

            Debug.Log(
                "Stylized character validation passed: skins, shared " +
                "controller, skeleton paths, animation driver and six prefabs."
            );
        }

        private static void ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new InvalidOperationException(
                    $"Texture was not imported: {path}"
                );
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        private static void ConfigureModel(string path, bool importAnimation)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                throw new InvalidOperationException(
                    $"Model was not imported: {path}"
                );
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = importAnimation;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static AnimatorController CreateAnimatorController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                ) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip idleClip = LoadClip("idle");
            AnimationClip runClip = LoadClip("run");
            AnimationClip jumpClip = LoadClip("jump");

            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = idleClip;
            AnimatorState run = stateMachine.AddState("Run");
            run.motion = runClip;
            AnimatorState jump = stateMachine.AddState("Jump");
            jump.motion = jumpClip;
            stateMachine.defaultState = idle;

            AnimatorStateTransition idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.12f;
            idleToRun.AddCondition(
                AnimatorConditionMode.Greater,
                0.06f,
                "Speed"
            );

            AnimatorStateTransition runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.12f;
            runToIdle.AddCondition(
                AnimatorConditionMode.Less,
                0.05f,
                "Speed"
            );

            AnimatorStateTransition enterJump =
                stateMachine.AddAnyStateTransition(jump);
            enterJump.hasExitTime = false;
            enterJump.duration = 0.06f;
            enterJump.canTransitionToSelf = false;
            enterJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            AnimatorStateTransition exitJump = jump.AddTransition(idle);
            exitJump.hasExitTime = true;
            exitJump.exitTime = 0.9f;
            exitJump.duration = 0.08f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip LoadClip(string name)
        {
            string animationPath =
                $"{SourceRoot}/Animations/{name}.fbx";
            AnimationClip clip = AssetDatabase
                .LoadAllAssetsAtPath(animationPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(
                    item =>
                        !item.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                        (string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase) ||
                         item.name.EndsWith(
                             "|" + name,
                             StringComparison.OrdinalIgnoreCase
                         ))
                );
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Animation clip '{name}' was not imported from {animationPath}."
                );
            }
            return clip;
        }

        private static Material CreateMaterial(CharacterSpec spec)
        {
            string path = $"{MaterialRoot}/MAT_Stylized_{spec.Id}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Unlit shader was not found."
                );
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{SourceRoot}/Skins/{spec.Skin}.png"
            );
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", spec.Tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateVisualPrefab(
            CharacterSpec spec,
            Material material,
            AnimatorController controller
        )
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{SourceRoot}/Model/characterMedium.fbx"
            );
            if (source == null)
            {
                throw new InvalidOperationException(
                    "Stylized character model was not imported."
                );
            }

            GameObject root = new(PrefabName(spec));
            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds? bounds = GetCombinedBounds(model);
            if (!bounds.HasValue || bounds.Value.size.y <= 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Stylized character has no bounds: {spec.Id}"
                );
            }

            model.transform.localScale *= 1.78f / bounds.Value.size.y;
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
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                renderer.sharedMaterials = materials;
            }

            Transform animationRoot = model.transform.Find("Root");
            if (animationRoot == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    "Stylized character model has no Root animation node."
                );
            }

            Animator animator = animationRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = animationRoot.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            root.AddComponent<ReadyMadeCharacterAnimator>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(spec));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateShowcaseScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.74f, 0.78f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "CharacterFloor";
            floor.transform.localScale = new Vector3(1.4f, 1f, 0.7f);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material floorMaterial = new(lit)
            {
                name = "MAT_Stylized_ShowcaseFloor",
                color = new Color(0.3f, 0.32f, 0.34f)
            };
            floorMaterial.SetFloat("_Smoothness", 0.15f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            CreateShowcaseCharacter("VIS_Stylized_Hero", new Vector3(-2.6f, 0f, 0f));
            CreateShowcaseCharacter("VIS_Stylized_Guide", new Vector3(0f, 0f, 0f));
            CreateShowcaseCharacter("VIS_Stylized_Official", new Vector3(2.6f, 0f, 0f));
            CreateWorldLabel("HERO", new Vector3(-2.6f, 2.25f, 0f));
            CreateWorldLabel("GUIDE", new Vector3(0f, 2.25f, 0f));
            CreateWorldLabel("OFFICIAL", new Vector3(2.6f, 2.25f, 0f));

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.48f, 0.55f, 0.59f);
            cameraObject.transform.position = new Vector3(0f, 1.25f, -7.2f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.05f, 0f));

            GameObject keyObject = new("Key Light");
            keyObject.transform.rotation = Quaternion.Euler(32f, -25f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = Color.white;
            key.intensity = 1.6f;

            GameObject fillObject = new("Fill Light");
            fillObject.transform.rotation = Quaternion.Euler(22f, 150f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.78f, 0.86f, 1f);
            fill.intensity = 1f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static void CreateShowcaseCharacter(
            string prefabName,
            Vector3 position
        )
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabRoot}/{prefabName}.prefab"
            );
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Stylized prefab was not found: {prefabName}"
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
            textMesh.color = new Color(0.92f, 0.86f, 0.72f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                textMesh.font = font;
                labelObject.GetComponent<MeshRenderer>().sharedMaterial =
                    font.material;
            }
            labelObject.transform.position = position;
        }

        private static string PrefabPath(CharacterSpec spec) =>
            $"{PrefabRoot}/VIS_Stylized_{spec.Id}.prefab";

        private static string PrefabName(CharacterSpec spec) =>
            $"VIS_Stylized_{spec.Id}";

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

        private readonly struct CharacterSpec
        {
            public CharacterSpec(string id, string skin, Color tint)
            {
                Id = id;
                Skin = skin;
                Tint = tint;
            }

            public string Id { get; }
            public string Skin { get; }
            public Color Tint { get; }
        }
    }
}
