using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Narrative
{
    [CreateAssetMenu(
        fileName = "ChapterDefinition",
        menuName = "Demo02/Content/Chapter Definition"
    )]
    public sealed class ChapterDefinition : ScriptableObject
    {
        [SerializeField] private string chapterId = "chapter-01";
        [SerializeField] private string displayName = "消失的盐引";
        [SerializeField] private string sceneName = "Greybox";
        [SerializeField] private string entrySpawnId = "guangling_start";
        [SerializeField] private string entryAreaId = "official-street";
        [SerializeField] private string entryAreaName = "官署前街";
        [SerializeField] private CaseDefinition caseContent;

        public string ChapterId => chapterId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public string EntrySpawnId => entrySpawnId;
        public string EntryAreaId => entryAreaId;
        public string EntryAreaName => entryAreaName;
        public CaseDefinition CaseContent => caseContent;

        public List<string> Validate()
        {
            List<string> errors = new();
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                errors.Add("Chapter ID is empty.");
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add($"Chapter '{chapterId}' has no display name.");
            }
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                errors.Add($"Chapter '{chapterId}' has no scene name.");
            }
            if (string.IsNullOrWhiteSpace(entrySpawnId))
            {
                errors.Add($"Chapter '{chapterId}' has no entry spawn.");
            }
            if (string.IsNullOrWhiteSpace(entryAreaId)
                || string.IsNullOrWhiteSpace(entryAreaName))
            {
                errors.Add($"Chapter '{chapterId}' has incomplete area data.");
            }
            if (caseContent == null)
            {
                errors.Add($"Chapter '{chapterId}' has no case content.");
            }
            else
            {
                errors.AddRange(caseContent.Validate());
            }
            return errors;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            string title,
            string scene,
            string spawnId,
            string areaId,
            string areaDisplayName,
            CaseDefinition content)
        {
            chapterId = id;
            displayName = title;
            sceneName = scene;
            entrySpawnId = spawnId;
            entryAreaId = areaId;
            entryAreaName = areaDisplayName;
            caseContent = content;
        }
#endif
    }

}
