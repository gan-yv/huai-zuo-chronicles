using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class InvestigationVisionController : MonoBehaviour
    {
        private MonoBehaviour[] interactableBehaviours = new MonoBehaviour[0];
        private MemoryEchoInteractable[] echoes = new MemoryEchoInteractable[0];

        public static InvestigationVisionController Instance { get; private set; }
        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (System.Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-vision"
            ) >= 0)
            {
                ToggleVision();
                string capturePath = GetArgumentValue("--capture-vision=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureVision(capturePath));
                }
            }
        }

        private void Update()
        {
            if (!Demo02Input.VisionPressed)
            {
                return;
            }

            if (Time.timeScale <= 0f
                || GameplayUiState.IsGameplayBlocked
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            ToggleVision();
        }

        public void ToggleVision()
        {
            IsActive = !IsActive;
            if (IsActive)
            {
                RefreshTargets();
            }
            else
            {
                ApplyEchoVisibility();
            }

            GameAudio.PlayInteraction();
        }

        private void RefreshTargets()
        {
            interactableBehaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            echoes = FindObjectsByType<MemoryEchoInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            ApplyEchoVisibility();
        }

        private void ApplyEchoVisibility()
        {
            foreach (MemoryEchoInteractable echo in echoes)
            {
                if (echo != null)
                {
                    echo.SetRevealed(IsActive);
                }
            }
        }

        private void OnGUI()
        {
            if (!IsActive
                || GameplayUiState.IsGameplayBlocked)
            {
                return;
            }

            if (interactableBehaviours.Length == 0)
            {
                RefreshTargets();
            }

            GUI.depth = -40;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.22f, 0.15f, 0.035f, 0.08f)
            );

            float scale = UiTheme.Scale;
            GUI.Label(
                new Rect(
                    Screen.width * 0.5f - 180f * scale,
                    132f * scale,
                    360f * scale,
                    32f * scale
                ),
                "修痕视界  [V]",
                UiTheme.Label(13, TextAnchor.MiddleCenter, bold: true)
            );

            Camera sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                return;
            }

            Transform player = FindAnyObjectByType<PlayerPartyController>()?.ActiveCharacter;
            if (player == null)
            {
                return;
            }

            List<MarkerTarget> targets = new();
            foreach (MonoBehaviour behaviour in interactableBehaviours)
            {
                if (behaviour == null
                    || behaviour is not IInteractable interactable
                    || !interactable.CanInteract)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    player.position,
                    behaviour.transform.position
                );
                float maxDistance = behaviour is MemoryEchoInteractable ? 35f : 19f;
                if (distance > maxDistance)
                {
                    continue;
                }

                targets.Add(new MarkerTarget(
                    behaviour,
                    interactable,
                    distance,
                    GetPriority(behaviour)
                ));
            }

            targets.Sort((left, right) =>
            {
                int priorityComparison = left.Priority.CompareTo(right.Priority);
                return priorityComparison != 0
                    ? priorityComparison
                    : left.Distance.CompareTo(right.Distance);
            });

            int drawnTargets = 0;
            foreach (MarkerTarget target in targets)
            {
                if (drawnTargets >= 5)
                {
                    break;
                }

                MonoBehaviour behaviour = target.Behaviour;
                Vector3 worldPoint = behaviour.transform.position + Vector3.up * 1.25f;
                Vector3 screenPoint = sceneCamera.WorldToScreenPoint(worldPoint);
                if (screenPoint.z <= 0f
                    || screenPoint.x < 0f
                    || screenPoint.x > Screen.width
                    || screenPoint.y < 0f
                    || screenPoint.y > Screen.height)
                {
                    continue;
                }

                string label = GetLabel(behaviour, target.Interactable);
                Rect marker = new(
                    screenPoint.x - 60f * scale,
                    Screen.height - screenPoint.y - 13f * scale,
                    120f * scale,
                    26f * scale
                );
                Color markerColor = GetMarkerColor(behaviour);
                UiTheme.DrawRect(marker, new Color(0.03f, 0.04f, 0.05f, 0.86f));
                UiTheme.DrawBorder(marker, markerColor);
                GUIStyle style = UiTheme.Label(10, TextAnchor.MiddleCenter, bold: true);
                style.normal.textColor = markerColor;
                GUI.Label(marker, $"{label}  {target.Distance:0.#}m", style);
                drawnTargets++;
            }
        }

        private static int GetPriority(MonoBehaviour behaviour)
        {
            return behaviour switch
            {
                ClueInteractable => 0,
                MemoryEchoInteractable => 1,
                ShenBoInteractable => 2,
                TimeAnchor => 3,
                RepairSite => 3,
                EndingChoiceInteractable => 3,
                NpcDialogueInteractable => 4,
                LocalWorldPortal => 5,
                _ => 6
            };
        }

        private static string GetLabel(MonoBehaviour behaviour, IInteractable interactable)
        {
            return behaviour switch
            {
                MemoryEchoInteractable => "残留回声",
                ClueInteractable => "证据",
                NpcDialogueInteractable => "人物",
                ShenBoInteractable => "沈伯",
                TimeAnchor => "时间锚点",
                RepairSite => "时空裂缝",
                EndingChoiceInteractable => "抉择",
                LocalWorldPortal => "街区通道",
                _ => interactable.Prompt
            };
        }

        private static Color GetMarkerColor(MonoBehaviour behaviour)
        {
            return behaviour switch
            {
                MemoryEchoInteractable => new Color(0.98f, 0.76f, 0.3f),
                ClueInteractable => new Color(0.66f, 0.84f, 0.98f),
                NpcDialogueInteractable => new Color(0.55f, 0.86f, 0.72f),
                ShenBoInteractable => new Color(0.55f, 0.86f, 0.72f),
                TimeAnchor => new Color(0.78f, 0.56f, 0.96f),
                RepairSite => new Color(0.95f, 0.42f, 0.42f),
                EndingChoiceInteractable => new Color(0.96f, 0.72f, 0.34f),
                _ => UiTheme.Text
            };
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CaptureVision(string path)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Investigation vision preview captured to {path}");
        }

        private void OnDisable()
        {
            foreach (MemoryEchoInteractable echo in echoes)
            {
                if (echo != null)
                {
                    echo.SetRevealed(false);
                }
            }

            IsActive = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private readonly struct MarkerTarget
        {
            public MarkerTarget(
                MonoBehaviour behaviour,
                IInteractable interactable,
                float distance,
                int priority)
            {
                Behaviour = behaviour;
                Interactable = interactable;
                Distance = distance;
                Priority = priority;
            }

            public MonoBehaviour Behaviour { get; }
            public IInteractable Interactable { get; }
            public float Distance { get; }
            public int Priority { get; }
        }
    }
}
