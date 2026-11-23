using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Interaction
{
    public enum PaperKiteInteractionKind
    {
        Storyteller,
        PresentTrace,
        PastTrace,
        WindlineKnot
    }

    public sealed class PaperKiteStoryInteractable :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField] private PaperKiteStoryController story;
        [SerializeField] private PaperKiteInteractionKind kind;

        public bool CanInteract
        {
            get
            {
                if (story == null)
                {
                    return false;
                }

                PaperKiteStoryStage stage = story.CurrentStage;
                return kind switch
                {
                    PaperKiteInteractionKind.Storyteller => true,
                    PaperKiteInteractionKind.PresentTrace =>
                        stage == PaperKiteStoryStage.FindTraces
                        && story.CurrentLayer == TimeLayer.Present
                        && !story.HasPresentTrace,
                    PaperKiteInteractionKind.PastTrace =>
                        stage == PaperKiteStoryStage.FindTraces
                        && story.CurrentLayer == TimeLayer.Past
                        && !story.HasPastTrace,
                    PaperKiteInteractionKind.WindlineKnot =>
                        stage == PaperKiteStoryStage.Knot
                        || stage == PaperKiteStoryStage.Choice,
                    _ => false
                };
            }
        }

        public string Prompt
        {
            get
            {
                if (story == null)
                {
                    return string.Empty;
                }

                return kind switch
                {
                    PaperKiteInteractionKind.Storyteller =>
                        story.GetStorytellerPrompt(),
                    PaperKiteInteractionKind.PresentTrace =>
                        "E  取下茶肆门框上的纸鸢线",
                    PaperKiteInteractionKind.PastTrace =>
                        "E  读取旧时风铃里的名字",
                    PaperKiteInteractionKind.WindlineKnot =>
                        story.GetKnotPrompt(),
                    _ => string.Empty
                };
            }
        }

        public void Configure(
            PaperKiteStoryController controller,
            PaperKiteInteractionKind interactionKind)
        {
            story = controller;
            kind = interactionKind;
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (story == null)
            {
                return "这段旧信尚未接入世界。";
            }

            switch (kind)
            {
                case PaperKiteInteractionKind.Storyteller:
                    story.InteractStoryteller();
                    return string.Empty;
                case PaperKiteInteractionKind.PresentTrace:
                    story.BeginPresentTrace();
                    return string.Empty;
                case PaperKiteInteractionKind.PastTrace:
                    story.BeginPastTrace();
                    return string.Empty;
                case PaperKiteInteractionKind.WindlineKnot:
                    return story.BeginKnotInteraction();
                default:
                    return string.Empty;
            }
        }
    }
}
