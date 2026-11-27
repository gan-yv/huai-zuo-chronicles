using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("InputUi")]
    public sealed class InputUiEditModeTests
    {
        [Test]
        public void InputActionsAndMigratedUi_RemainValid()
        {
            Assert.DoesNotThrow(
                () =>
                {
                    Demo02InputUiRegressionSuite
                        .VerifyInputActionsForBatch();
                    Demo02InputUiRegressionSuite
                        .VerifyUiArchitectureForBatch();
                }
            );
        }
    }
}
