using UnityEngine.InputSystem;

namespace Demo02.Core
{
    internal static class Demo02InputBindings
    {
        internal static InputActionMap CreateGameplayMap()
        {
            InputActionMap map = new("Gameplay");

            InputAction move = map.AddAction(
                "Move",
                InputActionType.Value
            );
            AddWasd(move);
            move.AddBinding("<Gamepad>/leftStick");

            InputAction look = map.AddAction(
                "Look",
                InputActionType.Value
            );
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            AddButton(map, "RotateCamera", "<Mouse>/rightButton");
            AddButton(map, "Sprint", "<Keyboard>/leftShift");
            AddButton(map, "Sprint", "<Gamepad>/leftStickPress");
            AddButton(map, "Jump", "<Keyboard>/space");
            AddButton(map, "Jump", "<Gamepad>/buttonSouth");
            AddButton(map, "Dodge", "<Keyboard>/leftCtrl");
            AddButton(map, "Dodge", "<Gamepad>/buttonEast");
            AddButton(map, "Attack", "<Mouse>/leftButton");
            AddButton(map, "Attack", "<Gamepad>/rightTrigger");
            AddButton(map, "Interact", "<Keyboard>/e");
            AddButton(map, "Interact", "<Gamepad>/buttonWest");
            AddButton(map, "ToggleTime", "<Keyboard>/q");
            AddButton(map, "ToggleTime", "<Gamepad>/buttonNorth");
            AddButton(map, "Skill", "<Keyboard>/f");
            AddButton(map, "Skill", "<Gamepad>/leftShoulder");
            AddButton(map, "Burst", "<Keyboard>/r");
            AddButton(map, "Burst", "<Gamepad>/rightShoulder");
            AddButton(map, "QuickHeal", "<Keyboard>/h");
            AddButton(map, "QuickHeal", "<Gamepad>/dpad/up");
            AddButton(map, "CycleParty", "<Keyboard>/tab");
            AddButton(map, "CycleParty", "<Gamepad>/dpad/down");
            for (int slot = 0; slot < 4; slot++)
            {
                AddButton(
                    map,
                    $"Party{slot + 1}",
                    $"<Keyboard>/digit{slot + 1}"
                );
            }
            AddButton(map, "Party1", "<Gamepad>/dpad/left");
            AddButton(map, "Party2", "<Gamepad>/dpad/right");
            return map;
        }

        internal static InputActionMap CreateUiMap()
        {
            InputActionMap map = new("UI");

            InputAction navigate = map.AddAction(
                "Navigate",
                InputActionType.Value
            );
            AddWasd(navigate);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");

            AddNavigationDirections(map);
            AddButton(map, "Submit", "<Keyboard>/enter");
            AddButton(map, "Submit", "<Keyboard>/numpadEnter");
            AddButton(map, "Submit", "<Keyboard>/space");
            AddButton(map, "Submit", "<Gamepad>/buttonSouth");
            AddButton(map, "Advance", "<Keyboard>/e");
            AddButton(map, "Advance", "<Keyboard>/space");
            AddButton(map, "Advance", "<Keyboard>/enter");
            AddButton(map, "Advance", "<Gamepad>/buttonSouth");
            AddButton(map, "Choice1", "<Keyboard>/digit1");
            AddButton(map, "Choice1", "<Gamepad>/dpad/left");
            AddButton(map, "Choice2", "<Keyboard>/digit2");
            AddButton(map, "Choice2", "<Gamepad>/dpad/right");
            AddButton(map, "Cancel", "<Keyboard>/escape");
            AddButton(map, "Cancel", "<Gamepad>/buttonEast");
            map.AddAction("Point", InputActionType.Value)
                .AddBinding("<Mouse>/position");
            AddButton(map, "Click", "<Mouse>/leftButton");

            AddButton(map, "Journal", "<Keyboard>/j");
            AddButton(map, "Journal", "<Gamepad>/buttonNorth");
            AddButton(map, "Map", "<Keyboard>/m");
            AddButton(map, "Map", "<Gamepad>/select");
            AddButton(map, "Inventory", "<Keyboard>/b");
            AddButton(map, "Settings", "<Keyboard>/o");
            AddButton(map, "Settings", "<Gamepad>/start");
            AddButton(map, "Testimony", "<Keyboard>/t");
            AddButton(map, "Vision", "<Keyboard>/v");
            AddButton(map, "QuickSave", "<Keyboard>/f5");
            AddButton(map, "QuickSave", "<Gamepad>/rightShoulder");
            AddButton(map, "QuickLoad", "<Keyboard>/f9");
            AddButton(map, "QuickLoad", "<Gamepad>/leftShoulder");
            AddButton(map, "Help", "<Keyboard>/f1");
            AddButton(map, "Mute", "<Keyboard>/f2");
            AddButton(map, "Diagnostics", "<Keyboard>/f3");
            AddButton(map, "Restart", "<Keyboard>/r");
            return map;
        }

        private static void AddNavigationDirections(InputActionMap map)
        {
            AddButton(map, "UiUp", "<Keyboard>/w");
            AddButton(map, "UiUp", "<Keyboard>/upArrow");
            AddButton(map, "UiUp", "<Gamepad>/leftStick/up");
            AddButton(map, "UiUp", "<Gamepad>/dpad/up");
            AddButton(map, "UiDown", "<Keyboard>/s");
            AddButton(map, "UiDown", "<Keyboard>/downArrow");
            AddButton(map, "UiDown", "<Gamepad>/leftStick/down");
            AddButton(map, "UiDown", "<Gamepad>/dpad/down");
            AddButton(map, "UiLeft", "<Keyboard>/a");
            AddButton(map, "UiLeft", "<Keyboard>/leftArrow");
            AddButton(map, "UiLeft", "<Gamepad>/leftStick/left");
            AddButton(map, "UiLeft", "<Gamepad>/dpad/left");
            AddButton(map, "UiRight", "<Keyboard>/d");
            AddButton(map, "UiRight", "<Keyboard>/rightArrow");
            AddButton(map, "UiRight", "<Gamepad>/leftStick/right");
            AddButton(map, "UiRight", "<Gamepad>/dpad/right");
        }

        private static void AddWasd(InputAction action)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        private static void AddButton(
            InputActionMap map,
            string name,
            string binding)
        {
            InputAction action = map.FindAction(name, false)
                ?? map.AddAction(name, InputActionType.Button);
            action.AddBinding(binding);
        }
    }
}
