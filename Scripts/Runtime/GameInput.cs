using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Hollow
{
    /// <summary>
    /// Single input facade that works with the new Input System and the legacy Input Manager
    /// (whichever "Active Input Handling" is selected in Player Settings). No input assets needed.
    /// The new-input branch also supports a gamepad (<see cref="UsingGamepad"/> tells which device
    /// produced the latest input). Everything here is allocation-free.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        // ---------------------------------------------------------------- tuning
        /// <summary>Radial deadzone for both sticks.</summary>
        const float StickDeadzone = 0.15f;
        /// <summary>Stick deflection needed for one menu navigation step.</summary>
        const float NavThreshold = 0.5f;
        /// <summary>Seconds before a held stick/dpad starts repeating.</summary>
        const float NavRepeatDelay = 0.40f;
        /// <summary>Seconds between repeats once repeating.</summary>
        const float NavRepeatRate = 0.14f;
        /// <summary>
        /// Right stick -> <see cref="Look"/> units per second at full deflection.
        /// Look is consumed as "legacy axis units" and CameraRig multiplies it by Sensitivity (2.2),
        /// so 180 deg/s at default sensitivity means 180 / 2.2 = ~82 units per second.
        /// </summary>
        const float PadLookSpeed = 82f;

        // ------------------------------------------------- active-device tracking
        static int _deviceFrame = -1;
        static bool _usingPad;

        /// <summary>True when the most recent input came from a gamepad (false for keyboard/mouse).</summary>
        public static bool UsingGamepad { get { PollActiveDevice(); return _usingPad; } }

        /// <summary>Cheap once-per-frame refresh of the "last used device" flag.</summary>
        static void PollActiveDevice()
        {
            int f = Time.frameCount;
            if (_deviceFrame == f) return;
            _deviceFrame = f;

            var gp = Gamepad.current;
            if (gp != null && PadHasActivity(gp)) { _usingPad = true; return; }
            if (KeyboardMouseHasActivity()) _usingPad = false;
        }

        static bool PadHasActivity(Gamepad gp)
        {
            if (gp.leftStick.ReadValue().sqrMagnitude > StickDeadzone * StickDeadzone) return true;
            if (gp.rightStick.ReadValue().sqrMagnitude > StickDeadzone * StickDeadzone) return true;
            if (gp.leftTrigger.ReadValue() > 0.2f || gp.rightTrigger.ReadValue() > 0.2f) return true;
            if (gp.buttonSouth.isPressed || gp.buttonEast.isPressed || gp.buttonWest.isPressed || gp.buttonNorth.isPressed) return true;
            if (gp.leftShoulder.isPressed || gp.rightShoulder.isPressed) return true;
            if (gp.leftStickButton.isPressed || gp.rightStickButton.isPressed) return true;
            if (gp.startButton.isPressed || gp.selectButton.isPressed) return true;
            var d = gp.dpad;
            return d.up.isPressed || d.down.isPressed || d.left.isPressed || d.right.isPressed;
        }

        static bool KeyboardMouseHasActivity()
        {
            var k = Keyboard.current;
            if (k != null && k.anyKey.isPressed) return true;
            var m = Mouse.current;
            if (m == null) return false;
            if (m.delta.ReadValue().sqrMagnitude > 4f) return true;      // moved more than ~2 px
            if (m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed) return true;
            return Mathf.Abs(m.scroll.ReadValue().y) > 0.1f;
        }

        // ---------------------------------------------------------- small helpers
        /// <summary>Radial deadzone + rescale so the usable range is still 0..1.</summary>
        static Vector2 ApplyDeadzone(Vector2 v)
        {
            float m = v.magnitude;
            if (m <= StickDeadzone) return Vector2.zero;
            float scaled = (Mathf.Min(m, 1f) - StickDeadzone) / (1f - StickDeadzone);
            return v * (scaled / m);
        }

        // Menu navigation edge-detect + repeat. Slots: 0 up, 1 down, 2 left, 3 right.
        // Uses unscaled time so it keeps working while the game is paused (Time.timeScale == 0),
        // and caches the result per frame so reading a property twice cannot eat a step.
        static readonly bool[] _navHeld = new bool[4];
        static readonly float[] _navNext = new float[4];
        static readonly bool[] _navFired = new bool[4];
        static readonly int[] _navFrame = { -1, -1, -1, -1 };

        static bool NavStep(int slot, bool active)
        {
            int f = Time.frameCount;
            if (_navFrame[slot] == f) return _navFired[slot];
            _navFrame[slot] = f;

            float t = Time.unscaledTime;
            bool fired;
            if (!active)
            {
                _navHeld[slot] = false;
                fired = false;
            }
            else if (!_navHeld[slot])
            {
                _navHeld[slot] = true;
                _navNext[slot] = t + NavRepeatDelay;
                fired = true;
            }
            else if (t >= _navNext[slot])
            {
                _navNext[slot] = t + NavRepeatRate;
                fired = true;
            }
            else fired = false;

            _navFired[slot] = fired;
            return fired;
        }

        /// <summary>Is the dpad/left stick pushed toward <paramref name="slot"/> right now?</summary>
        static bool NavHeld(int slot)
        {
            var gp = Gamepad.current;
            if (gp == null) return false;
            var d = gp.dpad;
            Vector2 s = gp.leftStick.ReadValue();
            float ax = Mathf.Abs(s.x), ay = Mathf.Abs(s.y);
            switch (slot)
            {
                case 0: return d.up.isPressed || (s.y > NavThreshold && ay >= ax);
                case 1: return d.down.isPressed || (s.y < -NavThreshold && ay >= ax);
                case 2: return d.left.isPressed || (s.x < -NavThreshold && ax >= ay);
                default: return d.right.isPressed || (s.x > NavThreshold && ax >= ay);
            }
        }

        // ------------------------------------------------------------- movement
        public static Vector2 Move
        {
            get
            {
                PollActiveDevice();
                Vector2 v = Vector2.zero;
                var k = Keyboard.current;
                if (k != null)
                    v = new Vector2(
                        (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f),
                        (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f));
                var gp = Gamepad.current;
                if (gp != null) v += ApplyDeadzone(gp.leftStick.ReadValue());
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>Mouse delta in "legacy axis" units (about 0.1 per pixel); right stick adds ~180 deg/s.</summary>
        public static Vector2 Look
        {
            get
            {
                PollActiveDevice();
                Vector2 v = Vector2.zero;
                var m = Mouse.current;
                if (m != null) v = m.delta.ReadValue() * 0.1f;
                var gp = Gamepad.current;
                if (gp != null) v += ApplyDeadzone(gp.rightStick.ReadValue()) * (PadLookSpeed * Time.deltaTime);
                return v;
            }
        }

        /// <summary>Sprint: Left Shift or left stick click.</summary>
        public static bool Sprint
        {
            get
            {
                var k = Keyboard.current;
                if (k != null && k.leftShiftKey.isPressed) return true;
                var gp = Gamepad.current;
                return gp != null && gp.leftStickButton.isPressed;
            }
        }

        /// <summary>Dodge roll: Left Ctrl, right mouse button or pad East (B).</summary>
        public static bool DodgePressed
        {
            get
            {
                var k = Keyboard.current; var m = Mouse.current; var gp = Gamepad.current;
                return (k != null && k.leftCtrlKey.wasPressedThisFrame)
                    || (m != null && m.rightButton.wasPressedThisFrame)
                    || (gp != null && gp.buttonEast.wasPressedThisFrame);
            }
        }

        /// <summary>Jump: Space or pad South (A).</summary>
        public static bool JumpPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.spaceKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            }
        }

        /// <summary>Interact: E or pad North (Y).</summary>
        public static bool InteractPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.eKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
            }
        }

        /// <summary>Escape only (cursor lock / close panels). Pad B is <see cref="MenuBackPressed"/>.</summary>
        public static bool EscapePressed { get { var k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame; } }

        public static bool RestartPressed { get { var k = Keyboard.current; return k != null && k.rKey.wasPressedThisFrame; } }

        /// <summary>Attack: left mouse button or pad West (X).</summary>
        public static bool AttackPressed
        {
            get
            {
                var m = Mouse.current; var gp = Gamepad.current;
                return (m != null && m.leftButton.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame);
            }
        }

        public static bool GateLeftPressed { get { var k = Keyboard.current; return (k != null && k.leftArrowKey.wasPressedThisFrame) || ScrollStep > 0; } }
        public static bool GateRightPressed { get { var k = Keyboard.current; return (k != null && k.rightArrowKey.wasPressedThisFrame) || ScrollStep < 0; } }

        /// <summary>Buy / confirm at a vendor: F or pad South (A).</summary>
        public static bool BuyPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.fKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            }
        }

        /// <summary>Return to town: T or pad Select/Back.</summary>
        public static bool ReturnPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.tKey.wasPressedThisFrame) || (gp != null && gp.selectButton.wasPressedThisFrame);
            }
        }

        public static bool RightClickPressed { get { var m = Mouse.current; return m != null && m.rightButton.wasPressedThisFrame; } }
        public static Vector2 PointerPosition { get { var m = Mouse.current; return m == null ? Vector2.zero : m.position.ReadValue(); } }
        /// <summary>+1 for a scroll notch up, -1 down, 0 otherwise.</summary>
        public static int ScrollStep { get { var m = Mouse.current; if (m == null) return 0; float y = m.scroll.ReadValue().y; return y > 0.1f ? 1 : (y < -0.1f ? -1 : 0); } }

        /// <summary>Lock-on: Q, middle mouse button or right stick click.</summary>
        public static bool LockOnPressed
        {
            get
            {
                var k = Keyboard.current; var m = Mouse.current; var gp = Gamepad.current;
                return (k != null && k.qKey.wasPressedThisFrame)
                    || (m != null && m.middleButton.wasPressedThisFrame)
                    || (gp != null && gp.rightStickButton.wasPressedThisFrame);
            }
        }

        /// <summary>Cycle lock-on target: Tab or dpad right.</summary>
        public static bool SwitchTargetPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.tabKey.wasPressedThisFrame) || (gp != null && gp.dpad.right.wasPressedThisFrame);
            }
        }

        /// <summary>Inventory: I or pad Start.</summary>
        public static bool InventoryPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.iKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame);
            }
        }

        public static bool MenuUpPressed
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame);
                bool pad = NavStep(0, NavHeld(0));
                return kb || pad;
            }
        }
        public static bool MenuDownPressed
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame);
                bool pad = NavStep(1, NavHeld(1));
                return kb || pad;
            }
        }
        public static bool MenuLeftPressed
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame);
                bool pad = NavStep(2, NavHeld(2));
                return kb || pad;
            }
        }
        public static bool MenuRightPressed
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame);
                bool pad = NavStep(3, NavHeld(3));
                return kb || pad;
            }
        }

        /// <summary>Confirm in menus: Enter or pad South (A).</summary>
        public static bool ConfirmPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            }
        }

        /// <summary>Discard in menus: X key or pad West (X).</summary>
        public static bool DiscardPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.xKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame);
            }
        }

        /// <summary>Menu "click": left mouse button or pad South (A).</summary>
        public static bool MenuClickPressed
        {
            get
            {
                var m = Mouse.current; var gp = Gamepad.current;
                return (m != null && m.leftButton.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            }
        }

        /// <summary>Menu "back": Escape or pad East (B). Escape also drives <see cref="EscapePressed"/>.</summary>
        public static bool MenuBackPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            }
        }

        /// <summary>Companion switch: G or dpad down.</summary>
        public static bool SwitchPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.gKey.wasPressedThisFrame) || (gp != null && gp.dpad.down.wasPressedThisFrame);
            }
        }

        /// <summary>Drink a potion: H or dpad left.</summary>
        public static bool PotionPressed
        {
            get
            {
                var k = Keyboard.current; var gp = Gamepad.current;
                return (k != null && k.hKey.wasPressedThisFrame) || (gp != null && gp.dpad.left.wasPressedThisFrame);
            }
        }

        /// <summary>Hotbar skills 0..4 (keys 1-5; pad LB, RB, LT, RT, dpad up).</summary>
        public static bool SkillPressed(int index)
        {
            var k = Keyboard.current;
            if (k != null)
            {
                switch (index)
                {
                    case 0: if (k.digit1Key.wasPressedThisFrame) return true; break;
                    case 1: if (k.digit2Key.wasPressedThisFrame) return true; break;
                    case 2: if (k.digit3Key.wasPressedThisFrame) return true; break;
                    case 3: if (k.digit4Key.wasPressedThisFrame) return true; break;
                    case 4: if (k.digit5Key.wasPressedThisFrame) return true; break;
                    default: return false;
                }
            }
            var gp = Gamepad.current;
            if (gp == null) return false;
            switch (index)
            {
                case 0: return gp.leftShoulder.wasPressedThisFrame;
                case 1: return gp.rightShoulder.wasPressedThisFrame;
                case 2: return gp.leftTrigger.wasPressedThisFrame;
                case 3: return gp.rightTrigger.wasPressedThisFrame;
                case 4: return gp.dpad.up.wasPressedThisFrame;
                default: return false;
            }
        }
#else
        // Legacy Input Manager: keyboard and mouse only. Pad-only features return false/zero,
        // because the legacy axes cannot be relied on for gamepads here.
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
        /// <summary>Dodge roll: Left Ctrl or right mouse button.</summary>
        public static bool DodgePressed { get { return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(1); } }
        public static bool JumpPressed { get { return Input.GetKeyDown(KeyCode.Space); } }
        public static bool InteractPressed { get { return Input.GetKeyDown(KeyCode.E); } }
        public static bool EscapePressed { get { return Input.GetKeyDown(KeyCode.Escape); } }
        public static bool RestartPressed { get { return Input.GetKeyDown(KeyCode.R); } }
        public static bool AttackPressed { get { return Input.GetMouseButtonDown(0); } }
        public static bool GateLeftPressed { get { return Input.GetKeyDown(KeyCode.LeftArrow) || ScrollStep > 0; } }
        public static bool GateRightPressed { get { return Input.GetKeyDown(KeyCode.RightArrow) || ScrollStep < 0; } }
        public static bool BuyPressed { get { return Input.GetKeyDown(KeyCode.F); } }
        public static bool ReturnPressed { get { return Input.GetKeyDown(KeyCode.T); } }
        public static bool RightClickPressed { get { return Input.GetMouseButtonDown(1); } }
        public static Vector2 PointerPosition { get { return Input.mousePosition; } }
        public static int ScrollStep { get { float y = Input.mouseScrollDelta.y; return y > 0.1f ? 1 : (y < -0.1f ? -1 : 0); } }

        public static bool LockOnPressed { get { return Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(2); } }
        public static bool SwitchTargetPressed { get { return Input.GetKeyDown(KeyCode.Tab); } }
        public static bool InventoryPressed { get { return Input.GetKeyDown(KeyCode.I); } }
        public static bool MenuUpPressed { get { return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W); } }
        public static bool MenuDownPressed { get { return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S); } }
        public static bool MenuLeftPressed { get { return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A); } }
        public static bool MenuRightPressed { get { return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D); } }
        public static bool ConfirmPressed { get { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); } }
        public static bool DiscardPressed { get { return Input.GetKeyDown(KeyCode.X); } }

        /// <summary>Menu "click": left mouse button (no pad in the legacy branch).</summary>
        public static bool MenuClickPressed { get { return Input.GetMouseButtonDown(0); } }
        /// <summary>Menu "back": Escape (no pad in the legacy branch).</summary>
        public static bool MenuBackPressed { get { return Input.GetKeyDown(KeyCode.Escape); } }
        /// <summary>Companion switch: G.</summary>
        public static bool SwitchPressed { get { return Input.GetKeyDown(KeyCode.G); } }
        /// <summary>Drink a potion: H.</summary>
        public static bool PotionPressed { get { return Input.GetKeyDown(KeyCode.H); } }
        /// <summary>Always false here: the legacy branch has no reliable gamepad detection.</summary>
        public static bool UsingGamepad { get { return false; } }

        /// <summary>Hotbar skills 0..4 (keys 1-5).</summary>
        public static bool SkillPressed(int index)
        {
            return index >= 0 && index < 5 && Input.GetKeyDown(KeyCode.Alpha1 + index);
        }
#endif
    }
}
