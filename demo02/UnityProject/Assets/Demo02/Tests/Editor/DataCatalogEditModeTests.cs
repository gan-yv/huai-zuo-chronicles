using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Data")]
    public sealed class DataCatalogEditModeTests
    {
        [Test]
        public void ContentModules_ValidateCatalogAndReferences()
        {
            Assert.DoesNotThrow(
                Demo02DataRegressionSuite.VerifyContentCatalog
            );
        }
    }
}
