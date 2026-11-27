using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demo02.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Demo02.Editor
{
    public static class AnimeFishAnimationSetup
    {
        private const string ArtRoot =
            "Assets/Demo02/Art/Characters/AnimeFishPilot";
        private const string ClipRoot = ArtRoot + "/Animations";
        private const string ControllerPath =
            ArtRoot + "/AC_AnimeFish.controller";
        private const string AnimationAssetPath =
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/Models/" +
            "AnimationLibrary_Godot_Standard.gltf";
        private const int BakeFrameRate = 30;

        private static readonly LocomotionSpec[] LocomotionSpecs =
        {
            new("Idle_Loop", "AC_AnimeFish_Idle"),
            new("Walk_Loop", "AC_AnimeFish_Walk"),
            new("Sprint_Loop", "AC_AnimeFish_Run")
        };

        private static readonly BoneMapping[] BoneMappings =
        {
            new("DEF-hips", "hips"),
            new("DEF-spine.001", "spine"),
            new("DEF-spine.002", "chest"),
            new("DEF-neck", "neck"),
            new("DEF-head", "head"),
            new("DEF-shoulder.L", "shoulder.L"),
            new("DEF-upper_arm.L", "upper_arm.L"),
            new("DEF-forearm.L", "lower_arm.L"),
            new("DEF-hand.L", "hand.L"),
            new("DEF-shoulder.R", "shoulder.R"),
            new("DEF-upper_arm.R", "upper_arm.R"),
            new("DEF-forearm.R", "lower_arm.R"),
            new("DEF-hand.R", "hand.R"),
            new("DEF-thigh.L", "upper_leg.L"),
            new("DEF-shin.L", "lower_leg.L"),
            new("DEF-foot.L", "foot.L"),
            new("DEF-toe.L", "toes.L"),
            new("DEF-thigh.R", "upper_leg.R"),
            new("DEF-shin.R", "lower_leg.R"),
            new("DEF-foot.R", "foot.R"),
            new("DEF-toe.R", "toes.R")
        };

        public static void Build(GameObject root, GameObject model)
        {
            EnsureFolder(ClipRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject animationSource =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnimationAssetPath
                );
            if (animationSource == null)
            {
                throw new InvalidOperationException(
                    $"Quaternius animation library was not imported: " +
                    AnimationAssetPath
                );
            }

            Animator animator =
                model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GameObject source = null;
            try
            {
                source =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        animationSource
                    );
                AnimationClip[] sourceClips = AssetDatabase
                    .LoadAllAssetsAtPath(AnimationAssetPath)
                    .OfType<AnimationClip>()
                    .ToArray();
                BakeContext context = CreateBakeContext(source, animator);
                Dictionary<string, AnimationClip> clips =
                    new(StringComparer.Ordinal);

                foreach (LocomotionSpec spec in LocomotionSpecs)
                {
                    AnimationClip clip = BakeClip(
                        source,
                        animator,
                        context,
                        FindSourceClip(sourceClips, spec.SourceClip),
                        spec.TargetClip,
                        loop: true
                    );
                    clips.Add(spec.TargetClip, clip);
                }
                AnimationClip talk = BakeClip(
                    source,
                    animator,
                    context,
                    FindSourceClip(
                        sourceClips,
                        "Idle_Talking_Loop"
                    ),
                    "AC_AnimeFish_Talk",
                    loop: true
                );
                AnimationClip alert = BakeClip(
                    source,
                    animator,
                    context,
                    FindSourceClip(sourceClips, "Hit_Chest"),
                    "AC_AnimeFish_Alert",
                    loop: false
                );
                ConfigureController(clips, talk, alert);
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        ControllerPath
                    );
                RestoreRestPose(context);
            }
            finally
            {
                if (source != null)
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            ReadyMadeCharacterAnimator driver =
                root.GetComponent<ReadyMadeCharacterAnimator>();
            if (driver == null)
            {
                driver = root.AddComponent<ReadyMadeCharacterAnimator>();
            }
            if (root.GetComponent<CharacterExpressionAnimator>() == null)
            {
                root.AddComponent<CharacterExpressionAnimator>();
            }
            SerializedObject serializedDriver = new(driver);
            SerializedProperty fullSpeed =
                serializedDriver.FindProperty("fullSpeed");
            if (fullSpeed != null)
            {
                fullSpeed.floatValue = 5.2f;
                serializedDriver.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void ValidateForBatch()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnimeFishPilotSetup.PrefabPath
                );
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                );
            if (prefab == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Anime fish prefab or movement controller is missing."
                );
            }
            if (prefab.GetComponent<ReadyMadeCharacterAnimator>() == null)
            {
                throw new InvalidOperationException(
                    "Anime fish prefab has no animation driver."
                );
            }
            CharacterExpressionAnimator expressions =
                prefab.GetComponent<CharacterExpressionAnimator>();
            if (expressions == null ||
                !expressions.HasBlinkTargets ||
                !expressions.HasMouthTargets)
            {
                throw new InvalidOperationException(
                    "Anime fish facial expression targets are missing."
                );
            }
            foreach (string parameter in new[]
                     {
                         "Speed",
                         "Talking",
                         "Alert"
                     })
            {
                if (!controller.parameters.Any(
                        item => item.name == parameter
                    ))
                {
                    throw new InvalidOperationException(
                        $"Anime fish controller is missing {parameter}."
                    );
                }
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Animator animator =
                    instance.GetComponentInChildren<Animator>(true);
                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(
                        true
                    );
                if (animator == null ||
                    animator.runtimeAnimatorController != controller ||
                    renderer == null ||
                    renderer.bones.Length < 100)
                {
                    throw new InvalidOperationException(
                        "Anime fish skeleton or Animator did not import."
                    );
                }

                foreach (string name in new[]
                         {
                             "AC_AnimeFish_Idle",
                             "AC_AnimeFish_Walk",
                             "AC_AnimeFish_Run",
                             "AC_AnimeFish_Talk",
                             "AC_AnimeFish_Alert"
                         })
                {
                    AnimationClip clip = LoadClip(name);
                    EditorCurveBinding[] bindings =
                        AnimationUtility.GetCurveBindings(clip);
                    if (clip == null ||
                        clip.length <= 0.05f ||
                        bindings.Length < 80)
                    {
                        throw new InvalidOperationException(
                            $"Anime fish clip is incomplete: {name}"
                        );
                    }
                    int missing = bindings.Count(
                        binding =>
                            animator.transform.Find(binding.path) == null
                    );
                    if (missing > 0)
                    {
                        throw new InvalidOperationException(
                            $"{name} has {missing} missing fish bone paths."
                        );
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log(
                "Anime fish validation passed: idle, walk, run, talk and " +
                "alert clips resolve against the fish skeleton."
            );
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
                        $"Anime fish skeleton is missing " +
                        $"{mapping.TargetName}."
                    );
                }
            }
            foreach (string required in new[] { "root", "hips", "head" })
            {
                if (!targetBones.ContainsKey(required))
                {
                    throw new InvalidOperationException(
                        $"Anime fish skeleton is missing {required}."
                    );
                }
            }

            Transform sourceHips = sourceBones["DEF-hips"];
            Transform sourceHead = sourceBones["DEF-head"];
            Transform targetHips = targetBones["hips"];
            Transform targetHead = targetBones["head"];
            Transform targetRoot = targetBones["root"];
            float sourceSpineLength =
                (sourceHead.position - sourceHips.position).magnitude;
            float targetSpineLength =
                (targetHead.position - targetHips.position).magnitude;
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
            bool loop)
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
            Transform targetRoot = context.TargetBones["root"];
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
            AddCurve(curves, path, "localPosition.x", bone.localPosition.x, time);
            AddCurve(curves, path, "localPosition.y", bone.localPosition.y, time);
            AddCurve(curves, path, "localPosition.z", bone.localPosition.z, time);
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
            Dictionary<string, AnimationClip> locomotion,
            AnimationClip talk,
            AnimationClip alert)
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
                "Alert",
                AnimatorControllerParameterType.Trigger
            );

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = locomotion["AC_AnimeFish_Idle"];
            AnimatorState walk = stateMachine.AddState("Walk");
            walk.motion = locomotion["AC_AnimeFish_Walk"];
            AnimatorState run = stateMachine.AddState("Run");
            run.motion = locomotion["AC_AnimeFish_Run"];
            AnimatorState talkState = stateMachine.AddState("Talk");
            talkState.motion = talk;
            AnimatorState alertState = stateMachine.AddState("Alert");
            alertState.motion = alert;
            stateMachine.defaultState = idle;

            AddTransition(
                idle,
                walk,
                AnimatorConditionMode.Greater,
                0.04f,
                0.14f
            );
            AddTransition(
                walk,
                run,
                AnimatorConditionMode.Greater,
                0.72f,
                0.14f
            );
            AddTransition(
                run,
                walk,
                AnimatorConditionMode.Less,
                0.68f,
                0.16f
            );
            AddTransition(
                walk,
                idle,
                AnimatorConditionMode.Less,
                0.04f,
                0.14f
            );

            AnimatorStateTransition enterTalk =
                stateMachine.AddAnyStateTransition(talkState);
            enterTalk.hasExitTime = false;
            enterTalk.duration = 0.12f;
            enterTalk.canTransitionToSelf = false;
            enterTalk.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Talking"
            );
            AnimatorStateTransition exitTalk =
                talkState.AddTransition(idle);
            exitTalk.hasExitTime = false;
            exitTalk.duration = 0.14f;
            exitTalk.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Talking"
            );
            AnimatorStateTransition enterAlert =
                stateMachine.AddAnyStateTransition(alertState);
            enterAlert.hasExitTime = false;
            enterAlert.duration = 0.08f;
            enterAlert.canTransitionToSelf = false;
            enterAlert.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Alert"
            );
            AnimatorStateTransition exitAlert =
                alertState.AddTransition(idle);
            exitAlert.hasExitTime = true;
            exitAlert.exitTime = 0.82f;
            exitAlert.duration = 0.1f;
            EditorUtility.SetDirty(controller);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            float threshold,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(mode, threshold, "Speed");
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

        private static AnimationClip LoadClip(string name) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath(name)
            );

        private static string ClipPath(string name) =>
            $"{ClipRoot}/{name}.anim";

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
