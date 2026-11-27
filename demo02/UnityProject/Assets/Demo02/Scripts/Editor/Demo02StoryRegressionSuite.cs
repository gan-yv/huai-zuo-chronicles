using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Demo02.Editor
{
    internal static class Demo02StoryRegressionSuite
    {
        public static void VerifyDialogueContentForBatch()
        {
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            GameObject probe = new("DialogueContentProbe");
            InvestigationState state = probe.AddComponent<InvestigationState>();
            state.ConfigureContent(content);

            DialogueScriptDefinition archiveArrival =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    state
                );
            if (archiveArrival == null
                || archiveArrival.Id != "archive-clerk-arrival"
                || DialogueContentRuntime.Find(content, "ahe", state) != null)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Archive clerk or hidden Ahe story gating is invalid."
                );
            }

            DialogueScriptDefinition arrival = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (arrival == null
                || arrival.Id != "shenbo-arrival"
                || arrival.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Arrival dialogue did not resolve from recorded data."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                arrival.Choices[0].Effects,
                state
            );
            DialogueContentRuntime.ApplyEffects(
                arrival.CompletionEffects,
                state
            );
            DialogueScriptDefinition reminder = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (state.Stage != ChapterStage.Investigate
                || state.Stance != WitnessStance.EvidenceFirst
                || reminder == null
                || reminder.Id != "shenbo-investigate-evidence")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Dialogue effects did not select the stance-specific reminder."
                );
            }
            if (OptionalObjectiveRuntime.FindActive(content, state) != null)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Optional tea objective activated before the seal was found."
                );
            }

            DialogueScriptDefinition teahouseEarly =
                DialogueContentRuntime.Find(content, "teahouse", state);
            if (teahouseEarly == null
                || teahouseEarly.Id != "teahouse-early")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The data-configured teahouse event did not match early game."
                );
            }

            state.CollectClue("old-street-sign");
            state.CollectClue("salt-ledger");
            state.CollectClue("official-seal");
            DialogueScriptDefinition clerkInk =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    state
                );
            if (clerkInk == null
                || clerkInk.Id != "archive-clerk-investigate")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The archive clerk did not offer the ink clue."
                );
            }
            DialogueContentRuntime.ApplyEffects(
                clerkInk.CompletionEffects,
                state
            );
            DialogueScriptDefinition clerkFatherChoice =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    state
                );
            if (clerkFatherChoice == null
                || clerkFatherChoice.Id != "archive-clerk-father-choice"
                || clerkFatherChoice.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The archive clerk's family story did not unlock."
                );
            }
            DialogueContentRuntime.ApplyEffects(
                clerkFatherChoice.Choices[0].Effects,
                state
            );
            DialogueContentRuntime.ApplyEffects(
                clerkFatherChoice.CompletionEffects,
                state
            );
            DialogueScriptDefinition clerkRecruited =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    state
                );
            if (!state.HasFact("archive-clerk-recruited")
                || clerkRecruited == null
                || clerkRecruited.Id != "archive-clerk-father-recruited")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The archive clerk family choice did not persist."
                );
            }
            DialogueScriptDefinition teahouseRumor =
                DialogueContentRuntime.Find(content, "teahouse", state);
            OptionalObjectiveDefinition teaObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            if (teahouseRumor == null
                || teahouseRumor.Id != "teahouse-rumor-seal"
                || teaObjective == null
                || teaObjective.Id != "optional-teahouse-cart-rumor")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The teahouse event did not react to the official seal."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                teahouseRumor.Choices[0].Effects,
                state
            );
            DialogueScriptDefinition teahouseAfterChoice =
                DialogueContentRuntime.Find(content, "teahouse", state);
            OptionalObjectiveDefinition recordObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            if (!state.HasFact("teahouse-cart-testimony")
                || !state.HasFact("teahouse-event-completed")
                || teahouseAfterChoice == null
                || teahouseAfterChoice.Id != "teahouse-after-cart-testimony"
                || recordObjective == null
                || recordObjective.Id
                    != "optional-record-teahouse-testimony")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The teahouse choice did not persist its story fact."
                );
            }
            state.SetFact("teahouse-written-record");
            OptionalObjectiveDefinition workerObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            DialogueScriptDefinition workerTestimony =
                DialogueContentRuntime.Find(
                    content,
                    "warehouse-worker",
                    state
                );
            if (workerObjective == null
                || workerObjective.Id != "optional-warehouse-night-shift"
                || workerTestimony == null
                || workerTestimony.Id != "warehouse-worker-testimony")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The warehouse witness did not unlock after the ledger."
                );
            }
            DialogueContentRuntime.ApplyEffects(
                workerTestimony.CompletionEffects,
                state
            );
            OptionalObjectiveDefinition mealObjective =
                OptionalObjectiveRuntime.FindActive(content, state);
            DialogueScriptDefinition daughterChoice =
                DialogueContentRuntime.Find(
                    content,
                    "warehouse-worker",
                    state
                );
            if (!state.HasFact("warehouse-night-shift")
                || mealObjective == null
                || mealObjective.Id != "optional-warehouse-second-meal"
                || daughterChoice == null
                || daughterChoice.Id != "warehouse-worker-daughter-choice"
                || daughterChoice.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The warehouse daughter story did not unlock cleanly."
                );
            }
            DialogueContentRuntime.ApplyEffects(
                daughterChoice.Choices[0].Effects,
                state
            );
            DialogueContentRuntime.ApplyEffects(
                daughterChoice.CompletionEffects,
                state
            );
            DialogueScriptDefinition daughterRepeat =
                DialogueContentRuntime.Find(
                    content,
                    "warehouse-worker",
                    state
                );
            if (!state.HasFact("warehouse-second-meal")
                || !state.HasFact("warehouse-daughter-public")
                || !state.HasFact("warehouse-daughter-remembered")
                || daughterRepeat == null
                || daughterRepeat.Id
                    != "warehouse-worker-daughter-public-repeat"
                || OptionalObjectiveRuntime.FindActive(content, state) != null)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The warehouse daughter choice did not persist."
                );
            }

            state.SetEvidencePlan(EvidencePlan.PublicRecord);
            DialogueScriptDefinition review = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (review == null || review.Choices.Count != 2)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Evidence review dialogue did not resolve from data."
                );
            }

            DialogueContentRuntime.ApplyEffects(
                review.CompletionEffects,
                state
            );
            DialogueScriptDefinition crossExamine = DialogueContentRuntime.Find(
                content,
                "shenbo",
                state
            );
            if (state.Stage != ChapterStage.CrossExamineTestimony
                || crossExamine == null
                || crossExamine.Id != "shenbo-cross-examine-teahouse")
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Dialogue completion effects did not advance the chapter."
                );
            }

            GameObject branchProbe = new("NpcStoryBranchProbe");
            InvestigationState branchState =
                branchProbe.AddComponent<InvestigationState>();
            branchState.ConfigureContent(content);
            branchState.RestoreProgress(
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                false,
                ConclusionChoice.None,
                ChapterStage.FinalDecision,
                null,
                WitnessStance.PersonFirst,
                EvidencePlan.PrivateProtection,
                true,
                0
            );
            DialogueScriptDefinition luChongFinal =
                DialogueContentRuntime.Find(
                    content,
                    "lu-chong",
                    branchState
                );
            if (luChongFinal == null
                || luChongFinal.Id != "lu-chong-final")
            {
                Object.DestroyImmediate(branchProbe);
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Lu Chong did not challenge the final decision."
                );
            }
            branchState.RestoreProgress(
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                true,
                ConclusionChoice.Preserve,
                ChapterStage.Resolved,
                null,
                WitnessStance.PersonFirst,
                EvidencePlan.PrivateProtection,
                true,
                0,
                new[]
                {
                    "warehouse-daughter-remembered",
                    "warehouse-daughter-private"
                }
            );
            DialogueScriptDefinition ahePrivate =
                DialogueContentRuntime.Find(content, "ahe", branchState);
            DialogueScriptDefinition clerkPreserve =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    branchState
                );
            DialogueScriptDefinition clothPreserve =
                DialogueContentRuntime.Find(
                    content,
                    "cloth-merchant",
                    branchState
                );
            DialogueScriptDefinition tailorPreserve =
                DialogueContentRuntime.Find(
                    content,
                    "caiyi-tailor",
                    branchState
                );
            DialogueScriptDefinition snackPreserve =
                DialogueContentRuntime.Find(
                    content,
                    "caiyi-snack-vendor",
                    branchState
                );
            DialogueScriptDefinition luChongPreserve =
                DialogueContentRuntime.Find(
                    content,
                    "lu-chong",
                    branchState
                );
            if (ahePrivate == null
                || ahePrivate.Id != "ahe-preserve-private"
                || clerkPreserve == null
                || clerkPreserve.Id != "archive-clerk-preserve"
                || clothPreserve == null
                || clothPreserve.Id != "cloth-merchant-preserve"
                || tailorPreserve == null
                || tailorPreserve.Id != "caiyi-tailor-preserve"
                || snackPreserve == null
                || snackPreserve.Id != "caiyi-snack-preserve-daughter"
                || luChongPreserve == null
                || luChongPreserve.Id != "lu-chong-preserve"
                || !ChapterCopy.GetEndingEpilogue(branchState)
                    .Contains("私人案卷"))
            {
                Object.DestroyImmediate(branchProbe);
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Preserve-ending NPC stories did not select correctly."
                );
            }

            branchState.RestoreProgress(
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                true,
                ConclusionChoice.Seal,
                ChapterStage.Resolved,
                null,
                WitnessStance.EvidenceFirst,
                EvidencePlan.PublicRecord,
                true,
                0,
                new[]
                {
                    "warehouse-daughter-remembered",
                    "warehouse-daughter-private"
                }
            );
            DialogueScriptDefinition clerkSeal =
                DialogueContentRuntime.Find(
                    content,
                    "archive-clerk",
                    branchState
                );
            DialogueScriptDefinition clothSeal =
                DialogueContentRuntime.Find(
                    content,
                    "cloth-merchant",
                    branchState
                );
            DialogueScriptDefinition tailorSeal =
                DialogueContentRuntime.Find(
                    content,
                    "caiyi-tailor",
                    branchState
                );
            DialogueScriptDefinition snackSeal =
                DialogueContentRuntime.Find(
                    content,
                    "caiyi-snack-vendor",
                    branchState
                );
            DialogueScriptDefinition luChongSeal =
                DialogueContentRuntime.Find(
                    content,
                    "lu-chong",
                    branchState
                );
            if (DialogueContentRuntime.Find(content, "ahe", branchState) != null
                || clerkSeal == null
                || clerkSeal.Id != "archive-clerk-seal"
                || clothSeal == null
                || clothSeal.Id != "cloth-merchant-seal"
                || tailorSeal == null
                || tailorSeal.Id != "caiyi-tailor-seal"
                || snackSeal == null
                || snackSeal.Id != "caiyi-snack-seal"
                || luChongSeal == null
                || luChongSeal.Id != "lu-chong-seal"
                || !ChapterCopy.GetEndingEpilogue(branchState)
                    .Contains("封条之外"))
            {
                Object.DestroyImmediate(branchProbe);
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Seal-ending NPC stories did not select correctly."
                );
            }
            Object.DestroyImmediate(branchProbe);

            Object.DestroyImmediate(probe);
            Debug.Log(
                "Dialogue data verification passed: conditions, choices, "
                + "effects and stage-specific scripts."
            );
        }

        public static void VerifyTimeLayerForBatch()
        {
            GameObject probe = new GameObject("TimeLayerProbe");
            TimeLayerController controller = probe.AddComponent<TimeLayerController>();

            controller.ToggleLayer();
            if (controller.CurrentLayer != TimeLayer.Past)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Time layer did not switch to Past.");
            }

            controller.ToggleLayer();
            if (controller.CurrentLayer != TimeLayer.Present)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Time layer did not switch back to Present.");
            }

            Object.DestroyImmediate(probe);
            Debug.Log("Time layer verification passed: Present <-> Past.");
        }

        public static void VerifyChapterFlowForBatch()
        {
            GameObject probe = new("ChapterFlowProbe");
            InvestigationState state = probe.AddComponent<InvestigationState>();
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            state.ConfigureContent(content);

            if (state.CanInvestigateClues
                || state.CollectClue("old-street-sign"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Clues were available before Shen Bo briefed the player."
                );
            }

            if (!state.CompleteWitnessBriefing()
                || state.Stage != ChapterStage.Investigate)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The chapter did not enter the investigation stage."
                );
            }
            state.SetWitnessStance(WitnessStance.PersonFirst);

            if (!state.CollectClue("old-street-sign")
                || !state.CollectClue("salt-ledger")
                || !state.CollectClue("official-seal")
                || state.Stage != ChapterStage.ReviewEvidence)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Collecting three clues did not require evidence review."
                );
            }

            if (state.TryStabilizeTimeAnchor("past-02", out _)
                != TimeAnchorResult.NotReady)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Time anchors unlocked before the evidence review."
                );
            }

            state.SetEvidencePlan(EvidencePlan.PrivateProtection);
            if (!state.CompleteEvidenceReview()
                || state.Stage != ChapterStage.CrossExamineTestimony
                || state.TryStabilizeTimeAnchor("past-02", out _)
                    != TimeAnchorResult.NotReady)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Evidence review did not open testimony cross-examination."
                );
            }
            if (state.SubmitContradiction(
                    content.Testimonies[1].Id,
                    content.Contradiction.EvidenceId
                ) != ContradictionResult.Incorrect
                || state.ContradictionAttempts != 1
                || state.Stage != ChapterStage.CrossExamineTestimony
                || state.SubmitContradiction(
                    content.Contradiction.StatementId,
                    content.Contradiction.EvidenceId
                ) != ContradictionResult.Correct
                || state.ContradictionAttempts != 2
                || !state.TestimonyContradictionSolved
                || state.Stage != ChapterStage.StabilizeAnchors)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Testimony contradiction did not validate and unlock anchors."
                );
            }
            if (state.TryStabilizeTimeAnchor("present-01", out _)
                    != TimeAnchorResult.WrongOrder
                || state.StabilizedTimeAnchorCount != 0
                || state.TryStabilizeTimeAnchor("past-02", out _)
                    != TimeAnchorResult.Stabilized
                || state.TryStabilizeTimeAnchor("present-01", out _)
                    != TimeAnchorResult.Stabilized
                || state.TryStabilizeTimeAnchor("present-03", out _)
                    != TimeAnchorResult.SequenceComplete
                || state.Stage != ChapterStage.RepairRift)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Three stable anchors did not unlock the repair step."
                );
            }

            state.RestoreProgress(
                new[] { "old-street-sign", "salt-ledger", "official-seal" },
                new[] { "past-02", "present-01", "present-03" },
                false,
                ConclusionChoice.None,
                ChapterStage.RepairRift,
                System.Array.Empty<string>(),
                WitnessStance.EvidenceFirst,
                EvidencePlan.PublicRecord,
                true,
                2,
                System.Array.Empty<string>()
            );
            if (state.Stage != ChapterStage.RepairRift)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "A RepairRift save was downgraded during restore."
                );
            }

            if (!state.CompleteRepair()
                || state.Stage != ChapterStage.FinalDecision
                || !state.SelectConclusion(ConclusionChoice.Preserve)
                || state.Stage != ChapterStage.Resolved)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "The chapter did not reach a resolved conclusion."
                );
            }

            state.RestoreProgress(
                new[] { "old-street-sign", "salt-ledger", "official-seal" },
                System.Array.Empty<string>(),
                false,
                ConclusionChoice.None,
                ChapterStage.StabilizeAnchors,
                null,
                WitnessStance.EvidenceFirst,
                EvidencePlan.PublicRecord
            );
            if (state.Stage != ChapterStage.StabilizeAnchors
                || !state.TestimonyContradictionSolved)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "An older anchor-stage save did not infer the completed deduction."
                );
            }

            state.RestoreProgress(
                new[] { "old-street-sign", "salt-ledger", "official-seal" },
                new[] { "past-02", "present-01", "present-03" },
                true,
                ConclusionChoice.Seal,
                ChapterStage.Resolved,
                null,
                WitnessStance.PersonFirst,
                EvidencePlan.PrivateProtection,
                true,
                2,
                new[] { "teahouse-cart-testimony" }
            );
            if (state.Stage != ChapterStage.Resolved
                || state.SelectedConclusion != ConclusionChoice.Seal
                || state.Stance != WitnessStance.PersonFirst
                || state.Plan != EvidencePlan.PrivateProtection
                || !state.TestimonyContradictionSolved
                || state.ContradictionAttempts != 2
                || !state.HasFact("teahouse-cart-testimony"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Chapter progress did not restore correctly from a save."
                );
            }

            GameFlowController flow =
                probe.AddComponent<GameFlowController>();
            flow.BindForTests(state, null);
            if (!flow.CanExploreAfterEnding)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Resolved chapter did not allow post-ending exploration."
                );
            }
            flow.ContinueAfterEnding();
            if (flow.IsGameEnding || !Mathf.Approximately(Time.timeScale, 1f))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Post-ending exploration did not restore gameplay."
                );
            }

            Object.DestroyImmediate(probe);
            Debug.Log(
                "Chapter verification passed: brief -> clues -> review -> "
                + "contradiction -> anchors -> repair -> ending."
            );
        }

    }
}
