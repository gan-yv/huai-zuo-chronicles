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

    public sealed class InvestigationState : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredClueCount = 3;
        [SerializeField, Min(1)] private int requiredTimeAnchorCount = 3;
        [SerializeField] private CaseDefinition caseContent;

        private readonly HashSet<string> collectedClueIds = new();
        private readonly HashSet<string> stabilizedTimeAnchorIds = new();
        private readonly HashSet<string> discoveredEchoIds = new();
        private readonly HashSet<string> storyFactIds = new();

        public event Action<int, int> ClueCountChanged;
        public event Action<int, int> AnchorCountChanged;
        public event Action<ChapterStage> StageChanged;
        public CaseDefinition CaseContent => caseContent;
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
        public int StoryFactCount => storyFactIds.Count;
        public bool TestimonyContradictionSolved { get; private set; }
        public int ContradictionAttempts { get; private set; }
        public bool IsRepaired { get; private set; }
        public ConclusionChoice SelectedConclusion { get; private set; }
        public bool IsConclusionReached => SelectedConclusion != ConclusionChoice.None;
        public bool CanInvestigateClues => Stage >= ChapterStage.Investigate;
        public bool CanOpenTestimonyBoard =>
            caseContent != null && Stage >= ChapterStage.ReviewEvidence;
        public bool HasReviewedEvidence => Stage >= ChapterStage.StabilizeAnchors;
        public bool HasClue(string clueId) => collectedClueIds.Contains(clueId);
        public bool HasStabilizedAnchor(string anchorId) =>
            stabilizedTimeAnchorIds.Contains(anchorId);
        public bool HasEcho(string echoId) => discoveredEchoIds.Contains(echoId);
        public bool HasFact(string factId) =>
            !string.IsNullOrWhiteSpace(factId) && storyFactIds.Contains(factId);

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

        public void ConfigureContent(CaseDefinition definition)
        {
            caseContent = definition;
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
                || caseContent?.Contradiction == null
                || string.IsNullOrWhiteSpace(statementId)
                || string.IsNullOrWhiteSpace(evidenceId))
            {
                return ContradictionResult.NotReady;
            }

            ContradictionAttempts++;
            ContradictionDefinition rule = caseContent.Contradiction;
            if (statementId != rule.StatementId
                || evidenceId != rule.EvidenceId
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

        public bool SetFact(string factId)
        {
            return !string.IsNullOrWhiteSpace(factId) && storyFactIds.Add(factId);
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
        public string[] ExportStoryFactIds() =>
            new List<string>(storyFactIds).ToArray();

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
            int contradictionAttempts = 0,
            IEnumerable<string> storyFactIds = null)
        {
            collectedClueIds.Clear();
            stabilizedTimeAnchorIds.Clear();
            discoveredEchoIds.Clear();
            this.storyFactIds.Clear();

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

            if (storyFactIds != null)
            {
                foreach (string factId in storyFactIds)
                {
                    if (!string.IsNullOrWhiteSpace(factId))
                    {
                        this.storyFactIds.Add(factId);
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
