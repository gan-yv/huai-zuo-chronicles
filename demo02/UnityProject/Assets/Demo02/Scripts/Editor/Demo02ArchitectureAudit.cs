using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class Demo02ArchitectureAudit
    {
        [Serializable]
        private sealed class AssemblyDefinition
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
        }

        public static void VerifyForBatch()
        {
            List<string> paths = AssetDatabase
                .FindAssets(
                    "t:AssemblyDefinitionAsset",
                    new[] { "Assets/Demo02" }
                )
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();
            Dictionary<string, AssemblyDefinition> definitions =
                new(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                AssemblyDefinition definition =
                    JsonUtility.FromJson<AssemblyDefinition>(
                        File.ReadAllText(path)
                    );
                if (definition?.name == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid assembly definition: {path}"
                    );
                }
                definitions[definition.name] = definition;
            }

            Require(definitions, "Demo02.Foundation");
            Require(definitions, "Demo02.Runtime");
            Require(definitions, "Demo02.Editor");
            Require(definitions, "Demo02.Tests.EditMode");
            Require(definitions, "Demo02.Tests.PlayMode");

            AssemblyDefinition foundation =
                definitions["Demo02.Foundation"];
            if (foundation.references != null
                && foundation.references.Any(reference =>
                    reference.StartsWith(
                        "Demo02.",
                        StringComparison.Ordinal
                    )))
            {
                throw new InvalidOperationException(
                    "Demo02.Foundation must not reference game assemblies."
                );
            }

            AssemblyDefinition runtime = definitions["Demo02.Runtime"];
            RequireReference(runtime, "Demo02.Foundation");
            RequireReference(runtime, "Unity.InputSystem");
            RequireReference(runtime, "UnityEngine.UI");
            RejectReference(runtime, "Demo02.Editor");
            RejectReference(runtime, "Demo02.Tests.EditMode");

            AssemblyDefinition editor = definitions["Demo02.Editor"];
            RequireReference(editor, "Demo02.Foundation");
            RequireReference(editor, "Demo02.Runtime");
            RejectReference(editor, "Demo02.Tests.EditMode");
            RequireEditorPlatform(editor);

            AssemblyDefinition tests =
                definitions["Demo02.Tests.EditMode"];
            RequireReference(tests, "Demo02.Foundation");
            RequireReference(tests, "Demo02.Runtime");
            RequireReference(tests, "Demo02.Editor");
            RequireEditorPlatform(tests);

            AssemblyDefinition playModeTests =
                definitions["Demo02.Tests.PlayMode"];
            RequireReference(playModeTests, "Demo02.Foundation");
            RequireReference(playModeTests, "Demo02.Runtime");
            RejectReference(playModeTests, "Demo02.Editor");
            RejectReference(playModeTests, "Demo02.Tests.EditMode");

            string runtimeRoot = Path.Combine(
                Application.dataPath,
                "Demo02/Scripts"
            );
            foreach (string sourcePath in
                     Directory.GetFiles(
                         runtimeRoot,
                         "*.cs",
                         SearchOption.AllDirectories
                     ))
            {
                if (sourcePath.Contains(
                        $"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    ))
                {
                    continue;
                }
                string source = File.ReadAllText(sourcePath);
                if (source.Contains(
                        "using Demo02.Editor",
                        StringComparison.Ordinal
                    )
                    || source.Contains(
                        "Demo02.Editor.",
                        StringComparison.Ordinal
                    ))
                {
                    throw new InvalidOperationException(
                        $"Runtime source references editor code: {sourcePath}"
                    );
                }
                if (!sourcePath.EndsWith(
                        "RuntimeLaunchOptions.cs",
                        StringComparison.Ordinal
                    )
                    && (source.Contains(
                            "Environment.GetCommandLineArgs()",
                            StringComparison.Ordinal
                        )
                        || source.Contains(
                            "System.Environment.GetCommandLineArgs()",
                            StringComparison.Ordinal
                        )))
                {
                    throw new InvalidOperationException(
                        $"Runtime source reads process arguments directly: "
                        + sourcePath
                    );
                }
                if (!sourcePath.EndsWith(
                        "GameServices.cs",
                        StringComparison.Ordinal
                    )
                    && source.Contains(
                        "FindAnyObjectByType",
                        StringComparison.Ordinal
                    ))
                {
                    throw new InvalidOperationException(
                        "Runtime source bypasses GameServices dependency "
                        + $"resolution: {sourcePath}"
                    );
                }
                if (source.Contains(
                        "GameObject.Find(",
                        StringComparison.Ordinal
                    ))
                {
                    throw new InvalidOperationException(
                        "Runtime source relies on mutable GameObject names: "
                        + sourcePath
                    );
                }
            }

            Debug.Log(
                "Architecture audit passed: foundation, runtime, editor, "
                + "EditMode and PlayMode assembly boundaries are one-way."
            );
        }

        private static void Require(
            IReadOnlyDictionary<string, AssemblyDefinition> definitions,
            string name)
        {
            if (!definitions.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"Required assembly definition is missing: {name}"
                );
            }
        }

        private static void RequireReference(
            AssemblyDefinition definition,
            string reference)
        {
            if (definition.references == null
                || !definition.references.Contains(reference))
            {
                throw new InvalidOperationException(
                    $"{definition.name} must reference {reference}."
                );
            }
        }

        private static void RejectReference(
            AssemblyDefinition definition,
            string reference)
        {
            if (definition.references != null
                && definition.references.Contains(reference))
            {
                throw new InvalidOperationException(
                    $"{definition.name} must not reference {reference}."
                );
            }
        }

        private static void RequireEditorPlatform(
            AssemblyDefinition definition)
        {
            if (definition.includePlatforms == null
                || !definition.includePlatforms.Contains("Editor"))
            {
                throw new InvalidOperationException(
                    $"{definition.name} must be Editor-only."
                );
            }
        }
    }
}
