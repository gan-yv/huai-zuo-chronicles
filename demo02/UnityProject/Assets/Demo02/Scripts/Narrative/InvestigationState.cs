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
        CrossExamineTestimony,
        StabilizeAnchors,
        RepairRift,
        FinalDecision,
        Resolved
    }

    public enum WitnessStance
    {
        Undecided,
        EvidenceFirst,
        PersonFirst
    }

    public enum EvidencePlan
    {
        Undecided,
        PublicRecord,
        PrivateProtection
    }

    public enum TimeAnchorResult
    {
        Stabilized,
        SequenceComplete,
        AlreadyStable,
        NotReady,
        WrongOrder
    }

    public enum ContradictionResult
    {
        NotReady,
        Incorrect,
        Correct,
        AlreadySolved
    }

    public static class TestimonyPuzzleRules
    {
        public const string StatementId = "shenbo-seal-date";
        public const string EvidenceId = "salt-ledger";
        public const string CorrectExplanation =
            "初五的封条已经写明要把沈砚除名，七车盐却在初七夜里才入城。"
            + "除名发生在私盐入城之前，说明陆崇不是查案后处置，而是提前抹掉知情人。";

        public static string GetIncorrectHint(int attempts)
        {
            return attempts >= 2
                ? "再比对一次时间。沈伯说的是封条何时贴上，账牌记录的是七车盐何时入城。"
                : "这两份记录可以同时成立。寻找一份写有具体日期、另一份记录同一事件先后的内容。";
        }
    }

    public sealed class InvestigationState : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredClueCount = 3;
        [SerializeField, Min(1)] private int requiredTimeAnchorCount = 3;

        private readonly HashSet<string> collectedClueIds = new();
        private readonly HashSet<string> stabilizedTimeAnchorIds = new();
        private readonly HashSet<string> discoveredEchoIds = new();

        public event Action<int, int> ClueCountChanged;
        public event Action<int, int> AnchorCountChanged;
        public event Action<ChapterStage> StageChanged;
        public ChapterStage Stage { get; private set; } = ChapterStage.Arrival;
        public WitnessStance Stance { get; private set; } = WitnessStance.Undecided;
        public EvidencePlan Plan { get; private set; } = EvidencePlan.Undecided;
        public int CollectedClueCount => collectedClueIds.Count;
        public int RequiredClueCount => requiredClueCount;
        public bool HasAllClues => CollectedClueCount >= requiredClueCount;
        public int StabilizedTimeAnchorCount => stabilizedTimeAnchorIds.Count;
        public int RequiredTimeAnchorCount => requiredTimeAnchorCount;
        public bool HasAllTimeAnchors => StabilizedTimeAnchorCount >= requiredTimeAnchorCount;
        public int DiscoveredEchoCount => discoveredEchoIds.Count;
        public bool TestimonyContradictionSolved { get; private set; }
        public int ContradictionAttempts { get; private set; }
        public bool IsRepaired { get; private set; }
        public ConclusionChoice SelectedConclusion { get; private set; }
        public bool IsConclusionReached => SelectedConclusion != ConclusionChoice.None;
        public bool CanInvestigateClues => Stage >= ChapterStage.Investigate;
        public bool CanOpenTestimonyBoard => Stage >= ChapterStage.ReviewEvidence;
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
            if (Stage != ChapterStage.ReviewEvidence
                || !HasAllClues
                || Plan == EvidencePlan.Undecided)
            {
                return false;
            }

            SetStage(ChapterStage.CrossExamineTestimony);
            return true;
        }

        public ContradictionResult SubmitContradiction(
            string statementId,
            string evidenceId)
        {
            if (TestimonyContradictionSolved)
            {
                return ContradictionResult.AlreadySolved;
            }

            if (Stage != ChapterStage.CrossExamineTestimony
                || !HasAllClues
                || string.IsNullOrWhiteSpace(statementId)
                || string.IsNullOrWhiteSpace(evidenceId))
            {
                return ContradictionResult.NotReady;
            }

            ContradictionAttempts++;
            if (statementId != TestimonyPuzzleRules.StatementId
                || evidenceId != TestimonyPuzzleRules.EvidenceId
                || !HasClue(evidenceId))
            {
                return ContradictionResult.Incorrect;
            }

            TestimonyContradictionSolved = true;
            SetStage(ChapterStage.StabilizeAnchors);
            return ContradictionResult.Correct;
        }

        public TimeAnchorResult TryStabilizeTimeAnchor(
            string anchorId,
            out string expectedAnchorId)
        {
            expectedAnchorId = TimeAnchorPuzzleRules.GetExpectedId(
                StabilizedTimeAnchorCount
            );
            if (string.IsNullOrWhiteSpace(anchorId)
                || !HasReviewedEvidence
                || !HasAllClues)
            {
                return TimeAnchorResult.NotReady;
            }

            if (HasStabilizedAnchor(anchorId))
            {
                return TimeAnchorResult.AlreadyStable;
            }

            if (anchorId != expectedAnchorId)
            {
                stabilizedTimeAnchorIds.Clear();
                AnchorCountChanged?.Invoke(
                    StabilizedTimeAnchorCount,
                    requiredTimeAnchorCount
                );
                return TimeAnchorResult.WrongOrder;
            }

            stabilizedTimeAnchorIds.Add(anchorId);
            AnchorCountChanged?.Invoke(
                StabilizedTimeAnchorCount,
                requiredTimeAnchorCount
            );
            if (HasAllTimeAnchors)
            {
                SetStage(ChapterStage.RepairRift);
                return TimeAnchorResult.SequenceComplete;
            }

            return TimeAnchorResult.Stabilized;
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

        public bool SetWitnessStance(WitnessStance stance)
        {
            if (Stance != WitnessStance.Undecided || stance == WitnessStance.Undecided)
            {
                return false;
            }

            Stance = stance;
            return true;
        }

        public bool SetEvidencePlan(EvidencePlan plan)
        {
            if (Plan != EvidencePlan.Undecided || plan == EvidencePlan.Undecided)
            {
                return false;
            }

            Plan = plan;
            return true;
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
            IEnumerable<string> echoIds = null,
            WitnessStance stance = WitnessStance.Undecided,
            EvidencePlan plan = EvidencePlan.Undecided,
            bool testimonyContradictionSolved = false,
            int contradictionAttempts = 0)
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
            Stance = stance;
            Plan = plan;
            Stage = stage;
            ContradictionAttempts = Mathf.Max(0, contradictionAttempts);
            TestimonyContradictionSolved = testimonyContradictionSolved
                || Stage >= ChapterStage.StabilizeAnchors;
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
            if (TestimonyContradictionSolved
                && Stage < ChapterStage.StabilizeAnchors)
            {
                Stage = ChapterStage.StabilizeAnchors;
            }

            ClueCountChanged?.Invoke(CollectedClueCount, requiredClueCount);
            AnchorCountChanged?.Invoke(
                StabilizedTimeAnchorCount,
                requiredTimeAnchorCount
            );
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
