using System;
using System.IO;
using UnityEngine;

namespace Demo02.Editor
{
    [Serializable]
    internal sealed class Demo02VersionData
    {
        public string version;
        public string channel;
        public int contentVersion;
        public int saveSchemaVersion;
    }

    internal static class Demo02VersionInfo
    {
        internal static string FilePath => Path.GetFullPath(
            Path.Combine(Application.dataPath, "../../version.json")
        );

        internal static Demo02VersionData Load()
        {
            if (!File.Exists(FilePath))
            {
                throw new InvalidOperationException(
                    $"Version file is missing: {FilePath}"
                );
            }
            Demo02VersionData data =
                JsonUtility.FromJson<Demo02VersionData>(
                    File.ReadAllText(FilePath)
                );
            if (data == null
                || string.IsNullOrWhiteSpace(data.version))
            {
                throw new InvalidOperationException(
                    $"Version file is invalid: {FilePath}"
                );
            }
            return data;
        }

        internal static string Version => Load().version;
    }
}
