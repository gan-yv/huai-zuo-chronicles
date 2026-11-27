using System.Collections.Generic;
using Demo02.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class CombatFeedbackHud : MonoBehaviour
    {
        private sealed class DamageView
        {
            public GameObject Root;
            public Text Text;
            public CanvasGroup Group;
            public bool InUse;
        }

        private sealed class DamageEntry
        {
            public Vector3 Position;
            public float Amount;
            public bool IsReaction;
            public bool IsPlayerDamage;
            public float StartedAt;
            public float Offset;
            public DamageView View;
        }

        private const float Lifetime = 0.9f;
        private readonly List<DamageEntry> entries = new();
        private readonly List<DamageView> pool = new();
        private GameObject canvasObject;
        private Canvas canvas;
        private Image hurtOverlay;
        private float hurtUntil;

        private void Awake()
        {
            EnsureUi();
        }

        private void OnEnable()
        {
            CombatFeedback.DamageReported += OnDamageReported;
            CombatFeedback.PlayerHurt += OnPlayerHurt;
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        private void OnDisable()
        {
            CombatFeedback.DamageReported -= OnDamageReported;
            CombatFeedback.PlayerHurt -= OnPlayerHurt;
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (canvasObject == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(canvasObject);
            }
            else
            {
                DestroyImmediate(canvasObject);
            }
            canvasObject = null;
        }

        private void Update()
        {
            EnsureUi();
            bool visible = !GameplayUiState.IsGameplayBlocked
                && Camera.main != null;
            canvas.enabled = visible;
            if (!visible)
            {
                return;
            }

            RefreshHurtOverlay();
            RefreshDamageEntries();
        }

        private void OnDamageReported(DamageFeedbackData data)
        {
            if (data == null)
            {
                return;
            }
            if (entries.Count >= 40)
            {
                Release(entries[0]);
                entries.RemoveAt(0);
            }

            DamageView view = GetView();
            view.InUse = true;
            view.Root.SetActive(true);
            entries.Add(new DamageEntry
            {
                Position = data.WorldPosition,
                Amount = data.Amount,
                IsReaction = data.IsReaction,
                IsPlayerDamage = data.IsPlayerDamage,
                StartedAt = Time.unscaledTime,
                Offset = Random.Range(-0.35f, 0.35f),
                View = view
            });
        }

        private void OnPlayerHurt()
        {
            hurtUntil = Time.unscaledTime + 0.2f;
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "CombatFeedbackCanvas",
                23
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();

            hurtOverlay = HudUiFactory.CreateImage(
                root,
                "HurtOverlay",
                Color.clear
            );
            HudUiFactory.Stretch(hurtOverlay.rectTransform);
            hurtOverlay.gameObject.SetActive(false);
            CreateView();
        }

        private DamageView GetView()
        {
            foreach (DamageView view in pool)
            {
                if (!view.InUse)
                {
                    return view;
                }
            }
            return CreateView();
        }

        private DamageView CreateView()
        {
            RectTransform root = HudUiFactory.CreatePanel(
                canvasObject.transform,
                "DamageEntry" + pool.Count,
                Vector2.zero,
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(96f, 30f),
                Color.clear
            );
            Text text = HudUiFactory.CreateText(
                root,
                "Value",
                string.Empty,
                15,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            Outline shadow = text.gameObject.AddComponent<Outline>();
            shadow.effectDistance = new Vector2(1f, -2f);
            shadow.useGraphicAlpha = true;
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            CanvasGroup group =
                root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            DamageView view = new()
            {
                Root = root.gameObject,
                Text = text,
                Group = group
            };
            view.Root.SetActive(false);
            pool.Add(view);
            return view;
        }

        private void Release(DamageEntry entry)
        {
            if (entry?.View == null)
            {
                return;
            }
            entry.View.InUse = false;
            entry.View.Root.SetActive(false);
        }

        private void RefreshDamageEntries()
        {
            Camera sceneCamera = Camera.main;
            for (int index = entries.Count - 1; index >= 0; index--)
            {
                DamageEntry entry = entries[index];
                float age = Time.unscaledTime - entry.StartedAt;
                if (age >= Lifetime)
                {
                    Release(entry);
                    entries.RemoveAt(index);
                    continue;
                }

                Vector3 screen = sceneCamera.WorldToScreenPoint(
                    entry.Position
                        + Vector3.up * (1.5f + age * 0.8f)
                );
                if (screen.z <= 0f)
                {
                    entry.View.Root.SetActive(false);
                    continue;
                }

                entry.View.Root.SetActive(true);
                entry.View.Root.transform.position = new Vector3(
                    screen.x + entry.Offset * 20f,
                    Screen.height - screen.y,
                    0f
                );
                entry.View.Group.alpha = Mathf.Clamp01(
                    1f - age / Lifetime
                );
                entry.View.Text.color = entry.IsPlayerDamage
                    ? new Color(1f, 0.36f, 0.26f)
                    : entry.IsReaction
                        ? new Color(1f, 0.72f, 0.28f)
                        : Color.white;
                string value = entry.IsReaction
                    ? $"时势 +{Mathf.RoundToInt(entry.Amount)}"
                    : $"{Mathf.RoundToInt(entry.Amount)}";
                if (entry.View.Text.text != value)
                {
                    entry.View.Text.text = value;
                }
            }
        }

        private void RefreshHurtOverlay()
        {
            bool visible = Time.unscaledTime < hurtUntil;
            hurtOverlay.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }
            float alpha = Mathf.Clamp01(
                (hurtUntil - Time.unscaledTime) / 0.2f
            );
            hurtOverlay.color =
                new Color(0.55f, 0.03f, 0.02f, 0.08f * alpha);
        }
    }
}
