using System;
using System.Collections.Generic;
using System.IO;
using Demo02.Combat;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    [DefaultExecutionOrder(50)]
    public sealed class SaveSystem : MonoBehaviour
    {
        public const int AutoSlot = 0;
        public const int ManualSlotCount = 3;
        private const string LegacySaveFileName = "demo02-save.json";
        private const string AutoSaveFileName = "demo02-save-auto.json";

        public static int ActiveManualSlot { get; private set; }

#if UNITY_EDITOR
        public static string SaveDirectoryOverrideForTests { get; set; }

        public static string GetSlotPathForTests(int slotIndex)
        {
            return GetSlotPath(slotIndex);
        }
#endif

        private InvestigationState investigationState;
        private PlayerPartyController party;
        private TimeLayerController timeLayerController;
        private string statusMessage;
        private float statusMessageUntil;
        private string lastStateSignature;
        private float nextStateCheck;
        private bool isPreviewMode;

        public static bool HasSave()
        {
            EnsureLegacySaveMigrated();
            for (int slot = AutoSlot; slot <= ManualSlotCount; slot++)
            {
                if (TryReadSave(slot, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSlotPath(int slotIndex)
        {
            string fileName = slotIndex == AutoSlot
                ? AutoSaveFileName
                : $"demo02-save-slot-{slotIndex}.json";
            return Path.Combine(GetSaveDirectory(), fileName);
        }

        private static string GetSaveDirectory()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(SaveDirectoryOverrideForTests))
            {
                return SaveDirectoryOverrideForTests;
            }
#endif
            return Application.persistentDataPath;
        }

        public static string GetSlotDisplayName(int slotIndex)
        {
            return slotIndex == AutoSlot ? "自动存档" : $"存档槽 {slotIndex}";
        }

        public static DateTime GetSlotLastWriteTime(int slotIndex)
        {
            if (slotIndex < AutoSlot || slotIndex > ManualSlotCount)
            {
                return DateTime.MinValue;
            }

            string savePath = GetSlotPath(slotIndex);
            return File.Exists(savePath)
                ? File.GetLastWriteTime(savePath)
                : DateTime.MinValue;
        }

        private static void EnsureLegacySaveMigrated()
        {
            string legacyPath = Path.Combine(
                GetSaveDirectory(),
                LegacySaveFileName
            );
            string autoPath = GetSlotPath(AutoSlot);
            if (!File.Exists(legacyPath) || File.Exists(autoPath))
            {
                return;
            }

            try
            {
                string legacyJson = File.ReadAllText(legacyPath);
                WorldSaveData legacyData = JsonUtility.FromJson<WorldSaveData>(
                    legacyJson
                );
                if (legacyData != null
                    && string.IsNullOrWhiteSpace(legacyData.SavedAtUtc))
                {
                    legacyData.SavedAtUtc = File
                        .GetLastWriteTimeUtc(legacyPath)
                        .ToString("O");
                    File.WriteAllText(
                        autoPath,
                        JsonUtility.ToJson(legacyData, prettyPrint: true)
                    );
                }
                else
                {
                    File.Copy(legacyPath, autoPath);
                }

                Debug.Log($"Migrated legacy save to {autoPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Start()
        {
            isPreviewMode = HasPreviewArgument();
            investigationState = FindAnyObjectByType<InvestigationState>();
            party = FindAnyObjectByType<PlayerPartyController>();
            timeLayerController = FindAnyObjectByType<TimeLayerController>();

            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime != null && runtime.PendingSave != null)
            {
                WorldSaveData pendingSave = runtime.PendingSave;
                runtime.ClearPendingSave();
                if (pendingSave.SceneName == SceneManager.GetActiveScene().name)
                {
                    ApplySave(pendingSave);
                    ShowStatus("存档已读取");
                }
            }
        }

        private void Update()
        {
            if (isPreviewMode || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                if (IsExclusiveUiOpen())
                {
                    return;
                }

                int quickSlot = ActiveManualSlot > 0 ? ActiveManualSlot : 1;
                SaveCurrentState(announce: true, slotIndex: quickSlot);
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (IsExclusiveUiOpen())
                {
                    return;
                }

                LoadSavedState();
            }

            if (Time.unscaledTime >= nextStateCheck)
            {
                nextStateCheck = Time.unscaledTime + 1f;
                string signature = BuildStateSignature();
                if (!string.IsNullOrWhiteSpace(lastStateSignature)
                    && signature != lastStateSignature)
                {
                    SaveCurrentState(announce: false, slotIndex: AutoSlot);
                }

                lastStateSignature = signature;
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(statusMessage)
                || Time.unscaledTime >= statusMessageUntil)
            {
                return;
            }

            float scale = UiTheme.Scale;
            Rect panel = new(
                18f * scale,
                Screen.height - 104f * scale,
                250f * scale,
                34f * scale
            );
            UiTheme.DrawRect(panel, UiTheme.Ink);
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 3f * scale, panel.height),
                UiTheme.Accent
            );
            GUI.Label(
                new Rect(
                    panel.x + 14f * scale,
                    panel.y,
                    panel.width - 20f * scale,
                    panel.height
                ),
                statusMessage,
                UiTheme.Label(13)
            );
        }

        public void SaveCurrentState(bool announce = true, int slotIndex = AutoSlot)
        {
            if (!RefreshReferences())
            {
                ShowStatus("没有可保存的游戏状态");
                return;
            }

            slotIndex = Mathf.Clamp(slotIndex, AutoSlot, ManualSlotCount);
            Transform activeCharacter = party.ActiveCharacter;
            WorldRuntime runtime = WorldRuntime.Instance;
            WorldSaveData data = new()
            {
                Version = 1,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                SceneName = SceneManager.GetActiveScene().name,
                PlayerX = activeCharacter.position.x,
                PlayerY = activeCharacter.position.y,
                PlayerZ = activeCharacter.position.z,
                PlayerRotationY = activeCharacter.eulerAngles.y,
                ActivePartyIndex = party.ActiveIndex,
                TimeLayer = timeLayerController.CurrentLayer,
                AreaId = runtime != null ? runtime.CurrentAreaId : string.Empty,
                AreaName = runtime != null ? runtime.CurrentAreaName : string.Empty,
                Stance = investigationState.Stance,
                EvidencePlan = investigationState.Plan,
                ClueIds = new List<string>(investigationState.ExportClueIds()),
                TimeAnchorIds = new List<string>(investigationState.ExportTimeAnchorIds()),
                EchoIds = new List<string>(investigationState.ExportEchoIds()),
                StoryFactIds = new List<string>(
                    investigationState.ExportStoryFactIds()
                ),
                ChapterStage = investigationState.Stage,
                TestimonyContradictionSolved =
                    investigationState.TestimonyContradictionSolved,
                ContradictionAttempts =
                    investigationState.ContradictionAttempts,
                IsRepaired = investigationState.IsRepaired,
                Conclusion = investigationState.SelectedConclusion
            };

            Combatant[] combatants = FindObjectsByType<Combatant>(FindObjectsInactive.Include);
            foreach (Combatant combatant in combatants)
            {
                data.Combatants.Add(new CombatantSaveData
                {
                    ObjectName = combatant.gameObject.name,
                    Health = combatant.CurrentHealth,
                    Active = combatant.gameObject.activeSelf
                });
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                string savePath = GetSlotPath(slotIndex);
                File.WriteAllText(savePath, json);
                if (slotIndex > AutoSlot)
                {
                    ActiveManualSlot = slotIndex;
                }

                Debug.Log(
                    $"{(announce ? "Saved" : "Auto-saved")} game to "
                    + $"{GetSlotDisplayName(slotIndex)} ({savePath})"
                );
                if (announce)
                {
                    ShowStatus($"已保存到{GetSlotDisplayName(slotIndex)}");
                    GameAudio.PlayConfirm();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowStatus("保存失败，请查看 Console");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && !isPreviewMode)
            {
                SaveCurrentState(announce: false, slotIndex: AutoSlot);
            }
        }

        private void OnApplicationQuit()
        {
            if (!isPreviewMode)
            {
                SaveCurrentState(announce: false, slotIndex: AutoSlot);
            }
        }

        private static bool HasPreviewArgument()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(
                    "--preview-",
                    StringComparison.Ordinal
                ))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool LoadLatestSavedGame(out WorldSaveData data)
        {
            if (!TryReadLatestSave(out data, out int slotIndex))
            {
                return false;
            }

            return LoadSaveData(data, slotIndex);
        }

        public static bool LoadSavedGame(int slotIndex, out WorldSaveData data)
        {
            if (!TryReadSave(slotIndex, out data))
            {
                return false;
            }

            return LoadSaveData(data, slotIndex);
        }

        private static bool LoadSaveData(WorldSaveData data, int slotIndex)
        {
            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime == null)
            {
                Debug.LogError("WorldRuntime is missing while loading a save.");
                return false;
            }

            Time.timeScale = 1f;
            runtime.QueueSaveLoad(data);
            ActiveManualSlot = slotIndex == AutoSlot ? 0 : slotIndex;
            SceneManager.LoadScene(data.SceneName);
            return true;
        }

        private void LoadSavedState()
        {
            bool loaded = ActiveManualSlot > 0
                ? LoadSavedGame(ActiveManualSlot, out _)
                : LoadLatestSavedGame(out _);
            if (!loaded)
            {
                ShowStatus("没有可读取的存档");
            }
        }

        private static bool IsExclusiveUiOpen()
        {
            return (SettingsController.Instance != null
                    && SettingsController.Instance.IsOpen)
                || (WorldMapController.Instance != null
                    && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
                || (TestimonyBoardController.Instance != null
                    && TestimonyBoardController.Instance.IsOpen)
                || (SaveSlotController.Instance != null
                    && SaveSlotController.Instance.IsOpen)
                || (GameFlowController.Instance != null
                    && GameFlowController.Instance.IsPaused);
        }

        private void ApplySave(WorldSaveData data)
        {
            investigationState.RestoreProgress(
                data.ClueIds,
                data.TimeAnchorIds,
                data.IsRepaired,
                data.Conclusion,
                data.ChapterStage,
                data.EchoIds,
                data.Stance,
                data.EvidencePlan,
                data.TestimonyContradictionSolved,
                data.ContradictionAttempts,
                data.StoryFactIds
            );
            timeLayerController.SetLayer(data.TimeLayer);
            party.RestoreActiveCharacter(data.ActivePartyIndex);

            Combatant[] sceneCombatants = FindObjectsByType<Combatant>(FindObjectsInactive.Include);
            Dictionary<string, Combatant> byName = new();
            foreach (Combatant combatant in sceneCombatants)
            {
                byName[combatant.gameObject.name] = combatant;
            }

            foreach (CombatantSaveData savedCombatant in data.Combatants)
            {
                if (byName.TryGetValue(savedCombatant.ObjectName, out Combatant combatant))
                {
                    combatant.RestoreHealth(savedCombatant.Health);
                    combatant.gameObject.SetActive(savedCombatant.Active);
                }
            }

            Transform activeCharacter = party.ActiveCharacter;
            if (activeCharacter != null)
            {
                CharacterController controller = activeCharacter.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }

                activeCharacter.SetPositionAndRotation(
                    new Vector3(data.PlayerX, data.PlayerY, data.PlayerZ),
                    Quaternion.Euler(0f, data.PlayerRotationY, 0f)
                );

                if (controller != null)
                {
                    controller.enabled = true;
                }
            }

            WorldRuntime.Instance?.SetCurrentArea(data.AreaId, data.AreaName);
        }

        private bool RefreshReferences()
        {
            investigationState ??= FindAnyObjectByType<InvestigationState>();
            party ??= FindAnyObjectByType<PlayerPartyController>();
            timeLayerController ??= FindAnyObjectByType<TimeLayerController>();
            return investigationState != null
                && party != null
                && party.ActiveCharacter != null
                && timeLayerController != null;
        }

        private string BuildStateSignature()
        {
            if (!RefreshReferences())
            {
                return string.Empty;
            }

            WorldRuntime runtime = WorldRuntime.Instance;
            Combatant[] combatants = FindObjectsByType<Combatant>(FindObjectsInactive.Include);
            int enemyHealth = 0;
            foreach (Combatant combatant in combatants)
            {
                if (combatant.Team == CombatTeam.Enemy)
                {
                    enemyHealth += Mathf.CeilToInt(combatant.CurrentHealth);
                }
            }

            return string.Join(
                "|",
                SceneManager.GetActiveScene().name,
                party.ActiveIndex,
                investigationState.CollectedClueCount,
                investigationState.StabilizedTimeAnchorCount,
                investigationState.DiscoveredEchoCount,
                investigationState.Stage,
                investigationState.TestimonyContradictionSolved,
                investigationState.ContradictionAttempts,
                investigationState.StoryFactCount,
                investigationState.Stance,
                investigationState.Plan,
                investigationState.IsRepaired,
                investigationState.SelectedConclusion,
                timeLayerController.CurrentLayer,
                runtime != null ? runtime.CurrentAreaId : string.Empty,
                enemyHealth
            );
        }

        public static bool TryReadSave(int slotIndex, out WorldSaveData data)
        {
            data = null;
            if (slotIndex < AutoSlot || slotIndex > ManualSlotCount)
            {
                return false;
            }

            string savePath = GetSlotPath(slotIndex);
            if (!File.Exists(savePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                data = JsonUtility.FromJson<WorldSaveData>(json);
                return data != null
                    && data.Version == 1
                    && !string.IsNullOrWhiteSpace(data.SceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                data = null;
                return false;
            }
        }

        public static bool TryReadLatestSave(
            out WorldSaveData data,
            out int slotIndex)
        {
            EnsureLegacySaveMigrated();
            data = null;
            slotIndex = -1;
            DateTime latestTime = DateTime.MinValue;
            for (int slot = AutoSlot; slot <= ManualSlotCount; slot++)
            {
                if (!TryReadSave(slot, out WorldSaveData candidate))
                {
                    continue;
                }

                DateTime timestamp = DateTime.TryParse(
                    candidate.SavedAtUtc,
                    out DateTime parsed
                )
                    ? parsed.ToUniversalTime()
                    : File.GetLastWriteTimeUtc(GetSlotPath(slot));
                if (timestamp > latestTime)
                {
                    latestTime = timestamp;
                    data = candidate;
                    slotIndex = slot;
                }
            }

            return data != null;
        }

        public static bool DeleteSlot(int slotIndex)
        {
            if (slotIndex <= AutoSlot || slotIndex > ManualSlotCount)
            {
                return false;
            }

            string savePath = GetSlotPath(slotIndex);
            if (!File.Exists(savePath))
            {
                return false;
            }

            try
            {
                File.Delete(savePath);
                if (ActiveManualSlot == slotIndex)
                {
                    ActiveManualSlot = 0;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageUntil = Time.unscaledTime + 3f;
        }
    }
}
