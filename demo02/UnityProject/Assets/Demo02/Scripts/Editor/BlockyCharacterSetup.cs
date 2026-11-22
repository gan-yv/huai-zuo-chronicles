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
    public static class BlockyCharacterSetup
    {
        private const string SourceRoot =
            "Assets/ThirdParty/Kenney/BlockyCharacters";
        private const string ArtRoot =
            "Assets/Demo02/Art/Characters/Blocky";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string ControllerPath =
            ArtRoot + "/AC_BlockyCharacter.controller";
        private const string ShowcaseScenePath =
            "Assets/Demo02/Scenes/Tests/BlockyCharacterShowcase.unity";
        private const string ShowcaseScreenshotPath =
            "../../docs/screenshots/blocky-character-showcase.png";

        private static readonly CharacterSpec[] Characters =
        {
            new("a", "VIS_Blocky_Hero"),
            new("b", "VIS_Blocky_Official"),
            new("c", "VIS_Blocky_Guide"),
            new("e", "VIS_Blocky_NPC_A"),
            new("f", "VIS_Blocky_NPC_B"),
            new("g", "VIS_Blocky_Enemy")
        };

        [MenuItem("Demo02/Art/Build Blocky Character Assets")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            foreach (CharacterSpec spec in Characters)
            {
                ConfigureTexture(
                    $"{SourceRoot}/Textures/texture-{spec.Id}.png"
                );
                ConfigureModel(
                    $"{SourceRoot}/Models/character-{spec.Id}.fbx"
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
                "Blocky character assets are ready: six visual prefabs, " +
                "six URP materials and one shared Generic Animator Controller."
            );
        }

        public static string ResolveCharacterPrefab(string characterName)
        {
            string visualName = characterName switch
            {
                _ when characterName.Contains("Player") => "VIS_Blocky_Hero",
                _ when characterName.Contains("Partner") => "VIS_Blocky_Guide",
                _ when characterName.Contains("Worker") => "VIS_Blocky_NPC_A",
                _ when characterName.Contains("Keeper") => "VIS_Blocky_NPC_B",
                _ when characterName.Contains("Storyteller") => "VIS_Blocky_Official",
                _ when characterName.Contains("Cloth") => "VIS_Blocky_NPC_A",
                _ when characterName.Contains("Tea") => "VIS_Blocky_NPC_B",
                _ when characterName.Contains("Tailor") => "VIS_Blocky_Official",
                _ when characterName.Contains("Snack") => "VIS_Blocky_Guide",
                _ => "VIS_Blocky_NPC_A"
            };

            return $"{PrefabRoot}/{visualName}.prefab";
        }

        public static string EnemyPrefabPath =>
            $"{PrefabRoot}/VIS_Blocky_Enemy.prefab";

        [MenuItem("Demo02/Art/Capture Blocky Character Showcase")]
        public static void CaptureShowcase()
        {
            EditorSceneManager.OpenScene(
                ShowcaseScenePath,
                OpenSceneMode.Single
            );
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Blocky character showcase has no Main Camera."
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
            Debug.Log($"Blocky character showcase captured at {path}");
        }

        public static void ValidateForBatch()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Missing Blocky Animator Controller: {ControllerPath}"
                );
            }

            foreach (string parameter in new[]
                     {
                         "Speed",
                         "Attack",
                         "Interact",
                         "PickUp",
                         "Die"
                     })
            {
                if (!controller.parameters.Any(item => item.name == parameter))
                {
                    throw new InvalidOperationException(
                        $"Animator Controller is missing parameter '{parameter}'."
                    );
                }
            }

            foreach (CharacterSpec spec in Characters)
            {
                string prefabPath = $"{PrefabRoot}/{spec.PrefabName}.prefab";
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing Blocky visual prefab: {prefabPath}"
                    );
                }
                if (prefab.GetComponent<ReadyMadeCharacterAnimator>() == null)
                {
                    throw new InvalidOperationException(
                        $"Blocky prefab has no animation driver: {prefabPath}"
                    );
                }

                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null ||
                    animator.runtimeAnimatorController != controller)
                {
                    throw new InvalidOperationException(
                        $"Blocky prefab has an invalid Animator: {prefabPath}"
                    );
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Physics.SyncTransforms();
                Bounds? bounds = GetCombinedBounds(instance);
                if (!bounds.HasValue || bounds.Value.size.y < 1.6f ||
                    bounds.Value.size.y > 1.95f)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    throw new InvalidOperationException(
                        $"Blocky prefab height is outside 1.6-1.95m: {prefabPath}"
                    );
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Missing Blocky character showcase: {ShowcaseScenePath}"
                );
            }

            Debug.Log(
                "Blocky character validation passed: Generic animation, " +
                "shared controller, parameters, materials and six visual prefabs."
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
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        private static void ConfigureModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                throw new InvalidOperationException(
                    $"Model was not imported: {path}"
                );
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
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
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip[] clips = LoadClips();
            AnimatorState idle = AddState(
                stateMachine,
                "Idle",
                FindClip(clips, "idle")
            );
            AnimatorState walk = AddState(
                stateMachine,
                "Walk",
                FindClip(clips, "walk")
            );
            AnimatorState sprint = AddState(
                stateMachine,
                "Sprint",
                FindClip(clips, "sprint")
            );
            AnimatorState attack = AddState(
                stateMachine,
                "Attack",
                FindClip(clips, "attack-melee-right")
            );
            AnimatorState interact = AddState(
                stateMachine,
                "Interact",
                FindClip(clips, "interact-right")
            );
            AnimatorState pickup = AddState(
                stateMachine,
                "PickUp",
                FindClip(clips, "pick-up")
            );
            AddState(
                stateMachine,
                "Die",
                FindClip(clips, "die")
            );
            stateMachine.defaultState = idle;

            AddFloatTransition(idle, walk, AnimatorConditionMode.Greater, 0.08f);
            AddFloatTransition(walk, idle, AnimatorConditionMode.Less, 0.06f);
            AddFloatTransition(walk, sprint, AnimatorConditionMode.Greater, 0.62f);
            AddFloatTransition(sprint, walk, AnimatorConditionMode.Less, 0.56f);
            AddFloatTransition(sprint, idle, AnimatorConditionMode.Less, 0.06f);

            AddTriggerState(
                stateMachine,
                "Attack",
                attack,
                idle
            );
            AddTriggerState(
                stateMachine,
                "Interact",
                interact,
                idle
            );
            AddTriggerState(
                stateMachine,
                "PickUp",
                pickup,
                idle
            );
            AddTriggerState(
                stateMachine,
                "Die",
                stateMachine.states.First(item => item.state.name == "Die").state,
                null
            );

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string name,
            AnimationClip clip
        )
        {
            AnimatorState state = stateMachine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddFloatTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            float threshold
        )
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, "Speed");
        }

        private static void AddTriggerState(
            AnimatorStateMachine stateMachine,
            string trigger,
            AnimatorState action,
            AnimatorState returnState
        )
        {
            AnimatorStateTransition enter =
                stateMachine.AddAnyStateTransition(action);
            enter.hasExitTime = false;
            enter.canTransitionToSelf = false;
            enter.duration = 0.05f;
            enter.AddCondition(
                AnimatorConditionMode.If,
                0f,
                trigger
            );

            if (returnState == null)
            {
                return;
            }

            AnimatorStateTransition exit = action.AddTransition(returnState);
            exit.hasExitTime = true;
            exit.exitTime = 0.94f;
            exit.duration = 0.08f;
        }

        private static Material CreateMaterial(CharacterSpec spec)
        {
            string path = $"{MaterialRoot}/MAT_Blocky_{spec.Id}.mat";
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
                $"{SourceRoot}/Textures/texture-{spec.Id}.png"
            );
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
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
                $"{SourceRoot}/Models/character-{spec.Id}.fbx"
            );
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Character source was not imported: {spec.Id}"
                );
            }

            GameObject root = new(spec.PrefabName);
            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = $"Model_{spec.Id}";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds? sourceBounds = GetCombinedBounds(model);
            if (!sourceBounds.HasValue || sourceBounds.Value.size.y <= 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Character has no render bounds: {spec.Id}"
                );
            }

            float scale = 1.78f / sourceBounds.Value.size.y;
            model.transform.localScale *= scale;
            Physics.SyncTransforms();
            Bounds scaledBounds = GetCombinedBounds(model)!.Value;
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z
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

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Imported character has no Animator: {spec.Id}"
                );
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            root.AddComponent<ReadyMadeCharacterAnimator>();
            PrefabUtility.SaveAsPrefabAsset(
                root,
                $"{PrefabRoot}/{spec.PrefabName}.prefab"
            );
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateShowcaseScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.72f, 0.75f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "CharacterFloor";
            floor.transform.localScale = new Vector3(1.4f, 1f, 0.7f);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material floorMaterial = new(lit)
            {
                name = "MAT_Blocky_ShowcaseFloor",
                color = new Color(0.3f, 0.32f, 0.34f)
            };
            floorMaterial.SetFloat("_Smoothness", 0.15f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            CreateShowcaseCharacter("VIS_Blocky_Hero", new Vector3(-2.6f, 0f, 0f));
            CreateShowcaseCharacter("VIS_Blocky_Guide", new Vector3(0f, 0f, 0f));
            CreateShowcaseCharacter("VIS_Blocky_Official", new Vector3(2.6f, 0f, 0f));

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
                    $"Blocky prefab was not found: {prefabName}"
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

        private static AnimationClip[] LoadClips()
        {
            string path = $"{SourceRoot}/Models/character-a.fbx";
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No animation clips were imported from {path}"
                );
            }
            return clips;
        }

        private static AnimationClip FindClip(
            IEnumerable<AnimationClip> clips,
            string name
        )
        {
            AnimationClip clip = clips.FirstOrDefault(
                item => item.name == name ||
                        item.name.EndsWith("|" + name, StringComparison.Ordinal)
            );
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Required animation clip was not found: {name}"
                );
            }
            return clip;
        }

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
                throw new InvalidOperationException($"Invalid asset folder: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct CharacterSpec
        {
            public CharacterSpec(string id, string prefabName)
            {
                Id = id;
                PrefabName = prefabName;
            }

            public string Id { get; }
            public string PrefabName { get; }
        }
    }
}
