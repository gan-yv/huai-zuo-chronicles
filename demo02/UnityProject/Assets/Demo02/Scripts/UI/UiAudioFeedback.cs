using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.UI
{
    [DefaultExecutionOrder(200)]
    public sealed class UiAudioFeedback : MonoBehaviour
    {
        private bool anyOverlayOpen;
        private bool bookOverlayOpen;

        private void Update()
        {
            UnityEngine.Object activeModal =
                GameplayUiState.ActiveModal;
            bool dialogueOpen =
                activeModal is DialogueController;
            bool bookOpen =
                activeModal is InvestigationJournalController
                || activeModal is TestimonyBoardController;
            bool overlayOpen = activeModal != null;

            if (overlayOpen && !anyOverlayOpen)
            {
                if (dialogueOpen)
                {
                    GameAudio.PlayInteraction();
                }
                else if (bookOpen)
                {
                    GameAudio.PlayBookOpen();
                }
                else
                {
                    GameAudio.PlayMenuOpen();
                }
            }
            else if (!overlayOpen && anyOverlayOpen)
            {
                if (bookOverlayOpen)
                {
                    GameAudio.PlayBookClose();
                }
                else
                {
                    GameAudio.PlayMenuClose();
                }
            }

            if (overlayOpen &&
                Demo02Input.ClickPressed)
            {
                if (bookOpen)
                {
                    GameAudio.PlayPageTurn();
                }
                else
                {
                    GameAudio.PlayClick();
                }
            }

            anyOverlayOpen = overlayOpen;
            bookOverlayOpen = bookOpen;
        }
    }
}
