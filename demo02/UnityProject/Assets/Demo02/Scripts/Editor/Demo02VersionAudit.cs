using System;
using System.IO;
using System.Linq;
using Demo02.Core;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class Demo02VersionAudit
    {
        public static void VerifyForBatch()
        {
            Demo02VersionData data = Demo02VersionInfo.Load();
            if (!System.Version.TryParse(
                    data.version,
                    out System.Version parsed
                )
                || parsed.Major < 1)
            {
                throw new InvalidOperationException(
                    $"Invalid semantic version: {data.version}"
                );
            }
            if (!string.Equals(
                    PlayerSettings.bundleVersion,
                    data.version,
                    StringComparison.Ordinal
                ))
            {
                throw new InvalidOperationException(
                    $"ProjectSettings bundleVersion {PlayerSettings.bundleVersion} "
                    + $"does not match version.json {data.version}."
                );
            }
            if (data.saveSchemaVersion != WorldSaveData.CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"Save schema version {data.saveSchemaVersion} does not "
                    + $"match runtime version {WorldSaveData.CurrentVersion}."
                );
            }

            string demoRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../..")
            );
            string changelog = File.ReadAllText(
                Path.GetFullPath(
                    Path.Combine(demoRoot, "../CHANGELOG.md")
                )
            );
            if (!changelog.Split('\n').Any(line =>
                    line.StartsWith("## ", StringComparison.Ordinal)
                    && line.Contains(
                        $"v{data.version}",
                        StringComparison.Ordinal
                    )))
            {
                throw new InvalidOperationException(
                    $"CHANGELOG has no release heading for v{data.version}."
                );
            }

            string readme = File.ReadAllText(
                Path.Combine(demoRoot, "README.md")
            );
            if (!readme.Contains(
                    data.version,
                    StringComparison.Ordinal
                ))
            {
                throw new InvalidOperationException(
                    $"README does not display version {data.version}."
                );
            }

            Debug.Log(
                $"Version audit passed: v{data.version}, "
                + $"content v{data.contentVersion}, "
                + $"save schema v{data.saveSchemaVersion}."
            );
        }
    }
}
