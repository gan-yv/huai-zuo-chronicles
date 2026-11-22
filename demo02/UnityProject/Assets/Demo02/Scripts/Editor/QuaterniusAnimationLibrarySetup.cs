using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Demo02.Editor
{
    public static class QuaterniusAnimationLibrarySetup
    {
        private const string AnimationAssetPath =
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/Models/" +
            "AnimationLibrary_Godot_Standard.gltf";
        private const string HeroPrefabPath =
            "Assets/Demo02/Art/Characters/Stylized/Prefabs/" +
            "VIS_Stylized_Hero.prefab";
        private const string ClipRoot =
            "Assets/Demo02/Art/Characters/Stylized/Animations";
        private const string ControllerPath =
            "Assets/Demo02/Art/Characters/Stylized/AC_StylizedCharacter.controller";
        private const string ShowcaseScenePath =
            "Assets/Demo02/Scenes/Tests/StylizedCharacterShowcase.unity";
        private const int BakeFrameRate = 30;

        private static readonly ClipSpec[] ClipSpecs =
        {
            new("Sword_Attack", "Attack", "AC_Quaternius_Attack"),
            new("Hit_Chest", "Hit", "AC_Quaternius_Hit"),
            new("Interact", "Interact", "AC_Quaternius_Interact"),
            new("PickUp_Table", "PickUp", "AC_Quaternius_PickUp"),
            new("Death01", "Die", "AC_Quaternius_Death")
        };

        private static readonly LocomotionSpec[] LocomotionSpecs =
        {
            new("Idle_Loop", "AC_Quaternius_Idle"),
            new("Walk_Loop", "AC_Quaternius_Walk"),
            new("Sprint_Loop", "AC_Quaternius_Run")
        };

        private static readonly ClipSpec TalkSpec =
            new("Idle_Talking_Loop", "Talking", "AC_Quaternius_Talk");

        private static readonly BoneMapping[] BoneMappings =
        {
            new("DEF-hips", "Hips"),
            new("DEF-spine.001", "Spine"),
            new("DEF-spine.002", "Chest"),
            new("DEF-spine.003", "UpperChest"),
            new("DEF-neck", "Neck"),
            new("DEF-head", "Head"),
            new("DEF-shoulder.L", "LeftShoulder"),
            new("DEF-upper_arm.L", "LeftArm"),
            new("DEF-forearm.L", "LeftForeArm"),
            new("DEF-hand.L", "LeftHand"),
            new("DEF-f_index.01.L", "LeftHandIndex1"),
            new("DEF-f_index.02.L", "LeftHandIndex2"),
            new("DEF-f_index.03.L", "LeftHandIndex3"),
            new("DEF-thumb.01.L", "LeftHandThumb1"),
            new("DEF-thumb.02.L", "LeftHandThumb2"),
            new("DEF-shoulder.R", "RightShoulder"),
            new("DEF-upper_arm.R", "RightArm"),
            new("DEF-forearm.R", "RightForeArm"),
            new("DEF-hand.R", "RightHand"),
            new("DEF-f_index.01.R", "RightHandIndex1"),
            new("DEF-f_index.02.R", "RightHandIndex2"),
            new("DEF-f_index.03.R", "RightHandIndex3"),
            new("DEF-thumb.01.R", "RightHandThumb1"),
            new("DEF-thumb.02.R", "RightHandThumb2"),
            new("DEF-thigh.L", "LeftUpLeg"),
            new("DEF-shin.L", "LeftLeg"),
            new("DEF-foot.L", "LeftFoot"),
            new("DEF-toe.L", "LeftToes"),
            new("DEF-thigh.R", "RightUpLeg"),
            new("DEF-shin.R", "RightLeg"),
            new("DEF-foot.R", "RightFoot"),
            new("DEF-toe.R", "RightToes")
        };

        [MenuItem("Demo02/Art/Quaternius/Rebuild Retargeted Character Actions")]
        public static void BuildAll()
        {
            EnsureFolder(ClipRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject sourcePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(AnimationAssetPath);
            GameObject targetPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            if (sourcePrefab == null)
            {
                throw new InvalidOperationException(
                    $"Animation library was not imported: {AnimationAssetPath}"
                );
            }
            if (targetPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Target character was not imported: {HeroPrefabPath}"
                );
            }

            AnimationClip[] sourceClips = AssetDatabase
                .LoadAllAssetsAtPath(AnimationAssetPath)
                .OfType<AnimationClip>()
                .ToArray();
            if (sourceClips.Length < ClipSpecs.Length)
            {
                throw new InvalidOperationException(
                    "Quaternius animation library is missing clips."
                );
            }

            GameObject source =
                (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            GameObject target =
                (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            try
            {
                Animator targetAnimator =
                    target.GetComponentInChildren<Animator>(true);
                if (targetAnimator == null)
                {
                    throw new InvalidOperationException(
                        "Target stylized character has no Animator."
                    );
                }

                BakeContext context = CreateBakeContext(
                    source,
                    targetAnimator
                );
                Dictionary<string, AnimationClip> baked =
                    new(StringComparer.Ordinal);
                foreach (ClipSpec spec in ClipSpecs)
                {
                    AnimationClip sourceClip = sourceClips.FirstOrDefault(
                        clip => string.Equals(
                            clip.name,
                            spec.SourceClip,
                            StringComparison.Ordinal
                        )
                    );
                    if (sourceClip == null)
                    {
                        throw new InvalidOperationException(
                            $"Quaternius source clip is missing: {spec.SourceClip}"
                        );
                    }

                    AnimationClip clip = BakeClip(
                        source,
                        targetAnimator,
                        context,
                        sourceClip,
                        spec.TargetClip
                    );
                    baked.Add(spec.Trigger, clip);
                    Debug.Log(
                        $"Retargeted {spec.SourceClip} -> {spec.TargetClip}: " +
                        $"{clip.length:0.00}s, {clip.frameRate:0} fps."
                    );
                }

                Dictionary<string, AnimationClip> locomotion =
                    new(StringComparer.Ordinal);
                foreach (LocomotionSpec spec in LocomotionSpecs)
                {
                    AnimationClip sourceClip = sourceClips.FirstOrDefault(
                        clip => string.Equals(
                            clip.name,
                            spec.SourceClip,
                            StringComparison.Ordinal
                        )
                    );
                    if (sourceClip == null)
                    {
                        throw new InvalidOperationException(
                            $"Quaternius locomotion clip is missing: {spec.SourceClip}"
                        );
                    }
                    AnimationClip clip = BakeClip(
                        source,
                        targetAnimator,
                        context,
                        sourceClip,
                        spec.TargetClip,
                        loop: true
                    );
                    locomotion.Add(spec.TargetClip, clip);
                    Debug.Log(
                        $"Retargeted locomotion {spec.SourceClip} -> " +
                        $"{spec.TargetClip}: {clip.length:0.00}s."
                    );
                }

                AnimationClip talkClip = BakeClip(
                    source,
                    targetAnimator,
                    context,
                    sourceClips.First(clip =>
                        string.Equals(
                            clip.name,
                            TalkSpec.SourceClip,
                            StringComparison.Ordinal
                        )
                    ),
                    TalkSpec.TargetClip,
                    loop: true
                );
                ConfigureController(baked, locomotion, talkClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(source);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Quaternius character animation set ready: " +
                $"{ClipSpecs.Length} actions and {LocomotionSpecs.Length} " +
                "locomotion clips plus dialogue."
            );
        }

        public static void ValidateForBatch()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            GameObject heroPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            if (controller == null || heroPrefab == null)
            {
                throw new InvalidOperationException(
                    "Quaternius action validation could not load the controller or hero."
                );
            }

            foreach (string parameter in new[]
                     {
                         "Attack",
                         "Hit",
                         "Interact",
                         "PickUp",
                         "Die",
                         "Talking"
                     })
            {
                if (!controller.parameters.Any(item => item.name == parameter))
                {
                    throw new InvalidOperationException(
                        $"Quaternius action controller is missing '{parameter}'."
                    );
                }
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            GameObject hero =
                (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
            try
            {
                Animator animator = hero.GetComponentInChildren<Animator>(true);
                foreach (ClipSpec spec in ClipSpecs)
                {
                    string clipPath = ClipPath(spec.TargetClip);
                    AnimationClip clip =
                        AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null || clip.length <= 0.05f)
                    {
                        throw new InvalidOperationException(
                            $"Retargeted action clip is missing: {clipPath}"
                        );
                    }

                    EditorCurveBinding[] bindings =
                        AnimationUtility.GetCurveBindings(clip);
                    if (bindings.Length < 100)
                    {
                        throw new InvalidOperationException(
                            $"Retargeted action has too few curves: {clipPath}"
                        );
                    }
                    int missing = bindings.Count(binding =>
                        animator.transform.Find(binding.path) == null
                    );
                    if (missing > 0)
                    {
                        throw new InvalidOperationException(
                            $"{clip.name} has {missing} target skeleton paths missing."
                        );
                    }

                    AnimatorState state = stateMachine.states
                        .Select(item => item.state)
                        .FirstOrDefault(item => item.name == spec.Trigger);
                    if (state == null || state.motion != clip)
                    {
                        throw new InvalidOperationException(
                            $"Controller state '{spec.Trigger}' does not use {clipPath}."
                        );
                    }
                    if (!stateMachine.anyStateTransitions.Any(
                            transition =>
                                transition.destinationState == state &&
                                transition.conditions.Any(
                                    condition =>
                                        condition.parameter == spec.Trigger
                                )
                        ))
                    {
                        throw new InvalidOperationException(
                            $"Controller has no trigger route for '{spec.Trigger}'."
                        );
                    }
                }

                foreach (LocomotionSpec spec in LocomotionSpecs)
                {
                    string path = ClipPath(spec.TargetClip);
                    AnimationClip clip =
                        AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    string stateName = spec.TargetClip switch
                    {
                        "AC_Quaternius_Idle" => "Idle",
                        "AC_Quaternius_Walk" => "Walk",
                        "AC_Quaternius_Run" => "Run",
                        _ => throw new InvalidOperationException(
                            $"Unknown locomotion target: {spec.TargetClip}"
                        )
                    };
                    AnimatorState state = stateMachine.states
                        .Select(item => item.state)
                        .FirstOrDefault(item => item.name == stateName);
                    if (clip == null ||
                        state == null ||
                        state.motion != clip ||
                        clip.wrapMode != WrapMode.Loop)
                    {
                        throw new InvalidOperationException(
                            $"Locomotion state '{stateName}' is invalid."
                        );
                    }
                }

                AnimationClip talkClip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        ClipPath(TalkSpec.TargetClip)
                    );
                AnimatorState talkState = stateMachine.states
                    .Select(item => item.state)
                    .FirstOrDefault(item => item.name == "Talk");
                if (talkClip == null ||
                    talkClip.wrapMode != WrapMode.Loop ||
                    talkState == null ||
                    talkState.motion != talkClip)
                {
                    throw new InvalidOperationException(
                        "Dialogue talking state is invalid."
                    );
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hero);
            }

            Debug.Log(
                "Quaternius action validation passed: attack, hit, interact, " +
                "pickup, death and locomotion clips resolve against the " +
                "Kenney skeleton."
            );
        }

        [MenuItem("Demo02/Art/Quaternius/Capture Animation Showcase")]
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
                    "Stylized character showcase has no Main Camera."
                );
            }

            GameObject hero = GameObject.Find("VIS_Stylized_Hero");
            GameObject guide = GameObject.Find("VIS_Stylized_Guide");
            GameObject official = GameObject.Find("VIS_Stylized_Official");
            AnimationClip attack = LoadTargetClip("Attack");
            AnimationClip interact = LoadTargetClip("Interact");
            AnimationClip hit = LoadTargetClip("Hit");

            AnimationMode.StartAnimationMode();
            try
            {
                SampleAtNormalizedTime(hero, attack, 0.72f);
                SampleAtNormalizedTime(guide, interact, 0.50f);
                SampleAtNormalizedTime(official, hit, 0.35f);
                CaptureCamera(camera, "quaternius-animation-showcase");
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        [MenuItem("Demo02/Art/Quaternius/Capture Locomotion Showcase")]
        public static void CaptureLocomotionShowcase()
        {
            EditorSceneManager.OpenScene(
                ShowcaseScenePath,
                OpenSceneMode.Single
            );
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stylized character showcase has no Main Camera."
                );
            }

            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath("AC_Quaternius_Idle")
            );
            AnimationClip walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath("AC_Quaternius_Walk")
            );
            AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath("AC_Quaternius_Run")
            );
            AnimationMode.StartAnimationMode();
            try
            {
                SampleAtNormalizedTime(
                    GameObject.Find("VIS_Stylized_Hero"),
                    run,
                    0.36f
                );
                SampleAtNormalizedTime(
                    GameObject.Find("VIS_Stylized_Guide"),
                    walk,
                    0.42f
                );
                SampleAtNormalizedTime(
                    GameObject.Find("VIS_Stylized_Official"),
                    idle,
                    0.55f
                );
                CaptureCamera(camera, "quaternius-locomotion-showcase");
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        public static void ProbeForBatch()
        {
            GameObject animationAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(AnimationAssetPath);
            if (animationAsset == null)
            {
                throw new InvalidOperationException(
                    $"Animation library was not imported: {AnimationAssetPath}"
                );
            }

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(AnimationAssetPath);
            AnimationClip[] clips = assets.OfType<AnimationClip>().ToArray();
            Debug.Log(
                $"QUATERNIUS_ANIMATION assets={assets.Length} clips={clips.Length}"
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
                        $"Quaternius skeleton is missing {mapping.SourceName}."
                    );
                }
                if (!targetBones.ContainsKey(mapping.TargetName))
                {
                    throw new InvalidOperationException(
                        $"Kenney skeleton is missing {mapping.TargetName}."
                    );
                }
            }

            Transform sourceHips = sourceBones["DEF-hips"];
            Transform targetHips = targetBones["Hips"];
            Transform sourceHead = sourceBones["DEF-head"];
            Transform targetHead = targetBones["Head"];
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
                targetHips.position,
                scale,
                sourceBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.rotation
                ),
                targetBones.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.rotation
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
            int sampleCount = Mathf.Max(
                2,
                Mathf.CeilToInt(sourceClip.length * BakeFrameRate) + 1
            );
            float timeStep = sourceClip.length / (sampleCount - 1);
            Dictionary<EditorCurveBinding, AnimationCurve> curves = new();
            Dictionary<EditorCurveBinding, Quaternion> previousRotations = new();

            AnimationMode.StartAnimationMode();
            try
            {
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    float time = sample * timeStep;
                    AnimationMode.SampleAnimationClip(source, sourceClip, time);
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
            foreach ((EditorCurveBinding binding, AnimationCurve curve) in curves)
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
            return clip;
        }

        private static void BakePose(
            Animator targetAnimator,
            BakeContext context,
            float time,
            Dictionary<EditorCurveBinding, AnimationCurve> curves,
            Dictionary<EditorCurveBinding, Quaternion> previousRotations)
        {
            Transform targetHips = context.TargetBones["Hips"];
            Transform sourceHips = context.SourceBones["DEF-hips"];
            Vector3 hipsDelta =
                (sourceHips.position - context.SourceHipsRestWorld) *
                context.Scale;
            targetHips.position =
                context.TargetHipsRestWorld + hipsDelta;
            AddPositionCurves(
                curves,
                targetAnimator.transform,
                targetHips,
                time
            );

            foreach (BoneMapping mapping in BoneMappings)
            {
                Transform sourceBone = context.SourceBones[mapping.SourceName];
                Transform targetBone = context.TargetBones[mapping.TargetName];
                Quaternion sourceDelta =
                    sourceBone.rotation *
                    Quaternion.Inverse(context.SourceRestRotations[mapping.SourceName]);
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
            string[] components = { "x", "y", "z", "w" };
            EditorCurveBinding firstBinding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                "localRotation.x"
            );
            if (previousRotations.TryGetValue(firstBinding, out Quaternion previous) &&
                Quaternion.Dot(previous, rotation) < 0f)
            {
                rotation = new Quaternion(
                    -rotation.x,
                    -rotation.y,
                    -rotation.z,
                    -rotation.w
                );
            }
            float[] values = { rotation.x, rotation.y, rotation.z, rotation.w };
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
            if (!curves.TryGetValue(binding, out AnimationCurve curve))
            {
                curve = new AnimationCurve();
                curves.Add(binding, curve);
            }
            curve.AddKey(time, value);
        }

        private static void ConfigureController(
            Dictionary<string, AnimationClip> clips,
            Dictionary<string, AnimationClip> locomotion,
            AnimationClip talkClip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Stylized character controller is missing: {ControllerPath}"
                );
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindOrAddState(stateMachine, "Idle");
            if (idle == null)
            {
                throw new InvalidOperationException(
                    "Stylized character controller has no Idle state."
                );
            }
            ConfigureLocomotion(
                controller,
                stateMachine,
                idle,
                locomotion
            );
            ConfigureTalking(
                controller,
                stateMachine,
                idle,
                talkClip
            );

            foreach ((string trigger, AnimationClip clip) in clips)
            {
                EnsureParameter(controller, trigger);
                AnimatorState state = stateMachine.states
                    .Select(item => item.state)
                    .FirstOrDefault(item => item.name == trigger);
                if (state == null)
                {
                    state = stateMachine.AddState(trigger);
                }
                state.motion = clip;
                state.speed = 1f;

                bool hasEntry = stateMachine.anyStateTransitions.Any(
                    transition => transition.destinationState == state &&
                                  transition.conditions.Any(
                                      condition =>
                                          condition.parameter == trigger
                                  )
                );
                if (!hasEntry)
                {
                    AnimatorStateTransition transition =
                        stateMachine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = 0.06f;
                    transition.canTransitionToSelf = false;
                    transition.AddCondition(
                        AnimatorConditionMode.If,
                        0f,
                        trigger
                    );
                }

                if (trigger == "Die")
                {
                    continue;
                }

                if (!state.transitions.Any(
                        transition => transition.destinationState == idle
                    ))
                {
                    AnimatorStateTransition exit = state.AddTransition(idle);
                    exit.hasExitTime = true;
                    exit.exitTime = 0.88f;
                    exit.duration = 0.08f;
                }
            }

            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureLocomotion(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            AnimatorState idle,
            Dictionary<string, AnimationClip> locomotion)
        {
            AnimatorState walk = FindOrAddState(stateMachine, "Walk");
            AnimatorState run = FindOrAddState(stateMachine, "Run");
            idle.motion = locomotion["AC_Quaternius_Idle"];
            walk.motion = locomotion["AC_Quaternius_Walk"];
            run.motion = locomotion["AC_Quaternius_Run"];
            idle.speed = 1f;
            walk.speed = 1f;
            run.speed = 1f;

            HashSet<AnimatorState> locomotionStates = new()
            {
                idle,
                walk,
                run
            };
            foreach (AnimatorState state in locomotionStates)
            {
                foreach (AnimatorStateTransition transition in
                         state.transitions.ToArray())
                {
                    if (transition.destinationState != null &&
                        locomotionStates.Contains(transition.destinationState))
                    {
                        state.RemoveTransition(transition);
                    }
                }
            }

            AnimatorStateTransition idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.14f;
            idleToWalk.AddCondition(
                AnimatorConditionMode.Greater,
                0.04f,
                "Speed"
            );

            AnimatorStateTransition walkToRun = walk.AddTransition(run);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.14f;
            walkToRun.AddCondition(
                AnimatorConditionMode.Greater,
                0.72f,
                "Speed"
            );

            AnimatorStateTransition runToWalk = run.AddTransition(walk);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.16f;
            runToWalk.AddCondition(
                AnimatorConditionMode.Less,
                0.68f,
                "Speed"
            );

            AnimatorStateTransition walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.14f;
            walkToIdle.AddCondition(
                AnimatorConditionMode.Less,
                0.04f,
                "Speed"
            );

            EnsureParameter(controller, "Speed");
        }

        private static void ConfigureTalking(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            AnimatorState idle,
            AnimationClip talkClip)
        {
            if (!controller.parameters.Any(
                    item => item.name == "Talking"
                ))
            {
                controller.AddParameter(
                    "Talking",
                    AnimatorControllerParameterType.Bool
                );
            }
            AnimatorState talk = FindOrAddState(stateMachine, "Talk");
            talk.motion = talkClip;
            talk.speed = 1f;

            bool hasEntry = stateMachine.anyStateTransitions.Any(
                transition =>
                    transition.destinationState == talk &&
                    transition.conditions.Any(
                        condition =>
                            condition.parameter == "Talking" &&
                            condition.mode == AnimatorConditionMode.If
                    )
            );
            if (!hasEntry)
            {
                AnimatorStateTransition enter =
                    stateMachine.AddAnyStateTransition(talk);
                enter.hasExitTime = false;
                enter.duration = 0.12f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    "Talking"
                );
            }

            bool hasExit = talk.transitions.Any(
                transition => transition.destinationState == idle
            );
            if (!hasExit)
            {
                AnimatorStateTransition exit = talk.AddTransition(idle);
                exit.hasExitTime = false;
                exit.duration = 0.14f;
                exit.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "Talking"
                );
            }
        }

        private static AnimatorState FindOrAddState(
            AnimatorStateMachine stateMachine,
            string name)
        {
            AnimatorState state = stateMachine.states
                .Select(item => item.state)
                .FirstOrDefault(item => item.name == name);
            return state ?? stateMachine.AddState(name);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameter)
        {
            if (controller.parameters.Any(item => item.name == parameter))
            {
                return;
            }
            controller.AddParameter(
                parameter,
                AnimatorControllerParameterType.Trigger
            );
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
            Animator animator = character.GetComponentInChildren<Animator>(true);
            AnimationMode.SampleAnimationClip(
                animator.gameObject,
                clip,
                clip.length * normalizedTime
            );
        }

        private static AnimationClip LoadTargetClip(string trigger)
        {
            ClipSpec spec = ClipSpecs.First(item => item.Trigger == trigger);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipPath(spec.TargetClip)
            );
        }

        private static void CaptureCamera(Camera camera, string fileName)
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
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            RenderTexture.active = previous;
            string path = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../../..",
                    "demo02/docs/screenshots",
                    fileName + ".png"
                )
            );
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(screenshot);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log($"Quaternius animation showcase captured at {path}");
        }

        private static string ClipPath(string clipName) =>
            $"{ClipRoot}/{clipName}.anim";

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

        private sealed class BakeContext
        {
            public BakeContext(
                Dictionary<string, Transform> sourceBones,
                Dictionary<string, Transform> targetBones,
                Vector3 sourceHipsRestWorld,
                Vector3 targetHipsRestWorld,
                float scale,
                Dictionary<string, Quaternion> sourceRestRotations,
                Dictionary<string, Quaternion> targetRestRotations)
            {
                SourceBones = sourceBones;
                TargetBones = targetBones;
                SourceHipsRestWorld = sourceHipsRestWorld;
                TargetHipsRestWorld = targetHipsRestWorld;
                Scale = scale;
                SourceRestRotations = sourceRestRotations;
                TargetRestRotations = targetRestRotations;
            }

            public Dictionary<string, Transform> SourceBones { get; }
            public Dictionary<string, Transform> TargetBones { get; }
            public Vector3 SourceHipsRestWorld { get; }
            public Vector3 TargetHipsRestWorld { get; }
            public float Scale { get; }
            public Dictionary<string, Quaternion> SourceRestRotations { get; }
            public Dictionary<string, Quaternion> TargetRestRotations { get; }
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
            public LocomotionSpec(string sourceClip, string targetClip)
            {
                SourceClip = sourceClip;
                TargetClip = targetClip;
            }

            public string SourceClip { get; }
            public string TargetClip { get; }
        }
    }
}
