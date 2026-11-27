using Demo02.Editor;
using Demo02.Core;
using Demo02.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Architecture")]
    public sealed class ArchitectureEditModeTests
    {
        [Test]
        public void AssemblyBoundaries_RemainOneWay()
        {
            Assert.DoesNotThrow(
                Demo02ArchitectureAudit.VerifyForBatch
            );
        }

        private sealed class FakeFlowState : IGameFlowState
        {
            public bool IsPaused { get; set; }
            public bool IsGameEnding { get; set; }
        }

        [Test]
        public void ServiceRegistry_AllowsContractReplacementAndRemoval()
        {
            GameServices.Clear();
            FakeFlowState first = new() { IsPaused = true };
            FakeFlowState second = new() { IsGameEnding = true };
            try
            {
                GameServices.Register<IGameFlowState>(first);
                Assert.That(
                    GameServices.TryGet<IGameFlowState>(
                        out IGameFlowState resolved
                    ),
                    Is.True
                );
                Assert.That(resolved.IsPaused, Is.True);

                GameServices.Register<IGameFlowState>(second);
                Assert.That(
                    GameServices.TryGet<IGameFlowState>(
                        out resolved
                    ),
                    Is.True
                );
                Assert.That(resolved.IsGameEnding, Is.True);
                Assert.That(
                    GameServices.Unregister<IGameFlowState>(first),
                    Is.False
                );
                Assert.That(
                    GameServices.Unregister<IGameFlowState>(second),
                    Is.True
                );
                Assert.That(
                    GameServices.TryGet<IGameFlowState>(out _),
                    Is.False
                );
            }
            finally
            {
                GameServices.Clear();
            }
        }

        private sealed class FakeContentProvider : IContentProvider
        {
            public GameContentCatalog Catalog => null;
            public ChapterDefinition DefaultChapter { get; set; }
            public ChapterDefinition FindChapter(string chapterId) =>
                DefaultChapter;
            public Combat.WorldEncounterDefinition FindEncounter(
                string encounterId) => null;
            public Player.CharacterDefinition FindCharacter(
                string characterId) => null;
            public WorldState.WorldStreamingRegionDefinition
                FindStreamingRegion(string regionId) => null;
        }

        [Test]
        public void ContentFacade_ResolvesRegisteredProvider()
        {
            GameServices.Clear();
            ChapterDefinition chapter =
                ScriptableObject.CreateInstance<ChapterDefinition>();
            FakeContentProvider provider = new()
            {
                DefaultChapter = chapter
            };
            try
            {
                GameServices.Register<IContentProvider>(provider);
                Assert.That(
                    GameContentRegistry.Provider,
                    Is.SameAs(provider)
                );
                Assert.That(
                    GameContentRegistry.DefaultChapter,
                    Is.SameAs(chapter)
                );
            }
            finally
            {
                GameServices.Clear();
                Object.DestroyImmediate(chapter);
            }
        }
    }
}
