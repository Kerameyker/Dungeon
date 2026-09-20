// Hand-written UnityEngine stub (Unity 6 signatures). Purpose: compile-check only.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ---------- attributes ----------
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponent : Attribute { public RequireComponent(Type t) { } public RequireComponent(Type a, Type b) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenu : Attribute { public AddComponentMenu(string s) { } }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    public enum Space { World, Self }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum CursorLockMode { None, Locked, Confined }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum FindObjectsInactive { Exclude, Include }
    public enum TextureFormat { Alpha8 = 1, RGB24 = 3, RGBA32 = 4, ARGB32 = 5 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum SpriteMeshType { FullRect, Tight }
    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum LightType { Spot, Directional, Point, Area, Rectangle = 3, Disc = 4 }
    public enum LightShadows { None, Hard, Soft }
    public enum ShadowQuality { Disable, HardOnly, All }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public enum AudioVelocityUpdateMode { Auto, Fixed, Dynamic }
    public enum ForceMode { Force, Impulse, VelocityChange = 2, Acceleration = 5 }
    public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }
    public enum SceneLoadModeDummy { }
    public enum LineTextureMode { Stretch, Tile, DistributeAsPerSegment, RepeatPerSegment }
    public enum LineAlignment { View, TransformZ, Local = TransformZ }
    public enum RigidbodyConstraints { None = 0, FreezeAll = 126 }
    public enum HideFlags { None = 0 }
    public enum LogType { Error, Assert, Warning, Log, Exception }

    // ---------- math ----------
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = PI * 2F / 360F;
        public const float Rad2Deg = 1F / Deg2Rad;
        public const float Epsilon = 1.401298E-45f;
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Tan(float f) { return (float)Math.Tan(f); }
        public static float Asin(float f) { return (float)Math.Asin(f); }
        public static float Acos(float f) { return (float)Math.Acos(f); }
        public static float Atan(float f) { return (float)Math.Atan(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int v) { return Math.Abs(v); }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Min(params float[] values) { return 0; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static int Min(params int[] values) { return 0; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Max(params float[] values) { return 0; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static int Max(params int[] values) { return 0; }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Exp(float p) { return (float)Math.Exp(p); }
        public static float Log(float f, float p) { return (float)Math.Log(f, p); }
        public static float Log(float f) { return (float)Math.Log(f); }
        public static float Log10(float f) { return (float)Math.Log10(f); }
        public static float Ceil(float f) { return (float)Math.Ceiling(f); }
        public static float Floor(float f) { return (float)Math.Floor(f); }
        public static float Round(float f) { return (float)Math.Round(f); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static float Sign(float f) { return f >= 0F ? 1F : -1F; }
        public static float Clamp(float value, float min, float max) { return value < min ? min : (value > max ? max : value); }
        public static int Clamp(int value, int min, int max) { return value < min ? min : (value > max ? max : value); }
        public static float Clamp01(float value) { return value < 0F ? 0F : (value > 1F ? 1F : value); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
        public static float LerpAngle(float a, float b, float t) { return a; }
        public static float MoveTowards(float current, float target, float maxDelta) { return current; }
        public static float MoveTowardsAngle(float current, float target, float maxDelta) { return current; }
        public static float SmoothStep(float from, float to, float t) { return from; }
        public static float Gamma(float value, float absmax, float gamma) { return value; }
        public static bool Approximately(float a, float b) { return Math.Abs(b - a) < 1e-6f; }
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = 0.02f) { return current; }
        public static float Repeat(float t, float length) { return t - (float)Math.Floor(t / length) * length; }
        public static float PingPong(float t, float length) { return t; }
        public static float InverseLerp(float a, float b, float value) { return 0; }
        public static float DeltaAngle(float current, float target) { return 0; }
        public static float PerlinNoise(float x, float y) { return 0; }
        public static int NextPowerOfTwo(int v) { return v; }
        public static bool IsPowerOfTwo(int v) { return true; }
    }

    public struct Vector2 : IEquatable<Vector2>
    {
        public float x; public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float this[int index] { get { return index == 0 ? x : y; } set { if (index == 0) x = value; else y = value; } }
        public void Set(float newX, float newY) { x = newX; y = newY; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a; }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) { return a; }
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta) { return current; }
        public static Vector2 Scale(Vector2 a, Vector2 b) { return a; }
        public void Scale(Vector2 scale) { }
        public void Normalize() { }
        public Vector2 normalized { get { return this; } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static float Angle(Vector2 from, Vector2 to) { return 0; }
        public static float SignedAngle(Vector2 from, Vector2 to) { return 0; }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static Vector2 ClampMagnitude(Vector2 v, float maxLength) { return v; }
        public static Vector2 Min(Vector2 a, Vector2 b) { return a; }
        public static Vector2 Max(Vector2 a, Vector2 b) { return a; }
        public static Vector2 Perpendicular(Vector2 d) { return d; }
        public static Vector2 Reflect(Vector2 d, Vector2 n) { return d; }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static bool operator ==(Vector2 lhs, Vector2 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y; }
        public static bool operator !=(Vector2 lhs, Vector2 rhs) { return !(lhs == rhs); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public override bool Equals(object o) { return o is Vector2 v && this == v; }
        public bool Equals(Vector2 o) { return this == o; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } }
        public static Vector2 down { get { return new Vector2(0, -1); } }
        public static Vector2 left { get { return new Vector2(-1, 0); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x; public float y; public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public float this[int index] { get { return index == 0 ? x : (index == 1 ? y : z); } set { if (index == 0) x = value; else if (index == 1) y = value; else z = value; } }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) { return a; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) { return a; }
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) { return current; }
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = 0.02f) { return current; }
        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public void Scale(Vector3 scale) { }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return a; }
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) { return inDirection; }
        public static Vector3 Normalize(Vector3 value) { return value; }
        public void Normalize() { }
        public Vector3 normalized { get { return this; } }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Project(Vector3 v, Vector3 onNormal) { return v; }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal) { return v; }
        public static float Angle(Vector3 from, Vector3 to) { return 0; }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) { return 0; }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public static Vector3 ClampMagnitude(Vector3 v, float maxLength) { return v; }
        public static float Magnitude(Vector3 v) { return v.magnitude; }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public static float SqrMagnitude(Vector3 v) { return v.sqrMagnitude; }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public static Vector3 Min(Vector3 a, Vector3 b) { return a; }
        public static Vector3 Max(Vector3 a, Vector3 b) { return a; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static bool operator ==(Vector3 lhs, Vector3 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z; }
        public static bool operator !=(Vector3 lhs, Vector3 rhs) { return !(lhs == rhs); }
        public override bool Equals(object o) { return o is Vector3 v && this == v; }
        public bool Equals(Vector3 o) { return this == o; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode(); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; w = 0; }
        public Vector4(float x, float y) { this.x = x; this.y = y; z = 0; w = 0; }
        public static implicit operator Vector4(Vector3 v) { return new Vector4(v.x, v.y, v.z, 0); }
        public static implicit operator Vector3(Vector4 v) { return new Vector3(v.x, v.y, v.z); }
        public static implicit operator Vector4(Vector2 v) { return new Vector4(v.x, v.y, 0, 0); }
        public static implicit operator Vector2(Vector4 v) { return new Vector2(v.x, v.y); }
        public static Vector4 zero { get { return new Vector4(0, 0, 0, 0); } }
    }

    public struct Quaternion : IEquatable<Quaternion>
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        public Vector3 eulerAngles { get { return default(Vector3); } set { } }
        public Quaternion normalized { get { return this; } }
        public static Quaternion Euler(float x, float y, float z) { return identity; }
        public static Quaternion Euler(Vector3 euler) { return identity; }
        public static Quaternion AngleAxis(float angle, Vector3 axis) { return identity; }
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards) { return identity; }
        public static Quaternion LookRotation(Vector3 forward) { return identity; }
        public static Quaternion FromToRotation(Vector3 from, Vector3 to) { return identity; }
        public static Quaternion Inverse(Quaternion rotation) { return rotation; }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return a; }
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t) { return a; }
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) { return a; }
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta) { return from; }
        public static float Angle(Quaternion a, Quaternion b) { return 0; }
        public static float Dot(Quaternion a, Quaternion b) { return 0; }
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) { return lhs; }
        public static Vector3 operator *(Quaternion rotation, Vector3 point) { return point; }
        public static bool operator ==(Quaternion lhs, Quaternion rhs) { return true; }
        public static bool operator !=(Quaternion lhs, Quaternion rhs) { return false; }
        public override bool Equals(object o) { return true; }
        public bool Equals(Quaternion o) { return true; }
        public override int GetHashCode() { return 0; }
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) { return default(Matrix4x4); }
        public static Matrix4x4 identity { get { return default(Matrix4x4); } }
        public Vector3 MultiplyPoint(Vector3 point) { return point; }
        public Vector3 MultiplyPoint3x4(Vector3 point) { return point; }
        public Vector3 MultiplyVector(Vector3 vector) { return vector; }
        public Matrix4x4 inverse { get { return this; } }
        public static Matrix4x4 operator *(Matrix4x4 lhs, Matrix4x4 rhs) { return lhs; }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float x { get; set; }
        public float y { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public Vector2 position { get { return new Vector2(x, y); } set { } }
        public Vector2 size { get { return new Vector2(width, height); } set { } }
        public Vector2 center { get { return new Vector2(x + width / 2, y + height / 2); } set { } }
        public float xMin { get { return x; } set { } }
        public float yMin { get { return y; } set { } }
        public float xMax { get { return x + width; } set { } }
        public float yMax { get { return y + height; } set { } }
        public bool Contains(Vector2 point) { return false; }
        public bool Contains(Vector3 point) { return false; }
    }

    public struct Bounds
    {
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get { return size * 0.5f; } set { } }
        public Vector3 min { get { return center - extents; } set { } }
        public Vector3 max { get { return center + extents; } set { } }
        public void Encapsulate(Vector3 point) { }
    }

    public struct Ray
    {
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 origin { get; set; }
        public Vector3 direction { get; set; }
        public Vector3 GetPoint(float distance) { return origin + direction * distance; }
    }

    public struct RaycastHit
    {
        public Vector3 point { get; set; }
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Collider collider { get { return null; } }
        public Transform transform { get { return null; } }
    }

    public struct Color : IEquatable<Color>
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1F; }
        public float this[int index] { get { return index == 0 ? r : (index == 1 ? g : (index == 2 ? b : a)); } set { } }
        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator *(Color a, float b) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }
        public static Color operator *(float b, Color a) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }
        public static Color operator /(Color a, float b) { return new Color(a.r / b, a.g / b, a.b / b, a.a / b); }
        public static bool operator ==(Color lhs, Color rhs) { return lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b && lhs.a == rhs.a; }
        public static bool operator !=(Color lhs, Color rhs) { return !(lhs == rhs); }
        public override bool Equals(object o) { return o is Color c && this == c; }
        public bool Equals(Color o) { return this == o; }
        public override int GetHashCode() { return r.GetHashCode(); }
        public static Color Lerp(Color a, Color b, float t) { return a; }
        public static Color LerpUnclamped(Color a, Color b, float t) { return a; }
        public static Color HSVToRGB(float H, float S, float V) { return default(Color); }
        public static Color HSVToRGB(float H, float S, float V, bool hdr) { return default(Color); }
        public static void RGBToHSV(Color rgbColor, out float H, out float S, out float V) { H = S = V = 0; }
        public static Color red { get { return new Color(1, 0, 0, 1); } }
        public static Color green { get { return new Color(0, 1, 0, 1); } }
        public static Color blue { get { return new Color(0, 0, 1, 1); } }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0, 1); } }
        public static Color yellow { get { return new Color(1, 0.92f, 0.016f, 1); } }
        public static Color cyan { get { return new Color(0, 1, 1, 1); } }
        public static Color magenta { get { return new Color(1, 0, 1, 1); } }
        public static Color gray { get { return new Color(.5f, .5f, .5f, 1); } }
        public static Color grey { get { return new Color(.5f, .5f, .5f, 1); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public float grayscale { get { return 0; } }
        public static implicit operator Vector4(Color c) { return new Vector4(c.r, c.g, c.b, c.a); }
        public static implicit operator Color(Vector4 v) { return new Color(v.x, v.y, v.z, v.w); }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) { return new Color32(0, 0, 0, 0); }
        public static implicit operator Color(Color32 c) { return default(Color); }
    }

    public static class ColorUtility
    {
        public static string ToHtmlStringRGB(Color color) { return ""; }
        public static string ToHtmlStringRGBA(Color color) { return ""; }
        public static bool TryParseHtmlString(string htmlString, out Color color) { color = default(Color); return false; }
    }
    public static class JsonUtility
    {
        public static string ToJson(object obj) { return ""; }
        public static string ToJson(object obj, bool prettyPrint) { return ""; }
        public static T FromJson<T>(string json) { return default(T); }
        public static object FromJson(string json, Type type) { return null; }
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }
    public struct Keyframe { public Keyframe(float time, float value) { } }

    public class AnimationCurve
    {
        public AnimationCurve() { }
        public AnimationCurve(params Keyframe[] keys) { }
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd) { return new AnimationCurve(); }
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd) { return new AnimationCurve(); }
        public static AnimationCurve Constant(float timeStart, float timeEnd, float value) { return new AnimationCurve(); }
        public float Evaluate(float time) { return 0; }
    }

    public class Gradient
    {
        public GradientColorKey[] colorKeys { get; set; }
        public GradientAlphaKey[] alphaKeys { get; set; }
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys) { }
        public Color Evaluate(float time) { return default(Color); }
    }
    public struct GradientColorKey { public GradientColorKey(Color col, float time) { color = col; this.time = time; } public Color color; public float time; }
    public struct GradientAlphaKey { public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; } public float alpha; public float time; }

    public struct LayerMask
    {
        public int value { get; set; }
        public static implicit operator int(LayerMask mask) { return mask.value; }
        public static implicit operator LayerMask(int intVal) { LayerMask l = default(LayerMask); l.value = intVal; return l; }
        public static int NameToLayer(string layerName) { return 0; }
        public static int GetMask(params string[] layerNames) { return 0; }
    }

    // ---------- object model ----------
    public class Coroutine : YieldInstruction { }
    public class YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float time) { } }
    public sealed class WaitForEndOfFrame : YieldInstruction { }
    public sealed class WaitForFixedUpdate : YieldInstruction { }
    public class CustomYieldInstruction : IEnumerator
    {
        public virtual bool keepWaiting { get { return false; } }
        public object Current { get { return null; } }
        public bool MoveNext() { return keepWaiting; }
        public void Reset() { }
    }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float time) { } }
    public sealed class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> predicate) { } }
    public sealed class WaitWhile : CustomYieldInstruction { public WaitWhile(Func<bool> predicate) { } }

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() { return 0; }
        public static void Destroy(Object obj, float t) { }
        public static void Destroy(Object obj) { }
        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static T FindAnyObjectByType<T>() where T : Object { return null; }
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead")] public static T FindObjectOfType<T>() where T : Object { return null; }
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead")] public static T[] FindObjectsOfType<T>() where T : Object { return null; }
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) { return null; }
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent) { return null; }
        public static Object Instantiate(Object original) { return null; }
        public static Object Instantiate(Object original, Transform parent) { return null; }
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace) { return null; }
        public static T Instantiate<T>(T original) where T : Object { return null; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object { return null; }
        public static T Instantiate<T>(T original, Transform parent) where T : Object { return null; }
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object { return null; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object { return null; }
        public static implicit operator bool(Object exists) { return !ReferenceEquals(exists, null); }
        public static bool operator ==(Object x, Object y) { return ReferenceEquals(x, y); }
        public static bool operator !=(Object x, Object y) { return !ReferenceEquals(x, y); }
        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return 0; }
        public override string ToString() { return name; }
    }

    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public string tag { get; set; }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponents<T>() { return null; }
        public bool CompareTag(string tag) { return false; }
        public void SendMessage(string methodName) { }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return false; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public bool useGUILayout { get; set; }
        public void Invoke(string methodName, float time) { }
        public void InvokeRepeating(string methodName, float time, float repeatRate) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string methodName) { }
        public bool IsInvoking() { return false; }
        public Coroutine StartCoroutine(string methodName) { return null; }
        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopCoroutine(Coroutine routine) { }
        public void StopCoroutine(string methodName) { }
        public void StopAllCoroutines() { }
        public static void print(object message) { }
    }

    public class ScriptableObject : Object
    {
        public static ScriptableObject CreateInstance(Type type) { return null; }
        public static T CreateInstance<T>() where T : ScriptableObject { return null; }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Vector3 forward { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get { return default(Vector3); } }
        public Transform parent { get; set; }
        public int childCount { get { return 0; } }
        public Transform root { get { return null; } }
        public Matrix4x4 localToWorldMatrix { get { return default(Matrix4x4); } }
        public Matrix4x4 worldToLocalMatrix { get { return default(Matrix4x4); } }
        public void SetParent(Transform p) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void Translate(Vector3 translation, Space relativeTo) { }
        public void Translate(Vector3 translation) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(float xAngle, float yAngle, float zAngle) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo) { }
        public void LookAt(Transform target) { }
        public void LookAt(Vector3 worldPosition) { }
        public void LookAt(Vector3 worldPosition, Vector3 worldUp) { }
        public Vector3 TransformPoint(Vector3 position) { return position; }
        public Vector3 InverseTransformPoint(Vector3 position) { return position; }
        public Vector3 TransformDirection(Vector3 direction) { return direction; }
        public Vector3 InverseTransformDirection(Vector3 direction) { return direction; }
        public void DetachChildren() { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void SetSiblingIndex(int index) { }
        public Transform Find(string n) { return null; }
        public bool IsChildOf(Transform parent) { return false; }
        public Transform GetChild(int index) { return null; }
        public IEnumerator GetEnumerator() { return null; }
    }

    public class RectTransform : Transform
    {
        public Rect rect { get { return default(Rect); } }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector3 anchoredPosition3D { get; set; }
        public void SetSizeWithCurrentAnchors(Axis axis, float size) { }
        public enum Axis { Horizontal, Vertical }
    }

    public static class RectTransformUtility
    {
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) { return false; }
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint) { return false; }
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint) { localPoint = default(Vector2); return false; }
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
        public static GameObject Find(string name) { return null; }
        public static GameObject FindWithTag(string tag) { return null; }
        public Transform transform { get { return null; } }
        public int layer { get; set; }
        public bool activeSelf { get { return false; } }
        public bool activeInHierarchy { get { return false; } }
        public bool isStatic { get; set; }
        public string tag { get; set; }
        public void SetActive(bool value) { }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T GetComponentInParent<T>() { return default(T); }
        public T AddComponent<T>() where T : Component { return null; }
        public Component AddComponent(Type componentType) { return null; }
        public bool CompareTag(string tag) { return false; }
    }

    // ---------- statics ----------
    public static class Time
    {
        public static float time { get { return 0; } }
        public static float deltaTime { get { return 0; } }
        public static float fixedDeltaTime { get; set; }
        public static float unscaledTime { get { return 0; } }
        public static float unscaledDeltaTime { get { return 0; } }
        public static float timeScale { get; set; }
        public static int frameCount { get { return 0; } }
        public static float realtimeSinceStartup { get { return 0; } }
        public static float smoothDeltaTime { get { return 0; } }
    }

    public static class Random
    {
        public static int seed { get; set; }
        public static void InitState(int seed) { }
        public static float value { get { return 0; } }
        public static Vector3 insideUnitSphere { get { return default(Vector3); } }
        public static Vector2 insideUnitCircle { get { return default(Vector2); } }
        public static Vector3 onUnitSphere { get { return default(Vector3); } }
        public static Quaternion rotation { get { return default(Quaternion); } }
        public static Quaternion rotationUniform { get { return default(Quaternion); } }
        public static float Range(float minInclusive, float maxInclusive) { return 0; }
        public static int Range(int minInclusive, int maxExclusive) { return 0; }
        public static Color ColorHSV() { return default(Color); }
    }

    public static class Application
    {
        public static string persistentDataPath { get { return ""; } }
        public static string dataPath { get { return ""; } }
        public static int targetFrameRate { get; set; }
        public static bool isPlaying { get { return true; } }
        public static bool isEditor { get { return false; } }
        public static bool runInBackground { get; set; }
        public static void Quit() { }
        public static void Quit(int exitCode) { }
    }

    public static class Cursor
    {
        public static bool visible { get; set; }
        public static CursorLockMode lockState { get; set; }
    }

    public static class Screen
    {
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
        public static bool fullScreen { get; set; }
        public static float dpi { get { return 0; } }
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static ShadowQuality shadows { get; set; }
        public static float shadowDistance { get; set; }
        public static int antiAliasing { get; set; }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogException(Exception exception) { }
        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color) { }
        public static void LogFormat(string format, params object[] args) { }
        public static void Assert(bool condition) { }
    }

    public static class Physics
    {
        public const int DefaultRaycastLayers = -5;
        public const int AllLayers = -1;
        public const int IgnoreRaycastLayer = 4;
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { hitInfo = default(RaycastHit); return false; }
        public static bool Linecast(Vector3 start, Vector3 end, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return false; }
        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction) { return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Ray ray, float radius, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return false; }
        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { hitInfo = default(RaycastHit); return false; }
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask = AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return null; }
        public static bool CheckSphere(Vector3 position, float radius, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) { return false; }
        public static Vector3 gravity { get; set; }
        public static void IgnoreCollision(Collider collider1, Collider collider2, bool ignore = true) { }
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore = true) { }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
        public static Object GetBuiltinResource(Type type, string path) { return null; }
        public static T Load<T>(string path) where T : Object { return null; }
    }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Material skybox { get; set; }
    }
    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    // ---------- rendering-ish components ----------
    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public static Camera current { get { return null; } }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float orthographicSize { get; set; }
        public bool orthographic { get; set; }
        public float aspect { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public int cullingMask { get; set; }
        public float depth { get; set; }
        public Vector3 WorldToScreenPoint(Vector3 position) { return position; }
        public Vector3 ScreenToWorldPoint(Vector3 position) { return position; }
        public Vector3 WorldToViewportPoint(Vector3 position) { return position; }
        public Ray ScreenPointToRay(Vector3 pos) { return default(Ray); }
    }

    public class AudioListener : Behaviour
    {
        public static float volume { get; set; }
        public static bool pause { get; set; }
    }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public float bounceIntensity { get; set; }
        public int cullingMask { get; set; }
        public float colorTemperature { get; set; }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) { return null; }
        public static int PropertyToID(string name) { return 0; }
        public static void EnableKeyword(string keyword) { }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Shader shader { get; set; }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Vector2 mainTextureScale { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public int renderQueue { get; set; }
        public bool HasProperty(string name) { return false; }
        public bool HasProperty(int nameID) { return false; }
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public Color GetColor(string name) { return default(Color); }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public float GetFloat(string name) { return 0; }
        public void SetInt(string name, int value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetTexture(string name, Texture value) { }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public void SetOverrideTag(string tag, string val) { }
    }

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public float mipMapBias { get; set; }
        public int anisoLevel { get; set; }
    }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain, bool linear) { }
        public static Texture2D whiteTexture { get { return null; } }
        public void SetPixel(int x, int y, Color color) { }
        public Color GetPixel(int x, int y) { return default(Color); }
        public void SetPixels(Color[] colors) { }
        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors) { }
        public void SetPixels32(Color32[] colors) { }
        public Color[] GetPixels() { return null; }
        public Color32[] GetPixels32() { return null; }
        public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false) { }
    }

    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) { return null; }
        public Texture2D texture { get { return null; } }
        public Rect rect { get { return default(Rect); } }
    }

    public sealed class Font : Object { public Material material { get; set; } }
    public struct CombineInstance
    {
        public Mesh mesh { get; set; }
        public int subMeshIndex { get; set; }
        public Matrix4x4 transform { get; set; }
    }
    public enum TextAlignment { Left, Center, Right }
    public class TextMesh : Component
    {
        public string text { get; set; }
        public float offsetZ { get; set; }
        public float characterSize { get; set; }
        public float lineSpacing { get; set; }
        public float tabSize { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool richText { get; set; }
        public Font font { get; set; }
        public TextAlignment alignment { get; set; }
        public TextAnchor anchor { get; set; }
        public Color color { get; set; }
    }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector2[] uv { get; set; }
        public Color[] colors { get; set; }
        public int[] triangles { get; set; }
        public Bounds bounds { get; set; }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void Clear() { }
        public void SetVertices(List<Vector3> inVertices) { }
        public void SetTriangles(List<int> triangles, int submesh) { }
        public UnityEngine.Rendering.IndexFormat indexFormat { get; set; }
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes, bool useMatrices) { }
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes) { }
        public void CombineMeshes(CombineInstance[] combine) { }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] materials { get; set; }
        public Material[] sharedMaterials { get; set; }
        public bool enabled { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public int sortingOrder { get; set; }
        public string sortingLayerName { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
    }
    public class MeshRenderer : Renderer { }
    public class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; }
        public Color color { get; set; }
    }
    public class MeshFilter : Component { public Mesh mesh { get; set; } public Mesh sharedMesh { get; set; } }
    public class TrailRenderer : Renderer
    {
        public float time { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public float widthMultiplier { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public bool emitting { get; set; }
        public bool autodestruct { get; set; }
        public int numCapVertices { get; set; }
        public Gradient colorGradient { get; set; }
        public AnimationCurve widthCurve { get; set; }
        public void Clear() { }
    }
    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public bool useWorldSpace { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
    }
    public class BoxCollider : Collider { public Vector3 center { get; set; } public Vector3 size { get; set; } }
    public class SphereCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } }
    public class CapsuleCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } public float height { get; set; } public int direction { get; set; } }
    public class MeshCollider : Collider { public Mesh sharedMesh { get; set; } public bool convex { get; set; } }
    public class Rigidbody : Component
    {
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
        public Vector3 linearVelocity { get; set; }
        public float mass { get; set; }
        public void AddForce(Vector3 force, ForceMode mode) { }
        public void AddForce(Vector3 force) { }
    }
    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public Vector3 center { get; set; }
        public float stepOffset { get; set; }
        public float slopeLimit { get; set; }
        public float skinWidth { get; set; }
        public float minMoveDistance { get; set; }
        public bool isGrounded { get { return false; } }
        public Vector3 velocity { get { return default(Vector3); } }
        public CollisionFlags collisionFlags { get { return default(CollisionFlags); } }
        public CollisionFlags Move(Vector3 motion) { return default(CollisionFlags); }
        public bool SimpleMove(Vector3 speed) { return false; }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public float scaleFactor { get; set; }
        public Camera worldCamera { get; set; }
        public bool overrideSorting { get; set; }
        public float planeDistance { get; set; }
    }
    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; }
        public bool interactable { get; set; }
        public bool blocksRaycasts { get; set; }
        public bool ignoreParentGroups { get; set; }
    }

    // ---------- audio ----------
    public class AudioClip : Object
    {
        public delegate void PCMReaderCallback(float[] data);
        public delegate void PCMSetPositionCallback(int position);
        public float length { get { return 0; } }
        public int samples { get { return 0; } }
        public int channels { get { return 0; } }
        public int frequency { get { return 0; } }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback) { return null; }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
        public bool GetData(float[] data, int offsetSamples) { return true; }
    }
    public class AudioSource : Behaviour
    {
        public float volume { get; set; }
        public float pitch { get; set; }
        public float time { get; set; }
        public AudioClip clip { get; set; }
        public bool isPlaying { get { return false; } }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public bool mute { get; set; }
        public float spatialBlend { get; set; }
        public float minDistance { get; set; }
        public float maxDistance { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public bool bypassEffects { get; set; }
        public bool ignoreListenerPause { get; set; }
        public void Play() { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) { }
    }

    // ---------- particles ----------
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemShapeType { Sphere, Cone = 4, Box = 5, Circle = 10, Edge = 12 }
    public enum ParticleSystemScalingMode { Hierarchy, Local, Shape }
    public enum ParticleSystemCurveMode { Constant, Curve, TwoCurves, TwoConstants }
    public enum ParticleSystemGradientMode { Color, Gradient, TwoColors, TwoGradients, RandomColor }
    public enum ParticleSystemRenderMode { Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh, None }

    public class ParticleSystemRenderer : Renderer
    {
        public ParticleSystemRenderMode renderMode { get; set; }
        public float lengthScale { get; set; }
        public float velocityScale { get; set; }
        public float maxParticleSize { get; set; }
    }

    public sealed class ParticleSystem : Component
    {
        public struct MinMaxCurve
        {
            public MinMaxCurve(float constant) { this = default(MinMaxCurve); }
            public MinMaxCurve(float min, float max) { this = default(MinMaxCurve); }
            public MinMaxCurve(float multiplier, AnimationCurve curve) { this = default(MinMaxCurve); }
            public MinMaxCurve(float multiplier, AnimationCurve min, AnimationCurve max) { this = default(MinMaxCurve); }
            public float constant { get; set; }
            public float constantMin { get; set; }
            public float constantMax { get; set; }
            public ParticleSystemCurveMode mode { get; set; }
            public static implicit operator MinMaxCurve(float constant) { return new MinMaxCurve(constant); }
        }
        public struct MinMaxGradient
        {
            public MinMaxGradient(Color color) { }
            public MinMaxGradient(Gradient gradient) { }
            public MinMaxGradient(Color min, Color max) { }
            public static implicit operator MinMaxGradient(Color color) { return new MinMaxGradient(color); }
            public static implicit operator MinMaxGradient(Gradient gradient) { return new MinMaxGradient(gradient); }
        }
        public struct Burst
        {
            public Burst(float _time, short _count) { }
            public Burst(float _time, MinMaxCurve _count) { }
        }
        public struct MainModule
        {
            public bool loop { get; set; }
            public bool playOnAwake { get; set; }
            public float duration { get; set; }
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxCurve startRotation { get; set; }
            public MinMaxGradient startColor { get; set; }
            public MinMaxCurve gravityModifier { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
            public int maxParticles { get; set; }
            public ParticleSystemScalingMode scalingMode { get; set; }
        }
        public struct EmissionModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
            public MinMaxCurve rateOverDistance { get; set; }
            public void SetBursts(Burst[] bursts) { }
        }
        public struct ShapeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float angle { get; set; }
            public float radius { get; set; }
            public Vector3 scale { get; set; }
            public Vector3 position { get; set; }
            public Vector3 rotation { get; set; }
        }
        public struct ColorOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxGradient color { get; set; }
        }
        public struct SizeOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve size { get; set; }
        }
        public struct VelocityOverLifetimeModule { public bool enabled { get; set; } public MinMaxCurve y { get; set; } }
        public MainModule main { get { return default(MainModule); } }
        public EmissionModule emission { get { return default(EmissionModule); } }
        public ShapeModule shape { get { return default(ShapeModule); } }
        public ColorOverLifetimeModule colorOverLifetime { get { return default(ColorOverLifetimeModule); } }
        public SizeOverLifetimeModule sizeOverLifetime { get { return default(SizeOverLifetimeModule); } }
        public VelocityOverLifetimeModule velocityOverLifetime { get { return default(VelocityOverLifetimeModule); } }
        public bool isPlaying { get { return false; } }
        public bool isStopped { get { return false; } }
        public bool isEmitting { get { return false; } }
        public int particleCount { get { return 0; } }
        public void Play() { }
        public void Play(bool withChildren) { }
        public void Stop() { }
        public void Stop(bool withChildren, ParticleSystemStopBehavior stopBehavior) { }
        public void Stop(bool withChildren) { }
        public void Clear() { }
        public void Emit(int count) { }
        public void Simulate(float t, bool withChildren, bool restart) { }
        public void Simulate(float t, bool withChildren) { }
        public void Simulate(float t) { }
    }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
    public class UnityEvent
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void Invoke() { }
    }
    public class UnityEvent<T0> { public void AddListener(UnityAction<T0> call) { } public void Invoke(T0 a) { } }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }
    public struct Scene { public string name { get { return ""; } } public int buildIndex { get { return 0; } } }
    public class SceneManager
    {
        public static event UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaded { add { } remove { } }
        public static Scene GetActiveScene() { return default(Scene); }
        public static void LoadScene(int sceneBuildIndex) { }
        public static void LoadScene(string sceneName) { }
    }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox, Trilight, Flat = 3, Custom = 4 }
    public enum IndexFormat { UInt16, UInt32 }
    public static class GraphicsSettings
    {
        public static RenderPipelineAsset currentRenderPipeline { get { return null; } }
        public static RenderPipelineAsset defaultRenderPipeline { get { return null; } set { } }
    }
    public abstract class RenderPipelineAsset : ScriptableObject { }

    public abstract class VolumeComponent : ScriptableObject { public bool active { get; set; } }
    public class VolumeParameter { public bool overrideState { get; set; } }
    public class VolumeParameter<T> : VolumeParameter
    {
        public T value { get; set; }
        public void Override(T x) { }
    }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float value, bool overrideState = false) { } }
    public class ClampedFloatParameter : VolumeParameter<float> { public ClampedFloatParameter(float value, float min, float max, bool overrideState = false) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color value, bool hdr = false, bool showAlpha = true, bool showEyeDropper = true, bool overrideState = false) { } }
    public sealed class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent { return null; }
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
        public bool Has<T>() where T : VolumeComponent { return false; }
    }
    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get; set; }
        public float priority { get; set; }
        public float weight { get; set; }
        public VolumeProfile sharedProfile { get; set; }
        public VolumeProfile profile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum TonemappingMode { None, Neutral, ACES }
    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
    }
    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) { return null; }
    }
    public sealed class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = null; public MinFloatParameter intensity = null; public ClampedFloatParameter scatter = null;
    }
    public sealed class Vignette : VolumeComponent
    {
        public ClampedFloatParameter intensity = null; public ClampedFloatParameter smoothness = null;
    }
    public sealed class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = null; public ClampedFloatParameter contrast = null; public ClampedFloatParameter saturation = null;
    }
    public sealed class Tonemapping : VolumeComponent { public TonemappingParameter mode = null; }
    public sealed class TonemappingParameter : VolumeParameter<TonemappingMode> { public TonemappingParameter(TonemappingMode value, bool overrideState = false) { } }
    public sealed class MinFloatParameter : VolumeParameter<float> { public MinFloatParameter(float value, float min, bool overrideState = false) { } }
}
