using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Demo02.UI;

namespace Demo02.Core
{
    /// <summary>
    /// Single runtime entry point for gameplay and menu input.
    /// Bindings stay data-driven through InputActionMap and can be rebound.
    /// </summary>
    public static class Demo02Input
    {
        private const string OverridePreference =
            "Demo02.InputBindingOverrides.v1";
        private const float GamepadStickThreshold = 0.18f;

        private static InputActionMap gameplayMap;
        private static InputActionMap uiMap;
        private static bool initialized;
        private static InputDevice lastUsedDevice;

        public static InputActionMap GameplayMap
        {
            get
            {
                EnsureInitialized();
                return gameplayMap;
            }
        }

        public static InputActionMap UiMap
        {
            get
            {
                EnsureInitialized();
                return uiMap;
            }
        }

        public static Vector2 Move => ReadVector2(gameplayMap, "Move");
        public static Vector2 Look => ReadVector2(gameplayMap, "Look");
        public static bool SprintHeld => Held(gameplayMap, "Sprint");
        public static bool JumpPressed => Pressed(gameplayMap, "Jump");
        public static bool DodgePressed => Pressed(gameplayMap, "Dodge");
        public static bool AttackPressed => Pressed(gameplayMap, "Attack");
        public static bool AttackHeld => Held(gameplayMap, "Attack");
        public static bool InteractPressed => Pressed(gameplayMap, "Interact");
        public static bool ToggleTimePressed => Pressed(gameplayMap, "ToggleTime");
        public static bool SkillPressed => Pressed(gameplayMap, "Skill");
        public static bool BurstPressed => Pressed(gameplayMap, "Burst");
        public static bool QuickHealPressed => Pressed(gameplayMap, "QuickHeal");
        public static bool CyclePartyPressed => Pressed(gameplayMap, "CycleParty");

        public static bool CameraRotateHeld =>
            !GameplayUiState.IsGameplayBlocked
            && (Held(gameplayMap, "RotateCamera")
                || (Gamepad.current != null
                    && Gamepad.current.rightStick.ReadValue()
                        .sqrMagnitude
                        >= GamepadStickThreshold
                            * GamepadStickThreshold));

        public static bool SubmitPressed => Pressed(uiMap, "Submit");
        public static bool AdvancePressed => Pressed(uiMap, "Advance");
        public static bool CancelPressed => Pressed(uiMap, "Cancel");
        public static bool ClickPressed => Pressed(uiMap, "Click");
        public static bool NavigateUpPressed => Pressed(uiMap, "UiUp");
        public static bool NavigateDownPressed => Pressed(uiMap, "UiDown");
        public static bool NavigateLeftPressed => Pressed(uiMap, "UiLeft");
        public static bool NavigateRightPressed => Pressed(uiMap, "UiRight");
        public static bool JournalPressed => Pressed(uiMap, "Journal");
        public static bool MapPressed => Pressed(uiMap, "Map");
        public static bool InventoryPressed => Pressed(uiMap, "Inventory");
        public static bool SettingsPressed => Pressed(uiMap, "Settings");
        public static bool TestimonyPressed => Pressed(uiMap, "Testimony");
        public static bool VisionPressed => Pressed(uiMap, "Vision");
        public static bool QuickSavePressed => Pressed(uiMap, "QuickSave");
        public static bool QuickLoadPressed => Pressed(uiMap, "QuickLoad");
        public static bool HelpPressed => Pressed(uiMap, "Help");
        public static bool MutePressed => Pressed(uiMap, "Mute");
        public static bool DiagnosticsPressed => Pressed(uiMap, "Diagnostics");
        public static bool RestartPressed => Pressed(uiMap, "Restart");
        public static bool PartySlotPressed(int slotIndex) =>
            Pressed(gameplayMap, $"Party{slotIndex + 1}");
        public static bool ChoicePressed(int choiceIndex) =>
            Pressed(uiMap, $"Choice{choiceIndex + 1}");
        public static Vector2 Navigation => ReadVector2(uiMap, "Navigate");
        public static Vector2 PointerPosition => ReadVector2(uiMap, "Point");
        public static bool UsingGamepad =>
            lastUsedDevice is Gamepad
            || (lastUsedDevice == null && Gamepad.current != null);
        public static bool UsingKeyboardMouse =>
            lastUsedDevice is Keyboard
            || lastUsedDevice is Mouse
            || (lastUsedDevice == null && Gamepad.current == null);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            gameplayMap = Demo02InputBindings.CreateGameplayMap();
            uiMap = Demo02InputBindings.CreateUiMap();
            gameplayMap.Enable();
            uiMap.Enable();
            LoadBindingOverrides();
            initialized = true;
        }

        public static bool TryRebind(
            string actionName,
            int bindingIndex,
            Action<bool, string> onComplete)
        {
            EnsureInitialized();
            InputAction action = FindAction(actionName);
            if (action == null
                || bindingIndex < 0
                || bindingIndex >= action.bindings.Count)
            {
                onComplete?.Invoke(
                    false,
                    $"Unknown input action or binding: {actionName}[{bindingIndex}]"
                );
                return false;
            }

            action.Disable();
            action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.12f)
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    if (action.bindings[bindingIndex].hasOverrides)
                    {
                        SaveBindingOverrides();
                    }
                    action.Enable();
                    onComplete?.Invoke(
                        true,
                        action.GetBindingDisplayString(bindingIndex)
                    );
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    action.Enable();
                    onComplete?.Invoke(false, "Rebind cancelled.");
                })
                .Start();
            return true;
        }

        public static string GetBindingDisplay(
            string actionName,
            int bindingIndex)
        {
            EnsureInitialized();
            InputAction action = FindAction(actionName);
            return action != null
                && bindingIndex >= 0
                && bindingIndex < action.bindings.Count
                    ? action.GetBindingDisplayString(bindingIndex)
                    : string.Empty;
        }

        public static string GetPreferredBindingDisplay(string actionName)
        {
            EnsureInitialized();
            InputAction action = FindAction(actionName);
            if (action == null)
            {
                return string.Empty;
            }

            bool wantsGamepad = UsingGamepad;
            int fallbackIndex = -1;
            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (binding.isComposite)
                {
                    continue;
                }
                string path = !string.IsNullOrWhiteSpace(
                        binding.overridePath
                    )
                    ? binding.overridePath
                    : binding.path;
                bool isGamepad = path.StartsWith(
                    "<Gamepad>",
                    StringComparison.OrdinalIgnoreCase
                );
                bool isKeyboardMouse = path.StartsWith(
                        "<Keyboard>",
                        StringComparison.OrdinalIgnoreCase
                    )
                    || path.StartsWith(
                        "<Mouse>",
                        StringComparison.OrdinalIgnoreCase
                    );
                if (fallbackIndex < 0 && (isGamepad || isKeyboardMouse))
                {
                    fallbackIndex = index;
                }
                if ((wantsGamepad && isGamepad)
                    || (!wantsGamepad && isKeyboardMouse))
                {
                    return action.GetBindingDisplayString(index);
                }
            }
            return fallbackIndex >= 0
                ? action.GetBindingDisplayString(fallbackIndex)
                : action.GetBindingDisplayString(0);
        }

        public static string ResolvePrompt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string interact = GetPreferredBindingDisplay("Interact");
            if (text.StartsWith("E  ", StringComparison.Ordinal)
                || text.StartsWith("E ", StringComparison.Ordinal))
            {
                return text.Substring(1).TrimStart().Insert(0, interact + "  ");
            }
            string toggle = GetPreferredBindingDisplay("ToggleTime");
            if (text.StartsWith("Q  ", StringComparison.Ordinal)
                || text.StartsWith("Q ", StringComparison.Ordinal))
            {
                return text.Substring(1).TrimStart().Insert(0, toggle + "  ");
            }
            return text;
        }

        public static void SaveBindingOverrides()
        {
            EnsureInitialized();
            string gameplay = gameplayMap.SaveBindingOverridesAsJson();
            string ui = uiMap.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(
                OverridePreference + ".gameplay",
                gameplay
            );
            PlayerPrefs.SetString(OverridePreference + ".ui", ui);
            PlayerPrefs.Save();
        }

        public static void ResetBindingOverrides()
        {
            EnsureInitialized();
            foreach (InputAction action in gameplayMap.actions)
            {
                action.RemoveAllBindingOverrides();
            }
            foreach (InputAction action in uiMap.actions)
            {
                action.RemoveAllBindingOverrides();
            }
            PlayerPrefs.DeleteKey(OverridePreference + ".gameplay");
            PlayerPrefs.DeleteKey(OverridePreference + ".ui");
            PlayerPrefs.Save();
        }

        private static void LoadBindingOverrides()
        {
            string gameplayJson =
                PlayerPrefs.GetString(OverridePreference + ".gameplay");
            string uiJson = PlayerPrefs.GetString(
                OverridePreference + ".ui"
            );
            if (!string.IsNullOrWhiteSpace(gameplayJson))
            {
                try
                {
                    gameplayMap.LoadBindingOverridesFromJson(gameplayJson);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning(
                        "Ignoring invalid gameplay binding overrides: "
                        + exception.Message
                    );
                }
            }
            if (!string.IsNullOrWhiteSpace(uiJson))
            {
                try
                {
                    uiMap.LoadBindingOverridesFromJson(uiJson);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning(
                        "Ignoring invalid UI binding overrides: "
                        + exception.Message
                    );
                }
            }
        }

        private static InputAction FindAction(string actionName)
        {
            return gameplayMap.FindAction(actionName, throwIfNotFound: false)
                ?? uiMap.FindAction(actionName, throwIfNotFound: false);
        }

        private static bool Pressed(InputActionMap map, string actionName)
        {
            EnsureInitialized();
            if (map == gameplayMap && GameplayUiState.IsGameplayBlocked)
            {
                return false;
            }
            InputAction action = map.FindAction(actionName, false);
            TrackDevice(action);
            return action != null && action.WasPressedThisFrame();
        }

        private static bool Held(InputActionMap map, string actionName)
        {
            EnsureInitialized();
            if (map == gameplayMap && GameplayUiState.IsGameplayBlocked)
            {
                return false;
            }
            InputAction action = map.FindAction(actionName, false);
            TrackDevice(action);
            return action != null && action.IsPressed();
        }

        private static Vector2 ReadVector2(
            InputActionMap map,
            string actionName)
        {
            EnsureInitialized();
            if (map == gameplayMap && GameplayUiState.IsGameplayBlocked)
            {
                return Vector2.zero;
            }
            InputAction action = map.FindAction(actionName, false);
            TrackDevice(action);
            return action != null
                ? action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private static void TrackDevice(InputAction action)
        {
            InputDevice device = action?.activeControl?.device;
            if (device != null)
            {
                lastUsedDevice = device;
            }
        }

    }
}
