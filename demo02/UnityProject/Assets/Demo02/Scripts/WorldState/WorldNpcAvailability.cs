using System.Collections.Generic;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.WorldState
{
    [DefaultExecutionOrder(-10)]
    public sealed class WorldNpcAvailability : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private ChapterStage minimumStage =
            ChapterStage.Resolved;
        [SerializeField] private ConclusionChoice requiredConclusion;
        [SerializeField] private List<string> requiredFactIds = new();
        [SerializeField] private List<string> forbiddenFactIds = new();

        private InvestigationState investigationState;

        public GameObject Target => target;

        private void Start()
        {
            investigationState =
                FindAnyObjectByType<InvestigationState>();
            if (investigationState != null)
            {
                investigationState.StageChanged += OnStageChanged;
                investigationState.StoryFactAdded += OnStoryFactAdded;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (investigationState != null)
            {
                investigationState.StageChanged -= OnStageChanged;
                investigationState.StoryFactAdded -= OnStoryFactAdded;
            }
        }

        public void Configure(
            GameObject npcTarget,
            ChapterStage stage,
            ConclusionChoice conclusion,
            IEnumerable<string> requiredFacts = null,
            IEnumerable<string> forbiddenFacts = null)
        {
            target = npcTarget;
            minimumStage = stage;
            requiredConclusion = conclusion;
            requiredFactIds = requiredFacts != null
                ? new List<string>(requiredFacts)
                : new List<string>();
            forbiddenFactIds = forbiddenFacts != null
                ? new List<string>(forbiddenFacts)
                : new List<string>();
        }

        public void Refresh()
        {
            if (target == null)
            {
                return;
            }

            bool available = investigationState != null
                && investigationState.Stage >= minimumStage
                && (requiredConclusion == ConclusionChoice.None
                    || investigationState.SelectedConclusion
                        == requiredConclusion);
            if (available)
            {
                for (int index = 0;
                     index < requiredFactIds.Count;
                     index++)
                {
                    if (!investigationState.HasFact(
                            requiredFactIds[index]
                        ))
                    {
                        available = false;
                        break;
                    }
                }
            }
            if (available)
            {
                for (int index = 0;
                     index < forbiddenFactIds.Count;
                     index++)
                {
                    if (investigationState.HasFact(
                            forbiddenFactIds[index]
                        ))
                    {
                        available = false;
                        break;
                    }
                }
            }

            target.SetActive(available);
        }

        private void OnStageChanged(ChapterStage stage)
        {
            Refresh();
        }

        private void OnStoryFactAdded(string factId)
        {
            Refresh();
        }
    }
}
