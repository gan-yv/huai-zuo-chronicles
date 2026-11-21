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

    public enum ChapterStage
    {
        Arrival,
        Investigate,
        ReviewEvidence,
        StabilizeAnchors,
        RepairRift,
        FinalDecision,
        Resolved
    }

    public sealed class InvestigationState : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredClueCount = 3;
        [SerializeField, Min(1)] private int requiredTimeAnchorCount = 3;

        private readonly HashSet<string> collectedClueIds = new();
        private readonly HashSet<string> stabilizedTimeAnchorIds = new();
        private readonly HashSet<string> discoveredEchoIds = new();

        public event Action<int, int> ClueCountChanged;
        public event Action<ChapterStage> StageChanged;
        public ChapterStage Stage { get; private set; } = ChapterStage.Arrival;
        public int CollectedClueCount => collectedClueIds.Count;
        public int RequiredClueCount => requiredClueCount;
        public bool HasAllClues => CollectedClueCount >= requiredClueCount;
        public int StabilizedTimeAnchorCount => stabilizedTimeAnchorIds.Count;
        public int RequiredTimeAnchorCount => requiredTimeAnchorCount;
        public bool HasAllTimeAnchors => StabilizedTimeAnchorCount >= requiredTimeAnchorCount;
        public int DiscoveredEchoCount => discoveredEchoIds.Count;
        public bool IsRepaired { get; private set; }
        public ConclusionChoice SelectedConclusion { get; private set; }
        public bool IsConclusionReached => SelectedConclusion != ConclusionChoice.None;
        public bool CanInvestigateClues => Stage >= ChapterStage.Investigate;
        public bool HasReviewedEvidence => Stage >= ChapterStage.StabilizeAnchors;
        public bool HasClue(string clueId) => collectedClueIds.Contains(clueId);
        public bool HasStabilizedAnchor(string anchorId) =>
            stabilizedTimeAnchorIds.Contains(anchorId);
        public bool HasEcho(string echoId) => discoveredEchoIds.Contains(echoId);

        public bool CollectClue(string clueId)
        {
            if (!CanInvestigateClues
                || string.IsNullOrWhiteSpace(clueId)
                || !collectedClueIds.Add(clueId))
            {
                return false;
            }

            ClueCountChanged?.Invoke(CollectedClueCount, requiredClueCount);
            if (HasAllClues)
            {
                SetStage(ChapterStage.ReviewEvidence);
            }

            return true;
        }

        public bool CompleteWitnessBriefing()
        {
            if (Stage != ChapterStage.Arrival)
            {
                return false;
            }

            SetStage(ChapterStage.Investigate);
            return true;
        }

        public bool CompleteEvidenceReview()
        {
            if (Stage != ChapterStage.ReviewEvidence || !HasAllClues)
            {
                return false;
            }

            SetStage(ChapterStage.StabilizeAnchors);
            return true;
        }

        public bool StabilizeTimeAnchor(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId)
                || !HasReviewedEvidence
                || !HasAllClues
                || !stabilizedTimeAnchorIds.Add(anchorId))
            {
                return false;
            }

            if (HasAllTimeAnchors)
            {
                SetStage(ChapterStage.RepairRift);
            }

            return true;
        }

        public bool CompleteRepair()
        {
            if (IsRepaired
                || Stage < ChapterStage.RepairRift
                || !HasAllClues
                || !HasAllTimeAnchors)
            {
                return false;
            }

            IsRepaired = true;
            SetStage(ChapterStage.FinalDecision);
            return true;
        }

        public bool SelectConclusion(ConclusionChoice choice)
        {
            if (!IsRepaired || IsConclusionReached || choice == ConclusionChoice.None)
            {
                return false;
            }

            SelectedConclusion = choice;
            SetStage(ChapterStage.Resolved);
            return true;
        }

        public bool DiscoverEcho(string echoId)
        {
            return !string.IsNullOrWhiteSpace(echoId) && discoveredEchoIds.Add(echoId);
        }

        public string[] ExportClueIds() => new List<string>(collectedClueIds).ToArray();
        public string[] ExportTimeAnchorIds() => new List<string>(stabilizedTimeAnchorIds).ToArray();
        public string[] ExportEchoIds() => new List<string>(discoveredEchoIds).ToArray();

        public void RestoreProgress(
            IEnumerable<string> clueIds,
            IEnumerable<string> timeAnchorIds,
            bool repaired,
            ConclusionChoice conclusion,
            ChapterStage stage,
            IEnumerable<string> echoIds = null)
        {
            collectedClueIds.Clear();
            stabilizedTimeAnchorIds.Clear();
            discoveredEchoIds.Clear();

            if (clueIds != null)
            {
                foreach (string clueId in clueIds)
                {
                    if (!string.IsNullOrWhiteSpace(clueId))
                    {
                        collectedClueIds.Add(clueId);
                    }
                }
            }

            if (timeAnchorIds != null)
            {
                foreach (string anchorId in timeAnchorIds)
                {
                    if (!string.IsNullOrWhiteSpace(anchorId))
                    {
                        stabilizedTimeAnchorIds.Add(anchorId);
                    }
                }
            }

            if (echoIds != null)
            {
                foreach (string echoId in echoIds)
                {
                    if (!string.IsNullOrWhiteSpace(echoId))
                    {
                        discoveredEchoIds.Add(echoId);
                    }
                }
            }

            IsRepaired = repaired;
            SelectedConclusion = repaired ? conclusion : ConclusionChoice.None;
            Stage = stage;
            if (collectedClueIds.Count > 0 && Stage < ChapterStage.Investigate)
            {
                Stage = HasAllClues
                    ? ChapterStage.ReviewEvidence
                    : ChapterStage.Investigate;
            }
            else if (HasAllClues && Stage == ChapterStage.Investigate)
            {
                Stage = ChapterStage.ReviewEvidence;
            }
            if (repaired)
            {
                Stage = conclusion == ConclusionChoice.None
                    ? ChapterStage.FinalDecision
                    : ChapterStage.Resolved;
            }
            else if (stabilizedTimeAnchorIds.Count > 0)
            {
                Stage = ChapterStage.StabilizeAnchors;
            }

            ClueCountChanged?.Invoke(CollectedClueCount, requiredClueCount);
            StageChanged?.Invoke(Stage);
        }

        private void SetStage(ChapterStage stage)
        {
            if (stage <= Stage)
            {
                return;
            }

            Stage = stage;
            StageChanged?.Invoke(Stage);
        }
    }
}
