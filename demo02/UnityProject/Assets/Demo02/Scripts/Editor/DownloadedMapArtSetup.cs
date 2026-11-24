using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    internal static class DownloadedMapArtSetup
    {
        private const string PavilionModelPath =
            "Assets/Demo02/Art/Environment/Downloaded/Pavilion/"
            + "VIS_LumingPavilion.glb";

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
    }
}
