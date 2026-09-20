using Hollow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow
{
    /// <summary>One row of the generic menu panel (shop, blacksmith, quest board, ending choice).</summary>
    public sealed class MenuRow
    {
        public string Text;
        public Sprite Icon;
        public Color Border = Color.clear;
        public bool Dim;
    }

    /// <summary>Shared HUD palette: dark glass, thin cyan accents.</summary>
    public static class HudStyle
    {
        public static readonly Color Cyan = new Color(0.42f, 0.88f, 1f, 1f);
        public static readonly Color CyanSoft = new Color(0.42f, 0.88f, 1f, 0.45f);
        public static readonly Color CyanFaint = new Color(0.42f, 0.88f, 1f, 0.18f);
        public static readonly Color Glass = new Color(0.035f, 0.065f, 0.105f, 0.80f);
        public static readonly Color GlassDark = new Color(0.020f, 0.038f, 0.062f, 0.92f);
        public static readonly Color GlassDeep = new Color(0.014f, 0.028f, 0.048f, 0.97f);
        public static readonly Color Ink = new Color(0.90f, 0.95f, 1f, 1f);
        public static readonly Color Muted = new Color(0.60f, 0.71f, 0.82f, 1f);
        public static readonly Color Faded = new Color(0.42f, 0.50f, 0.60f, 1f);
        public static readonly Color HpHigh = new Color(0.36f, 0.88f, 0.48f, 1f);
        public static readonly Color HpLow = new Color(0.94f, 0.32f, 0.26f, 1f);
        public static readonly Color BarBack = new Color(0.08f, 0.12f, 0.15f, 0.92f);
        public static readonly Color GhostBar = new Color(1f, 0.84f, 0.40f, 0.55f);
        public static readonly Color Xp = new Color(0.40f, 0.72f, 1f, 1f);
        public static readonly Color GoldColor = new Color(1f, 0.82f, 0.32f, 1f);
        public static readonly Color ShardColor = new Color(1f, 0.55f, 0.30f, 1f);
        public static readonly Color BossRed = new Color(0.95f, 0.26f, 0.22f, 1f);
        public static readonly Color Elite = new Color(1f, 0.72f, 0.25f, 1f);
        public static readonly Color Good = new Color(0.49f, 1f, 0.54f, 1f);
        public static readonly Color Bad = new Color(1f, 0.48f, 0.48f, 1f);

        public const string GoodHex = "#7CFF8A";
        public const string BadHex = "#FF7C7C";
        public const string MutedHex = "#9FB7C8";
        public const string CyanHex = "#6BE0FF";
        public const string GoldHex = "#FFD152";
    }

    /// <summary>Tiny code-only uGUI builder helpers (no prefabs, no imported sprites).</summary>
    public static class Ui
    {
        public static readonly Vector2 TL = new Vector2(0f, 1f);
        public static readonly Vector2 TC = new Vector2(0.5f, 1f);
        public static readonly Vector2 TR = new Vector2(1f, 1f);
        public static readonly Vector2 ML = new Vector2(0f, 0.5f);
        public static readonly Vector2 MC = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 MR = new Vector2(1f, 0.5f);
        public static readonly Vector2 BL = new Vector2(0f, 0f);
        public static readonly Vector2 BC = new Vector2(0.5f, 0f);
        public static readonly Vector2 BR = new Vector2(1f, 0f);

        static Font _font;
        static Sprite _white, _soft, _ring, _arrow, _vignette, _diamond;

        public static Font Face
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite White
        {
            get
            {
                if (_white == null) _white = ProcAssets.WhiteSprite();
                return _white;
            }
        }

        public static Sprite Soft
        {
            get
            {
                if (_soft == null) _soft = FromTexture(ProcAssets.SoftCircle());
                return _soft;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (_ring == null) _ring = FromTexture(ProcAssets.RingTexture());
                return _ring;
            }
        }

        /// <summary>Solid triangle pointing up (+Y): the player marker on the minimap.</summary>
        public static Sprite Arrow
        {
            get
            {
                if (_arrow != null) return _arrow;
                const int n = 32;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                var on = new Color32(255, 255, 255, 255);
                var off = new Color32(255, 255, 255, 0);
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float fx = (x + 0.5f) / n, fy = (y + 0.5f) / n;
                        float half = (1f - fy) * 0.52f;
                        bool inside = fy > 0.10f && Mathf.Abs(fx - 0.5f) <= half;
                        if (inside && fy < 0.34f && Mathf.Abs(fx - 0.5f) < (0.34f - fy) * 1.2f) inside = false;
                        px[y * n + x] = inside ? on : off;
                    }
                }
                t.SetPixels32(px);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Apply();
                _arrow = Sprite.Create(t, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
                return _arrow;
            }
        }

        /// <summary>Transparent in the middle, opaque at the edges: the damage vignette.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int n = 96;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                        float a = Mathf.Clamp01((d - 0.35f) / 0.62f);
                        a *= a;
                        px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                    }
                }
                t.SetPixels32(px);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Apply();
                _vignette = Sprite.Create(t, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
                return _vignette;
            }
        }

        /// <summary>Small filled diamond: map markers.</summary>
        public static Sprite Diamond
        {
            get
            {
                if (_diamond != null) return _diamond;
                const int n = 24;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                var on = new Color32(255, 255, 255, 255);
                var off = new Color32(255, 255, 255, 0);
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float fx = Mathf.Abs((x + 0.5f) / n - 0.5f), fy = Mathf.Abs((y + 0.5f) / n - 0.5f);
                        px[y * n + x] = (fx + fy) <= 0.48f ? on : off;
                    }
                }
                t.SetPixels32(px);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Apply();
                _diamond = Sprite.Create(t, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
                return _diamond;
            }
        }

        static Sprite FromTexture(Texture2D t)
        {
            return Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ builders

        public static RectTransform MakeRect(string name, Transform parent, Vector2 aMin, Vector2 aMax,
                                             Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            return MakeRect(name, parent, anchor, anchor, pivot, pos, size);
        }

        public static Image MakeImg(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                    Vector2 pos, Vector2 size, Color color)
        {
            return MakeImg(name, parent, anchor, pivot, pos, size, color, null);
        }

        public static Image MakeImg(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                    Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = MakeRect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos,
                                    Vector2 size, string text, int fontSize, TextAnchor align, Color color)
        {
            var rt = MakeRect(name, parent, anchor, pivot, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Face;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Switches a text to word wrapping inside its rect.</summary>
        public static Text Wrapped(Text t)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        /// <summary>Four thin images hugging the edges of a rect.</summary>
        public static Image[] Frame(RectTransform target, Color color, float thickness)
        {
            var f = new Image[4];
            f[0] = Edge(target, "FrameT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness), color);
            f[1] = Edge(target, "FrameB", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness), color);
            f[2] = Edge(target, "FrameL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f), color);
            f[3] = Edge(target, "FrameR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f), color);
            return f;
        }

        static Image Edge(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Color color)
        {
            var rt = MakeRect(name, parent, aMin, aMax, pivot, Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static void FrameColor(Image[] frame, Color color)
        {
            if (frame == null) return;
            for (int i = 0; i < frame.Length; i++) if (frame[i] != null) frame[i].color = color;
        }

        /// <summary>Bright accent line along the top edge, covering a fraction of the width.</summary>
        public static Image Accent(RectTransform target, float widthFraction, Color color)
        {
            var rt = MakeRect("Accent", target, new Vector2(0f, 1f), new Vector2(Mathf.Clamp01(widthFraction), 1f),
                              new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 2f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Dark glass panel with a thin border and a cyan accent.</summary>
        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos,
                                  Vector2 size, Color fill, Color edge)
        {
            var img = MakeImg(name, parent, anchor, pivot, pos, size, fill);
            Frame(img.rectTransform, edge, 2f);
            return img;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var cg = go.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            return cg;
        }

        public static bool Hit(RectTransform rt, Vector2 screenPos)
        {
            return rt != null && rt.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
        }
    }

    /// <summary>Horizontal bar with a smoothly lerped fill, a slow "ghost" trail and an optional label.</summary>
    public sealed class UiBar
    {
        public readonly RectTransform Root;
        readonly Image _back, _ghost, _fill;
        readonly Text _label;
        float _target, _shown, _ghostValue;

        public UiBar(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                     Color fill, Color back, int labelSize, Color labelColor)
        {
            _back = Ui.MakeImg(name, parent, anchor, pivot, pos, size, back);
            Root = _back.rectTransform;

            _ghost = Ui.MakeImg("Ghost", Root, Ui.MC, Ui.MC, Vector2.zero, size, HudStyle.GhostBar);
            MakeFilled(_ghost);
            _fill = Ui.MakeImg("Fill", Root, Ui.MC, Ui.MC, Vector2.zero, size, fill);
            MakeFilled(_fill);

            if (labelSize > 0)
            {
                _label = Ui.MakeText("Label", Root, Ui.MC, Ui.MC, Vector2.zero, size, "", labelSize, TextAnchor.MiddleCenter, labelColor);
                _label.fontStyle = FontStyle.Bold;
            }
        }

        static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 0f;
        }

        public float Target { get { return _target; } }

        public void Set(float value01) { _target = Mathf.Clamp01(value01); }

        public void SetInstant(float value01)
        {
            _target = _shown = _ghostValue = Mathf.Clamp01(value01);
            Apply();
        }

        public void Tick(float dt)
        {
            if (Mathf.Abs(_shown - _target) > 0.0005f)
            {
                _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-14f * dt));
                if (Mathf.Abs(_shown - _target) <= 0.0015f) _shown = _target;
            }
            else _shown = _target;

            if (_ghostValue > _shown) _ghostValue = Mathf.MoveTowards(_ghostValue, _shown, dt * 0.5f);
            else _ghostValue = _shown;
            Apply();
        }

        void Apply()
        {
            _fill.fillAmount = _shown;
            _ghost.fillAmount = _ghostValue;
        }

        public void SetLabel(string s)
        {
            if (_label != null && _label.text != s) _label.text = s;
        }

        public void SetFillColor(Color c) { if (_fill.color != c) _fill.color = c; }
        public void SetBackColor(Color c) { if (_back.color != c) _back.color = c; }
        public void SetGhostVisible(bool on) { if (_ghost.enabled != on) _ghost.enabled = on; }
        public void SetActive(bool on) { if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on); }
    }

    /// <summary>One hotbar slot: icon, key label, radial cooldown, lock overlay and mastery pips.</summary>
    public sealed class SkillSlotWidget
    {
        public readonly RectTransform Root;
        readonly Image _bg, _glow, _icon, _cd, _lockShade;
        readonly Image[] _frame;
        readonly Image[] _pips;
        readonly Text _cdText, _lockText, _name;

        Sprite _iconSprite;
        int _pipLevel = -1, _cdTenths = -1, _lockLevel = -1;
        bool _locked, _lockKnown;
        float _glowAlpha = -1f;

        public SkillSlotWidget(Transform parent, Vector2 pos, float size, int keyNumber)
        {
            _bg = Ui.MakeImg("Skill" + keyNumber, parent, Ui.BC, Ui.BC, pos, new Vector2(size, size), HudStyle.Glass);
            Root = _bg.rectTransform;

            _glow = Ui.MakeImg("Glow", Root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(size + 30f, size + 30f),
                               new Color(0.42f, 0.88f, 1f, 0f), Ui.Soft);
            _icon = Ui.MakeImg("Icon", Root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(size - 16f, size - 16f), Color.white);
            _icon.preserveAspect = true;
            _icon.enabled = false;

            _cd = Ui.MakeImg("Cooldown", Root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(size, size), new Color(0.02f, 0.04f, 0.07f, 0.78f));
            _cd.type = Image.Type.Filled;
            _cd.fillMethod = Image.FillMethod.Radial360;
            _cd.fillOrigin = (int)Image.Origin360.Top;
            _cd.fillClockwise = false;
            _cd.fillAmount = 0f;

            _cdText = Ui.MakeText("CdText", Root, Ui.MC, Ui.MC, new Vector2(0f, 1f), new Vector2(size, 30f), "", 22,
                                  TextAnchor.MiddleCenter, Color.white);
            _cdText.fontStyle = FontStyle.Bold;

            _lockShade = Ui.MakeImg("LockShade", Root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(size, size),
                                    new Color(0.01f, 0.02f, 0.035f, 0.80f));
            _lockShade.enabled = false;
            _lockText = Ui.MakeText("Lock", Root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(size, 28f), "", 20,
                                    TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.38f));
            _lockText.enabled = false;

            Ui.MakeImg("KeyChip", Root, Ui.TL, Ui.TL, new Vector2(4f, -4f), new Vector2(22f, 20f),
                       new Color(0.02f, 0.05f, 0.08f, 0.88f));
            Ui.MakeText("Key", Root, Ui.TL, Ui.TL, new Vector2(4f, -4f), new Vector2(22f, 20f),
                        keyNumber.ToString(), 15, TextAnchor.MiddleCenter, HudStyle.Cyan);

            _pips = new Image[SkillMastery.MaxLevel];
            const float pw = 12f, pg = 3f;
            float total = _pips.Length * pw + (_pips.Length - 1) * pg;
            for (int i = 0; i < _pips.Length; i++)
                _pips[i] = Ui.MakeImg("Pip" + i, Root, Ui.BC, Ui.BC,
                                      new Vector2(-total * 0.5f + pw * 0.5f + i * (pw + pg), 5f), new Vector2(pw, 3f), HudStyle.Faded);

            _name = Ui.MakeText("Name", Root, Ui.TC, Ui.BC, new Vector2(0f, 6f), new Vector2(140f, 20f), "", 14,
                                TextAnchor.LowerCenter, HudStyle.Muted);

            _frame = Ui.Frame(Root, HudStyle.CyanFaint, 2f);
        }

        public void SetName(string s) { if (_name.text != s) _name.text = s; }

        public void SetIcon(Sprite s)
        {
            if (_iconSprite == s) return;
            _iconSprite = s;
            _icon.sprite = s;
            _icon.enabled = s != null;
        }

        public void SetLocked(bool locked, int unlockLevel)
        {
            if (_lockKnown && _locked == locked && _lockLevel == unlockLevel) return;
            _lockKnown = true;
            _locked = locked;
            _lockLevel = unlockLevel;
            _lockShade.enabled = locked;
            _lockText.enabled = locked;
            if (locked) _lockText.text = "Lv " + unlockLevel;
            Ui.FrameColor(_frame, locked ? new Color(0.30f, 0.36f, 0.42f, 0.45f) : HudStyle.CyanFaint);
            _icon.color = locked ? new Color(0.45f, 0.50f, 0.58f, 1f) : Color.white;
        }

        public void SetCooldown(float normalized, float remaining)
        {
            _cd.fillAmount = Mathf.Clamp01(normalized);
            int tenths = remaining > 0.05f ? Mathf.CeilToInt(remaining * 10f) : 0;
            if (tenths == _cdTenths) return;
            _cdTenths = tenths;
            if (tenths <= 0) _cdText.text = "";
            else if (tenths >= 100) _cdText.text = Mathf.CeilToInt(tenths * 0.1f).ToString();
            else _cdText.text = (tenths * 0.1f).ToString("0.0");
        }

        public void SetMastery(int level)
        {
            if (_pipLevel == level) return;
            _pipLevel = level;
            for (int i = 0; i < _pips.Length; i++)
                _pips[i].color = i < level ? new Color(1f, 0.82f, 0.35f, 1f) : HudStyle.Faded;
        }

        public void SetGlow(float alpha)
        {
            if (Mathf.Abs(_glowAlpha - alpha) < 0.005f) return;
            _glowAlpha = alpha;
            _glow.color = new Color(0.42f, 0.88f, 1f, alpha);
        }

        public void SetActive(bool on)
        {
            if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on);
        }
    }

    /// <summary>Framed square that shows an item / skill / consumable icon with a rarity-colored border.</summary>
    public sealed class IconCell
    {
        public readonly RectTransform Root;
        readonly Image _bg, _select, _icon;
        readonly Image[] _frame;
        Sprite _iconSprite;
        Color _border;
        bool _selected;

        public IconCell(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                        float padding, Color background)
        {
            _bg = Ui.MakeImg(name, parent, anchor, pivot, pos, size, background);
            Root = _bg.rectTransform;
            _select = Ui.MakeImg("Select", Root, Ui.MC, Ui.MC, Vector2.zero, size + new Vector2(22f, 22f),
                                 new Color(0.42f, 0.88f, 1f, 0f), Ui.Soft);
            _icon = Ui.MakeImg("Icon", Root, Ui.MC, Ui.MC, Vector2.zero, size - new Vector2(padding * 2f, padding * 2f), Color.white);
            _icon.preserveAspect = true;
            _icon.enabled = false;
            _border = new Color(1f, 1f, 1f, 0.16f);
            _frame = Ui.Frame(Root, _border, 2f);
        }

        public void SetIcon(Sprite s)
        {
            if (_iconSprite == s) return;
            _iconSprite = s;
            _icon.sprite = s;
            _icon.enabled = s != null;
        }

        public void SetIconColor(Color c) { if (_icon.color != c) _icon.color = c; }
        public void SetBackground(Color c) { if (_bg.color != c) _bg.color = c; }

        public void SetBorder(Color c)
        {
            if (_border == c) return;
            _border = c;
            Ui.FrameColor(_frame, c);
        }

        public void SetSelected(bool on, float pulse)
        {
            _selected = on;
            _select.color = new Color(0.42f, 0.88f, 1f, on ? 0.22f + 0.20f * pulse : 0f);
        }

        public bool Selected { get { return _selected; } }

        public void SetActive(bool on)
        {
            if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on);
        }

        public bool Contains(Vector2 screenPos) { return Ui.Hit(Root, screenPos); }
    }
}
