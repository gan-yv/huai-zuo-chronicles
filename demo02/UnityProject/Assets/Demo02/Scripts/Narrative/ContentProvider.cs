using Demo02.Combat;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Narrative
{
    public interface IContentProvider
    {
        GameContentCatalog Catalog { get; }
        ChapterDefinition DefaultChapter { get; }
        ChapterDefinition FindChapter(string chapterId);
        CharacterDefinition FindCharacter(string characterId);
        WorldEncounterDefinition FindEncounter(string encounterId);
        WorldStreamingRegionDefinition FindStreamingRegion(
            string regionId);
    }

    internal sealed class ResourcesContentProvider : IContentProvider
    {
        public const string CatalogResourcePath =
            "Data/Demo02ContentCatalog";

        private GameContentCatalog catalog;

        public GameContentCatalog Catalog =>
            catalog != null
                ? catalog
                : catalog = Resources.Load<GameContentCatalog>(
                    CatalogResourcePath
                );

        public ChapterDefinition DefaultChapter =>
            Catalog?.DefaultChapter;

        public ChapterDefinition FindChapter(string chapterId) =>
            Catalog?.FindChapter(chapterId);

        public CharacterDefinition FindCharacter(string characterId) =>
            Catalog?.FindCharacter(characterId);

        public WorldEncounterDefinition FindEncounter(
            string encounterId) =>
            Catalog?.FindEncounter(encounterId);

        public WorldStreamingRegionDefinition FindStreamingRegion(
            string regionId) =>
            Catalog?.FindStreamingRegion(regionId);
    }
}
