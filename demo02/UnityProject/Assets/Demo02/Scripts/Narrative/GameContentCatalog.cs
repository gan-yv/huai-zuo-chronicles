using System;
using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Narrative
{
    [CreateAssetMenu(
        fileName = "GameContentCatalog",
        menuName = "Demo02/Content/Game Content Catalog"
    )]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private string defaultChapterId = "chapter-01";
        [SerializeField] private List<ChapterDefinition> chapters = new();
        [SerializeField] private List<CharacterDefinition> characters = new();
        [SerializeField] private List<WorldEncounterDefinition> encounters =
            new();
        [SerializeField]
        private List<WorldStreamingRegionDefinition> streamingRegions = new();
        [NonSerialized]
        private Dictionary<string, ChapterDefinition> chapterIndex;
        [NonSerialized]
        private Dictionary<string, CharacterDefinition> characterIndex;
        [NonSerialized]
        private Dictionary<string, WorldEncounterDefinition> encounterIndex;
        [NonSerialized]
        private Dictionary<string, WorldStreamingRegionDefinition>
            streamingRegionIndex;

        public string DefaultChapterId => defaultChapterId;
        public IReadOnlyList<ChapterDefinition> Chapters => chapters;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public IReadOnlyList<WorldEncounterDefinition> Encounters =>
            encounters;
        public IReadOnlyList<WorldStreamingRegionDefinition>
            StreamingRegions => streamingRegions;

        public ChapterDefinition FindChapter(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return null;
            }
            EnsureIndexes();
            return chapterIndex.TryGetValue(
                chapterId,
                out ChapterDefinition definition
            )
                ? definition
                : null;
        }

        public ChapterDefinition DefaultChapter =>
            FindChapter(defaultChapterId);

        public CharacterDefinition FindCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }
            EnsureIndexes();
            return characterIndex.TryGetValue(
                characterId,
                out CharacterDefinition definition
            )
                ? definition
                : null;
        }

        public WorldEncounterDefinition FindEncounter(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                return null;
            }
            EnsureIndexes();
            return encounterIndex.TryGetValue(
                encounterId,
                out WorldEncounterDefinition definition
            )
                ? definition
                : null;
        }

        public WorldStreamingRegionDefinition FindStreamingRegion(
            string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return null;
            }
            EnsureIndexes();
            return streamingRegionIndex.TryGetValue(
                regionId,
                out WorldStreamingRegionDefinition definition
            )
                ? definition
                : null;
        }

        public List<string> Validate()
        {
            List<string> errors = new();
            if (chapters == null || chapters.Count == 0)
            {
                errors.Add("Game content catalog has no chapters.");
                return errors;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (ChapterDefinition chapter in chapters)
            {
                if (chapter == null)
                {
                    errors.Add("Game content catalog contains a null chapter.");
                    continue;
                }
                if (!ids.Add(chapter.ChapterId))
                {
                    errors.Add($"Duplicate chapter ID: {chapter.ChapterId}");
                }
                errors.AddRange(chapter.Validate());
            }
            if (FindChapter(defaultChapterId) == null)
            {
                errors.Add(
                    $"Default chapter is missing: {defaultChapterId}"
                );
            }
            ValidateCharacters(errors);
            ValidateEncounters(errors);
            ValidateStreamingRegions(errors);
            return errors;
        }

        private void ValidateCharacters(List<string> errors)
        {
            if (characters == null || characters.Count == 0)
            {
                errors.Add("Game content catalog has no characters.");
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (CharacterDefinition definition in characters)
            {
                if (definition == null)
                {
                    errors.Add("Game content catalog has a null character.");
                    continue;
                }
                if (!ids.Add(definition.CharacterId))
                {
                    errors.Add(
                        $"Duplicate character ID: " +
                        $"{definition.CharacterId}"
                    );
                }
                if (!definition.IsValid(out string error))
                {
                    errors.Add(error);
                }
            }
        }

        private void ValidateEncounters(List<string> errors)
        {
            if (encounters == null || encounters.Count == 0)
            {
                errors.Add("Game content catalog has no encounters.");
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (WorldEncounterDefinition definition in encounters)
            {
                if (definition == null)
                {
                    errors.Add("Game content catalog has a null encounter.");
                    continue;
                }
                if (!ids.Add(definition.EncounterId))
                {
                    errors.Add(
                        $"Duplicate encounter ID: " +
                        $"{definition.EncounterId}"
                    );
                }
                if (!definition.IsValid(out string error))
                {
                    errors.Add(error);
                }
            }
        }

        private void ValidateStreamingRegions(List<string> errors)
        {
            if (streamingRegions == null || streamingRegions.Count == 0)
            {
                errors.Add(
                    "Game content catalog has no streaming regions."
                );
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (WorldStreamingRegionDefinition definition in
                     streamingRegions)
            {
                if (definition == null)
                {
                    errors.Add(
                        "Game content catalog has a null streaming region."
                    );
                    continue;
                }
                if (!ids.Add(definition.RegionId))
                {
                    errors.Add(
                        $"Duplicate streaming region ID: " +
                        $"{definition.RegionId}"
                    );
                }
                errors.AddRange(definition.Validate());
                if (definition.EncounterContent != null
                    && FindEncounter(
                        definition.EncounterContent.EncounterId
                    ) == null)
                {
                    errors.Add(
                        $"Streaming region '{definition.RegionId}' " +
                        "references an encounter outside the catalog: " +
                        $"{definition.EncounterContent.EncounterId}"
                    );
                }
            }
        }

        private void EnsureIndexes()
        {
            if (chapterIndex != null
                && characterIndex != null
                && encounterIndex != null
                && streamingRegionIndex != null)
            {
                return;
            }

            chapterIndex = BuildIndex(
                chapters,
                chapter => chapter != null ? chapter.ChapterId : null
            );
            characterIndex = BuildIndex(
                characters,
                character => character != null
                    ? character.CharacterId
                    : null
            );
            encounterIndex = BuildIndex(
                encounters,
                encounter => encounter != null
                    ? encounter.EncounterId
                    : null
            );
            streamingRegionIndex = BuildIndex(
                streamingRegions,
                region => region != null ? region.RegionId : null
            );
        }

        private static Dictionary<string, T> BuildIndex<T>(
            IReadOnlyList<T> definitions,
            Func<T, string> getId)
            where T : UnityEngine.Object
        {
            Dictionary<string, T> result =
                new(StringComparer.Ordinal);
            if (definitions == null)
            {
                return result;
            }
            foreach (T definition in definitions)
            {
                string id = getId(definition);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.TryAdd(id, definition);
                }
            }
            return result;
        }

        private void ClearIndexes()
        {
            chapterIndex = null;
            characterIndex = null;
            encounterIndex = null;
            streamingRegionIndex = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClearIndexes();
        }
#endif

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string defaultId,
            List<ChapterDefinition> availableChapters,
            List<CharacterDefinition> availableCharacters,
            List<WorldEncounterDefinition> availableEncounters,
            List<WorldStreamingRegionDefinition>
                availableStreamingRegions)
        {
            defaultChapterId = defaultId;
            chapters = availableChapters ?? new List<ChapterDefinition>();
            characters =
                availableCharacters ?? new List<CharacterDefinition>();
            encounters =
                availableEncounters ?? new List<WorldEncounterDefinition>();
            streamingRegions = availableStreamingRegions
                ?? new List<WorldStreamingRegionDefinition>();
            ClearIndexes();
        }
#endif
    }
}
