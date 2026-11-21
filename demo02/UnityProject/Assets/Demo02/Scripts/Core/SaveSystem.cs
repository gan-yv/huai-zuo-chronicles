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
        private const string SaveFileName = "demo02-save.json";
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        private InvestigationState investigationState;
        private PlayerPartyController party;
        private TimeLayerController timeLayerController;
        private string statusMessage;
        private float statusMessageUntil;
        private string lastStateSignature;
        private float nextStateCheck;

        public static bool HasSave()
        {
            return TryReadSave(out _);
        }

        private void Start()
        {
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
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveCurrentState();
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LoadSavedState();
            }

            if (Time.unscaledTime >= nextStateCheck)
            {
                nextStateCheck = Time.unscaledTime + 1f;
                string signature = BuildStateSignature();
                if (!string.IsNullOrWhiteSpace(lastStateSignature)
                    && signature != lastStateSignature)
                {
                    SaveCurrentState(announce: false);
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

        public void SaveCurrentState(bool announce = true)
        {
            if (!RefreshReferences())
            {
                ShowStatus("没有可保存的游戏状态");
                return;
            }

            Transform activeCharacter = party.ActiveCharacter;
            WorldRuntime runtime = WorldRuntime.Instance;
            WorldSaveData data = new()
            {
                Version = 1,
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
                ChapterStage = investigationState.Stage,
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
                File.WriteAllText(SavePath, json);
                Debug.Log($"{(announce ? "Saved" : "Auto-saved")} game to {SavePath}");
                if (announce)
                {
                    ShowStatus("游戏已保存");
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
            if (pauseStatus)
            {
                SaveCurrentState(announce: false);
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentState(announce: false);
        }

        public static bool LoadSavedGame(out WorldSaveData data)
        {
            if (!TryReadSave(out data))
            {
                return false;
            }

            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime == null)
            {
                Debug.LogError("WorldRuntime is missing while loading a save.");
                return false;
            }

            Time.timeScale = 1f;
            runtime.QueueSaveLoad(data);
            SceneManager.LoadScene(data.SceneName);
            return true;
        }

        private void LoadSavedState()
        {
            if (!LoadSavedGame(out _))
            {
                ShowStatus("没有可读取的存档");
            }
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
                data.EvidencePlan
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
                investigationState.Stance,
                investigationState.Plan,
                investigationState.IsRepaired,
                investigationState.SelectedConclusion,
                timeLayerController.CurrentLayer,
                runtime != null ? runtime.CurrentAreaId : string.Empty,
                enemyHealth
            );
        }

        private static bool TryReadSave(out WorldSaveData data)
        {
            data = null;
            if (!File.Exists(SavePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
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

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageUntil = Time.unscaledTime + 3f;
        }
    }
}
