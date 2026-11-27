using UnityEditor;

namespace Demo02.Editor
{
    /// <summary>
    /// Stable editor entry point. Scene construction is delegated to
    /// category assemblers coordinated by Demo02GreyboxSceneAssembler.
    /// </summary>
    public static class Demo02GreyboxBuilder
    {
        [InitializeOnLoadMethod]
        private static void QueueInitialBuild()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Demo02/Rebuild Greybox Scene")]
        public static void RebuildFromMenu()
        {
            Demo02ContentPipeline.BuildAll();
        }

        public static void RebuildForBatch()
        {
            Demo02ContentPipeline.BuildAll();
        }

        internal static void BuildWorldArtifactsForPipeline()
        {
            Demo02GreyboxSceneAssembler.BuildScene(forceRebuild: true);
            Demo02GreyboxGameplayAssembler
                .BuildStreamingEncounterScenes();
            Demo02GreyboxBootAssembler.BuildBootScene();
            Demo02GreyboxBootAssembler.ConfigureBuildSettings();
        }

        public static void VerifyCombatDamageForBatch()
        {
            Demo02RegressionVerifier.VerifyCombatDamageForBatch();
        }

        public static void VerifyInventoryForBatch()
        {
            Demo02RegressionVerifier.VerifyInventoryForBatch();
        }

        public static void VerifyDialogueContentForBatch()
        {
            Demo02RegressionVerifier.VerifyDialogueContentForBatch();
        }

        public static void VerifyTimeLayerForBatch()
        {
            Demo02RegressionVerifier.VerifyTimeLayerForBatch();
        }

        public static void VerifySaveSlotsForBatch()
        {
            Demo02RegressionVerifier.VerifySaveSlotsForBatch();
        }

        public static void VerifyChapterFlowForBatch()
        {
            Demo02RegressionVerifier.VerifyChapterFlowForBatch();
        }

        public static void VerifyInputActionsForBatch()
        {
            Demo02RegressionVerifier.VerifyInputActionsForBatch();
        }

        public static void VerifyEnemyDiversityForBatch()
        {
            Demo02RegressionVerifier.VerifyEnemyDiversityForBatch();
        }

        public static void VerifyUiArchitectureForBatch()
        {
            Demo02RegressionVerifier.VerifyUiArchitectureForBatch();
        }

        public static void VerifySaveSuiteForBatch()
        {
            Demo02RegressionVerifier.VerifySaveSuiteForBatch();
        }

        public static void VerifyStorySuiteForBatch()
        {
            Demo02RegressionVerifier.VerifyStorySuiteForBatch();
        }

        public static void VerifyCombatSuiteForBatch()
        {
            Demo02RegressionVerifier.VerifyCombatSuiteForBatch();
        }

        public static void VerifyDataSuiteForBatch()
        {
            Demo02RegressionVerifier.VerifyDataSuiteForBatch();
        }

        public static void VerifyWorldSuiteForBatch()
        {
            Demo02RegressionVerifier.VerifyWorldSuiteForBatch();
        }

        public static void VerifyInputUiSuiteForBatch()
        {
            Demo02RegressionVerifier.VerifyInputUiSuiteForBatch();
        }

        public static void VerifyAllForBatch()
        {
            Demo02RegressionVerifier.VerifyAllForBatch();
        }

        private static void BuildIfNeeded()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    Demo02GreyboxScenePaths.ScenePath
                ) == null)
            {
                EditorApplication.delayCall += BuildIfNeeded;
                return;
            }

            Demo02GreyboxSceneAssembler.BuildScene(
                forceRebuild: false
            );
            Demo02GreyboxBootAssembler.BuildBootScene();
            Demo02GreyboxBootAssembler.ConfigureBuildSettings();
        }
    }
}
