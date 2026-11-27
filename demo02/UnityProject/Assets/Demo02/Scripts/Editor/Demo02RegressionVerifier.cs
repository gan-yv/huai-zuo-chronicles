using UnityEngine;

namespace Demo02.Editor
{
    /// <summary>
    /// Stable regression facade used by editor menus, command-line entry
    /// points and EditMode tests. Domain implementations live in the
    /// Demo02*RegressionSuite classes.
    /// </summary>
    internal static class Demo02RegressionVerifier
    {
        public static void VerifyAllForBatch()
        {
            VerifySaveSuiteForBatch();
            VerifyStorySuiteForBatch();
            VerifyCombatSuiteForBatch();
            VerifyDataSuiteForBatch();
            VerifyWorldSuiteForBatch();
            VerifyInputUiSuiteForBatch();
            Debug.Log(
                "All automated checks passed: save, story, combat, data, "
                + "world streaming and input/UI."
            );
        }

        public static void VerifySaveSuiteForBatch()
        {
            Demo02SaveRegressionSuite.VerifySaveSlotsForBatch();
        }

        public static void VerifyStorySuiteForBatch()
        {
            Demo02StoryRegressionSuite.VerifyDialogueContentForBatch();
            Demo02StoryRegressionSuite.VerifyTimeLayerForBatch();
            Demo02StoryRegressionSuite.VerifyChapterFlowForBatch();
        }

        public static void VerifyCombatSuiteForBatch()
        {
            Demo02CombatRegressionSuite.VerifyCombatDamageForBatch();
            Demo02CombatRegressionSuite.VerifyMeleeComboForBatch();
            Demo02CombatRegressionSuite.VerifyPerfectDodgeCounterForBatch();
            Demo02CombatRegressionSuite.VerifyEnemyAttackCadenceForBatch();
            Demo02CombatRegressionSuite.VerifyEnemyDiversityForBatch();
            Demo02CombatRegressionSuite.VerifyInventoryForBatch();
            Demo02CombatRegressionSuite.VerifyCharacterFrameworkForBatch();
        }

        public static void VerifyDataSuiteForBatch()
        {
            Demo02DataRegressionSuite.VerifyContentCatalog();
        }

        public static void VerifyWorldSuiteForBatch()
        {
            Demo02WorldRegressionSuite.VerifyOpenWorldFrameworkForBatch();
            Demo02WorldRegressionSuite.VerifyStreamingSceneForBatch();
            Demo02WorldRegressionSuite.VerifyMapReadabilityForBatch();
        }

        public static void VerifyInputUiSuiteForBatch()
        {
            Demo02InputUiRegressionSuite.VerifyInputActionsForBatch();
            Demo02InputUiRegressionSuite.VerifyUiArchitectureForBatch();
        }

        public static void VerifyCombatDamageForBatch()
        {
            Demo02CombatRegressionSuite.VerifyCombatDamageForBatch();
        }

        public static void VerifyInventoryForBatch()
        {
            Demo02CombatRegressionSuite.VerifyInventoryForBatch();
        }

        public static void VerifyDialogueContentForBatch()
        {
            Demo02StoryRegressionSuite.VerifyDialogueContentForBatch();
        }

        public static void VerifyTimeLayerForBatch()
        {
            Demo02StoryRegressionSuite.VerifyTimeLayerForBatch();
        }

        public static void VerifySaveSlotsForBatch()
        {
            Demo02SaveRegressionSuite.VerifySaveSlotsForBatch();
        }

        public static void VerifyChapterFlowForBatch()
        {
            Demo02StoryRegressionSuite.VerifyChapterFlowForBatch();
        }

        public static void VerifyInputActionsForBatch()
        {
            Demo02InputUiRegressionSuite.VerifyInputActionsForBatch();
        }

        public static void VerifyEnemyDiversityForBatch()
        {
            Demo02CombatRegressionSuite.VerifyEnemyDiversityForBatch();
        }

        public static void VerifyUiArchitectureForBatch()
        {
            Demo02InputUiRegressionSuite.VerifyUiArchitectureForBatch();
        }
    }
}
