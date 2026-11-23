using Demo02.Core;
using UnityEngine;

namespace Demo02.Combat
{
    public sealed class AbilityPulseVisual : MonoBehaviour
    {
        private const float Duration = 0.42f;

        private ColorTint colorTint;
        private Vector3 startScale;
        private Vector3 targetScale;
        private float elapsed;
        private bool playing;

        private void Awake()
        {
            CacheComponents();
            transform.localScale = new Vector3(0.3f, 0.03f, 0.3f);
            playing = false;
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                eased
            );
            if (progress >= 1f)
            {
                playing = false;
                gameObject.SetActive(false);
            }
        }

        public void Play(Color color, float radius)
        {
            gameObject.SetActive(true);
            CacheComponents();
            colorTint?.Configure(color, 0.12f);
            elapsed = 0f;
            playing = true;
            startScale = new Vector3(0.3f, 0.035f, 0.3f);
            targetScale = new Vector3(
                Mathf.Max(0.5f, radius * 2f),
                0.035f,
                Mathf.Max(0.5f, radius * 2f)
            );
            transform.localScale = startScale;
        }

        private void CacheComponents()
        {
            colorTint ??= GetComponent<ColorTint>();
        }
    }
}
