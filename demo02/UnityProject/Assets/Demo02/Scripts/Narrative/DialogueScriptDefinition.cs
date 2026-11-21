using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Narrative
{
    public enum DialogueConditionType
    {
        Always = 0,
        StageEquals = 1,
        StanceEquals = 2,
        EvidencePlanEquals = 3,
        ConclusionEquals = 4,
        StageAtLeast = 5,
        StageBefore = 6,
        HasEvidence = 7,
        HasFact = 8
    }

    public enum DialogueEffectType
    {
        None = 0,
        SetWitnessStance = 1,
        SetEvidencePlan = 2,
        CompleteWitnessBriefing = 3,
        CompleteEvidenceReview = 4,
        SetFact = 5
    }

    [Serializable]
    public sealed class DialogueConditionDefinition
    {
        public DialogueConditionType Type;
        public ChapterStage Stage;
        public WitnessStance Stance;
        public EvidencePlan EvidencePlan;
        public ConclusionChoice Conclusion;
        public string EvidenceId;
        public string StoryFactId;
        public bool Negate;
    }

    [Serializable]
    public sealed class DialogueEffectDefinition
    {
        public DialogueEffectType Type;
        public WitnessStance Stance;
        public EvidencePlan EvidencePlan;
        public string StoryFactId;
    }

    [Serializable]
    public sealed class DialogueChoiceDefinition
    {
        public string Text;
        public List<DialogueConditionDefinition> Conditions = new();
        public List<DialogueEffectDefinition> Effects = new();
    }

    [Serializable]
    public sealed class DialogueScriptDefinition
    {
        public string Id;
        public string NpcId;
        public string InteractionPrompt;
        public List<DialogueConditionDefinition> Conditions = new();
        public List<DialogueLine> Lines = new();
        public List<DialogueChoiceDefinition> Choices = new();
        public List<DialogueEffectDefinition> CompletionEffects = new();
        public bool ShowChoicesImmediately;
    }

    public static class DialogueContentRuntime
    {
        public static DialogueScriptDefinition Find(
            CaseDefinition content,
            string npcId,
            InvestigationState state)
        {
            if (content == null
                || state == null
                || string.IsNullOrWhiteSpace(npcId))
            {
                return null;
            }

            IReadOnlyList<DialogueScriptDefinition> scripts =
                content.DialogueScripts;
            for (int index = 0; index < scripts.Count; index++)
            {
                DialogueScriptDefinition script = scripts[index];
                if (script != null
                    && script.NpcId == npcId
                    && ConditionsMatch(script.Conditions, state))
                {
                    return script;
                }
            }

            return null;
        }

        public static bool TryPlay(
            CaseDefinition content,
            InvestigationState state,
            DialogueController dialogue,
            string npcId,
            Transform cameraFocus,
            bool forceShowChoices = false)
        {
            if (dialogue == null || dialogue.IsOpen)
            {
                return false;
            }

            DialogueScriptDefinition script = Find(content, npcId, state);
            if (script == null || script.Lines == null || script.Lines.Count == 0)
            {
                return false;
            }

            List<DialogueChoiceDefinition> available = new();
            for (int index = 0; index < script.Choices.Count; index++)
            {
                DialogueChoiceDefinition choice = script.Choices[index];
                if (choice != null
                    && ConditionsMatch(choice.Conditions, state))
                {
                    available.Add(choice);
                }
            }

            DialogueLine[] lines = script.Lines.ToArray();
            Action completed = () => ApplyEffects(script.CompletionEffects, state);
            if (available.Count == 0)
            {
                dialogue.Begin(lines, completed, cameraFocus);
                return true;
            }

            DialogueChoice[] choices = new DialogueChoice[available.Count];
            for (int index = 0; index < available.Count; index++)
            {
                choices[index] = new DialogueChoice(available[index].Text);
            }

            dialogue.BeginWithChoice(
                lines,
                choices,
                index =>
                {
                    if (index >= 0 && index < available.Count)
                    {
                        ApplyEffects(available[index].Effects, state);
                    }
                },
                completed,
                cameraFocus,
                forceShowChoices || script.ShowChoicesImmediately
            );
            return true;
        }

        public static bool ConditionsMatch(
            IReadOnlyList<DialogueConditionDefinition> conditions,
            InvestigationState state)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < conditions.Count; index++)
            {
                DialogueConditionDefinition condition = conditions[index];
                if (condition != null
                    && Evaluate(condition, state) != !condition.Negate)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ApplyEffects(
            IReadOnlyList<DialogueEffectDefinition> effects,
            InvestigationState state)
        {
            if (effects == null || state == null)
            {
                return;
            }

            for (int index = 0; index < effects.Count; index++)
            {
                DialogueEffectDefinition effect = effects[index];
                if (effect == null)
                {
                    continue;
                }

                switch (effect.Type)
                {
                    case DialogueEffectType.SetWitnessStance:
                        state.SetWitnessStance(effect.Stance);
                        break;
                    case DialogueEffectType.SetEvidencePlan:
                        state.SetEvidencePlan(effect.EvidencePlan);
                        break;
                    case DialogueEffectType.CompleteWitnessBriefing:
                        state.CompleteWitnessBriefing();
                        break;
                    case DialogueEffectType.CompleteEvidenceReview:
                        state.CompleteEvidenceReview();
                        break;
                    case DialogueEffectType.SetFact:
                        state.SetFact(effect.StoryFactId);
                        break;
                }
            }
        }

        private static bool Evaluate(
            DialogueConditionDefinition condition,
            InvestigationState state)
        {
            return condition.Type switch
            {
                DialogueConditionType.Always => true,
                DialogueConditionType.StageEquals =>
                    state.Stage == condition.Stage,
                DialogueConditionType.StageAtLeast =>
                    state.Stage >= condition.Stage,
                DialogueConditionType.StageBefore =>
                    state.Stage < condition.Stage,
                DialogueConditionType.HasEvidence =>
                    state.HasClue(condition.EvidenceId),
                DialogueConditionType.HasFact =>
                    state.HasFact(condition.StoryFactId),
                DialogueConditionType.StanceEquals =>
                    state.Stance == condition.Stance,
                DialogueConditionType.EvidencePlanEquals =>
                    state.Plan == condition.EvidencePlan,
                DialogueConditionType.ConclusionEquals =>
                    state.SelectedConclusion == condition.Conclusion,
                _ => false
            };
        }
    }
}
