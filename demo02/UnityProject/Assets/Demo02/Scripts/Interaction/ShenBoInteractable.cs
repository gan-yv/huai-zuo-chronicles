using System.Collections;
using System.IO;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class ShenBoInteractable : MonoBehaviour, IInteractable
    {
        private InvestigationState investigationState;

        public bool CanInteract => true;

        public string Prompt
        {
            get
            {
                DialogueScriptDefinition script =
                    investigationState?.CaseContent?.FindDialogueScript(
                        "shenbo",
                        investigationState
                    );
                return script != null
                    ? script.InteractionPrompt
                    : "E  与守仓人交谈";
            }
        }

        private void Start()
        {
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
            if (System.Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-choice"
            ) >= 0)
            {
                StartCoroutine(CaptureChoicePreview());
            }
        }

        private IEnumerator CaptureChoicePreview()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            DialogueController dialogue = Demo02.Core.GameServices.Resolve<DialogueController>();
            if (dialogue == null || investigationState == null)
            {
                yield break;
            }

            DialogueContentRuntime.TryPlay(
                investigationState.CaseContent,
                investigationState,
                dialogue,
                "shenbo",
                transform,
                forceShowChoices: true
            );

            string path = GetArgumentValue("--capture-choice=");
            if (string.IsNullOrWhiteSpace(path))
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Dialogue choice preview captured to {path}");
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

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController dialogue = Demo02.Core.GameServices.Resolve<DialogueController>();
            if (dialogue == null || investigationState == null)
            {
                return "沈伯似乎已经听不见你的声音。";
            }

            return DialogueContentRuntime.TryPlay(
                investigationState.CaseContent,
                investigationState,
                dialogue,
                "shenbo",
                transform
            )
                ? string.Empty
                : "沈伯没有可说的内容。";
        }
    }
}
