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

    [Serializable]
    public sealed class StoryFactDefinition
    {
        public string Id;
        public string Title;
        [TextArea(2, 5)] public string Detail;
    }

    public enum InventoryItemEffect
    {
        None = 0,
        RestoreHealth = 1,
        GrantStoryFact = 2
    }

    [Serializable]
    public sealed class InventoryItemDefinition
    {
        public string Id;
        public string DisplayName;
        public string Category;
        [TextArea(2, 5)] public string Description;
        [Min(1)] public int MaxStack = 1;
        public InventoryItemEffect Effect;
        [Min(0f)] public float EffectAmount;
        public string UseLabel;
        [TextArea(2, 5)] public string UseResultText;
        public string RequiredStoryFactId;
        public string ResultStoryFactId;
        public bool ConsumeOnUse = true;
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
        [SerializeField] private List<StoryFactDefinition> storyFacts = new();
        [SerializeField] private List<InventoryItemDefinition> inventoryItems = new();

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
        public IReadOnlyList<StoryFactDefinition> StoryFacts => storyFacts;
        public IReadOnlyList<InventoryItemDefinition> InventoryItems =>
            inventoryItems;

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

        public StoryFactDefinition FindStoryFact(string factId)
        {
            if (string.IsNullOrWhiteSpace(factId))
            {
                return null;
            }

            for (int index = 0; index < storyFacts.Count; index++)
            {
                if (storyFacts[index].Id == factId)
                {
                    return storyFacts[index];
                }
            }

            return null;
        }

        public InventoryItemDefinition FindInventoryItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            for (int index = 0; index < inventoryItems.Count; index++)
            {
                if (inventoryItems[index].Id == itemId)
                {
                    return inventoryItems[index];
                }
            }

            return null;
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
            ValidateStoryFacts(errors);
            ValidateInventoryItems(errors);
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
                    if (condition != null
                        && condition.Type == DialogueConditionType.HasFact
                        && FindStoryFact(condition.StoryFactId) == null)
                    {
                        errors.Add(
                            $"Dialogue '{script.Id}' references missing story fact "
                            + $"condition: {condition.StoryFactId}"
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
                    else
                    {
                        ValidateEffectFacts(
                            errors,
                            script.Id,
                            choice.Effects
                        );
                    }
                }

                ValidateEffectFacts(
                    errors,
                    script.Id,
                    script.CompletionEffects
                );
            }

            foreach (ChapterStage stage in Enum.GetValues(typeof(ChapterStage)))
            {
                if (!coveredStages.Contains(stage))
                {
                    errors.Add($"No dialogue script covers stage: {stage}");
                }
            }
        }

        private void ValidateStoryFacts(List<string> errors)
        {
            if (storyFacts == null)
            {
                errors.Add("No story fact list is configured.");
                return;
            }

            HashSet<string> ids = new();
            for (int index = 0; index < storyFacts.Count; index++)
            {
                StoryFactDefinition fact = storyFacts[index];
                if (fact == null
                    || string.IsNullOrWhiteSpace(fact.Id)
                    || string.IsNullOrWhiteSpace(fact.Title)
                    || string.IsNullOrWhiteSpace(fact.Detail))
                {
                    errors.Add($"Story fact [{index}] is incomplete.");
                    continue;
                }
                if (!ids.Add(fact.Id))
                {
                    errors.Add($"Duplicate story fact ID: {fact.Id}");
                }
            }
        }

        private void ValidateInventoryItems(List<string> errors)
        {
            if (inventoryItems == null || inventoryItems.Count == 0)
            {
                errors.Add("No inventory item definitions are configured.");
                return;
            }

            HashSet<string> ids = new();
            for (int index = 0; index < inventoryItems.Count; index++)
            {
                InventoryItemDefinition item = inventoryItems[index];
                if (item == null
                    || string.IsNullOrWhiteSpace(item.Id)
                    || string.IsNullOrWhiteSpace(item.DisplayName)
                    || string.IsNullOrWhiteSpace(item.Category)
                    || string.IsNullOrWhiteSpace(item.Description))
                {
                    errors.Add($"Inventory item [{index}] is incomplete.");
                    continue;
                }
                if (!ids.Add(item.Id))
                {
                    errors.Add($"Duplicate inventory item ID: {item.Id}");
                }
                if (item.MaxStack < 1)
                {
                    errors.Add($"Inventory item '{item.Id}' has invalid stack size.");
                }
                if (item.Effect == InventoryItemEffect.RestoreHealth
                    && item.EffectAmount <= 0f)
                {
                    errors.Add(
                        $"Inventory item '{item.Id}' has invalid heal amount."
                    );
                }
                if (item.Effect == InventoryItemEffect.GrantStoryFact
                    && (string.IsNullOrWhiteSpace(item.UseLabel)
                        || string.IsNullOrWhiteSpace(item.UseResultText)
                        || FindStoryFact(item.ResultStoryFactId) == null))
                {
                    errors.Add(
                        $"Inventory item '{item.Id}' has invalid story effect."
                    );
                }
                if (!string.IsNullOrWhiteSpace(item.RequiredStoryFactId)
                    && FindStoryFact(item.RequiredStoryFactId) == null)
                {
                    errors.Add(
                        $"Inventory item '{item.Id}' requires missing fact "
                        + $"'{item.RequiredStoryFactId}'."
                    );
                }
            }
        }

        private void ValidateEffectFacts(
            List<string> errors,
            string dialogueId,
            List<DialogueEffectDefinition> effects)
        {
            if (effects == null)
            {
                return;
            }

            for (int index = 0; index < effects.Count; index++)
            {
                DialogueEffectDefinition effect = effects[index];
                if (effect != null
                    && effect.Type == DialogueEffectType.SetFact
                    && FindStoryFact(effect.StoryFactId) == null)
                {
                    errors.Add(
                        $"Dialogue '{dialogueId}' references missing story fact "
                        + $"effect: {effect.StoryFactId}"
                    );
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
            List<DialogueScriptDefinition> newDialogueScripts,
            List<StoryFactDefinition> newStoryFacts,
            List<InventoryItemDefinition> newInventoryItems)
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
            storyFacts = newStoryFacts;
            inventoryItems = newInventoryItems;
            contentVersion = 6;
        }

        public void ConfigureStoryContentForEditor(
            List<DialogueScriptDefinition> newDialogueScripts,
            List<StoryFactDefinition> newStoryFacts)
        {
            dialogueScripts = newDialogueScripts;
            storyFacts = newStoryFacts;
            contentVersion = 4;
        }

        public void ConfigureInventoryContentForEditor(
            List<DialogueScriptDefinition> newDialogueScripts,
            List<StoryFactDefinition> newStoryFacts,
            List<InventoryItemDefinition> newInventoryItems)
        {
            dialogueScripts = newDialogueScripts;
            storyFacts = newStoryFacts;
            inventoryItems = newInventoryItems;
            contentVersion = 6;
        }
#endif
    }
}
