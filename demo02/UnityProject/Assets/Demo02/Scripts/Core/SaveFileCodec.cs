using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Demo02.Core
{
    internal static class SaveFileCodec
    {
        private const string BackupSuffix = ".bak";
        private const string TemporarySuffix = ".tmp";

        public static string Encode(WorldSaveData data)
        {
            data.Version = WorldSaveData.CurrentVersion;
            string payload = JsonUtility.ToJson(data);
            SaveFileEnvelope envelope = new()
            {
                SaveVersion = data.Version,
                Payload = payload,
                Checksum = ComputeChecksum(payload)
            };
            return JsonUtility.ToJson(envelope, prettyPrint: true);
        }

        public static bool TryDecode(
            string json,
            out WorldSaveData data,
            out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Save file is empty.";
                return false;
            }

            try
            {
                SaveFileEnvelope envelope =
                    JsonUtility.FromJson<SaveFileEnvelope>(json);
                if (envelope != null
                    && string.Equals(
                        envelope.Format,
                        SaveFileEnvelope.CurrentFormat,
                        StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(envelope.Checksum)
                    && !string.IsNullOrWhiteSpace(envelope.Payload))
                {
                    return TryDecodeEnvelope(
                        envelope,
                        out data,
                        out error
                    );
                }

                // Version 1 saves were stored as a bare WorldSaveData object.
                data = JsonUtility.FromJson<WorldSaveData>(json);
                int legacyVersion = data != null && data.Version > 0
                    ? data.Version
                    : 1;
                return SaveDataMigration.TryUpgrade(
                    data,
                    legacyVersion,
                    out error
                );
            }
            catch (Exception exception)
            {
                data = null;
                error = $"Could not parse save file: {exception.Message}";
                return false;
            }
        }

        public static void WriteFile(string path, WorldSaveData data)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = Encode(data);
            string temporaryPath = path + TemporarySuffix;
            File.WriteAllText(
                temporaryPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );

            if (!TryDecode(json, out _, out string validationError))
            {
                TryDelete(temporaryPath);
                throw new InvalidDataException(
                    $"Generated save failed validation: {validationError}"
                );
            }

            string backupPath = GetBackupPath(path);
            if (File.Exists(path)
                && TryReadFile(path, out _, out _))
            {
                File.Copy(path, backupPath, overwrite: true);
            }
            File.Copy(temporaryPath, path, overwrite: true);
            TryDelete(temporaryPath);
        }

        public static bool TryReadFile(
            string path,
            out WorldSaveData data,
            out string error)
        {
            data = null;
            error = null;
            if (!File.Exists(path))
            {
                error = "Save file does not exist.";
                return false;
            }

            try
            {
                return TryDecode(File.ReadAllText(path), out data, out error);
            }
            catch (Exception exception)
            {
                error = $"Could not read save file: {exception.Message}";
                return false;
            }
        }

        public static bool TryReadWithRecovery(
            string path,
            out WorldSaveData data,
            out bool recovered,
            out string error)
        {
            recovered = false;
            if (TryReadFile(path, out data, out error))
            {
                return true;
            }

            string primaryError = error;
            string backupPath = GetBackupPath(path);
            if (!TryReadFile(backupPath, out data, out string backupError))
            {
                error = primaryError + " " + backupError;
                return false;
            }

            try
            {
                WriteFile(path, data);
                recovered = true;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error =
                    $"Recovered save from backup but could not repair the " +
                    $"primary file: {exception.Message}";
                return true;
            }
        }

        public static string GetBackupPath(string path)
        {
            return path + BackupSuffix;
        }

        private static bool TryDecodeEnvelope(
            SaveFileEnvelope envelope,
            out WorldSaveData data,
            out string error)
        {
            data = null;
            error = null;
            if (envelope.FormatVersion
                > SaveFileEnvelope.CurrentFormatVersion)
            {
                error =
                    $"Save container version {envelope.FormatVersion} " +
                    "is not supported.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(envelope.Payload)
                || string.IsNullOrWhiteSpace(envelope.Checksum))
            {
                error = "Save container is missing its payload or checksum.";
                return false;
            }

            string expected = ComputeChecksum(envelope.Payload);
            if (!string.Equals(
                    expected,
                    envelope.Checksum,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Save checksum does not match its payload.";
                return false;
            }

            data = JsonUtility.FromJson<WorldSaveData>(
                envelope.Payload
            );
            return SaveDataMigration.TryUpgrade(
                data,
                envelope.SaveVersion,
                out error
            );
        }

        private static string ComputeChecksum(string payload)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(payload)
            );
            return BitConverter
                .ToString(hash)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
