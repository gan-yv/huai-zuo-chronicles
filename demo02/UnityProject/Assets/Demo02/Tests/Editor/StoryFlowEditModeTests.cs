using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Story")]
    public sealed class StoryFlowEditModeTests
    {
        [Test]
        public void DialogueContent_UnlocksExpectedScripts()
        {
            Assert.DoesNotThrow(
                Demo02StoryRegressionSuite.VerifyDialogueContentForBatch
            );
        }

        [Test]
        public void ChapterAndTimeLayer_CompleteExpectedFlow()
        {
            Assert.DoesNotThrow(() =>
            {
                Demo02StoryRegressionSuite.VerifyTimeLayerForBatch();
                Demo02StoryRegressionSuite.VerifyChapterFlowForBatch();
            });
        }
    }
}
