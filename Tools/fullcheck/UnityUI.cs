using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class EventSystem : UIBehaviour { public static EventSystem current { get; set; } }
    public class BaseRaycaster : UIBehaviour { }
}

namespace UnityEngine.UI
{
    public interface IMaterialModifier { }
    public abstract class Graphic : EventSystems.UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
        public virtual Material material { get; set; }
        public virtual Material materialForRendering { get { return null; } }
        public Canvas canvas { get { return null; } }
        public virtual Texture mainTexture { get { return null; } }
        public virtual void SetAllDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
    }
    public class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public enum OriginVertical { Bottom, Top }
        public enum Origin90 { BottomLeft, TopLeft, TopRight, BottomRight }
        public enum Origin180 { Bottom, Left, Top, Right }
        public enum Origin360 { Bottom, Right, Top, Left }
        public Sprite sprite { get; set; }
        public Sprite overrideSprite { get; set; }
        public Type type { get; set; }
        public bool preserveAspect { get; set; }
        public bool fillCenter { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
        public bool fillClockwise { get; set; }
        public int fillOrigin { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public virtual void SetNativeSize() { }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; }
    }

    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public virtual string text { get; set; }
        public bool supportRichText { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public int fontSize { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
        public FontStyle fontStyle { get; set; }
        public float preferredWidth { get { return 0; } }
        public float preferredHeight { get { return 0; } }
    }

    public abstract class BaseMeshEffect : EventSystems.UIBehaviour { }
    public class Shadow : BaseMeshEffect
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
        public bool useGraphicAlpha { get; set; }
    }
    public class Outline : Shadow { }

    public class CanvasScaler : EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight = 0, Expand = 1, Shrink = 2 }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float scaleFactor { get; set; }
    }
    public class GraphicRaycaster : EventSystems.BaseRaycaster { }
}

namespace UnityEngine.InputSystem.Controls
{
    public class AxisControl : InputControl<float> { }
    public class ButtonControl : AxisControl
    {
        public bool isPressed { get { return false; } }
        public bool wasPressedThisFrame { get { return false; } }
        public bool wasReleasedThisFrame { get { return false; } }
    }
    public class AnyKeyControl : ButtonControl { }
    public class KeyControl : ButtonControl { }
    public class Vector2Control : InputControl<Vector2> { }
    public class DeltaControl : Vector2Control { }
    public class StickControl : Vector2Control { }
    public class DpadControl : Vector2Control
    {
        public ButtonControl up { get { return null; } }
        public ButtonControl down { get { return null; } }
        public ButtonControl left { get { return null; } }
        public ButtonControl right { get { return null; } }
    }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;
    public abstract class InputControl { }
    public abstract class InputControl<TValue> : InputControl where TValue : struct
    {
        public TValue ReadValue() { return default(TValue); }
    }
    public abstract class InputDevice { }
    public class Keyboard : InputDevice
    {
        public static Keyboard current { get { return null; } }
        public AnyKeyControl anyKey { get { return null; } }
        public KeyControl aKey { get { return null; } }
        public KeyControl dKey { get { return null; } }
        public KeyControl wKey { get { return null; } }
        public KeyControl sKey { get { return null; } }
        public KeyControl eKey { get { return null; } }
        public KeyControl fKey { get { return null; } }
        public KeyControl gKey { get { return null; } }
        public KeyControl hKey { get { return null; } }
        public KeyControl iKey { get { return null; } }
        public KeyControl qKey { get { return null; } }
        public KeyControl rKey { get { return null; } }
        public KeyControl tKey { get { return null; } }
        public KeyControl xKey { get { return null; } }
        public KeyControl leftShiftKey { get { return null; } }
        public KeyControl leftCtrlKey { get { return null; } }
        public KeyControl spaceKey { get { return null; } }
        public KeyControl escapeKey { get { return null; } }
        public KeyControl tabKey { get { return null; } }
        public KeyControl enterKey { get { return null; } }
        public KeyControl numpadEnterKey { get { return null; } }
        public KeyControl upArrowKey { get { return null; } }
        public KeyControl downArrowKey { get { return null; } }
        public KeyControl leftArrowKey { get { return null; } }
        public KeyControl rightArrowKey { get { return null; } }
        public KeyControl digit1Key { get { return null; } }
        public KeyControl digit2Key { get { return null; } }
        public KeyControl digit3Key { get { return null; } }
        public KeyControl digit4Key { get { return null; } }
        public KeyControl digit5Key { get { return null; } }
    }
    public class Pointer : InputDevice { }
    public class Mouse : Pointer
    {
        public static Mouse current { get { return null; } }
        public DeltaControl delta { get { return null; } }
        public Vector2Control scroll { get { return null; } }
        public Vector2Control position { get { return null; } }
        public ButtonControl leftButton { get { return null; } }
        public ButtonControl rightButton { get { return null; } }
        public ButtonControl middleButton { get { return null; } }
    }
    public class Gamepad : InputDevice
    {
        public static Gamepad current { get { return null; } }
        public StickControl leftStick { get { return null; } }
        public StickControl rightStick { get { return null; } }
        public ButtonControl leftTrigger { get { return null; } }
        public ButtonControl rightTrigger { get { return null; } }
        public ButtonControl buttonSouth { get { return null; } }
        public ButtonControl buttonEast { get { return null; } }
        public ButtonControl buttonWest { get { return null; } }
        public ButtonControl buttonNorth { get { return null; } }
        public ButtonControl leftShoulder { get { return null; } }
        public ButtonControl rightShoulder { get { return null; } }
        public ButtonControl leftStickButton { get { return null; } }
        public ButtonControl rightStickButton { get { return null; } }
        public ButtonControl startButton { get { return null; } }
        public ButtonControl selectButton { get { return null; } }
        public DpadControl dpad { get { return null; } }
    }
}

namespace UnityEngine
{
    public enum KeyCode
    {
        None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Keypad0 = 256, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9, KeypadEnter = 271,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        RightShift = 303, LeftShift = 304, RightControl = 305, LeftControl = 306,
        Mouse0 = 323, Mouse1 = 324,
    }
    public static class Input
    {
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKey(string name) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetKeyDown(string name) { return false; }
        public static bool GetKeyUp(KeyCode key) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButtonUp(int button) { return false; }
        public static float GetAxis(string axisName) { return 0; }
        public static float GetAxisRaw(string axisName) { return 0; }
        public static bool GetButton(string buttonName) { return false; }
        public static bool GetButtonDown(string buttonName) { return false; }
        public static Vector3 mousePosition { get { return default(Vector3); } }
        public static Vector2 mouseScrollDelta { get { return default(Vector2); } }
        public static bool anyKey { get { return false; } }
        public static bool anyKeyDown { get { return false; } }
    }
}
