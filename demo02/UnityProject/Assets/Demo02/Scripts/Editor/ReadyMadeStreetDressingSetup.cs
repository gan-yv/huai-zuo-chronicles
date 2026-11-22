using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class ReadyMadeStreetDressingSetup
    {
        private const string ArtRoot =
            "Assets/Demo02/Art/Environment/StreetDressing";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string KenneyRoot =
            "Assets/ThirdParty/Kenney/FantasyTownKit";
        private const string KenneyMaterialPath =
            "Assets/Demo02/Art/Materials/ThirdPartySamples/" +
            "MAT_Kenney_FantasyTownAtlas.mat";
        private const string QuaterniusPrefabRoot =
            "Assets/Demo02/Art/Environment/Quaternius/Prefabs";
        private const string VisualSuffix = "_ReadyMadeVisual";

        private static readonly string[] KenneyModels =
        {
            "stall",
            "tree-high",
            "lantern"
        };

        [MenuItem("Demo02/Art/Rebuild Ready-Made Street Dressing")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(PrefabRoot);
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(KenneyMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Kenney atlas material is missing: {KenneyMaterialPath}"
                );
            }

            foreach (string modelName in KenneyModels)
            {
                string modelPath = $"{KenneyRoot}/Models/{modelName}.fbx";
                GameObject model =
                    AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    throw new InvalidOperationException(
                        $"Kenney street model is missing: {modelPath}"
                    );
                }

                GameObject root = new(
                    $"VIS_Kenney_{ToPascalCase(modelName)}"
                );
                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "Model";
                instance.transform.SetParent(root.transform, false);
                foreach (Renderer renderer in
                         instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int index = 0; index < materials.Length; index++)
                    {
                        materials[index] = material;
                    }
                    renderer.sharedMaterials = materials;
                }

                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    $"{PrefabRoot}/VIS_Kenney_{ToPascalCase(modelName)}.prefab"
                );
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Ready-made street dressing is ready: Kenney stall, tree and lantern."
            );
        }

        public static void ApplyToGreybox(Transform greyboxRoot)
        {
            string cratePrefab =
                $"{QuaterniusPrefabRoot}/ART_Quaternius_Prop_Crate.prefab";
            string awningPrefab =
                $"{QuaterniusPrefabRoot}/ART_Quaternius_RoofAwning.prefab";
            string stallPrefab =
                $"{PrefabRoot}/VIS_Kenney_Stall.prefab";
            string treePrefab =
                $"{PrefabRoot}/VIS_Kenney_TreeHigh.prefab";
            string lanternPrefab =
                $"{PrefabRoot}/VIS_Kenney_Lantern.prefab";
            string signPrefab =
                $"{QuaterniusPrefabRoot}/ART_Quaternius_WoodSign.prefab";
            string framePrefab =
                $"{QuaterniusPrefabRoot}/ART_Quaternius_Frame.prefab";

            foreach (string name in new[]
                     {
                         "Crate_A",
                         "Crate_B",
                         "Courtyard_Crate_A",
                         "Courtyard_Crate_B",
                         "Consequence_ProtectedEvidence"
                     })
            {
                ReplaceSingle(
                    FindDirect(greyboxRoot, name),
                    cratePrefab,
                    uniformScale: true
                );
            }

            for (char suffix = 'A'; suffix <= 'D'; suffix++)
            {
                ReplaceSingle(
                    FindDirect(greyboxRoot, $"Dongguan_Stall_{suffix}"),
                    stallPrefab,
                    uniformScale: true
                );
            }

            foreach (Transform transform in
                     greyboxRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name.StartsWith(
                        "Caiyi_LeftAwning_",
                        StringComparison.Ordinal
                    ) ||
                    transform.name.StartsWith(
                        "Caiyi_RightAwning_",
                        StringComparison.Ordinal
                    ))
                {
                    ReplaceSingle(
                        transform,
                        awningPrefab,
                        uniformScale: false
                    );
                }
            }

            ReplaceCaiyiTrees(greyboxRoot, treePrefab);

            foreach (string name in new[]
                     {
                         "Pickup_SaltToken",
                         "Pickup_BlankLedgerPage",
                         "Pickup_GingerTea",
                         "Echo_SaltToken",
                         "Echo_FireAsh",
                         "Echo_SevenPages"
                     })
            {
                ReplaceInteractive(
                    FindDirect(greyboxRoot, name),
                    lanternPrefab,
                    uniformScale: true
                );
            }

            foreach (string name in new[]
                     {
                         "Clue_OldStreetSign",
                         "Clue_SaltLedger",
                         "Clue_OfficialSeal",
                         "Consequence_PublicRecord",
                         "Choice_Preserve",
                         "Choice_Seal"
                     })
            {
                ReplaceInteractive(
                    FindDirect(greyboxRoot, name),
                    signPrefab,
                    uniformScale: false
                );
            }

            foreach (string name in new[]
                     {
                         "TimeAnchor_Present_01",
                         "TimeAnchor_Past_02",
                         "TimeAnchor_Present_03",
                         "RepairSite_TimeRift"
                     })
            {
                ReplaceInteractive(
                    FindDirect(greyboxRoot, name),
                    framePrefab,
                    uniformScale: false
                );
            }
        }

        public static void ValidateForBatch()
        {
            foreach (string path in new[]
                     {
                         $"{PrefabRoot}/VIS_Kenney_Stall.prefab",
                         $"{PrefabRoot}/VIS_Kenney_TreeHigh.prefab",
                         $"{PrefabRoot}/VIS_Kenney_Lantern.prefab",
                         $"{QuaterniusPrefabRoot}/ART_Quaternius_Prop_Crate.prefab",
                         $"{QuaterniusPrefabRoot}/ART_Quaternius_RoofAwning.prefab",
                         $"{QuaterniusPrefabRoot}/ART_Quaternius_WoodSign.prefab",
                         $"{QuaterniusPrefabRoot}/ART_Quaternius_Frame.prefab"
                     })
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null ||
                    prefab.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Ready-made street visual is missing: {path}"
                    );
                }
            }

            Debug.Log(
                "Ready-made street dressing validation passed: crates, stalls, " +
                "trees, lanterns, notices and awnings."
            );
        }

        private static void ReplaceCaiyiTrees(
            Transform greyboxRoot,
            string treePrefab)
        {
            for (int index = 1; index <= 8; index++)
            {
                Transform trunk = FindDirect(
                    greyboxRoot,
                    $"Caiyi_Tree_{index}_Trunk"
                );
                Transform canopy = FindDirect(
                    greyboxRoot,
                    $"Caiyi_Tree_{index}_Canopy"
                );
                if (trunk == null || canopy == null)
                {
                    continue;
                }

                MeshRenderer trunkRenderer = trunk.GetComponent<MeshRenderer>();
                MeshRenderer canopyRenderer =
                    canopy.GetComponent<MeshRenderer>();
                if (trunkRenderer == null || canopyRenderer == null)
                {
                    continue;
                }
                string visualName = $"Caiyi_Tree_{index}{VisualSuffix}";
                if (trunk.parent.Find(visualName) != null)
                {
                    continue;
                }

                Bounds desired = trunkRenderer.bounds;
                desired.Encapsulate(canopyRenderer.bounds);
                GameObject visual = InstantiateVisual(
                    treePrefab,
                    trunk.parent,
                    visualName
                );
                FitVisual(visual, desired, uniformScale: true);
                trunkRenderer.enabled = false;
                canopyRenderer.enabled = false;
            }
        }

        private static void ReplaceSingle(
            Transform target,
            string prefabPath,
            bool uniformScale)
        {
            if (target == null)
            {
                return;
            }

            MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();
            if (targetRenderer == null)
            {
                return;
            }
            string visualName = target.name + VisualSuffix;
            if (target.parent.Find(visualName) != null)
            {
                return;
            }

            Bounds desired = targetRenderer.bounds;
            GameObject visual = InstantiateVisual(
                prefabPath,
                target.parent,
                visualName
            );
            visual.transform.rotation = target.rotation;
            FitVisual(visual, desired, uniformScale);
            targetRenderer.enabled = false;
        }

        private static void ReplaceInteractive(
            Transform target,
            string prefabPath,
            bool uniformScale)
        {
            if (target == null)
            {
                return;
            }
            MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();
            if (targetRenderer == null)
            {
                return;
            }

            Transform anchor = target.Find("ReadyMadeVisualAnchor");
            if (anchor == null)
            {
                GameObject anchorObject = new("ReadyMadeVisualAnchor");
                anchor = anchorObject.transform;
                anchor.SetParent(target, false);
                Vector3 scale = target.lossyScale;
                anchor.localScale = new Vector3(
                    1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                    1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                    1f / Mathf.Max(0.001f, Mathf.Abs(scale.z))
                );
            }

            string visualName = target.name + VisualSuffix;
            if (anchor.Find(visualName) != null)
            {
                return;
            }

            Bounds desired = targetRenderer.bounds;
            GameObject visual = InstantiateVisual(
                prefabPath,
                anchor,
                visualName
            );
            visual.transform.rotation = target.rotation;
            FitVisual(visual, desired, uniformScale);
            targetRenderer.enabled = false;
        }

        private static GameObject InstantiateVisual(
            string prefabPath,
            Transform parent,
            string visualName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Ready-made visual prefab is missing: {prefabPath}"
                );
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                parent
            );
            visual.name = visualName;
            foreach (Transform child in
                     visual.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.isStatic = true;
            }
            return visual;
        }

        private static void FitVisual(
            GameObject visual,
            Bounds desired,
            bool uniformScale)
        {
            Physics.SyncTransforms();
            Bounds? source = GetCombinedBounds(visual);
            if (!source.HasValue)
            {
                throw new InvalidOperationException(
                    $"Ready-made visual has no renderer bounds: {visual.name}"
                );
            }

            Vector3 ratio = new(
                desired.size.x / Mathf.Max(0.001f, source.Value.size.x),
                desired.size.y / Mathf.Max(0.001f, source.Value.size.y),
                desired.size.z / Mathf.Max(0.001f, source.Value.size.z)
            );
            if (uniformScale)
            {
                float scale = Mathf.Min(ratio.x, Mathf.Min(ratio.y, ratio.z));
                ratio = Vector3.one * scale;
            }

            visual.transform.localScale = ratio;
            Physics.SyncTransforms();
            Bounds fitted = GetCombinedBounds(visual)!.Value;
            visual.transform.position += desired.center - fitted.center;
        }

        private static Bounds? GetCombinedBounds(GameObject gameObject)
        {
            Renderer[] renderers =
                gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return null;
            }
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static Transform FindDirect(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
        }

        private static string ToPascalCase(string value)
        {
            return string.Concat(
                value.Split('-')
                    .Select(part =>
                        part.Length == 0
                            ? string.Empty
                            : char.ToUpperInvariant(part[0]) + part.Substring(1)
                    )
            );
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Invalid folder: {path}");
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
