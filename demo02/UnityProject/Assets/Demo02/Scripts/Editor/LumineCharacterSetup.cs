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
    public static class LumineCharacterSetup
    {
        private const string ArtRoot =
            "Assets/Demo02/Art/Characters/Lumine";
        private const string ModelPath =
            ArtRoot + "/Models/VIS_Lumine.glb";
        private const string ClipRoot =
            ArtRoot + "/Animations";
        private const string MaterialRoot =
            ArtRoot + "/Materials";
        private const string ControllerPath =
            ArtRoot + "/AC_LumineCharacter.controller";
        private const string ShowcaseScenePath =
            "Assets/Demo02/Scenes/Tests/LumineCharacterShowcase.unity";
        private const string GameplayScenePath =
            "Assets/Demo02/Scenes/Guangling/Greybox.unity";
        private const string ScreenshotPath =
            "../../docs/screenshots/lumine-character-showcase.png";
        private const string AirScreenshotPath =
            "../../docs/screenshots/lumine-air-showcase.png";
        private const string AnimationAssetPath =
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/Models/" +
            "AnimationLibrary_Godot_Standard.gltf";
        private const int BakeFrameRate = 30;

        public const string PlayerPrefabPath =
            ArtRoot + "/VIS_Lumine.prefab";

        private static readonly ClipSpec[] ClipSpecs =
        {
            new("Sword_Attack", "Attack", "AC_Lumine_Attack"),
            new("Hit_Chest", "Hit", "AC_Lumine_Hit"),
            new("Interact", "Interact", "AC_Lumine_Interact"),
            new("PickUp_Table", "PickUp", "AC_Lumine_PickUp"),
            new("Death01", "Die", "AC_Lumine_Death")
        };

        private static readonly LocomotionSpec[] LocomotionSpecs =
        {
            new("Idle_Loop", "AC_Lumine_Idle"),
            new("Walk_Loop", "AC_Lumine_Walk"),
            new("Sprint_Loop", "AC_Lumine_Run")
        };

        private static readonly ClipSpec TalkSpec =
            new("Idle_Talking_Loop", "Talking", "AC_Lumine_Talk");

        private static readonly AirSpec[] AirSpecs =
        {
            new("Jump_Start", "Jump", "AC_Lumine_Jump"),
            new("Jump_Loop", "Fall", "AC_Lumine_Fall"),
            new("Jump_Land", "Land", "AC_Lumine_Land")
        };

        private static readonly BoneMapping[] BoneMappings =
        {
            new("DEF-hips", "腰"),
            new("DEF-spine.001", "上半身"),
            new("DEF-spine.002", "上半身3"),
            new("DEF-spine.003", "上半身2"),
            new("DEF-neck", "首"),
            new("DEF-head", "頭"),
            new("DEF-shoulder.L", "肩.L"),
            new("DEF-upper_arm.L", "腕.L"),
            new("DEF-forearm.L", "ひじ.L"),
            new("DEF-hand.L", "手首.L"),
            new("DEF-f_index.01.L", "人指１.L"),
            new("DEF-f_index.02.L", "人指２.L"),
            new("DEF-f_index.03.L", "人指３.L"),
            new("DEF-thumb.01.L", "親指１.L"),
            new("DEF-thumb.02.L", "親指２.L"),
            new("DEF-shoulder.R", "肩.R"),
            new("DEF-upper_arm.R", "腕.R"),
            new("DEF-forearm.R", "ひじ.R"),
            new("DEF-hand.R", "手首.R"),
            new("DEF-f_index.01.R", "人指１.R"),
            new("DEF-f_index.02.R", "人指２.R"),
            new("DEF-f_index.03.R", "人指３.R"),
            new("DEF-thumb.01.R", "親指１.R"),
            new("DEF-thumb.02.R", "親指２.R"),
            new("DEF-thigh.L", "足.L"),
            new("DEF-shin.L", "ひざ.L"),
            new("DEF-foot.L", "足首.L"),
            new("DEF-toe.L", "つま先.L"),
            new("DEF-thigh.R", "足.R"),
            new("DEF-shin.R", "ひざ.R"),
            new("DEF-foot.R", "足首.R"),
            new("DEF-toe.R", "つま先.R")
        };

        [MenuItem("Demo02/Art/Lumine/Rebuild Character And Actions")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ArtRoot + "/Models");
            EnsureFolder(ClipRoot);
            EnsureFolder(MaterialRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject animationSource =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnimationAssetPath
                );
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Lumine model was not imported: {ModelPath}"
                );
            }
            if (animationSource == null)
            {
                throw new InvalidOperationException(
                    $"Quaternius animation library was not imported: " +
                    AnimationAssetPath
                );
            }

            GameObject root = new("VIS_Lumine");
            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds? sourceBounds = GetCombinedBounds(model);
            if (!sourceBounds.HasValue ||
                sourceBounds.Value.size.y <= 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    "Lumine model has no renderer bounds."
                );
            }

            model.transform.localScale *=
                1.78f / sourceBounds.Value.size.y;
            Physics.SyncTransforms();
            Bounds scaled = GetCombinedBounds(model)!.Value;
            model.transform.localPosition = new Vector3(
                -scaled.center.x,
                -scaled.min.y,
                -scaled.center.z
            );

            Animator animator =
                model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GameObject animationRig = null;
            try
            {
                animationRig =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        animationSource
                    );
                AnimationClip[] sourceClips = AssetDatabase
                    .LoadAllAssetsAtPath(AnimationAssetPath)
                    .OfType<AnimationClip>()
                    .ToArray();
                BakeContext context =
                    CreateBakeContext(animationRig, animator);

                Dictionary<string, AnimationClip> actions =
                    new(StringComparer.Ordinal);
                foreach (ClipSpec spec in ClipSpecs)
                {
                    AnimationClip sourceClip =
                        FindSourceClip(sourceClips, spec.SourceClip);
                    AnimationClip clip = BakeClip(
                        animationRig,
                        animator,
                        context,
                        sourceClip,
                        spec.TargetClip
                    );
                    actions.Add(spec.Trigger, clip);
                }

                Dictionary<string, AnimationClip> locomotion =
                    new(StringComparer.Ordinal);
                foreach (LocomotionSpec spec in LocomotionSpecs)
                {
                    AnimationClip sourceClip =
                        FindSourceClip(sourceClips, spec.SourceClip);
                    AnimationClip clip = BakeClip(
                        animationRig,
                        animator,
                        context,
                        sourceClip,
                        spec.TargetClip,
                        loop: true
                    );
                    locomotion.Add(spec.TargetClip, clip);
                }

                AnimationClip talkClip = BakeClip(
                    animationRig,
                    animator,
                    context,
                    FindSourceClip(sourceClips, TalkSpec.SourceClip),
                    TalkSpec.TargetClip,
                    loop: true
                );
                Dictionary<string, AnimationClip> air =
                    new(StringComparer.Ordinal);
                foreach (AirSpec spec in AirSpecs)
                {
                    AnimationClip clip = BakeClip(
                        animationRig,
                        animator,
                        context,
                        FindSourceClip(sourceClips, spec.SourceClip),
                        spec.TargetClip,
                        loop: spec.TargetClip == "AC_Lumine_Fall"
                    );
                    air.Add(spec.State, clip);
                }
                ConfigureController(actions, locomotion, talkClip, air);
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        ControllerPath
                    );
                RestoreRestPose(context);
            }
            finally
            {
                if (animationRig != null)
                {
                    UnityEngine.Object.DestroyImmediate(animationRig);
                }
            }

            int materialCount = ConfigureRenderers(model);
            int rendererCount = model
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Length;
            int clipCount =
                ClipSpecs.Length +
                LocomotionSpecs.Length +
                AirSpecs.Length +
                1;
            root.AddComponent<ReadyMadeCharacterAnimator>();
            root.AddComponent<CharacterExpressionAnimator>();
            Bounds finalBounds = GetCombinedBounds(root)!.Value;
            if (finalBounds.size.y < 1.6f ||
                finalBounds.size.y > 1.95f)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Lumine prefab height is outside 1.6-1.95m: " +
                    $"{finalBounds.size.y:0.000}m."
                );
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Lumine character ready: " +
                $"{rendererCount} skinned renderer(s), " +
                $"{materialCount} materials, " +
                $"{BoneMappings.Length} retargeted bones, " +
                $"{clipCount} clips."
            );
        }

        [MenuItem("Demo02/Art/Lumine/Capture Showcase")]
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
                    "Lumine showcase has no Main Camera."
                );
            }

            GameObject hero = GameObject.Find("Lumine_Idle");
            GameObject runner = GameObject.Find("Lumine_Run");
            GameObject attacker = GameObject.Find("Lumine_Attack");
            AnimationClip idle = LoadTargetClip("AC_Lumine_Idle");
            AnimationClip run = LoadTargetClip("AC_Lumine_Run");
            AnimationClip attack = LoadTargetClip("AC_Lumine_Attack");

            AnimationMode.StartAnimationMode();
            try
            {
                SampleAtNormalizedTime(hero, idle, 0.45f);
                SampleAtNormalizedTime(runner, run, 0.18f);
                SampleAtNormalizedTime(attacker, attack, 0.35f);
                CaptureCamera(camera, ScreenshotPath);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        [MenuItem("Demo02/Art/Lumine/Capture Air Showcase")]
        public static void CaptureAirShowcase()
        {
            EditorSceneManager.OpenScene(
                ShowcaseScenePath,
                OpenSceneMode.Single
            );
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Lumine showcase has no Main Camera."
                );
            }

            GameObject hero = GameObject.Find("Lumine_Idle");
            GameObject runner = GameObject.Find("Lumine_Run");
            GameObject attacker = GameObject.Find("Lumine_Attack");
            GameObject idleLabel = GameObject.Find("IDLE");
            GameObject runLabel = GameObject.Find("RUN");
            GameObject attackLabel = GameObject.Find("ATTACK");
            if (idleLabel != null) idleLabel.SetActive(false);
            if (runLabel != null) runLabel.SetActive(false);
            if (attackLabel != null) attackLabel.SetActive(false);
            CreateWorldLabel(
                "JUMP",
                new Vector3(-2.4f, 2.08f, 0f),
                camera.transform.position
            );
            CreateWorldLabel(
                "FALL",
                new Vector3(0f, 2.08f, 0f),
                camera.transform.position
            );
            CreateWorldLabel(
                "LAND",
                new Vector3(2.4f, 2.08f, 0f),
                camera.transform.position
            );

            AnimationMode.StartAnimationMode();
            try
            {
                SampleAtNormalizedTime(
                    hero,
                    LoadTargetClip("AC_Lumine_Jump"),
                    0.25f
                );
                SampleAtNormalizedTime(
                    runner,
                    LoadTargetClip("AC_Lumine_Fall"),
                    0.45f
                );
                SampleAtNormalizedTime(
                    attacker,
                    LoadTargetClip("AC_Lumine_Land"),
                    0.35f
                );
                CaptureCamera(camera, AirScreenshotPath);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        [MenuItem("Demo02/Art/Lumine/Capture Gameplay")]
        public static void CaptureGameplay()
        {
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single
            );
            GameObject player = GameObject.Find("PlayerPlaceholder");
            if (player == null)
            {
                throw new InvalidOperationException(
                    "Gameplay scene has no PlayerPlaceholder."
                );
            }
            Animator animator =
                player.GetComponentInChildren<Animator>(true);
            if (animator == null ||
                !animator.gameObject.name.Contains("Model"))
            {
                throw new InvalidOperationException(
                    "Gameplay player is not using the Lumine visual."
                );
            }
            AnimationMode.StartAnimationMode();
            try
            {
                SampleAtNormalizedTime(
                    player,
                    LoadTargetClip("AC_Lumine_Idle"),
                    0.45f
                );

                GameObject cameraObject = new("Lumine Capture Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = 42f;
                camera.nearClipPlane = 0.05f;
                camera.clearFlags = CameraClearFlags.Skybox;
                cameraObject.transform.position =
                    player.transform.position +
                    new Vector3(2.8f, 2.3f, -4.2f);
                cameraObject.transform.LookAt(
                    player.transform.position +
                    new Vector3(0f, 1.05f, 2.2f)
                );
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
                LogShowcaseInstance("gameplay", player);
                CaptureCamera(
                    camera,
                    "../../docs/screenshots/lumine-gameplay.png"
                );
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        public static void ValidateForBatch()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PlayerPrefabPath
                );
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                );
            if (prefab == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Lumine prefab or controller is missing."
                );
            }

            foreach (string parameter in new[]
                     {
                         "Speed",
                         "Attack",
                         "Hit",
                         "Interact",
                         "PickUp",
                         "Die",
                         "Talking",
                         "Grounded",
                         "VerticalSpeed"
                     })
            {
                if (!controller.parameters.Any(
                        item => item.name == parameter
                    ))
                {
                    throw new InvalidOperationException(
                        $"Lumine controller is missing '{parameter}'."
                    );
                }
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Animator animator =
                    instance.GetComponentInChildren<Animator>(true);
                if (animator == null ||
                    animator.runtimeAnimatorController != controller)
                {
                    throw new InvalidOperationException(
                        "Lumine prefab has an invalid Animator."
                    );
                }

                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(
                        true
                    );
                if (renderer == null ||
                    renderer.bones.Length < 300 ||
                    renderer.sharedMesh == null ||
                    renderer.sharedMesh.blendShapeCount < 60)
                {
                    throw new InvalidOperationException(
                        "Lumine skeleton or facial blend shapes did not import."
                    );
                }
                CharacterExpressionAnimator expressions =
                    instance.GetComponent<CharacterExpressionAnimator>();
                if (expressions == null ||
                    !expressions.HasBlinkTargets ||
                    !expressions.HasMouthTargets)
                {
                    throw new InvalidOperationException(
                        "Lumine facial expression targets did not import."
                    );
                }

                Bounds? bounds = GetCombinedBounds(instance);
                if (!bounds.HasValue ||
                    bounds.Value.size.y < 1.6f ||
                    bounds.Value.size.y > 1.95f)
                {
                    throw new InvalidOperationException(
                        "Lumine prefab height is outside 1.6-1.95m."
                    );
                }

                foreach (string clipName in new[]
                         {
                             "AC_Lumine_Attack",
                             "AC_Lumine_Hit",
                             "AC_Lumine_Interact",
                             "AC_Lumine_PickUp",
                             "AC_Lumine_Death",
                             "AC_Lumine_Idle",
                             "AC_Lumine_Walk",
                             "AC_Lumine_Run",
                             "AC_Lumine_Talk",
                             "AC_Lumine_Jump",
                             "AC_Lumine_Fall",
                             "AC_Lumine_Land"
                         })
                {
                    AnimationClip clip = LoadTargetClip(clipName);
                    EditorCurveBinding[] bindings =
                        AnimationUtility.GetCurveBindings(clip);
                    if (clip.length <= 0.05f || bindings.Length < 100)
                    {
                        throw new InvalidOperationException(
                            $"Lumine clip is incomplete: {clipName}"
                        );
                    }
                    int missing = bindings.Count(
                        binding =>
                            animator.transform.Find(binding.path) == null
                    );
                    if (missing > 0)
                    {
                        throw new InvalidOperationException(
                            $"{clipName} has {missing} missing bone paths."
                        );
                    }
                }

                int texturedMaterials = renderer.sharedMaterials.Count(
                    material => material != null &&
                        GetBaseTexture(material) != null
                );
                if (texturedMaterials < 10)
                {
                    throw new InvalidOperationException(
                        "Lumine materials lost their textures during import."
                    );
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log(
                "Lumine validation passed: skeleton, expressions, " +
                "textures and all retargeted action paths."
            );
        }

        private static int ConfigureRenderers(GameObject model)
        {
            SkinnedMeshRenderer[] renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Lumine GLB imported without a skinned renderer."
                );
            }

            int materialCount = 0;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.updateWhenOffscreen = true;
                renderer.skinnedMotionVectors = true;
                renderer.quality = SkinQuality.Bone4;
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = ConvertMaterial(
                        materials[index],
                        index
                    );
                    if (materials[index] != null)
                    {
                        materialCount++;
                    }
                }
                renderer.sharedMaterials = materials;
            }
            return materialCount;
        }

        private static Material ConvertMaterial(
            Material source,
            int index)
        {
            if (source == null)
            {
                return null;
            }

            Texture texture = GetBaseTexture(source);
            string path =
                $"{MaterialRoot}/MAT_Lumine_{index:00}.mat";
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
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
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.name =
                $"MAT_Lumine_{index:00}_{source.name}";
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.22f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.28f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture GetBaseTexture(Material material)
        {
            foreach (string property in new[]
                     {
                         "baseColorTexture",
                         "_BaseMap",
                         "_MainTex"
                     })
            {
                if (material.HasProperty(property))
                {
                    Texture texture = material.GetTexture(property);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }
            return null;
        }

        private static BakeContext CreateBakeContext(
            GameObject source,
            Animator targetAnimator)
        {
            Dictionary<string, Transform> sourceBones =
                source.GetComponentsInChildren<Transform>(true)
                    .GroupBy(item => item.name)
                    .ToDictionary(group => group.Key, group => group.First());
            Dictionary<string, Transform> targetBones =
                targetAnimator.GetComponentsInChildren<Transform>(true)
                    .GroupBy(item => item.name)
                    .ToDictionary(group => group.Key, group => group.First());

            foreach (BoneMapping mapping in BoneMappings)
            {
                if (!sourceBones.ContainsKey(mapping.SourceName))
                {
                    throw new InvalidOperationException(
                        $"Quaternius skeleton is missing " +
                        $"{mapping.SourceName}."
                    );
                }
                if (!targetBones.ContainsKey(mapping.TargetName))
                {
                    throw new InvalidOperationException(
                        $"Lumine skeleton is missing " +
                        $"{mapping.TargetName}."
                    );
                }
            }
            foreach (string required in new[] { "センター", "腰", "頭" })
            {
                if (!targetBones.ContainsKey(required))
                {
                    throw new InvalidOperationException(
                        $"Lumine skeleton is missing {required}."
                    );
                }
            }

            Transform sourceHips = sourceBones["DEF-hips"];
            Transform sourceHead = sourceBones["DEF-head"];
            Transform targetWaist = targetBones["腰"];
            Transform targetHead = targetBones["頭"];
            Transform targetRoot = targetBones["センター"];
            float sourceSpineLength =
                (sourceHead.position - sourceHips.position).magnitude;
            float targetSpineLength =
                (targetHead.position - targetWaist.position).magnitude;
            float scale = targetSpineLength /
                Mathf.Max(0.001f, sourceSpineLength);

            return new BakeContext(
                sourceBones,
                targetBones,
                sourceHips.position,
                targetRoot.position,
                scale,
                sourceBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.rotation
                ),
                targetBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.rotation
                ),
                targetBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.localPosition
                ),
                targetBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.localRotation
                )
            );
        }

        private static AnimationClip BakeClip(
            GameObject source,
            Animator targetAnimator,
            BakeContext context,
            AnimationClip sourceClip,
            string targetName,
            bool loop = false)
        {
            RestoreRestPose(context);
            int sampleCount = Mathf.Max(
                2,
                Mathf.CeilToInt(sourceClip.length * BakeFrameRate) + 1
            );
            float timeStep = sourceClip.length / (sampleCount - 1);
            Dictionary<EditorCurveBinding, AnimationCurve> curves = new();
            Dictionary<EditorCurveBinding, Quaternion> previousRotations =
                new();

            AnimationMode.StartAnimationMode();
            try
            {
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    float time = sample * timeStep;
                    AnimationMode.SampleAnimationClip(
                        source,
                        sourceClip,
                        time
                    );
                    BakePose(
                        targetAnimator,
                        context,
                        time,
                        curves,
                        previousRotations
                    );
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            AnimationClip clip = new()
            {
                name = targetName,
                frameRate = BakeFrameRate,
                wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever
            };
            foreach (
                (EditorCurveBinding binding, AnimationCurve curve) in curves
            )
            {
                clip.SetCurve(
                    binding.path,
                    binding.type,
                    binding.propertyName,
                    curve
                );
            }
            clip.EnsureQuaternionContinuity();

            string path = ClipPath(targetName);
            AnimationClip existing =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                existing.ClearCurves();
                EditorUtility.CopySerialized(clip, existing);
                UnityEngine.Object.DestroyImmediate(clip);
                clip = existing;
                EditorUtility.SetDirty(clip);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }
            RestoreRestPose(context);
            return clip;
        }

        private static void BakePose(
            Animator targetAnimator,
            BakeContext context,
            float time,
            Dictionary<EditorCurveBinding, AnimationCurve> curves,
            Dictionary<EditorCurveBinding, Quaternion> previousRotations)
        {
            Transform targetRoot = context.TargetBones["センター"];
            Transform sourceHips = context.SourceBones["DEF-hips"];
            Vector3 hipsDelta =
                (sourceHips.position - context.SourceHipsRestWorld) *
                context.Scale;
            targetRoot.position =
                context.TargetRootRestWorld + hipsDelta;
            AddPositionCurves(
                curves,
                targetAnimator.transform,
                targetRoot,
                time
            );

            foreach (BoneMapping mapping in BoneMappings)
            {
                Transform sourceBone =
                    context.SourceBones[mapping.SourceName];
                Transform targetBone =
                    context.TargetBones[mapping.TargetName];
                Quaternion sourceDelta =
                    sourceBone.rotation *
                    Quaternion.Inverse(
                        context.SourceRestRotations[mapping.SourceName]
                    );
                Quaternion desiredGlobal =
                    sourceDelta *
                    context.TargetRestRotations[mapping.TargetName];
                Quaternion parentGlobal = targetBone.parent != null
                    ? targetBone.parent.rotation
                    : Quaternion.identity;
                targetBone.localRotation =
                    Quaternion.Inverse(parentGlobal) * desiredGlobal;
                AddRotationCurves(
                    curves,
                    previousRotations,
                    targetAnimator.transform,
                    targetBone,
                    time
                );
            }
        }

        private static void AddPositionCurves(
            Dictionary<EditorCurveBinding, AnimationCurve> curves,
            Transform animationRoot,
            Transform bone,
            float time)
        {
            string path = AnimationUtility.CalculateTransformPath(
                bone,
                animationRoot
            );
            AddCurve(
                curves,
                path,
                "localPosition.x",
                bone.localPosition.x,
                time
            );
            AddCurve(
                curves,
                path,
                "localPosition.y",
                bone.localPosition.y,
                time
            );
            AddCurve(
                curves,
                path,
                "localPosition.z",
                bone.localPosition.z,
                time
            );
        }

        private static void AddRotationCurves(
            Dictionary<EditorCurveBinding, AnimationCurve> curves,
            Dictionary<EditorCurveBinding, Quaternion> previousRotations,
            Transform animationRoot,
            Transform bone,
            float time)
        {
            Quaternion rotation = bone.localRotation;
            string path = AnimationUtility.CalculateTransformPath(
                bone,
                animationRoot
            );
            EditorCurveBinding firstBinding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                "localRotation.x"
            );
            if (previousRotations.TryGetValue(
                    firstBinding,
                    out Quaternion previous
                ) && Quaternion.Dot(previous, rotation) < 0f)
            {
                rotation = new Quaternion(
                    -rotation.x,
                    -rotation.y,
                    -rotation.z,
                    -rotation.w
                );
            }
            string[] components = { "x", "y", "z", "w" };
            float[] values =
            {
                rotation.x,
                rotation.y,
                rotation.z,
                rotation.w
            };
            for (int index = 0; index < components.Length; index++)
            {
                AddCurve(
                    curves,
                    path,
                    $"localRotation.{components[index]}",
                    values[index],
                    time
                );
            }
            previousRotations[firstBinding] = rotation;
        }

        private static void AddCurve(
            Dictionary<EditorCurveBinding, AnimationCurve> curves,
            string path,
            string property,
            float value,
            float time)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                property
            );
            if (!curves.TryGetValue(
                    binding,
                    out AnimationCurve curve
                ))
            {
                curve = new AnimationCurve();
                curves.Add(binding, curve);
            }
            curve.AddKey(time, value);
        }

        private static void ConfigureController(
            Dictionary<string, AnimationClip> clips,
            Dictionary<string, AnimationClip> locomotion,
            AnimationClip talkClip,
            Dictionary<string, AnimationClip> air)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                ) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath
                );
            controller.AddParameter(
                "Speed",
                AnimatorControllerParameterType.Float
            );
            controller.AddParameter(
                "Talking",
                AnimatorControllerParameterType.Bool
            );
            controller.AddParameter(
                "Grounded",
                AnimatorControllerParameterType.Bool
            );
            controller.AddParameter(
                "VerticalSpeed",
                AnimatorControllerParameterType.Float
            );
            foreach (string trigger in clips.Keys)
            {
                controller.AddParameter(
                    trigger,
                    AnimatorControllerParameterType.Trigger
                );
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = locomotion["AC_Lumine_Idle"];
            AnimatorState walk = stateMachine.AddState("Walk");
            walk.motion = locomotion["AC_Lumine_Walk"];
            AnimatorState run = stateMachine.AddState("Run");
            run.motion = locomotion["AC_Lumine_Run"];
            AnimatorState talk = stateMachine.AddState("Talk");
            talk.motion = talkClip;
            AnimatorState jump = stateMachine.AddState("Jump");
            jump.motion = air["Jump"];
            AnimatorState fall = stateMachine.AddState("Fall");
            fall.motion = air["Fall"];
            AnimatorState land = stateMachine.AddState("Land");
            land.motion = air["Land"];
            stateMachine.defaultState = idle;

            AddTransition(
                idle,
                walk,
                "Speed",
                AnimatorConditionMode.Greater,
                0.04f,
                0.14f,
                grounded: true
            );
            AddTransition(
                walk,
                run,
                "Speed",
                AnimatorConditionMode.Greater,
                0.72f,
                0.14f,
                grounded: true
            );
            AddTransition(
                run,
                walk,
                "Speed",
                AnimatorConditionMode.Less,
                0.68f,
                0.16f,
                grounded: true
            );
            AddTransition(
                walk,
                idle,
                "Speed",
                AnimatorConditionMode.Less,
                0.04f,
                0.14f,
                grounded: true
            );

            AnimatorStateTransition enterJump =
                stateMachine.AddAnyStateTransition(jump);
            enterJump.hasExitTime = false;
            enterJump.duration = 0.08f;
            enterJump.canTransitionToSelf = false;
            enterJump.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Grounded"
            );
            enterJump.AddCondition(
                AnimatorConditionMode.Greater,
                0.1f,
                "VerticalSpeed"
            );

            AnimatorStateTransition jumpToFall =
                jump.AddTransition(fall);
            jumpToFall.hasExitTime = false;
            jumpToFall.duration = 0.1f;
            jumpToFall.AddCondition(
                AnimatorConditionMode.Less,
                -0.1f,
                "VerticalSpeed"
            );

            AnimatorStateTransition fallToLand =
                fall.AddTransition(land);
            fallToLand.hasExitTime = false;
            fallToLand.duration = 0.08f;
            fallToLand.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Grounded"
            );

            AnimatorStateTransition landToIdle =
                land.AddTransition(idle);
            landToIdle.hasExitTime = true;
            landToIdle.exitTime = 0.78f;
            landToIdle.duration = 0.1f;

            AnimatorStateTransition enterTalk =
                stateMachine.AddAnyStateTransition(talk);
            enterTalk.hasExitTime = false;
            enterTalk.duration = 0.12f;
            enterTalk.canTransitionToSelf = false;
            enterTalk.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Talking"
            );
            AnimatorStateTransition exitTalk = talk.AddTransition(idle);
            exitTalk.hasExitTime = false;
            exitTalk.duration = 0.14f;
            exitTalk.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Talking"
            );

            foreach ((string trigger, AnimationClip clip) in clips)
            {
                AnimatorState state = stateMachine.AddState(trigger);
                state.motion = clip;
                AnimatorStateTransition enter =
                    stateMachine.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0.06f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    trigger
                );
                if (trigger == "Die")
                {
                    continue;
                }
                AddActionExit(state, idle, walk, run);
            }

            EditorUtility.SetDirty(controller);
        }

        private static void AddActionExit(
            AnimatorState action,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run)
        {
            AnimatorStateTransition toRun = action.AddTransition(run);
            toRun.hasExitTime = true;
            toRun.exitTime = 0.88f;
            toRun.duration = 0.08f;
            toRun.AddCondition(
                AnimatorConditionMode.Greater,
                0.72f,
                "Speed"
            );

            AnimatorStateTransition toWalk = action.AddTransition(walk);
            toWalk.hasExitTime = true;
            toWalk.exitTime = 0.88f;
            toWalk.duration = 0.08f;
            toWalk.AddCondition(
                AnimatorConditionMode.Greater,
                0.04f,
                "Speed"
            );

            AnimatorStateTransition toIdle = action.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.88f;
            toIdle.duration = 0.08f;
            toIdle.AddCondition(
                AnimatorConditionMode.Less,
                0.04f,
                "Speed"
            );
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            AnimatorConditionMode mode,
            float threshold,
            float duration,
            bool grounded = false)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(mode, threshold, parameter);
            if (grounded)
            {
                transition.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    "Grounded"
                );
            }
        }

        private static void RestoreRestPose(BakeContext context)
        {
            foreach (Transform transform in context.TargetBones.Values)
            {
                transform.localPosition =
                    context.TargetRestLocalPositions[transform.name];
                transform.localRotation =
                    context.TargetRestLocalRotations[transform.name];
            }
        }

        private static AnimationClip FindSourceClip(
            IEnumerable<AnimationClip> clips,
            string name)
        {
            AnimationClip clip = clips.FirstOrDefault(
                item => string.Equals(
                    item.name,
                    name,
                    StringComparison.Ordinal
                )
            );
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Quaternius source clip is missing: {name}"
                );
            }
            return clip;
        }

        private static AnimationClip LoadTargetClip(string name) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath(name)
            );

        private static string ClipPath(string name) =>
            $"{ClipRoot}/{name}.anim";

        private static void CreateShowcaseScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(0.43f, 0.47f, 0.53f);
            RenderSettings.fog = false;

            GameObject floor = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );
            floor.name = "Studio_Floor";
            floor.transform.position = new Vector3(0f, -0.09f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.16f, 8f);
            SetStudioColor(floor, new Color(0.19f, 0.23f, 0.29f));

            GameObject backdrop = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );
            backdrop.name = "Studio_Backdrop";
            backdrop.transform.position =
                new Vector3(0f, 2.7f, -2.7f);
            backdrop.transform.localScale =
                new Vector3(8f, 6f, 0.12f);
            SetStudioColor(
                backdrop,
                new Color(0.08f, 0.13f, 0.18f)
            );

            CreateLight(
                "Key_Light",
                new Vector3(30f, -32f, 0f),
                new Color(1f, 0.88f, 0.72f),
                1.65f
            );
            CreateLight(
                "Fill_Light",
                new Vector3(12f, 145f, 0f),
                new Color(0.58f, 0.72f, 1f),
                0.8f
            );
            CreateLight(
                "Rim_Light",
                new Vector3(55f, 190f, 0f),
                new Color(1f, 0.56f, 0.34f),
                0.95f
            );

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.035f, 0.065f, 0.095f);
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.05f;
            cameraObject.transform.position =
                new Vector3(4.4f, 1.35f, 5.4f);
            cameraObject.transform.LookAt(
                new Vector3(0f, 0.98f, 0f)
            );
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            CreateShowcaseCharacter(
                "Lumine_Idle",
                new Vector3(-2.4f, 0f, 0f)
            );
            CreateShowcaseCharacter(
                "Lumine_Run",
                new Vector3(0f, 0f, 0f)
            );
            CreateShowcaseCharacter(
                "Lumine_Attack",
                new Vector3(2.4f, 0f, 0f)
            );
            CreateWorldLabel(
                "IDLE",
                new Vector3(-2.4f, 2.08f, 0f),
                cameraObject.transform.position
            );
            CreateWorldLabel(
                "RUN",
                new Vector3(0f, 2.08f, 0f),
                cameraObject.transform.position
            );
            CreateWorldLabel(
                "ATTACK",
                new Vector3(2.4f, 2.08f, 0f),
                cameraObject.transform.position
            );

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ShowcaseScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save Lumine showcase: " +
                    ShowcaseScenePath
                );
            }
        }

        private static void CreateShowcaseCharacter(
            string name,
            Vector3 position)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PlayerPrefabPath
                );
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Lumine player prefab is missing."
                );
            }
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
        }

        private static void CreateWorldLabel(
            string text,
            Vector3 position,
            Vector3 cameraPosition)
        {
            GameObject labelObject = new(text);
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.055f;
            textMesh.color = new Color(0.95f, 0.87f, 0.68f);
            Font font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
            if (font != null)
            {
                textMesh.font = font;
                labelObject.GetComponent<MeshRenderer>().sharedMaterial =
                    font.material;
            }
            labelObject.transform.position = position;
            labelObject.transform.rotation =
                Quaternion.LookRotation(
                    position - cameraPosition,
                    Vector3.up
                );
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
            )
            {
                name = "MAT_Lumine_Studio"
            };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            renderer.sharedMaterial = material;
        }

        private static void SampleAtNormalizedTime(
            GameObject character,
            AnimationClip clip,
            float normalizedTime)
        {
            if (character == null || clip == null)
            {
                return;
            }
            Animator animator =
                character.GetComponentInChildren<Animator>(true);
            AnimationMode.SampleAnimationClip(
                animator.gameObject,
                clip,
                clip.length * normalizedTime
            );
        }

        private static void LogShowcaseInstance(
            string label,
            GameObject character)
        {
            if (character == null)
            {
                Debug.Log($"LUMINE_SHOWCASE {label}: missing");
                return;
            }
            Bounds? bounds = GetCombinedBounds(character);
            Renderer[] renderers =
                character.GetComponentsInChildren<Renderer>(true);
            Debug.Log(
                $"LUMINE_SHOWCASE {label}: active={character.activeInHierarchy} " +
                $"position={character.transform.position} " +
                $"bounds={bounds?.ToString() ?? "none"} " +
                $"renderers={renderers.Length} " +
                $"enabled={renderers.Count(item => item.enabled)} " +
                $"visible={renderers.Count(item => item.isVisible)}"
            );
        }

        private static void CaptureCamera(
            Camera camera,
            string relativePath)
        {
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
                Path.Combine(Application.dataPath, relativePath)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Lumine showcase captured at {path}");
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent =
                Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(
                    $"Invalid folder: {path}"
                );
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct ClipSpec
        {
            public ClipSpec(
                string sourceClip,
                string trigger,
                string targetClip)
            {
                SourceClip = sourceClip;
                Trigger = trigger;
                TargetClip = targetClip;
            }

            public string SourceClip { get; }
            public string Trigger { get; }
            public string TargetClip { get; }
        }

        private readonly struct LocomotionSpec
        {
            public LocomotionSpec(
                string sourceClip,
                string targetClip)
            {
                SourceClip = sourceClip;
                TargetClip = targetClip;
            }

            public string SourceClip { get; }
            public string TargetClip { get; }
        }

        private readonly struct AirSpec
        {
            public AirSpec(
                string sourceClip,
                string state,
                string targetClip)
            {
                SourceClip = sourceClip;
                State = state;
                TargetClip = targetClip;
            }

            public string SourceClip { get; }
            public string State { get; }
            public string TargetClip { get; }
        }

        private readonly struct BoneMapping
        {
            public BoneMapping(string sourceName, string targetName)
            {
                SourceName = sourceName;
                TargetName = targetName;
            }

            public string SourceName { get; }
            public string TargetName { get; }
        }

        private sealed class BakeContext
        {
            public BakeContext(
                Dictionary<string, Transform> sourceBones,
                Dictionary<string, Transform> targetBones,
                Vector3 sourceHipsRestWorld,
                Vector3 targetRootRestWorld,
                float scale,
                Dictionary<string, Quaternion> sourceRestRotations,
                Dictionary<string, Quaternion> targetRestRotations,
                Dictionary<string, Vector3> targetRestLocalPositions,
                Dictionary<string, Quaternion> targetRestLocalRotations)
            {
                SourceBones = sourceBones;
                TargetBones = targetBones;
                SourceHipsRestWorld = sourceHipsRestWorld;
                TargetRootRestWorld = targetRootRestWorld;
                Scale = scale;
                SourceRestRotations = sourceRestRotations;
                TargetRestRotations = targetRestRotations;
                TargetRestLocalPositions = targetRestLocalPositions;
                TargetRestLocalRotations = targetRestLocalRotations;
            }

            public Dictionary<string, Transform> SourceBones { get; }
            public Dictionary<string, Transform> TargetBones { get; }
            public Vector3 SourceHipsRestWorld { get; }
            public Vector3 TargetRootRestWorld { get; }
            public float Scale { get; }
            public Dictionary<string, Quaternion> SourceRestRotations { get; }
            public Dictionary<string, Quaternion> TargetRestRotations { get; }
            public Dictionary<string, Vector3> TargetRestLocalPositions
            {
                get;
            }
            public Dictionary<string, Quaternion> TargetRestLocalRotations
            {
                get;
            }
        }
    }
}
