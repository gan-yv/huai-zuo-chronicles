using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("World")]
    public sealed class WorldStreamingEditModeTests
    {
        [Test]
        public void OpenWorldAndStreamingScenes_RemainPlayable()
        {
            Assert.DoesNotThrow(
                () =>
                {
                    Demo02WorldRegressionSuite
                        .VerifyOpenWorldFrameworkForBatch();
                    Demo02WorldRegressionSuite
                        .VerifyStreamingSceneForBatch();
                    Demo02WorldRegressionSuite
                        .VerifyStreamingRetryStateForBatch();
                    Demo02WorldRegressionSuite
                        .VerifyMapReadabilityForBatch();
                }
            );
        }
    }
}
