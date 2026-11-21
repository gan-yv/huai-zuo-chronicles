using System;
using System.Collections;
using UnityEngine;

namespace Demo02.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class ScreenFade : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.2f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.25f;

        private float alpha;

        public static ScreenFade Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool PlayTransition(Action midpoint)
        {
            if (IsTransitioning)
            {
                return false;
            }

            StartCoroutine(TransitionRoutine(midpoint));
            return true;
        }

        private IEnumerator TransitionRoutine(Action midpoint)
        {
            IsTransitioning = true;
            yield return FadeTo(1f, fadeOutDuration);
            midpoint?.Invoke();
            if (blackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(blackHoldDuration);
            }

            yield return FadeTo(0f, fadeInDuration);
            IsTransitioning = false;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            alpha = target;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            alpha = 0f;
            IsTransitioning = false;
        }

        private void OnGUI()
        {
            if (alpha <= 0.001f)
            {
                return;
            }

            int previousDepth = GUI.depth;
            GUI.depth = -1000;
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
