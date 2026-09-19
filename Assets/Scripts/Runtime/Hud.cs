using Hollow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow
{
    /// <summary>Whole HUD built in code: HP/XP, skill bar with cooldowns, floor label, boss bar, toasts, minimap.</summary>
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);
        static readonly Color Panel = new Color(0.05f, 0.08f, 0.12f, 0.72f);

        Font _font;
        Sprite _white;
        Canvas _canvas;
        RectTransform _root;

        // player panel
        Image _hpFill, _xpFill;
        Text _hpText, _levelText;
        // skills
        Image[] _cdOverlay;
        Text[] _lockText;
        // texts
        Text _floorText, _toastText, _promptText, _hintText, _bossName, _statText;
        GameObject _invPanel;
        Text _invLeft, _invRight;
        Image _bossFill, _bossBack;
        Image _flash, _deathOverlay;
        Text _deathText;
        // minimap
        RawImage _mapImage;
        Texture2D _mapTex;
        Color32[] _mapBase;
        bool[,] _explored;
        DungeonLayout _layout;
        float _mapTimer;
        Vector3 _stairsPos;
        bool _hasStairs;

        Enemy _boss;
        float _toastUntil, _promptTime = -10f, _hintUntil, _flashAlpha;
        string _promptString = "";

        public static Hud Create()
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<Hud>();
            hud.Build();
            return hud;
        }

        void Awake() { Instance = this; }

        // ------------------------------------------------------------------ building blocks

        RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var rt = Rect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _white;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                 string text, int fontSize, TextAnchor align, Color color)
        {
            var rt = Rect(name, parent, anchor, pivot, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        void Build()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _white = ProcAssets.WhiteSprite();

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _root = _canvas.GetComponent<RectTransform>();

            Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), TC = new Vector2(0.5f, 1f);
            Vector2 BC = new Vector2(0.5f, 0f), BL = new Vector2(0f, 0f), MC = new Vector2(0.5f, 0.5f);

            // --- player panel (top left) ---
            Img("PanelBg", _root, TL, TL, new Vector2(30f, -30f), new Vector2(440f, 124f), Panel);
            _levelText = Txt("Level", _root, TL, TL, new Vector2(46f, -38f), new Vector2(200f, 30f), "Wanderer   Lv 1", 24, TextAnchor.MiddleLeft, Cyan);
            Img("HpBack", _root, TL, TL, new Vector2(46f, -72f), new Vector2(408f, 22f), new Color(0.15f, 0.2f, 0.18f, 0.9f));
            _hpFill = Img("HpFill", _root, TL, TL, new Vector2(46f, -72f), new Vector2(408f, 22f), new Color(0.35f, 0.9f, 0.45f));
            _hpText = Txt("HpText", _root, TL, TL, new Vector2(46f, -72f), new Vector2(408f, 22f), "100 / 100", 16, TextAnchor.MiddleCenter, Color.white);
            Img("XpBack", _root, TL, TL, new Vector2(46f, -100f), new Vector2(408f, 8f), new Color(0.12f, 0.15f, 0.2f, 0.9f));
            _xpFill = Img("XpFill", _root, TL, TL, new Vector2(46f, -100f), new Vector2(0f, 8f), new Color(0.4f, 0.75f, 1f));
            _statText = Txt("Stats", _root, TL, TL, new Vector2(46f, -112f), new Vector2(408f, 22f), "", 16, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.85f));
            SetFillPivotLeft(_hpFill);
            SetFillPivotLeft(_xpFill);

            // --- floor label ---
            _floorText = Txt("Floor", _root, TC, TC, new Vector2(0f, -28f), new Vector2(600f, 50f), "FLOOR 1", 40, TextAnchor.MiddleCenter, Cyan);

            // --- boss bar ---
            _bossBack = Img("BossBack", _root, TC, TC, new Vector2(0f, -110f), new Vector2(700f, 18f), new Color(0.2f, 0.05f, 0.05f, 0.9f));
            _bossFill = Img("BossFill", _root, TC, new Vector2(0f, 0.5f), new Vector2(-350f, -119f), new Vector2(700f, 18f), new Color(0.95f, 0.2f, 0.2f));
            _bossName = Txt("BossName", _root, TC, TC, new Vector2(0f, -88f), new Vector2(700f, 30f), "", 22, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.55f));
            _bossBack.gameObject.SetActive(false);
            _bossFill.gameObject.SetActive(false);
            _bossName.gameObject.SetActive(false);

            // --- skill bar ---
            int n = SkillCatalog.All.Length;
            float slot = 96f, gap = 14f;
            float total = n * slot + (n - 1) * gap;
            _cdOverlay = new Image[n];
            _lockText = new Text[n];
            for (int i = 0; i < n; i++)
            {
                var def = SkillCatalog.All[i];
                float x = -total * 0.5f + i * (slot + gap) + slot * 0.5f;
                var pos = new Vector2(x, 34f);
                Img("SkillBg" + i, _root, BC, new Vector2(0.5f, 0f), pos, new Vector2(slot, slot), Panel);
                Img("SkillEdge" + i, _root, BC, new Vector2(0.5f, 0f), pos + new Vector2(0f, slot - 4f), new Vector2(slot, 4f), Cyan);
                Txt("SkillName" + i, _root, BC, new Vector2(0.5f, 0.5f), pos + new Vector2(0f, slot * 0.5f + 4f), new Vector2(slot - 8f, slot - 20f),
                    def.Name.Replace(" ", "\n"), 17, TextAnchor.MiddleCenter, Color.white);
                Txt("SkillKey" + i, _root, BC, new Vector2(0f, 0f), pos + new Vector2(-slot * 0.5f + 6f, 4f), new Vector2(30f, 24f),
                    (i + 1).ToString(), 20, TextAnchor.LowerLeft, Cyan);
                var cd = Img("SkillCd" + i, _root, BC, new Vector2(0.5f, 0f), pos, new Vector2(slot, slot), new Color(0f, 0f, 0f, 0.7f));
                cd.type = Image.Type.Filled;
                cd.fillMethod = Image.FillMethod.Radial360;
                cd.fillOrigin = (int)Image.Origin360.Top;
                cd.fillClockwise = false;
                cd.fillAmount = 0f;
                _cdOverlay[i] = cd;
                _lockText[i] = Txt("SkillLock" + i, _root, BC, new Vector2(0.5f, 0.5f), pos + new Vector2(0f, slot * 0.5f), new Vector2(slot, 30f),
                    "Lv " + def.UnlockLevel, 22, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.4f));
            }

            // --- center texts ---
            _toastText = Txt("Toast", _root, MC, MC, new Vector2(0f, 180f), new Vector2(900f, 120f), "", 44, TextAnchor.MiddleCenter, Cyan);
            _promptText = Txt("Prompt", _root, BC, BC, new Vector2(0f, 170f), new Vector2(900f, 40f), "", 28, TextAnchor.MiddleCenter, Color.white);
            _hintText = Txt("Hint", _root, BL, BL, new Vector2(30f, 30f), new Vector2(620f, 170f),
                "WASD  move       Shift  sprint\nSpace  dodge roll       LMB  attack combo\n1-4  sword skills       E  interact\nQ  lock-on (Tab switches)       I  inventory\nEsc  free / capture mouse",
                18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.75f));
            _hintUntil = Time.time + 20f;

            // --- overlays ---
            _flash = Img("Flash", _root, MC, MC, Vector2.zero, new Vector2(4000f, 3000f), new Color(1f, 0.1f, 0.1f, 0f));
            _deathOverlay = Img("Death", _root, MC, MC, Vector2.zero, new Vector2(4000f, 3000f), new Color(0f, 0f, 0f, 0.65f));
            _deathText = Txt("DeathText", _root, MC, MC, Vector2.zero, new Vector2(900f, 200f), "YOU DIED\nPress R to respawn on this floor", 48, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f));
            _deathOverlay.gameObject.SetActive(false);
            _deathText.gameObject.SetActive(false);

            BuildInventory(MC);

            // --- minimap ---
            var frame = Img("MapFrame", _root, TR, TR, new Vector2(-30f, -30f), new Vector2(248f, 248f), Panel);
            var mapRt = Rect("Map", _root, TR, TR, new Vector2(-38f, -38f), new Vector2(232f, 232f));
            _mapImage = mapRt.gameObject.AddComponent<RawImage>();
            _mapImage.raycastTarget = false;
            frame.raycastTarget = false;
        }

        static void SetFillPivotLeft(Image img)
        {
            img.rectTransform.pivot = new Vector2(0f, img.rectTransform.pivot.y);
        }

        // ------------------------------------------------------------------ public API

        public void Toast(string text, float seconds = 2.5f)
        {
            _toastText.text = text;
            _toastUntil = Time.time + seconds;
        }

        public void Prompt(string text)
        {
            _promptString = text;
            _promptTime = Time.time;
        }

        public void Flash() { _flashAlpha = 0.45f; }

        public void SetFloor(int floor)
        {
            _floorText.text = "FLOOR " + floor;
            _boss = null;
            _hasStairs = false;
        }

        public void SetBoss(Enemy boss)
        {
            _boss = boss;
            _bossName.text = boss.Def.Name;
            _bossBack.gameObject.SetActive(true);
            _bossFill.gameObject.SetActive(true);
            _bossName.gameObject.SetActive(true);
        }

        public void ClearBoss()
        {
            _boss = null;
            _bossBack.gameObject.SetActive(false);
            _bossFill.gameObject.SetActive(false);
            _bossName.gameObject.SetActive(false);
        }

        public void ShowDeath(bool show)
        {
            _deathOverlay.gameObject.SetActive(show);
            _deathText.gameObject.SetActive(show);
        }

        public void MarkStairs(Vector3 worldPos)
        {
            _stairsPos = worldPos;
            _hasStairs = true;
        }

        public void SetMinimap(DungeonLayout layout)
        {
            _layout = layout;
            _explored = new bool[layout.Width, layout.Height];
            _mapTex = new Texture2D(layout.Width, layout.Height, TextureFormat.RGBA32, false);
            _mapTex.filterMode = FilterMode.Point;
            _mapTex.wrapMode = TextureWrapMode.Clamp;
            _mapBase = new Color32[layout.Width * layout.Height];
            _mapImage.texture = _mapTex;
            ClearBoss();
            RedrawMap(true);
        }


        // ------------------------------------------------------------------ inventory

        void BuildInventory(Vector2 MC)
        {
            var bg = Img("InvBg", _root, MC, MC, Vector2.zero, new Vector2(1240f, 780f), new Color(0.03f, 0.05f, 0.09f, 0.95f));
            _invPanel = bg.gameObject;
            Txt("InvTitle", _invPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 50f),
                "INVENTORY", 38, TextAnchor.MiddleCenter, Cyan);
            _invLeft = Txt("InvLeft", _invPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -90f), new Vector2(520f, 600f),
                "", 22, TextAnchor.UpperLeft, Color.white);
            _invRight = Txt("InvRight", _invPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, -90f), new Vector2(620f, 600f),
                "", 22, TextAnchor.UpperLeft, Color.white);
            Txt("InvHelp", _invPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1100f, 36f),
                "W / S  select        Enter  equip        X  discard        I  close", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
            _invPanel.SetActive(false);
        }

        public void ShowInventory(bool show)
        {
            if (_invPanel != null) _invPanel.SetActive(show);
        }

        public void RefreshInventory(PlayerController p, int cursor)
        {
            if (p == null || _invLeft == null) return;

            string left = "<b>EQUIPPED</b>\n\n";
            string[] slotNames = { "Weapon ", "Armor  ", "Trinket" };
            for (int i = 0; i < 3; i++)
            {
                var it = p.Gear.Slots[i];
                left += slotNames[i] + "  ";
                left += it == null ? "<color=#888888>(empty)</color>" : ItemUi.Colored(it);
                left += "\n";
                if (it != null) left += "           <color=#9FB7C8>" + ItemUi.Stats(it) + "</color>\n";
                left += "\n";
            }
            left += "<b>STATS</b>\n\n";
            left += "Level " + p.Level + "     XP " + p.Xp + " / " + Progression.XpForNextLevel(p.Level) + "\n";
            left += "HP " + p.Hp + " / " + p.MaxHp + "\n";
            left += "ATK " + p.Attack + "     DEF " + p.Defense + "\n";
            left += "CRIT " + Mathf.RoundToInt(p.CritChance * 100f) + "%";
            _invLeft.text = left;

            var items = p.Bag.Items;
            string right = "<b>BACKPACK  " + items.Count + " / " + Inventory.Capacity + "</b>\n\n";
            if (items.Count == 0)
                right += "<color=#888888>(empty)  Defeat enemies to find loot.</color>";
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var worn = p.Gear.Get(it.Slot);
                float delta = it.Score - (worn != null ? worn.Score : 0f);
                string d = delta >= 0f ? "<color=#7CFF8A>+" + Mathf.RoundToInt(delta) + "</color>" : "<color=#FF7C7C>" + Mathf.RoundToInt(delta) + "</color>";
                string line = (i == cursor ? "<b>> " : "   ") + ItemUi.Colored(it) + (i == cursor ? "</b>" : "");
                right += line + "\n";
                if (i == cursor) right += "      <color=#9FB7C8>" + ItemUi.Stats(it) + "</color>   power " + d + "\n";
            }
            _invRight.text = right;
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            var g = Game.Instance;
            var p = g != null ? g.Player : null;

            if (p != null)
            {
                float hp = Mathf.Clamp01(p.Hp / (float)Mathf.Max(1, p.MaxHp));
                _hpFill.rectTransform.sizeDelta = new Vector2(408f * hp, 22f);
                _hpFill.color = Color.Lerp(new Color(0.95f, 0.3f, 0.25f), new Color(0.35f, 0.9f, 0.45f), Mathf.Clamp01(hp * 1.6f));
                _hpText.text = p.Hp + " / " + p.MaxHp;
                _levelText.text = "Wanderer   Lv " + p.Level;
                _statText.text = "ATK " + p.Attack + "    DEF " + p.Defense + "    CRIT " + Mathf.RoundToInt(p.CritChance * 100f) + "%";
                float xp = Mathf.Clamp01(p.Xp / (float)Progression.XpForNextLevel(p.Level));
                _xpFill.rectTransform.sizeDelta = new Vector2(408f * xp, 8f);

                for (int i = 0; i < _cdOverlay.Length; i++)
                {
                    bool unlocked = SkillCatalog.IsUnlocked(SkillCatalog.All[i], p.Level);
                    _lockText[i].gameObject.SetActive(!unlocked);
                    _cdOverlay[i].fillAmount = unlocked ? p.SkillCd[i].Normalized : 1f;
                }
            }

            _toastText.gameObject.SetActive(Time.time < _toastUntil);
            _promptText.text = Time.time - _promptTime < 0.15f ? _promptString : "";
            _hintText.gameObject.SetActive(Time.time < _hintUntil);

            if (_flashAlpha > 0f)
            {
                _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 1.8f);
                _flash.color = new Color(1f, 0.1f, 0.1f, _flashAlpha);
            }

            if (_boss != null)
            {
                if (_boss.IsDead) ClearBoss();
                else
                {
                    float r = Mathf.Clamp01(_boss.Hp / (float)_boss.MaxHp);
                    _bossFill.rectTransform.sizeDelta = new Vector2(700f * r, 18f);
                }
            }

            _mapTimer -= Time.deltaTime;
            if (_mapTimer <= 0f)
            {
                _mapTimer = 0.1f;
                RedrawMap(false);
            }
        }

        void RedrawMap(bool full)
        {
            if (_layout == null || _mapTex == null) return;
            var g = Game.Instance;
            var p = g != null ? g.Player : null;

            if (p != null)
            {
                var c = DungeonBuilder.ToCell(p.transform.position);
                const int R = 5;
                for (int dx = -R; dx <= R; dx++)
                    for (int dy = -R; dy <= R; dy++)
                    {
                        int x = c.X + dx, y = c.Y + dy;
                        if (dx * dx + dy * dy <= R * R && _layout.InBounds(x, y)) _explored[x, y] = true;
                    }
            }

            var wallCol = new Color32(0, 0, 0, 0);
            var unseen = new Color32(0, 0, 0, 110);
            var floorCol = new Color32(90, 190, 220, 255);
            for (int y = 0; y < _layout.Height; y++)
                for (int x = 0; x < _layout.Width; x++)
                {
                    Color32 col = unseen;
                    if (_explored[x, y]) col = _layout.IsFloor(x, y) ? floorCol : wallCol;
                    _mapBase[y * _layout.Width + x] = col;
                }

            // dynamic markers
            if (_hasStairs) Dot(_stairsPos, new Color32(255, 230, 90, 255), 1);
            if (p != null)
            {
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.IsDead) continue;
                    var ec = DungeonBuilder.ToCell(e.transform.position);
                    if (_layout.InBounds(ec.X, ec.Y) && _explored[ec.X, ec.Y])
                        Dot(e.transform.position, e.Def.IsBoss ? new Color32(255, 60, 60, 255) : new Color32(230, 90, 90, 255), e.Def.IsBoss ? 1 : 0);
                }
                Dot(p.transform.position, new Color32(255, 255, 255, 255), 1);
            }

            _mapTex.SetPixels32(_mapBase);
            _mapTex.Apply(false);
        }

        void Dot(Vector3 world, Color32 color, int radius)
        {
            var c = DungeonBuilder.ToCell(world);
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = c.X + dx, y = c.Y + dy;
                    if (_layout.InBounds(x, y)) _mapBase[y * _layout.Width + x] = color;
                }
        }
    }
}
