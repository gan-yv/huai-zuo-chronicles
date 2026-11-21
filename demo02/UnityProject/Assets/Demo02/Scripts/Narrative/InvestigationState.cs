using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Narrative
{
    public enum ConclusionChoice
    {
        None,
        Preserve,
        Seal
    }

    public sealed class InvestigationState : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredClueCount = 3;
        [SerializeField, Min(1)] private int requiredTimeAnchorCount = 3;

        private readonly HashSet<string> collectedClueIds = new();
        private readonly HashSet<string> stabilizedTimeAnchorIds = new();

        public event Action<int, int> ClueCountChanged;
        public int CollectedClueCount => collectedClueIds.Count;
        public int RequiredClueCount => requiredClueCount;
        public bool HasAllClues => CollectedClueCount >= requiredClueCount;
        public int StabilizedTimeAnchorCount => stabilizedTimeAnchorIds.Count;
        public int RequiredTimeAnchorCount => requiredTimeAnchorCount;
        public bool HasAllTimeAnchors => StabilizedTimeAnchorCount >= requiredTimeAnchorCount;
        public bool IsRepaired { get; private set; }
        public ConclusionChoice SelectedConclusion { get; private set; }
        public bool IsConclusionReached => SelectedConclusion != ConclusionChoice.None;
        public bool HasClue(string clueId) => collectedClueIds.Contains(clueId);
        public bool HasStabilizedAnchor(string anchorId) =>
            stabilizedTimeAnchorIds.Contains(anchorId);

        public bool CollectClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !collectedClueIds.Add(clueId))
            {
                return false;
            }

            ClueCountChanged?.Invoke(CollectedClueCount, requiredClueCount);
            return true;
        }

        public bool StabilizeTimeAnchor(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId)
                || !HasAllClues
                || !stabilizedTimeAnchorIds.Add(anchorId))
            {
                return false;
            }

            return true;
        }

        public bool CompleteRepair()
        {
            if (IsRepaired || !HasAllClues)
            {
                return false;
            }

            IsRepaired = true;
            return true;
        }

        public bool SelectConclusion(ConclusionChoice choice)
        {
            if (!IsRepaired || IsConclusionReached || choice == ConclusionChoice.None)
            {
                return false;
            }

            SelectedConclusion = choice;
            return true;
        }
    }
}
