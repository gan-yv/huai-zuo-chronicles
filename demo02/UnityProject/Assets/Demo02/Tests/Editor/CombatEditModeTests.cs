using Demo02.Editor;
using NUnit.Framework;

namespace Demo02.Tests.EditMode
{
    [TestFixture]
    [Category("Demo02")]
    [Category("Combat")]
    public sealed class CombatEditModeTests
    {
        [Test]
        public void CombatProfiles_DamageInventoryAndClassesRemainValid()
        {
            Assert.DoesNotThrow(
                () =>
                {
                    Demo02CombatRegressionSuite
                        .VerifyCombatDamageForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyMeleeComboForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyPerfectDodgeCounterForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyEnemyAttackCadenceForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyEnemyDiversityForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyInventoryForBatch();
                    Demo02CombatRegressionSuite
                        .VerifyCharacterFrameworkForBatch();
                }
            );
        }

        [Test]
        public void EnemyArchetypes_ExposeDistinctBehaviors()
        {
            Assert.DoesNotThrow(
                Demo02CombatRegressionSuite
                    .VerifyEnemyDiversityForBatch
            );
        }
    }
}
