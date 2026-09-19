using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Hollow
{
    /// <summary>
    /// Single input facade that works with the new Input System and the legacy Input Manager
    /// (whichever "Active Input Handling" is selected in Player Settings). No input assets needed.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        public static Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                var v = new Vector2(
                    (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f),
                    (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f));
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>Mouse delta in "legacy axis" units (about 0.1 per pixel).</summary>
        public static Vector2 Look
        {
            get { var m = Mouse.current; return m == null ? Vector2.zero : m.delta.ReadValue() * 0.1f; }
        }

        public static bool Sprint { get { var k = Keyboard.current; return k != null && k.leftShiftKey.isPressed; } }
        public static bool DodgePressed { get { var k = Keyboard.current; return k != null && k.spaceKey.wasPressedThisFrame; } }
        public static bool InteractPressed { get { var k = Keyboard.current; return k != null && k.eKey.wasPressedThisFrame; } }
        public static bool EscapePressed { get { var k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame; } }
        public static bool RestartPressed { get { var k = Keyboard.current; return k != null && k.rKey.wasPressedThisFrame; } }
        public static bool AttackPressed { get { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; } }

        public static bool LockOnPressed
        {
            get
            {
                var k = Keyboard.current; var m = Mouse.current;
                return (k != null && k.qKey.wasPressedThisFrame) || (m != null && m.middleButton.wasPressedThisFrame);
            }
        }
        public static bool SwitchTargetPressed { get { var k = Keyboard.current; return k != null && k.tabKey.wasPressedThisFrame; } }
        public static bool InventoryPressed { get { var k = Keyboard.current; return k != null && k.iKey.wasPressedThisFrame; } }
        public static bool MenuUpPressed
        {
            get { var k = Keyboard.current; return k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame); }
        }
        public static bool MenuDownPressed
        {
            get { var k = Keyboard.current; return k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame); }
        }
        public static bool MenuLeftPressed
        {
            get { var k = Keyboard.current; return k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame); }
        }
        public static bool MenuRightPressed
        {
            get { var k = Keyboard.current; return k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame); }
        }
        public static bool ConfirmPressed
        {
            get { var k = Keyboard.current; return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame); }
        }
        public static bool DiscardPressed { get { var k = Keyboard.current; return k != null && k.xKey.wasPressedThisFrame; } }

        public static bool SkillPressed(int index)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            switch (index)
            {
                case 0: return k.digit1Key.wasPressedThisFrame;
                case 1: return k.digit2Key.wasPressedThisFrame;
                case 2: return k.digit3Key.wasPressedThisFrame;
                case 3: return k.digit4Key.wasPressedThisFrame;
                default: return false;
            }
        }
#else
        public static Vector2 Move
        {
            get
            {
                var v = new Vector2(
                    (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                    (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public static Vector2 Look { get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } }
        public static bool Sprint { get { return Input.GetKey(KeyCode.LeftShift); } }
        public static bool DodgePressed { get { return Input.GetKeyDown(KeyCode.Space); } }
        public static bool InteractPressed { get { return Input.GetKeyDown(KeyCode.E); } }
        public static bool EscapePressed { get { return Input.GetKeyDown(KeyCode.Escape); } }
        public static bool RestartPressed { get { return Input.GetKeyDown(KeyCode.R); } }
        public static bool AttackPressed { get { return Input.GetMouseButtonDown(0); } }

        public static bool LockOnPressed { get { return Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(2); } }
        public static bool SwitchTargetPressed { get { return Input.GetKeyDown(KeyCode.Tab); } }
        public static bool InventoryPressed { get { return Input.GetKeyDown(KeyCode.I); } }
        public static bool MenuUpPressed { get { return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W); } }
        public static bool MenuDownPressed { get { return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S); } }
        public static bool MenuLeftPressed { get { return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A); } }
        public static bool MenuRightPressed { get { return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D); } }
        public static bool ConfirmPressed { get { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); } }
        public static bool DiscardPressed { get { return Input.GetKeyDown(KeyCode.X); } }

        public static bool SkillPressed(int index)
        {
            return index >= 0 && index < 4 && Input.GetKeyDown(KeyCode.Alpha1 + index);
        }
#endif
    }
}
