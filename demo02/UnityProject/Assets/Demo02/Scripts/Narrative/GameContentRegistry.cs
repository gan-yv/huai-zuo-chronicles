using Demo02.Core;

namespace Demo02.Narrative
{
    public static class GameContentRegistry
    {
        public const string CatalogResourcePath =
            ResourcesContentProvider.CatalogResourcePath;

        private static IContentProvider fallbackProvider;

        public static IContentProvider Provider
        {
            get
            {
                if (GameServices.TryGet<IContentProvider>(
                        out IContentProvider registered
                    ))
                {
                    return registered;
                }
                return fallbackProvider ??=
                    new ResourcesContentProvider();
            }
        }

        public static GameContentCatalog Catalog => Provider.Catalog;

        public static ChapterDefinition DefaultChapter =>
            Provider.DefaultChapter;

        public static ChapterDefinition FindChapter(string chapterId) =>
            Provider.FindChapter(chapterId);

        public static Combat.WorldEncounterDefinition FindEncounter(
            string encounterId) =>
            Provider.FindEncounter(encounterId);

        public static Player.CharacterDefinition FindCharacter(
            string characterId) =>
            Provider.FindCharacter(characterId);

        public static WorldState.WorldStreamingRegionDefinition
            FindStreamingRegion(string regionId) =>
                Provider.FindStreamingRegion(regionId);
    }
}
