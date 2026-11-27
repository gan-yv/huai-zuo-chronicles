using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Versioning")]
    public sealed class VersioningEditModeTests
    {
        [Test]
        public void VersionSources_RemainSynchronized()
        {
            Assert.DoesNotThrow(
                Demo02VersionAudit.VerifyForBatch
            );
        }
    }
}
