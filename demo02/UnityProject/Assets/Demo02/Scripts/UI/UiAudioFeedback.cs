using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    [DefaultExecutionOrder(200)]
    public sealed class UiAudioFeedback : MonoBehaviour
    {
        private bool anyOverlayOpen;
        private bool bookOverlayOpen;

        private void Update()
        {
            bool dialogueOpen =
                DialogueController.Instance != null &&
                DialogueController.Instance.IsOpen;
            bool bookOpen =
                (InvestigationJournalController.Instance != null &&
                 InvestigationJournalController.Instance.IsOpen) ||
                (TestimonyBoardController.Instance != null &&
                 TestimonyBoardController.Instance.IsOpen);
            bool menuOpen =
                (WorldMapController.Instance != null &&
                 WorldMapController.Instance.IsOpen) ||
                (InventoryController.Instance != null &&
                 InventoryController.Instance.IsOpen) ||
                (SettingsController.Instance != null &&
                 SettingsController.Instance.IsOpen) ||
                (SaveSlotController.Instance != null &&
                 SaveSlotController.Instance.IsOpen);
            bool overlayOpen = dialogueOpen || bookOpen || menuOpen;

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
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
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
