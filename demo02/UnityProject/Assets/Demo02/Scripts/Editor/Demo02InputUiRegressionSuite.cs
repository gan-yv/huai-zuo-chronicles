using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Demo02.Editor
{
    internal static class Demo02InputUiRegressionSuite
    {
        public static void VerifyInputActionsForBatch()
        {
            Demo02Input.EnsureInitialized();
            InputActionMap gameplay = Demo02Input.GameplayMap;
            InputActionMap ui = Demo02Input.UiMap;
            if (gameplay.name != "Gameplay"
                || ui.name != "UI"
                || gameplay.actions.Count < 15
                || ui.actions.Count < 15)
            {
                throw new System.InvalidOperationException(
                    "Input action maps are incomplete."
                );
            }

            RequireBindings(
                gameplay.FindAction("Move", true),
                "<Keyboard>/w",
                "<Gamepad>/leftStick"
            );
            RequireBindings(
                gameplay.FindAction("Attack", true),
                "<Mouse>/leftButton",
                "<Gamepad>/rightTrigger"
            );
            RequireBindings(
                ui.FindAction("Submit", true),
                "<Keyboard>/enter",
                "<Gamepad>/buttonSouth"
            );
            RequireBindings(
                ui.FindAction("Advance", true),
                "<Keyboard>/e",
                "<Gamepad>/buttonSouth"
            );
            RequireBindings(
                ui.FindAction("Choice1", true),
                "<Keyboard>/digit1",
                "<Gamepad>/dpad/left"
            );
            RequireBindings(
                ui.FindAction("UiUp", true),
                "<Keyboard>/upArrow",
                "<Gamepad>/dpad/up"
            );

            InputAction interact = gameplay.FindAction("Interact", true);
            string overrides =
                gameplay.SaveBindingOverridesAsJson();
            try
            {
                interact.ApplyBindingOverride(0, "<Keyboard>/x");
                if (!interact.bindings[0].overridePath.Contains(
                        "<Keyboard>/x"
                    ))
                {
                    throw new System.InvalidOperationException(
                        "Runtime input rebinding did not apply."
                    );
                }
            }
            finally
            {
                gameplay.LoadBindingOverridesFromJson(overrides);
            }

            Debug.Log(
                "Input verification passed: gameplay/UI maps, keyboard, "
                + "mouse, gamepad and rebinding overrides."
            );
        }

        public static void VerifyUiArchitectureForBatch()
        {
            MethodInfo onGui = typeof(AbilityHud).GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo canvasField = typeof(AbilityHud).GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
                .FirstOrDefault(field =>
                    field.FieldType == typeof(Canvas));
            FieldInfo imageField = typeof(AbilityHud).GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
                .FirstOrDefault(field =>
                    field.FieldType == typeof(Image));
            if (onGui != null
                || canvasField == null
                || imageField == null)
            {
                throw new System.InvalidOperationException(
                    "AbilityHud has not migrated to the persistent UGUI path."
                );
            }

            GameObject hudProbe = new("AbilityHudLayoutProbe");
            AbilityHud hud = hudProbe.AddComponent<AbilityHud>();
            MethodInfo ensureUi = typeof(AbilityHud).GetMethod(
                "EnsureUi",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            ensureUi?.Invoke(hud, null);
            Canvas canvas = hudProbe.GetComponentInChildren<Canvas>(true);
            CanvasScaler scaler = canvas != null
                ? canvas.GetComponent<CanvasScaler>()
                : null;
            Text[] labels = canvas != null
                ? canvas.GetComponentsInChildren<Text>(true)
                : new Text[0];
            bool layoutValid = ensureUi != null
                && canvas != null
                && scaler != null
                && labels.Length >= 6;
            Object.DestroyImmediate(hudProbe);
            if (!layoutValid)
            {
                throw new System.InvalidOperationException(
                    "Migrated AbilityHud did not create its persistent "
                    + "UGUI hierarchy."
                );
            }

            GameObject firstModal = new("FirstModalProbe");
            GameObject secondModal = new("SecondModalProbe");
            try
            {
                GameplayUiState.ResetModals();
                GameplayUiState.RegisterModal(firstModal);
                GameplayUiState.RegisterModal(secondModal);
                if (!GameplayUiState.IsModalOpen
                    || GameplayUiState.OpenModalCount != 2
                    || GameplayUiState.ActiveModal != secondModal
                    || !GameplayUiState.IsOpen(firstModal)
                    || !GameplayUiState.HasOpenModalExcept(firstModal)
                    || !GameplayUiState.HasOpenModalExcept(secondModal))
                {
                    throw new System.InvalidOperationException(
                        "The shared modal stack did not track open panels."
                    );
                }

                GameplayUiState.UnregisterModal(secondModal);
                GameplayUiState.UnregisterModal(firstModal);
                if (GameplayUiState.IsModalOpen
                    || GameplayUiState.OpenModalCount != 0
                    || GameplayUiState.ActiveModal != null)
                {
                    throw new System.InvalidOperationException(
                        "The shared modal stack did not release closed panels."
                    );
                }
            }
            finally
            {
                GameplayUiState.ResetModals();
                Object.DestroyImmediate(firstModal);
                Object.DestroyImmediate(secondModal);
            }

            VerifyMigratedHud<PartyHud>(
                "PartyHudMigrationProbe",
                minimumTexts: 10,
                minimumButtons: 4
            );
            VerifyMigratedHud<EliteEnemyHud>(
                "EliteHudMigrationProbe",
                minimumTexts: 2,
                minimumButtons: 0
            );
            VerifyMigratedHud<ContextHintController>(
                "ContextHintMigrationProbe",
                minimumTexts: 2,
                minimumButtons: 0
            );
            VerifyMigratedHud<CombatHud>(
                "CombatHudMigrationProbe",
                minimumTexts: 14,
                minimumButtons: 5
            );
            VerifyMigratedHud<ObjectiveDisplay>(
                "ObjectiveHudMigrationProbe",
                minimumTexts: 4,
                minimumButtons: 0
            );
            VerifyMigratedHud<QuestNavigationHud>(
                "QuestNavigationMigrationProbe",
                minimumTexts: 2,
                minimumButtons: 0
            );
            VerifyMigratedHud<CombatFeedbackHud>(
                "CombatFeedbackMigrationProbe",
                minimumTexts: 1,
                minimumButtons: 0
            );
            VerifyMigratedHud<StoryFactNotification>(
                "StoryFactMigrationProbe",
                minimumTexts: 3,
                minimumButtons: 0
            );
            VerifyMigratedHud<WorldContentToast>(
                "WorldContentToastMigrationProbe",
                minimumTexts: 1,
                minimumButtons: 0
            );
            VerifyMigratedHud<AreaBanner>(
                "AreaBannerMigrationProbe",
                minimumTexts: 2,
                minimumButtons: 0
            );
            VerifyMigratedHud<SettingsController>(
                "SettingsMigrationProbe",
                minimumTexts: 10,
                minimumButtons: 14
            );
            VerifyMigratedHud<SaveSlotController>(
                "SaveSlotMigrationProbe",
                minimumTexts: 20,
                minimumButtons: 10
            );
            VerifyMigratedHud<WorldMapController>(
                "WorldMapMigrationProbe",
                minimumTexts: 12,
                minimumButtons: 7
            );
            VerifyMigratedHud<DialogueController>(
                "DialogueMigrationProbe",
                minimumTexts: 4,
                minimumButtons: 4
            );

            Debug.Log(
                "UI architecture passed: AbilityHud creates a persistent "
                + "UGUI tree; Party/Elite/Context/Combat/Objective/Quest "
                + "toast, settings, save-slot, map and dialogue views use persistent "
                + "UGUI; "
                + "and the shared modal stack tracks panel ownership."
            );
        }

        private static void VerifyMigratedHud<T>(
            string probeName,
            int minimumTexts,
            int minimumButtons)
            where T : MonoBehaviour
        {
            MethodInfo onGui = typeof(T).GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (onGui != null)
            {
                throw new System.InvalidOperationException(
                    $"{typeof(T).Name} still uses OnGUI."
                );
            }

            GameObject probe = new(probeName);
            try
            {
                T hud = probe.AddComponent<T>();
                MethodInfo ensureUi = typeof(T).GetMethod(
                    "EnsureUi",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                ensureUi?.Invoke(hud, null);
                Canvas canvas =
                    probe.GetComponentInChildren<Canvas>(true);
                if (ensureUi == null
                    || canvas == null
                    || canvas
                        .GetComponentsInChildren<Text>(true)
                        .Length < minimumTexts
                    || canvas
                        .GetComponentsInChildren<Button>(true)
                        .Length < minimumButtons)
                {
                    throw new System.InvalidOperationException(
                        $"{typeof(T).Name} did not create its UGUI tree."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        private static void RequireBindings(
            InputAction action,
            string keyboardBinding,
            string gamepadBinding)
        {
            bool hasKeyboard = action.bindings.Any(binding =>
                binding.path == keyboardBinding
                || (!string.IsNullOrWhiteSpace(binding.overridePath)
                    && binding.overridePath.Contains(
                        keyboardBinding
                    )));
            bool hasGamepad = action.bindings.Any(binding =>
                binding.path == gamepadBinding);
            if (!hasKeyboard || !hasGamepad)
            {
                throw new System.InvalidOperationException(
                    $"Input action {action.name} is missing its {keyboardBinding} "
                    + $"or {gamepadBinding} binding."
                );
            }
        }

    }
}
