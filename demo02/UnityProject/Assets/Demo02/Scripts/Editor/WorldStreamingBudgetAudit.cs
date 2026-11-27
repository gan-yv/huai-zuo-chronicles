using System;
using System.Collections.Generic;
using System.Linq;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Editor
{
    public static class WorldStreamingBudgetAudit
    {
        private const int MaxObjectsPerRegion = 750;
        private const int MaxRenderersPerRegion = 300;
        private const int MaxUniqueMaterialsPerRegion = 24;
        private const long MaxVerticesPerRegion = 1_000_000;
        private const long MaxTrianglesPerRegion = 1_000_000;

        public static void ValidateForBatch()
        {
            IReadOnlyList<WorldStreamingRegionDefinition> definitions =
                WorldStreamingRegionSeeder.LoadAll();
            List<string> errors = new();
            foreach (WorldStreamingRegionDefinition definition in definitions)
            {
                string path =
                    WorldStreamingRegionSeeder.GetScenePath(definition);
                Scene scene = SceneManager.GetSceneByPath(path);
                bool openedHere = !scene.IsValid() || !scene.isLoaded;
                if (openedHere)
                {
                    scene = EditorSceneManager.OpenScene(
                        path,
                        OpenSceneMode.Additive
                    );
                }

                try
                {
                    BudgetReport report = Audit(scene);
                    if (report.Objects > MaxObjectsPerRegion)
                    {
                        errors.Add(
                            $"{definition.RegionId}: " +
                            $"{report.Objects} objects exceed " +
                            $"{MaxObjectsPerRegion}."
                        );
                    }
                    if (report.Renderers > MaxRenderersPerRegion)
                    {
                        errors.Add(
                            $"{definition.RegionId}: " +
                            $"{report.Renderers} renderers exceed " +
                            $"{MaxRenderersPerRegion}."
                        );
                    }
                    if (report.Materials > MaxUniqueMaterialsPerRegion)
                    {
                        errors.Add(
                            $"{definition.RegionId}: " +
                            $"{report.Materials} materials exceed " +
                            $"{MaxUniqueMaterialsPerRegion}."
                        );
                    }
                    if (report.Vertices > MaxVerticesPerRegion)
                    {
                        errors.Add(
                            $"{definition.RegionId}: " +
                            $"{report.Vertices} vertices exceed " +
                            $"{MaxVerticesPerRegion}."
                        );
                    }
                    if (report.Triangles > MaxTrianglesPerRegion)
                    {
                        errors.Add(
                            $"{definition.RegionId}: " +
                            $"{report.Triangles} triangles exceed " +
                            $"{MaxTrianglesPerRegion}."
                        );
                    }
                    Debug.Log(
                        $"Streaming region budget: " +
                        $"{definition.RegionId} objects={report.Objects} " +
                        $"renderers={report.Renderers} " +
                        $"materials={report.Materials} " +
                        $"vertices={report.Vertices} " +
                        $"triangles={report.Triangles}"
                    );
                }
                finally
                {
                    if (openedHere)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Streaming region budget validation failed:\n" +
                    string.Join(
                        "\n",
                        errors.Select(error => $"- {error}")
                    )
                );
            }
        }

        private static BudgetReport Audit(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            int objects = 0;
            int renderers = 0;
            long vertices = 0;
            long triangles = 0;
            HashSet<Material> materials = new();
            foreach (GameObject root in roots)
            {
                objects += root.GetComponentsInChildren<Transform>(true)
                    .Length;
                Renderer[] sceneRenderers =
                    root.GetComponentsInChildren<Renderer>(true);
                renderers += sceneRenderers.Length;
                foreach (Renderer renderer in sceneRenderers)
                {
                    foreach (Material material in
                             renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }

                    Mesh mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }
                    vertices += mesh.vertexCount;
                    for (int subMesh = 0;
                         subMesh < mesh.subMeshCount;
                         subMesh++)
                    {
                        triangles += mesh.GetIndexCount(subMesh) / 3;
                    }
                }
            }

            return new BudgetReport(
                objects,
                renderers,
                materials.Count,
                vertices,
                triangles
            );
        }

        private readonly struct BudgetReport
        {
            public BudgetReport(
                int objects,
                int renderers,
                int materials,
                long vertices,
                long triangles)
            {
                Objects = objects;
                Renderers = renderers;
                Materials = materials;
                Vertices = vertices;
                Triangles = triangles;
            }

            public int Objects { get; }
            public int Renderers { get; }
            public int Materials { get; }
            public long Vertices { get; }
            public long Triangles { get; }
        }
    }
}
