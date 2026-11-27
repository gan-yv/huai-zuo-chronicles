using System;
using System.Collections.Generic;
using System.IO;
using Demo02.Combat;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEngine;
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
        private const float StateCheckInterval = 1f;

        [SerializeField, Min(0f)] private float autoSaveDebounce = 2f;
        [SerializeField, Min(1f)] private float autoSaveMinInterval = 8f;

        public static int ActiveManualSlot { get; private set; }

#if UNITY_EDITOR
        public static string SaveDirectoryOverrideForTests { get; set; }

        public static string GetSlotPathForTests(int slotIndex)
        {
            return GetSlotPath(slotIndex);
        }

        public static string GetSlotBackupPathForTests(int slotIndex)
        {
            return SaveFileCodec.GetBackupPath(GetSlotPath(slotIndex));
        }

        public static void WriteSaveFileForTests(
            string path,
            WorldSaveData data)
        {
            SaveFileCodec.WriteFile(path, data);
        }
#endif

        private InvestigationState investigationState;
        private InventoryState inventoryState;
        private PlayerPartyController party;
        private TimeLayerController timeLayerController;
        private string statusMessage;
        private float statusMessageUntil;
        private string lastStateSignature;
        private float nextStateCheck;
        private float nextAutoSaveAt;
        private float lastAutoSaveAt = float.NegativeInfinity;
        private bool autoSavePending;
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
                if (!SaveFileCodec.TryReadFile(
                        legacyPath,
                        out WorldSaveData legacyData,
                        out string error))
                {
                    throw new InvalidDataException(error);
                }
                if (string.IsNullOrWhiteSpace(legacyData.SavedAtUtc))
                {
                    legacyData.SavedAtUtc = File
                        .GetLastWriteTimeUtc(legacyPath)
                        .ToString("O");
                }
                SaveFileCodec.WriteFile(autoPath, legacyData);

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
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
            inventoryState = Demo02.Core.GameServices.Resolve<InventoryState>();
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            timeLayerController = Demo02.Core.GameServices.Resolve<TimeLayerController>();

            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
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
            if (isPreviewMode)
            {
                return;
            }

            if (Demo02Input.QuickSavePressed)
            {
                if (IsExclusiveUiOpen())
                {
                    return;
                }

                int quickSlot = ActiveManualSlot > 0 ? ActiveManualSlot : 1;
                SaveCurrentState(announce: true, slotIndex: quickSlot);
            }
            else if (Demo02Input.QuickLoadPressed)
            {
                if (IsExclusiveUiOpen())
                {
                    return;
                }

                LoadSavedState();
            }

            if (Time.unscaledTime >= nextStateCheck)
            {
                nextStateCheck = Time.unscaledTime + StateCheckInterval;
                string signature = BuildStateSignature();
                if (!string.IsNullOrWhiteSpace(lastStateSignature)
                    && signature != lastStateSignature)
                {
                    autoSavePending = true;
                    nextAutoSaveAt = Time.unscaledTime
                        + Mathf.Max(0f, autoSaveDebounce);
                }

                lastStateSignature = signature;
            }

            if (autoSavePending
                && Time.unscaledTime >= nextAutoSaveAt
                && Time.unscaledTime >= lastAutoSaveAt
                    + autoSaveMinInterval)
            {
                autoSavePending = false;
                lastAutoSaveAt = Time.unscaledTime;
                SaveCurrentState(announce: false, slotIndex: AutoSlot);
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
            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            WorldSaveData data = new()
            {
                Version = WorldSaveData.CurrentVersion,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                SceneName = SceneManager.GetActiveScene().name,
                ChapterId = runtime != null
                    ? runtime.CurrentChapterId
                    : string.Empty,
                CaseId = runtime != null
                    ? runtime.CurrentCaseId
                    : string.Empty,
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
                InventoryItems = inventoryState != null
                    ? inventoryState.ExportItems()
                    : new List<InventoryItemSaveData>(),
                CollectedPickupIds = inventoryState != null
                    ? new List<string>(
                        inventoryState.ExportCollectedPickupIds()
                    )
                    : new List<string>(),
                UnlockedAnchorIds = runtime != null
                    ? new List<string>(
                        runtime.ExportUnlockedAnchorIds()
                    )
                    : new List<string>(),
                CompletedStreamingRegionIds = runtime != null
                    ? new List<string>(
                        runtime.ExportCompletedStreamingRegionIds()
                    )
                    : new List<string>(),
                CompletedEncounterIds = runtime != null
                    ? new List<string>(
                        runtime.ExportCompletedEncounterIds()
                    )
                    : new List<string>(),
                CompletedWorldContentIds = runtime != null
                    ? new List<string>(
                        runtime.ExportCompletedWorldContentIds()
                    )
                    : new List<string>(),
                StartedWorldContentIds = runtime != null
                    ? new List<string>(
                        runtime.ExportStartedWorldContentIds()
                    )
                    : new List<string>(),
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
                    PersistentId = combatant.PersistentId,
                    ObjectName = combatant.gameObject.name,
                    Health = combatant.CurrentHealth,
                    Active = combatant.gameObject.activeSelf
                });
            }

            try
            {
                string savePath = GetSlotPath(slotIndex);
                SaveFileCodec.WriteFile(savePath, data);
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
            foreach (string argument in Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(
                    "--preview-",
                    StringComparison.Ordinal
                )
                    || argument.StartsWith(
                        "--benchmark-",
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
            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
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
            return GameplayUiState.IsModalOpen
                || GameServices.GetOrNull<IGameFlowState>() is { } flow
                    && flow.IsPaused;
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
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreUnlockedAnchors(
                data.UnlockedAnchorIds
            );
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreCompletedStreamingRegions(
                data.CompletedStreamingRegionIds
            );
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreCompletedEncounters(
                data.CompletedEncounterIds
            );
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreCompletedWorldContent(
                data.CompletedWorldContentIds
            );
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreStartedWorldContent(
                data.StartedWorldContentIds
            );
            inventoryState?.Restore(
                data.InventoryItems,
                data.CollectedPickupIds
            );

            RestoreCombatantStates(
                FindObjectsByType<Combatant>(FindObjectsInactive.Include),
                data.Combatants
            );

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

            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RestoreChapterIdentity(
                data.ChapterId,
                data.CaseId
            );
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.SetCurrentArea(data.AreaId, data.AreaName);
        }

        private static void RestoreCombatantStates(
            IReadOnlyList<Combatant> sceneCombatants,
            IReadOnlyList<CombatantSaveData> savedCombatants)
        {
            Dictionary<string, Combatant> byPersistentId =
                new(StringComparer.Ordinal);
            Dictionary<string, Combatant> byLegacyName =
                new(StringComparer.Ordinal);
            foreach (Combatant combatant in sceneCombatants)
            {
                if (combatant == null)
                {
                    continue;
                }

                if (!byPersistentId.TryAdd(
                        combatant.PersistentId,
                        combatant
                    ))
                {
                    Debug.LogWarning(
                        "Duplicate combatant persistent ID: "
                        + combatant.PersistentId
                    );
                }
                if (!string.IsNullOrWhiteSpace(combatant.gameObject.name))
                {
                    byLegacyName.TryAdd(
                        combatant.gameObject.name,
                        combatant
                    );
                }
            }

            if (savedCombatants == null)
            {
                return;
            }
            foreach (CombatantSaveData savedCombatant in savedCombatants)
            {
                if (savedCombatant == null)
                {
                    continue;
                }

                Combatant combatant = null;
                bool matched = !string.IsNullOrWhiteSpace(
                        savedCombatant.PersistentId
                    )
                    && byPersistentId.TryGetValue(
                        savedCombatant.PersistentId,
                        out combatant
                    );
                if (!matched
                    && !string.IsNullOrWhiteSpace(
                        savedCombatant.ObjectName
                    ))
                {
                    matched = byLegacyName.TryGetValue(
                        savedCombatant.ObjectName,
                        out combatant
                    );
                }
                if (!matched)
                {
                    continue;
                }

                combatant.RestoreHealth(savedCombatant.Health);
                combatant.gameObject.SetActive(savedCombatant.Active);
            }
        }

#if UNITY_EDITOR
        public static void RestoreCombatantStatesForTests(
            IReadOnlyList<Combatant> sceneCombatants,
            IReadOnlyList<CombatantSaveData> savedCombatants)
        {
            RestoreCombatantStates(sceneCombatants, savedCombatants);
        }
#endif

        private bool RefreshReferences()
        {
            investigationState ??= Demo02.Core.GameServices.Resolve<InvestigationState>();
            inventoryState ??= Demo02.Core.GameServices.Resolve<InventoryState>();
            party ??= Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            timeLayerController ??= Demo02.Core.GameServices.Resolve<TimeLayerController>();
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

            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            List<string> state = new()
            {
                "scene:" + SceneManager.GetActiveScene().name,
                "chapter:"
                    + (runtime != null
                        ? runtime.CurrentChapterId
                        : string.Empty),
                "case:"
                    + (runtime != null
                        ? runtime.CurrentCaseId
                        : string.Empty),
                "party:" + party.ActiveIndex,
                "stage:" + investigationState.Stage,
                "solved:"
                    + investigationState.TestimonyContradictionSolved,
                "attempts:" + investigationState.ContradictionAttempts,
                "stance:" + investigationState.Stance,
                "plan:" + investigationState.Plan,
                "repaired:" + investigationState.IsRepaired,
                "conclusion:" + investigationState.SelectedConclusion,
                "layer:" + timeLayerController.CurrentLayer,
                "area:"
                    + (runtime != null ? runtime.CurrentAreaId : string.Empty),
                "inventory:"
                    + (inventoryState != null
                        ? inventoryState.BuildSignature()
                        : string.Empty)
            };
            AppendSortedIds(
                state,
                "clue",
                investigationState.ExportClueIds()
            );
            AppendSortedIds(
                state,
                "anchor",
                investigationState.ExportTimeAnchorIds()
            );
            AppendSortedIds(
                state,
                "echo",
                investigationState.ExportEchoIds()
            );
            AppendSortedIds(
                state,
                "fact",
                investigationState.ExportStoryFactIds()
            );
            if (inventoryState != null)
            {
                AppendSortedIds(
                    state,
                    "pickup",
                    inventoryState.ExportCollectedPickupIds()
                );
            }
            if (runtime != null)
            {
                AppendSortedIds(
                    state,
                    "fast-travel",
                    runtime.ExportUnlockedAnchorIds()
                );
                AppendSortedIds(
                    state,
                    "stream",
                    runtime.ExportCompletedStreamingRegionIds()
                );
                AppendSortedIds(
                    state,
                    "encounter",
                    runtime.ExportCompletedEncounterIds()
                );
                AppendSortedIds(
                    state,
                    "content",
                    runtime.ExportCompletedWorldContentIds()
                );
                AppendSortedIds(
                    state,
                    "started",
                    runtime.ExportStartedWorldContentIds()
                );
            }
            AppendCombatantState(state);
            return string.Join("|", state);
        }

        private static void AppendSortedIds(
            List<string> state,
            string prefix,
            IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            List<string> sorted = new();
            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    sorted.Add(id);
                }
            }
            sorted.Sort(StringComparer.Ordinal);
            foreach (string id in sorted)
            {
                state.Add(prefix + ":" + id);
            }
        }

        private static void AppendCombatantState(List<string> state)
        {
            Combatant[] combatants = FindObjectsByType<Combatant>(
                FindObjectsInactive.Include
            );
            List<string> identities = new(combatants.Length);
            foreach (Combatant combatant in combatants)
            {
                if (combatant == null)
                {
                    continue;
                }
                identities.Add(
                    combatant.PersistentId
                    + ":"
                    + Mathf.CeilToInt(combatant.CurrentHealth)
                    + ":"
                    + combatant.gameObject.activeSelf
                );
            }
            identities.Sort(StringComparer.Ordinal);
            foreach (string identity in identities)
            {
                state.Add("combatant:" + identity);
            }
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

            if (SaveFileCodec.TryReadWithRecovery(
                    savePath,
                    out data,
                    out bool recovered,
                    out string error))
            {
                if (recovered)
                {
                    Debug.LogWarning(
                        $"Recovered damaged save from backup: {savePath}"
                    );
                }
                return true;
            }

            Debug.LogWarning($"Ignoring invalid save {savePath}: {error}");
            data = null;
            return false;
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
                string backupPath =
                    SaveFileCodec.GetBackupPath(savePath);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
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
