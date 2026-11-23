using Demo02.Core;
using Demo02.Narrative;

namespace Demo02.UI
{
    public static class GameplayUiState
    {
        public static bool IsModalOpen =>
            (DialogueController.Instance != null
                && DialogueController.Instance.IsOpen)
            || (WorldMapController.Instance != null
                && WorldMapController.Instance.IsOpen)
            || (InvestigationJournalController.Instance != null
                && InvestigationJournalController.Instance.IsOpen)
            || (InventoryController.Instance != null
                && InventoryController.Instance.IsOpen)
            || (SettingsController.Instance != null
                && SettingsController.Instance.IsOpen)
            || (TestimonyBoardController.Instance != null
                && TestimonyBoardController.Instance.IsOpen)
            || (SaveSlotController.Instance != null
                && SaveSlotController.Instance.IsOpen);

        public static bool IsGameplayBlocked =>
            IsModalOpen
            || (GameFlowController.Instance != null
                && (GameFlowController.Instance.IsPaused
                    || GameFlowController.Instance.IsGameEnding));
    }
}
