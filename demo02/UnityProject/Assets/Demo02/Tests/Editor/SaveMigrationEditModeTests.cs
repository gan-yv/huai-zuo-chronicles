using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Save")]
    public sealed class SaveMigrationEditModeTests
    {
        [Test]
        public void SaveSlots_MigrateValidateAndRecover()
        {
            Assert.DoesNotThrow(
                Demo02SaveRegressionSuite.VerifySaveSlotsForBatch
            );
        }
    }
}
