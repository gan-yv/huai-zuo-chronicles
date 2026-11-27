using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Player
{
    [DisallowMultipleComponent]
    public sealed class CharacterExpressionAnimator : MonoBehaviour
    {
        private static readonly string[] BlinkNames =
        {
            "まばたき",
            "blink"
        };

        private static readonly string[] MouthOpenNames =
        {
            "あ",
            "い",
            "う",
            "え",
            "お",
            "aa",
            "ih",
            "ou",
            "ee",
            "oh"
        };

        private readonly List<ShapeTarget> blinkTargets = new();
        private readonly List<ShapeTarget> mouthTargets = new();
        private bool isTalking;
        private float nextBlinkTime;
        private float blinkRemaining;
        private float mouthTime;
        private int activeMouthIndex = -1;

        public bool HasBlinkTargets
        {
            get
            {
                EnsureShapeTargets();
                return blinkTargets.Count > 0;
            }
        }
        public bool HasMouthTargets
        {
            get
            {
                EnsureShapeTargets();
                return mouthTargets.Count > 0;
            }
        }
        public bool IsTalking => isTalking;

        private void Awake()
        {
            CacheShapeTargets();
            ScheduleNextBlink();
        }

        private void OnEnable()
        {
            EnsureShapeTargets();
            ResetShapeWeights();
            ScheduleNextBlink();
        }

        private void OnDisable()
        {
            ResetShapeWeights();
        }

        public void SetTalking(bool talking)
        {
            EnsureShapeTargets();
            isTalking = talking && mouthTargets.Count > 0;
            if (!isTalking)
            {
                SetMouthWeights(0f);
                activeMouthIndex = -1;
            }
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (Time.unscaledTime >= nextBlinkTime)
            {
                blinkRemaining = 0.14f;
                ScheduleNextBlink();
            }
            UpdateBlink(deltaTime);
            UpdateMouth(deltaTime);
        }

        private void UpdateBlink(float deltaTime)
        {
            if (blinkRemaining <= 0f || blinkTargets.Count == 0)
            {
                return;
            }

            blinkRemaining = Mathf.Max(0f, blinkRemaining - deltaTime);
            float normalized = 1f - blinkRemaining / 0.14f;
            float weight = Mathf.Sin(normalized * Mathf.PI) * 100f;
            foreach (ShapeTarget target in blinkTargets)
            {
                SetWeight(target, weight);
            }
        }

        private void UpdateMouth(float deltaTime)
        {
            if (!isTalking || mouthTargets.Count == 0)
            {
                return;
            }

            mouthTime += deltaTime;
            int frame = Mathf.FloorToInt(mouthTime * 7.5f);
            activeMouthIndex = frame % mouthTargets.Count;
            float openness = 22f +
                Mathf.Abs(Mathf.Sin(mouthTime * 9.2f)) * 54f;
            for (int index = 0; index < mouthTargets.Count; index++)
            {
                SetWeight(
                    mouthTargets[index],
                    index == activeMouthIndex ? openness : 0f
                );
            }
        }

        private void CacheShapeTargets()
        {
            blinkTargets.Clear();
            mouthTargets.Clear();
            foreach (SkinnedMeshRenderer renderer in
                     GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0)
                {
                    continue;
                }
                for (int index = 0; index < mesh.blendShapeCount; index++)
                {
                    string name = mesh.GetBlendShapeName(index);
                    if (ContainsName(BlinkNames, name))
                    {
                        blinkTargets.Add(new ShapeTarget(renderer, index));
                    }
                    if (ContainsName(MouthOpenNames, name))
                    {
                        mouthTargets.Add(new ShapeTarget(renderer, index));
                    }
                }
            }
        }

        private void EnsureShapeTargets()
        {
            if (blinkTargets.Count == 0 && mouthTargets.Count == 0)
            {
                CacheShapeTargets();
            }
        }

        private void ResetShapeWeights()
        {
            foreach (ShapeTarget target in blinkTargets)
            {
                SetWeight(target, 0f);
            }
            SetMouthWeights(0f);
            blinkRemaining = 0f;
            activeMouthIndex = -1;
        }

        private void SetMouthWeights(float weight)
        {
            foreach (ShapeTarget target in mouthTargets)
            {
                SetWeight(target, weight);
            }
        }

        private static void SetWeight(
            ShapeTarget target,
            float weight)
        {
            if (target.Renderer != null)
            {
                target.Renderer.SetBlendShapeWeight(
                    target.Index,
                    weight
                );
            }
        }

        private void ScheduleNextBlink()
        {
            nextBlinkTime =
                Time.unscaledTime + Random.Range(2.8f, 5.8f);
        }

        private static bool ContainsName(
            string[] names,
            string candidate)
        {
            foreach (string name in names)
            {
                if (string.Equals(
                        name,
                        candidate,
                        System.StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return true;
                }
            }
            return false;
        }

        private readonly struct ShapeTarget
        {
            public ShapeTarget(
                SkinnedMeshRenderer renderer,
                int index)
            {
                Renderer = renderer;
                Index = index;
            }

            public SkinnedMeshRenderer Renderer { get; }
            public int Index { get; }
        }
    }
}
