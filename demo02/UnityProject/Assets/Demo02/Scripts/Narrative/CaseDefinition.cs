using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Narrative
{
    [Serializable]
    public sealed class EvidenceDefinition
    {
        public string Id;
        public string Title;
        [TextArea(2, 6)] public string Detail;
        public string InteractionPrompt;
        [TextArea(2, 6)] public string DiscoveredText;
    }

    [Serializable]
    public sealed class TestimonyDefinition
    {
        public string Id;
        public string Source;
        [TextArea(2, 6)] public string Text;
    }

    [Serializable]
    public sealed class ContradictionDefinition
    {
        public string StatementId;
        public string EvidenceId;
        [TextArea(3, 8)] public string Explanation;
        [TextArea(2, 5)] public string FirstHint;
        [TextArea(2, 5)] public string RepeatedHint;
    }

    [Serializable]
    public sealed class StageCopyDefinition
    {
        public ChapterStage Stage;
        [TextArea(2, 5)] public string Objective;
        [TextArea(3, 8)] public string Reasoning;
    }

    [Serializable]
    public sealed class ConclusionCopyDefinition
    {
        public ConclusionChoice Conclusion;
        [TextArea(2, 5)] public string Objective;
        [TextArea(3, 8)] public string Reasoning;
    }

    [CreateAssetMenu(
        fileName = "Chapter01_Case",
        menuName = "Demo02/Content/Case Definition"
    )]
    public sealed class CaseDefinition : ScriptableObject
    {
        [SerializeField] private int contentVersion;
        [SerializeField] private string caseId = "chapter-01";
        [SerializeField] private string title = "消失的盐引";
        [SerializeField] private string chapterLabel = "CHAPTER 01";
        [SerializeField] private string culpritEvidenceId = "official-seal";
        [SerializeField] private List<EvidenceDefinition> evidence = new();
        [SerializeField] private List<TestimonyDefinition> testimonies = new();
        [SerializeField] private ContradictionDefinition contradiction = new();
        [SerializeField] private List<StageCopyDefinition> stageCopy = new();
        [SerializeField] private List<ConclusionCopyDefinition> conclusionCopy = new();
        [SerializeField] private List<DialogueScriptDefinition> dialogueScripts = new();

        public int ContentVersion => contentVersion;
        public string CaseId => caseId;
        public string Title => title;
        public string ChapterLabel => chapterLabel;
        public string CulpritEvidenceId => culpritEvidenceId;
        public IReadOnlyList<EvidenceDefinition> Evidence => evidence;
        public IReadOnlyList<TestimonyDefinition> Testimonies => testimonies;
        public ContradictionDefinition Contradiction => contradiction;
        public IReadOnlyList<DialogueScriptDefinition> DialogueScripts =>
            dialogueScripts;

        public EvidenceDefinition FindEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId))
            {
                return null;
            }

            for (int index = 0; index < evidence.Count; index++)
            {
                if (evidence[index].Id == evidenceId)
                {
                    return evidence[index];
                }
            }

            return null;
        }

        public TestimonyDefinition FindTestimony(string testimonyId)
        {
            if (string.IsNullOrWhiteSpace(testimonyId))
            {
                return null;
            }

            for (int index = 0; index < testimonies.Count; index++)
            {
                if (testimonies[index].Id == testimonyId)
                {
                    return testimonies[index];
                }
            }

            return null;
        }

        public DialogueScriptDefinition FindDialogueScript(
            string npcId,
            InvestigationState state)
        {
            return DialogueContentRuntime.Find(this, npcId, state);
        }

        public string GetObjective(ChapterStage stage)
        {
            return GetStageObjectiveText(stage);
        }

        public string GetObjective(
            ChapterStage stage,
            params object[] formatArguments)
        {
            string text = GetStageObjectiveText(stage);
            if (formatArguments == null || formatArguments.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(text, formatArguments);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        private string GetStageObjectiveText(ChapterStage stage)
        {
            StageCopyDefinition copy = FindStageCopy(stage);
            return copy != null && !string.IsNullOrWhiteSpace(copy.Objective)
                ? copy.Objective
                : "当前阶段目标尚未配置";
        }

        public string GetReasoning(ChapterStage stage)
        {
            StageCopyDefinition copy = FindStageCopy(stage);
            return copy != null && !string.IsNullOrWhiteSpace(copy.Reasoning)
                ? copy.Reasoning
                : "当前阶段推理文本尚未配置。";
        }

        public string GetConclusionObjective(ConclusionChoice conclusion)
        {
            ConclusionCopyDefinition copy = FindConclusionCopy(conclusion);
            return copy != null && !string.IsNullOrWhiteSpace(copy.Objective)
                ? copy.Objective
                : "调查已经结束";
        }

        public string GetConclusionReasoning(ConclusionChoice conclusion)
        {
            ConclusionCopyDefinition copy = FindConclusionCopy(conclusion);
            return copy != null && !string.IsNullOrWhiteSpace(copy.Reasoning)
                ? copy.Reasoning
                : "结局推理文本尚未配置。";
        }

        public List<string> Validate()
        {
            List<string> errors = new();
            if (string.IsNullOrWhiteSpace(caseId))
            {
                errors.Add("Case ID is empty.");
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add("Case title is empty.");
            }
            if (evidence == null || evidence.Count == 0)
            {
                errors.Add("No evidence definitions are configured.");
            }
            if (testimonies == null || testimonies.Count == 0)
            {
                errors.Add("No testimony definitions are configured.");
            }

            ValidateEvidence(errors);
            ValidateTestimonies(errors);
            ValidateContradiction(errors);
            ValidateStageCopy(errors);
            ValidateConclusionCopy(errors);
            ValidateDialogueScripts(errors);
            return errors;
        }

        private StageCopyDefinition FindStageCopy(ChapterStage stage)
        {
            for (int index = 0; index < stageCopy.Count; index++)
            {
                if (stageCopy[index].Stage == stage)
                {
                    return stageCopy[index];
                }
            }

            return null;
        }

        private ConclusionCopyDefinition FindConclusionCopy(
            ConclusionChoice conclusion)
        {
            for (int index = 0; index < conclusionCopy.Count; index++)
            {
                if (conclusionCopy[index].Conclusion == conclusion)
                {
                    return conclusionCopy[index];
                }
            }

            return null;
        }

        private void ValidateEvidence(List<string> errors)
        {
            HashSet<string> ids = new();
            if (evidence == null)
            {
                return;
            }

            for (int index = 0; index < evidence.Count; index++)
            {
                EvidenceDefinition item = evidence[index];
                string label = $"Evidence [{index}]";
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    errors.Add($"{label} has no ID.");
                    continue;
                }
                if (!ids.Add(item.Id))
                {
                    errors.Add($"Duplicate evidence ID: {item.Id}");
                }
                if (string.IsNullOrWhiteSpace(item.Title)
                    || string.IsNullOrWhiteSpace(item.Detail)
                    || string.IsNullOrWhiteSpace(item.InteractionPrompt)
                    || string.IsNullOrWhiteSpace(item.DiscoveredText))
                {
                    errors.Add($"Evidence '{item.Id}' has missing display text.");
                }
            }
        }

        private void ValidateTestimonies(List<string> errors)
        {
            HashSet<string> ids = new();
            if (testimonies == null)
            {
                return;
            }

            for (int index = 0; index < testimonies.Count; index++)
            {
                TestimonyDefinition item = testimonies[index];
                string label = $"Testimony [{index}]";
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    errors.Add($"{label} has no ID.");
                    continue;
                }
                if (!ids.Add(item.Id))
                {
                    errors.Add($"Duplicate testimony ID: {item.Id}");
                }
                if (string.IsNullOrWhiteSpace(item.Source)
                    || string.IsNullOrWhiteSpace(item.Text))
                {
                    errors.Add($"Testimony '{item.Id}' has missing source or text.");
                }
            }
        }

        private void ValidateContradiction(List<string> errors)
        {
            if (contradiction == null
                || string.IsNullOrWhiteSpace(contradiction.StatementId)
                || string.IsNullOrWhiteSpace(contradiction.EvidenceId))
            {
                errors.Add("Contradiction statement or evidence ID is missing.");
                return;
            }
            if (FindTestimony(contradiction.StatementId) == null)
            {
                errors.Add(
                    $"Contradiction references missing testimony: {contradiction.StatementId}"
                );
            }
            if (FindEvidence(contradiction.EvidenceId) == null)
            {
                errors.Add(
                    $"Contradiction references missing evidence: {contradiction.EvidenceId}"
                );
            }
            if (string.IsNullOrWhiteSpace(contradiction.Explanation)
                || string.IsNullOrWhiteSpace(contradiction.FirstHint)
                || string.IsNullOrWhiteSpace(contradiction.RepeatedHint))
            {
                errors.Add("Contradiction feedback text is incomplete.");
            }
            if (FindEvidence(culpritEvidenceId) == null)
            {
                errors.Add(
                    $"Culprit evidence ID is missing: {culpritEvidenceId}"
                );
            }
        }

        private void ValidateStageCopy(List<string> errors)
        {
            HashSet<ChapterStage> configured = new();
            if (stageCopy == null)
            {
                errors.Add("No stage copy is configured.");
                return;
            }

            foreach (StageCopyDefinition item in stageCopy)
            {
                if (item == null || !configured.Add(item.Stage))
                {
                    errors.Add("Stage copy contains a null or duplicate stage.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.Objective)
                    || string.IsNullOrWhiteSpace(item.Reasoning))
                {
                    errors.Add($"Stage '{item.Stage}' has incomplete copy.");
                }
            }

            foreach (ChapterStage stage in Enum.GetValues(typeof(ChapterStage)))
            {
                if (!configured.Contains(stage))
                {
                    errors.Add($"Stage copy is missing: {stage}");
                }
            }
        }

        private void ValidateConclusionCopy(List<string> errors)
        {
            HashSet<ConclusionChoice> configured = new();
            if (conclusionCopy == null)
            {
                errors.Add("No conclusion copy is configured.");
                return;
            }

            foreach (ConclusionCopyDefinition item in conclusionCopy)
            {
                if (item == null
                    || item.Conclusion == ConclusionChoice.None
                    || !configured.Add(item.Conclusion))
                {
                    errors.Add("Conclusion copy contains an invalid duplicate.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.Objective)
                    || string.IsNullOrWhiteSpace(item.Reasoning))
                {
                    errors.Add(
                        $"Conclusion '{item.Conclusion}' has incomplete copy."
                    );
                }
            }

            if (!configured.Contains(ConclusionChoice.Preserve)
                || !configured.Contains(ConclusionChoice.Seal))
            {
                errors.Add("Both Preserve and Seal conclusion copy are required.");
            }
        }

        private void ValidateDialogueScripts(List<string> errors)
        {
            if (dialogueScripts == null || dialogueScripts.Count == 0)
            {
                errors.Add("No dialogue scripts are configured.");
                return;
            }

            HashSet<string> ids = new();
            HashSet<ChapterStage> coveredStages = new();
            for (int index = 0; index < dialogueScripts.Count; index++)
            {
                DialogueScriptDefinition script = dialogueScripts[index];
                string label = $"Dialogue [{index}]";
                if (script == null
                    || string.IsNullOrWhiteSpace(script.Id)
                    || string.IsNullOrWhiteSpace(script.NpcId)
                    || string.IsNullOrWhiteSpace(script.InteractionPrompt))
                {
                    errors.Add($"{label} is missing ID, NPC ID or prompt.");
                    continue;
                }
                if (!ids.Add(script.Id))
                {
                    errors.Add($"Duplicate dialogue script ID: {script.Id}");
                }
                if (script.Lines == null || script.Lines.Count == 0)
                {
                    errors.Add($"Dialogue '{script.Id}' has no lines.");
                }
                else
                {
                    for (int line = 0; line < script.Lines.Count; line++)
                    {
                        DialogueLine dialogueLine = script.Lines[line];
                        if (dialogueLine == null
                            || string.IsNullOrWhiteSpace(dialogueLine.Speaker)
                            || string.IsNullOrWhiteSpace(dialogueLine.Text))
                        {
                            errors.Add(
                                $"Dialogue '{script.Id}' line {line} is incomplete."
                            );
                        }
                    }
                }

                for (int conditionIndex = 0;
                    conditionIndex < script.Conditions.Count;
                    conditionIndex++)
                {
                    DialogueConditionDefinition condition =
                        script.Conditions[conditionIndex];
                    if (condition != null
                        && condition.Type == DialogueConditionType.StageEquals)
                    {
                        coveredStages.Add(condition.Stage);
                    }
                    if (condition != null
                        && condition.Type == DialogueConditionType.HasEvidence
                        && FindEvidence(condition.EvidenceId) == null)
                    {
                        errors.Add(
                            $"Dialogue '{script.Id}' references missing evidence "
                            + $"condition: {condition.EvidenceId}"
                        );
                    }
                }

                for (int choiceIndex = 0;
                    choiceIndex < script.Choices.Count;
                    choiceIndex++)
                {
                    DialogueChoiceDefinition choice = script.Choices[choiceIndex];
                    if (choice == null || string.IsNullOrWhiteSpace(choice.Text))
                    {
                        errors.Add(
                            $"Dialogue '{script.Id}' choice {choiceIndex} has no text."
                        );
                    }
                }
            }

            foreach (ChapterStage stage in Enum.GetValues(typeof(ChapterStage)))
            {
                if (!coveredStages.Contains(stage))
                {
                    errors.Add($"No dialogue script covers stage: {stage}");
                }
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string newCaseId,
            string newTitle,
            string newChapterLabel,
            string newCulpritEvidenceId,
            List<EvidenceDefinition> newEvidence,
            List<TestimonyDefinition> newTestimonies,
            ContradictionDefinition newContradiction,
            List<StageCopyDefinition> newStageCopy,
            List<ConclusionCopyDefinition> newConclusionCopy,
            List<DialogueScriptDefinition> newDialogueScripts)
        {
            caseId = newCaseId;
            title = newTitle;
            chapterLabel = newChapterLabel;
            culpritEvidenceId = newCulpritEvidenceId;
            evidence = newEvidence;
            testimonies = newTestimonies;
            contradiction = newContradiction;
            stageCopy = newStageCopy;
            conclusionCopy = newConclusionCopy;
            dialogueScripts = newDialogueScripts;
            contentVersion = 3;
        }

        public void ConfigureDialogueScriptsForEditor(
            List<DialogueScriptDefinition> newDialogueScripts)
        {
            dialogueScripts = newDialogueScripts;
            contentVersion = 3;
        }
#endif
    }
}
