using System;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class PublicPropDressingSetup
    {
        private const string ModelRoot =
            "Assets/ThirdParty/PolyHaven/Models";
        private const string GroupName = "PublicPropDressing";

        public static void ApplyToGreybox(Transform greyboxRoot)
        {
            if (greyboxRoot.Find(GroupName) != null)
            {
                return;
            }

            Transform group = new GameObject(GroupName).transform;
            group.SetParent(greyboxRoot, false);

            InstantiateModel(
                "chinese_screen_panels",
                group,
                "TeaHouse_Screen",
                new Vector3(63.6f, 0f, 113.6f),
                0f
            );
            InstantiateModel(
                "chinese_tea_table",
                group,
                "TeaHouse_Table",
                new Vector3(62.6f, 0f, 112.2f),
                0f
            );
            InstantiateModel(
                "chinese_armchair",
                group,
                "TeaHouse_Chair_Left",
                new Vector3(61.6f, 0f, 111f),
                -18f
            );
            InstantiateModel(
                "chinese_armchair",
                group,
                "TeaHouse_Chair_Right",
                new Vector3(63.7f, 0f, 111f),
                18f
            );
            InstantiateModel(
                "chinese_cabinet",
                group,
                "Warehouse_ChineseCabinet",
                new Vector3(27.1f, 0f, 7f),
                -90f
            );
        }

        public static void ValidateForBatch()
        {
            foreach (string assetId in new[]
                     {
                         "chinese_screen_panels",
                         "chinese_armchair",
                         "chinese_tea_table",
                         "chinese_cabinet"
                     })
            {
                string path = $"{ModelRoot}/{assetId}/" +
                    $"{assetId}_1k.gltf";
                GameObject model =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null ||
                    model.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Public prop model is missing or has no renderers: {path}"
                    );
                }
            }

            Debug.Log(
                "Public prop validation passed: Poly Haven Chinese screen, " +
                "armchair, tea table and cabinet."
            );
        }

        private static void InstantiateModel(
            string assetId,
            Transform parent,
            string objectName,
            Vector3 position,
            float rotationY)
        {
            string path = $"{ModelRoot}/{assetId}/" +
                $"{assetId}_1k.gltf";
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Public prop model is missing: {path}"
                );
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = objectName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            foreach (MeshRenderer renderer in
                     instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.gameObject.isStatic = true;
            }
        }
    }
}
