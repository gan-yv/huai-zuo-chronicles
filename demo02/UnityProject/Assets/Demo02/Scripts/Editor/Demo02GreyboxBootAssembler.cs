using System.Collections.Generic;
using System.Linq;
using Demo02.Core;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    /// <summary>
    /// Builds the boot scene, title presentation and Build Settings.
    /// </summary>
    internal static class Demo02GreyboxBootAssembler
    {
        public static void BuildBootScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            bool hasUnsavedStartupScene =
                string.IsNullOrWhiteSpace(previousActiveScene.path);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    Demo02GreyboxScenePaths.BootScenePath
                ) != null)
            {
                Scene existingBootScene =
                    EditorSceneManager.OpenScene(
                        Demo02GreyboxScenePaths.BootScenePath,
                        OpenSceneMode.Additive
                    );
                GameObject existingBootRoot = existingBootScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root => root.name == "Boot");
                if (existingBootRoot != null)
                {
                    ConfigurePresentation(
                        existingBootScene,
                        existingBootRoot
                    );
                    EditorSceneManager.MarkSceneDirty(
                        existingBootScene
                    );
                    EditorSceneManager.SaveScene(
                        existingBootScene,
                        Demo02GreyboxScenePaths.BootScenePath
                    );
                }
                EditorSceneManager.CloseScene(
                    existingBootScene,
                    removeScene: true
                );
                if (previousActiveScene.IsValid()
                    && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
                ConfigureBuildSettings();
                return;
            }

            Scene bootScene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                hasUnsavedStartupScene
                    ? NewSceneMode.Single
                    : NewSceneMode.Additive
            );
            SceneManager.SetActiveScene(bootScene);

            GameObject bootRoot = new("Boot");
            SceneManager.MoveGameObjectToScene(bootRoot, bootScene);
            bootRoot.AddComponent<BootController>();

            GameObject worldRuntimeObject = new("WorldRuntime");
            SceneManager.MoveGameObjectToScene(
                worldRuntimeObject,
                bootScene
            );
            worldRuntimeObject.AddComponent<WorldRuntime>();
            ConfigurePresentation(bootScene, bootRoot);

            EditorSceneManager.SaveScene(
                bootScene,
                Demo02GreyboxScenePaths.BootScenePath
            );
            if (!hasUnsavedStartupScene)
            {
                EditorSceneManager.CloseScene(
                    bootScene,
                    removeScene: true
                );
            }
            if (!hasUnsavedStartupScene
                && previousActiveScene.IsValid()
                && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }
            Debug.Log("Demo02 boot scene generated.");
        }

        public static void ConfigureBuildSettings()
        {
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                Demo02GreyboxScenePaths.BootScenePath
            );
            SceneAsset greyboxScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    Demo02GreyboxScenePaths.ScenePath
                );
            if (bootScene == null || greyboxScene == null)
            {
                Debug.LogWarning(
                    "World scenes are not ready for Build Settings yet."
                );
                return;
            }

            List<EditorBuildSettingsScene> buildScenes = new()
            {
                new EditorBuildSettingsScene(
                    Demo02GreyboxScenePaths.BootScenePath,
                    true
                ),
                new EditorBuildSettingsScene(
                    Demo02GreyboxScenePaths.ScenePath,
                    true
                )
            };
            foreach (WorldStreamingRegionDefinition definition in
                     WorldStreamingRegionSeeder.LoadAll())
            {
                string scenePath =
                    WorldStreamingRegionSeeder.GetScenePath(definition);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)
                    == null)
                {
                    Debug.LogWarning(
                        "Streaming scene is not ready for Build Settings: "
                        + scenePath
                    );
                    return;
                }
                buildScenes.Add(
                    new EditorBuildSettingsScene(scenePath, true)
                );
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static void ConfigurePresentation(
            Scene bootScene,
            GameObject bootRoot)
        {
            Camera camera = bootScene
                .GetRootGameObjects()
                .Select(root => root.GetComponent<Camera>())
                .FirstOrDefault(item => item != null);
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                SceneManager.MoveGameObjectToScene(
                    cameraObject,
                    bootScene
                );
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.035f, 0.055f, 0.085f);
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.05f;
            camera.transform.position =
                new Vector3(0.9f, 1.08f, 3.8f);
            camera.transform.LookAt(
                new Vector3(0.9f, 0.94f, 0f)
            );

            Light keyLight = bootScene
                .GetRootGameObjects()
                .Select(root => root.GetComponent<Light>())
                .FirstOrDefault(item => item != null);
            if (keyLight == null)
            {
                GameObject lightObject = new("Title Key Light");
                SceneManager.MoveGameObjectToScene(
                    lightObject,
                    bootScene
                );
                keyLight = lightObject.AddComponent<Light>();
            }
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.86f, 0.7f);
            keyLight.intensity = 1.8f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.transform.rotation =
                Quaternion.Euler(32f, -28f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(0.25f, 0.29f, 0.35f);
            EnsureTitleLight(
                bootRoot.transform,
                "Title Fill Light",
                new Vector3(12f, 145f, 0f),
                new Color(0.56f, 0.72f, 1f),
                0.75f
            );
            EnsureTitleLight(
                bootRoot.transform,
                "Title Rim Light",
                new Vector3(55f, 190f, 0f),
                new Color(1f, 0.58f, 0.34f),
                0.9f
            );

            Transform floor = bootRoot.transform.Find(
                "TitleStudioFloor"
            );
            if (floor == null)
            {
                floor = Demo02SceneObjectFactory.CreateBlock(
                    bootRoot.transform,
                    "TitleStudioFloor",
                    new Vector3(0f, -0.08f, 0f),
                    new Vector3(7f, 0.15f, 6f),
                    new Color(0.055f, 0.075f, 0.1f)
                ).transform;
            }
            floor.position = new Vector3(0f, -0.08f, 0f);
            floor.localScale = new Vector3(7f, 0.15f, 6f);

            Transform backdrop = bootRoot.transform.Find(
                "TitleStudioBackdrop"
            );
            if (backdrop == null)
            {
                backdrop = Demo02SceneObjectFactory.CreateBlock(
                    bootRoot.transform,
                    "TitleStudioBackdrop",
                    new Vector3(0f, 2.4f, -2.5f),
                    new Vector3(7f, 5f, 0.12f),
                    new Color(0.035f, 0.05f, 0.075f)
                ).transform;
            }
            backdrop.position = new Vector3(0f, 2.4f, -2.5f);
            backdrop.localScale = new Vector3(7f, 5f, 0.12f);

            GameObject staleLumine =
                bootRoot.transform.Find("TitleLumine")?.gameObject;
            if (staleLumine != null)
            {
                Object.DestroyImmediate(staleLumine);
            }

            GameObject existingCharacter =
                bootRoot.transform.Find("TitleAnimeFish")?.gameObject;
            if (existingCharacter != null)
            {
                existingCharacter.transform.localPosition =
                    Vector3.zero;
                if (existingCharacter.GetComponent<
                        TitleCharacterPresentation>() == null)
                {
                    existingCharacter.AddComponent<
                        TitleCharacterPresentation>();
                }
                return;
            }

            GameObject titleCharacterPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnimeFishPilotSetup.PrefabPath
                );
            if (titleCharacterPrefab == null)
            {
                throw new System.InvalidOperationException(
                    "Title anime fish prefab is missing."
                );
            }
            GameObject titleCharacter =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    titleCharacterPrefab
                );
            titleCharacter.name = "TitleAnimeFish";
            titleCharacter.transform.SetParent(
                bootRoot.transform,
                false
            );
            titleCharacter.transform.localPosition = Vector3.zero;
            titleCharacter.transform.localRotation =
                Quaternion.identity;
            titleCharacter.AddComponent<TitleCharacterPresentation>();
            Debug.Log("Demo02 boot title character configured.");
        }

        private static Light EnsureTitleLight(
            Transform parent,
            string name,
            Vector3 euler,
            Color color,
            float intensity)
        {
            Transform existing = parent.Find(name);
            GameObject lightObject = existing != null
                ? existing.gameObject
                : new GameObject(name);
            if (existing == null)
            {
                lightObject.transform.SetParent(parent, false);
            }
            lightObject.transform.localRotation =
                Quaternion.Euler(euler);
            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
