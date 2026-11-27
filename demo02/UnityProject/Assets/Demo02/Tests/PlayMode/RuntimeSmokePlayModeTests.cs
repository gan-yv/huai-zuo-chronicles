using System.Collections;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Demo02.Tests.PlayMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("PlayMode")]
    public sealed class RuntimeSmokePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDownRuntime()
        {
            GameServices.Clear();
            Time.timeScale = 1f;

            foreach (GameFlowController controller in
                     Object.FindObjectsByType<GameFlowController>(
                         FindObjectsSortMode.None
                     ))
            {
                Object.Destroy(controller.gameObject);
            }

            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime != null)
            {
                Object.Destroy(runtime.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BootScene_LoadsAndResolvesContent()
        {
            yield return LoadSingleScene(SceneNames.Boot);
            yield return null;

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(SceneNames.Boot)
            );
            Assert.That(
                Object.FindAnyObjectByType<BootController>(),
                Is.Not.Null
            );

            ChapterDefinition chapter =
                GameContentRegistry.DefaultChapter;
            Assert.That(chapter, Is.Not.Null);
            Assert.That(chapter.CaseContent, Is.Not.Null);
            Assert.That(
                chapter.SceneName,
                Is.EqualTo(SceneNames.Guangling)
            );
        }

        [UnityTest]
        public IEnumerator GameplayScene_StartsCoreRuntimeServices()
        {
            yield return LoadSingleScene(SceneNames.Boot);

            ChapterDefinition chapter =
                GameContentRegistry.DefaultChapter;
            Assert.That(chapter, Is.Not.Null);
            yield return LoadSingleScene(chapter.SceneName);
            yield return null;

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(chapter.SceneName)
            );
            Assert.That(WorldRuntime.Instance, Is.Not.Null);
            Assert.That(GameFlowController.Instance, Is.Not.Null);
            Assert.That(
                Object.FindAnyObjectByType<SaveSystem>(),
                Is.Not.Null
            );
            Assert.That(
                Object.FindAnyObjectByType<PlayerPartyController>(),
                Is.Not.Null
            );
            Assert.That(
                GameServices.TryGet<IGameFlowState>(
                    out IGameFlowState flowState
                ),
                Is.True
            );
            Assert.That(flowState, Is.SameAs(GameFlowController.Instance));
        }

        [UnityTest]
        public IEnumerator StreamingScenes_LoadAndUnloadFromBuildSettings()
        {
            yield return LoadSingleScene(SceneNames.Boot);

            GameContentCatalog catalog = GameContentRegistry.Catalog;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.StreamingRegions.Count, Is.GreaterThan(0));

            foreach (WorldStreamingRegionDefinition region in
                     catalog.StreamingRegions)
            {
                Assert.That(region, Is.Not.Null);
                Assert.That(
                    string.IsNullOrWhiteSpace(region.SceneName),
                    Is.False
                );

                AsyncOperation load = SceneManager.LoadSceneAsync(
                    region.SceneName,
                    LoadSceneMode.Additive
                );
                Assert.That(load, Is.Not.Null);
                yield return load;

                Scene scene = SceneManager.GetSceneByName(region.SceneName);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static IEnumerator LoadSingleScene(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );
            Assert.That(operation, Is.Not.Null);
            yield return operation;
        }
    }
}
