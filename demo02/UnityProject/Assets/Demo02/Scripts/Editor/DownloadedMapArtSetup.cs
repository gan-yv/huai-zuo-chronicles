using Demo02.Core;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    internal static class DownloadedMapArtSetup
    {
        private const string PavilionModelPath =
            "Assets/Demo02/Art/Environment/Downloaded/Pavilion/"
            + "VIS_LumingPavilion.glb";
        private const string DongguanTreeLineModelPath =
            "Assets/Demo02/Art/Environment/Downloaded/BillboardTrees/"
            + "VIS_DongguanTreeLine.glb";
        private const string CottageCourtyardModelPath =
            "Assets/Demo02/Art/Environment/Downloaded/Cottage/"
            + "VIS_CottageCourtyard.glb";

        public static GameObject InstantiateDongguanPavilion(
            Transform parent)
        {
            AssetDatabase.ImportAsset(
                PavilionModelPath,
                ImportAssetOptions.ForceUpdate
                    | ImportAssetOptions.ForceSynchronousImport
            );
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PavilionModelPath
                );
            if (source == null)
            {
                throw new System.InvalidOperationException(
                    $"Downloaded pavilion model is missing: "
                    + PavilionModelPath
                );
            }

            GameObject pavilion =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    source,
                    parent
                );
            pavilion.name = "ART_Dongguan_PavilionLandmark";
            pavilion.transform.localPosition =
                new Vector3(60f, 0.25f, 129f);
            pavilion.transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
            pavilion.transform.localScale =
                Vector3.one * 0.4f;

            Renderer[] renderers =
                pavilion.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }

                BoxCollider collider =
                    pavilion.AddComponent<BoxCollider>();
                collider.center =
                    pavilion.transform.InverseTransformPoint(
                        bounds.center
                    );
                collider.size =
                    pavilion.transform.InverseTransformVector(
                        bounds.size
                    );
            }

            foreach (Transform child in
                     pavilion.GetComponentsInChildren<Transform>())
            {
                child.gameObject.isStatic = true;
            }

            Debug.Log(
                $"Downloaded pavilion placed at street end with "
                + $"{renderers.Length} renderers."
            );
            return pavilion;
        }

        public static GameObject InstantiateDongguanTreeLine(
            Transform parent)
        {
            AssetDatabase.ImportAsset(
                DongguanTreeLineModelPath,
                ImportAssetOptions.ForceUpdate
                    | ImportAssetOptions.ForceSynchronousImport
            );
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    DongguanTreeLineModelPath
                );
            if (source == null)
            {
                throw new System.InvalidOperationException(
                    $"Downloaded tree line model is missing: "
                    + DongguanTreeLineModelPath
                );
            }

            GameObject treeLine =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    source,
                    parent
                );
            treeLine.name = "ART_Dongguan_DownloadedTreeLine";
            treeLine.transform.localPosition =
                new Vector3(60f, 0.02f, 136.5f);
            treeLine.transform.localRotation = Quaternion.identity;
            treeLine.transform.localScale = Vector3.one;
            foreach (Transform child in
                     treeLine.GetComponentsInChildren<Transform>())
            {
                child.gameObject.isStatic = true;
            }

            Debug.Log(
                "Downloaded billboard tree line placed behind "
                + "the Dongguan end wall."
            );
            return treeLine;
        }

        public static GameObject InstantiateCottageCourtyard(
            Transform parent,
            out Transform spawn)
        {
            AssetDatabase.ImportAsset(
                CottageCourtyardModelPath,
                ImportAssetOptions.ForceUpdate
                    | ImportAssetOptions.ForceSynchronousImport
            );
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CottageCourtyardModelPath
                );
            if (source == null)
            {
                throw new System.InvalidOperationException(
                    $"Downloaded cottage model is missing: "
                    + CottageCourtyardModelPath
                );
            }

            GameObject container = new("ART_CottageCourtyard");
            container.transform.SetParent(parent, false);
            container.transform.localPosition =
                new Vector3(0f, 0f, -100f);

            GameObject model =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    source,
                    container.transform
                );
            model.name = "VIS_CottageCourtyard";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "Downloaded cottage model has no renderers."
                );
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            model.transform.position += new Vector3(
                container.transform.position.x - bounds.center.x,
                container.transform.position.y - bounds.min.y,
                container.transform.position.z - bounds.center.z
            );
            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            GameObject ground = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );
            ground.name = "ART_CottageCourtyardGround";
            ground.transform.SetParent(container.transform, true);
            ground.transform.position = new Vector3(
                bounds.center.x,
                -0.1f,
                bounds.center.z
            );
            ground.transform.localScale = new Vector3(
                Mathf.Max(18f, bounds.size.x + 6f),
                0.2f,
                Mathf.Max(22f, bounds.size.z + 8f)
            );
            ground.AddComponent<ColorTint>().Configure(
                new Color(0.25f, 0.34f, 0.2f)
            );
            ground.isStatic = true;

            foreach (Renderer renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                MeshCollider collider =
                    renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }

            spawn = new GameObject("ART_CottageCourtyardSpawn").transform;
            spawn.SetParent(container.transform, true);
            spawn.position = new Vector3(
                bounds.center.x,
                0.1f,
                bounds.min.z - 2f
            );
            spawn.rotation = Quaternion.identity;

            foreach (Transform child in
                     model.GetComponentsInChildren<Transform>())
            {
                child.gameObject.isStatic = true;
            }

            Debug.Log(
                $"Downloaded cottage courtyard placed at "
                + $"{container.transform.position} with "
                + $"{renderers.Length} renderers."
            );
            return container;
        }
    }
}
