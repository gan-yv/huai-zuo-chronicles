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
        private const string MeshRoot = ArtRoot + "/Meshes";
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
            "lantern",
            "pillar-wood",
            "cart"
        };

        [MenuItem("Demo02/Art/Rebuild Ready-Made Street Dressing")]
        public static void BuildAll()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(MeshRoot);
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
                if (AssetImporter.GetAtPath(modelPath) is ModelImporter importer)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
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
                "Ready-made street dressing is ready: Kenney stalls, trees, " +
                "lanterns, poles and cart."
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
                    uniformScale: true,
                    minimumSize: new Vector3(2.8f, 2.2f, 2f)
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
                    uniformScale: true,
                    minimumSize: new Vector3(0.45f, 0.65f, 0.45f)
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

            CreateStreetScenery(greyboxRoot);
        }

        public static void ValidateForBatch()
        {
            foreach (string path in new[]
                     {
                         $"{PrefabRoot}/VIS_Kenney_Stall.prefab",
                         $"{PrefabRoot}/VIS_Kenney_TreeHigh.prefab",
                         $"{PrefabRoot}/VIS_Kenney_Lantern.prefab",
                         $"{PrefabRoot}/VIS_Kenney_PillarWood.prefab",
                         $"{PrefabRoot}/VIS_Kenney_Cart.prefab",
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
            bool uniformScale,
            Vector3 minimumSize = default)
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
            desired.size = Vector3.Max(desired.size, minimumSize);
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
            bool uniformScale,
            Vector3 minimumSize = default)
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
            desired.size = Vector3.Max(desired.size, minimumSize);
            GameObject visual = InstantiateVisual(
                prefabPath,
                anchor,
                visualName
            );
            visual.transform.rotation = target.rotation;
            FitVisual(visual, desired, uniformScale);
            targetRenderer.enabled = false;
        }

        private static void CreateStreetScenery(Transform greyboxRoot)
        {
            if (greyboxRoot.Find("ReadyMadeStreetScenery") != null)
            {
                return;
            }

            GameObject group = new("ReadyMadeStreetScenery");
            group.transform.SetParent(greyboxRoot, false);
            string polePrefab =
                $"{PrefabRoot}/VIS_Kenney_PillarWood.prefab";
            string lanternPrefab =
                $"{PrefabRoot}/VIS_Kenney_Lantern.prefab";
            Vector3[] lampPositions =
            {
                new(-5.2f, 0f, -2f),
                new(5.2f, 0f, 6f),
                new(-5.2f, 0f, 15f),
                new(5.2f, 0f, 24f),
                new(56.2f, 0f, 18f),
                new(63.8f, 0f, 48f),
                new(56.2f, 0f, 72f),
                new(63.8f, 0f, 102f),
                new(56.2f, 0f, 126f),
                new(55.8f, 0f, 181f),
                new(64.2f, 0f, 216f),
                new(55.8f, 0f, 251f),
                new(64.2f, 0f, 282f)
            };
            for (int index = 0; index < lampPositions.Length; index++)
            {
                Vector3 position = lampPositions[index];
                GameObject pole = InstantiateVisual(
                    polePrefab,
                    group.transform,
                    $"StreetPole_{index + 1}"
                );
                FitVisual(
                    pole,
                    new Bounds(
                        position + Vector3.up * 1.5f,
                        new Vector3(0.3f, 3f, 0.3f)
                    ),
                    uniformScale: true
                );

                GameObject lantern = InstantiateVisual(
                    lanternPrefab,
                    group.transform,
                    $"StreetLantern_{index + 1}"
                );
                FitVisual(
                    lantern,
                    new Bounds(
                        position + Vector3.up * 3.1f,
                        new Vector3(0.5f, 0.55f, 0.5f)
                    ),
                    uniformScale: true
                );
            }

            string cartPrefabPath =
                $"{PrefabRoot}/VIS_Kenney_Cart.prefab";
            GameObject cartPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(cartPrefabPath);
            if (cartPrefab != null)
            {
                GameObject cart = (GameObject)PrefabUtility.InstantiatePrefab(
                    cartPrefab,
                    group.transform
                );
                cart.name = "StreetCart";
                cart.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                FitVisual(
                    cart,
                    new Bounds(
                        new Vector3(4.55f, 0.9f, 12.5f),
                        new Vector3(1.9f, 1.8f, 3.8f)
                    ),
                    uniformScale: true
                );
                cart.isStatic = true;
            }

            CombineStreetScenery(group);
        }

        private static void CombineStreetScenery(GameObject group)
        {
            List<CombineInstance> instances = new();
            foreach (MeshFilter filter in
                     group.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable)
                {
                    throw new InvalidOperationException(
                        $"Street scenery mesh is not readable: {filter.name}"
                    );
                }
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    instances.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = subMesh,
                        transform =
                            group.transform.worldToLocalMatrix *
                            filter.transform.localToWorldMatrix
                    });
                }
            }

            if (instances.Count == 0)
            {
                return;
            }

            Mesh combined = new()
            {
                name = "SM_ReadyMadeStreetScenery",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            combined.CombineMeshes(
                instances.ToArray(),
                mergeSubMeshes: true,
                useMatrices: true,
                hasLightmapData: false
            );
            combined.RecalculateBounds();
            string meshPath = $"{MeshRoot}/SM_ReadyMadeStreetScenery.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(combined, existing);
                UnityEngine.Object.DestroyImmediate(combined);
                combined = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(combined, meshPath);
            }

            foreach (Transform child in group.transform.Cast<Transform>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            GameObject visual = new("StreetScenery_Merged");
            visual.transform.SetParent(group.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = combined;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                KenneyMaterialPath
            );
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            visual.isStatic = true;
            EditorUtility.SetDirty(combined);
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
