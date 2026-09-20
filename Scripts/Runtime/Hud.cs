using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow
{
    /// <summary>
    /// The whole HUD, built in code: player frame, skill bar, consumables, target and boss frames,
    /// companion status, minimap, toasts/banners/prompts, dialogue, generic menu and the inventory screen.
    /// Everything animates on unscaled time so it keeps working while the game is paused.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        public enum MapMarkerKind { Chest, Shrine, Boss, Gate }

        struct MapMarker
        {
            public Vector3 Pos;
            public MapMarkerKind Kind;
            public int Frame;
        }

        const int MaxBossBars = 5;
        const int MaxMenuRows = 14;
        const int GridCols = 5;

        Canvas _canvas;
        RectTransform _root;

        // --- player frame ---
        RectTransform _playerFrame;
        Image _badgeRing;
        Text _levelText, _nameText, _weaponText, _statText, _goldText, _shardText;
        UiBar _hpBar, _xpBar;

        // --- consumables ---
        IconCell _potionCell, _crystalCell, _reviveCell;
        Image _potionCdFill;
        Text _potionCount, _crystalCount, _reviveCount;

        // --- companion ---
        GameObject _compPanel;
        Image[] _compFrame;
        Text _compName, _compState;
        UiBar _compBar;

        // --- top centre ---
        Text _floorText, _biomeText;
        RectTransform _bossRoot, _targetRoot;
        Text _bossName, _bossTitle, _targetName;
        UiBar[] _bossBars;
        Image[] _bossPips;
        UiBar _targetBar;
        float _bossHeight;

        // --- skill bar ---
        RectTransform _skillRoot;
        SkillSlotWidget[] _skillSlots;
        string[] _skillIds;
        WeaponType _skillWeapon = (WeaponType)(-1);

        // --- messages ---
        Text _promptText, _hintText, _toastText, _bannerTitle, _bannerSub;
        Image _promptBg;
        CanvasGroup _toastGroup, _bannerGroup;
        RectTransform _bannerRoot;
        Image _flash;
        GameObject _deathRoot;
        Text _deathText;

        // --- minimap ---
        RectTransform _mapRect;
        RawImage _mapImage;
        Texture2D _mapTex;
        Color32[] _mapBase;
        bool[,] _explored;
        DungeonLayout _layout;
        Image _playerArrow, _stairMarker;
        readonly List<MapMarker> _markers = new List<MapMarker>();
        readonly List<Image> _markerImages = new List<Image>();
        GameObject _questPanel;
        Text _questText;

        // --- dialogue ---
        GameObject _dlgPanel;
        Text _dlgSpeaker, _dlgBody, _dlgHint;

        // --- menu ---
        GameObject _menuPanel;
        Text _menuTitle, _menuHelp, _menuDetail, _menuFooter;
        Image[] _menuRowBg;
        Image[] _menuRowIcon;
        Image[][] _menuRowFrame;
        Text[] _menuRowText;
        int _menuRowCount;

        // --- inventory ---
        GameObject _invPanel;
        IconCell[] _gearCells;
        Text[] _gearNames;
        Text _invStats, _invCount;
        IconCell[] _invCells;
        IconCell _detailCell, _compareCell;
        Text _detailName, _detailMeta, _detailStats, _detailCompare, _detailHint;
        Text _compareTitle, _compareName, _compareStats;
        int _bagCount;

        // --- state ---
        Enemy _boss;
        Enemy _lastTarget;
        float _toastUntil, _toastStart, _promptTime = -10f, _hintUntil;
        float _bannerUntil, _bannerStart;
        float _flashAmount, _mapTimer;
        Vector3 _stairsPos;
        bool _hasStairs;

        // cached values so per-frame code never rebuilds identical strings
        int _cHp = -1, _cMaxHp = -1, _cLevel = -1, _cXp = -1, _cAtk = -1, _cDef = -1, _cCrit = -1, _cGold = -1, _cShards = -1;
        int _cPotion = -1, _cCrystal = -1, _cRevive = -1, _cTargetHp = -1;
        WeaponType _cWeapon = (WeaponType)(-1);
        string _cName = "";

        // ------------------------------------------------------------------ lifecycle

        public static void Create()
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<Hud>();
            hud.Build();
        }

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_mapTex != null) Destroy(_mapTex);
        }

        void Build()
        {
            Instance = this;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _root = _canvas.GetComponent<RectTransform>();

            BuildPlayerFrame();
            BuildConsumables();
            BuildCompanion();
            BuildTopCenter();
            BuildMinimap();
            BuildBottom();
            BuildDialogue();
            BuildMenu();
            BuildInventory();
            BuildOverlays();

            _hintUntil = Time.unscaledTime + 20f;
        }

        // ------------------------------------------------------------------ build: player frame

        void BuildPlayerFrame()
        {
            var panel = Ui.Panel("PlayerFrame", _root, Ui.TL, Ui.TL, new Vector2(30f, -30f), new Vector2(486f, 176f),
                                 HudStyle.Glass, HudStyle.CyanFaint);
            _playerFrame = panel.rectTransform;
            Ui.Accent(_playerFrame, 0.42f, HudStyle.Cyan);

            Ui.MakeImg("BadgeGlow", _playerFrame, Ui.TL, Ui.TL, new Vector2(12f, -12f), new Vector2(76f, 76f),
                       new Color(0.42f, 0.88f, 1f, 0.14f), Ui.Soft);
            Ui.MakeImg("Badge", _playerFrame, Ui.TL, Ui.TL, new Vector2(20f, -20f), new Vector2(60f, 60f),
                       new Color(0.05f, 0.12f, 0.18f, 0.95f), Ui.Soft);
            _badgeRing = Ui.MakeImg("BadgeRing", _playerFrame, Ui.TL, Ui.TL, new Vector2(18f, -18f), new Vector2(64f, 64f),
                                    HudStyle.Cyan, Ui.Ring);
            _levelText = Ui.MakeText("Level", _playerFrame, Ui.TL, Ui.TL, new Vector2(20f, -20f), new Vector2(60f, 60f),
                                     "1", 28, TextAnchor.MiddleCenter, Color.white);
            _levelText.fontStyle = FontStyle.Bold;

            _nameText = Ui.MakeText("Name", _playerFrame, Ui.TL, Ui.TL, new Vector2(98f, -18f), new Vector2(300f, 28f),
                                    "Wanderer", 24, TextAnchor.MiddleLeft, HudStyle.Ink);
            _weaponText = Ui.MakeText("Weapon", _playerFrame, Ui.TL, Ui.TL, new Vector2(98f, -48f), new Vector2(320f, 22f),
                                      "", 15, TextAnchor.MiddleLeft, HudStyle.Muted);

            _hpBar = new UiBar(_playerFrame, "HpBar", Ui.TL, Ui.TL, new Vector2(20f, -84f), new Vector2(446f, 26f),
                               HudStyle.HpHigh, HudStyle.BarBack, 16, Color.white);
            _xpBar = new UiBar(_playerFrame, "XpBar", Ui.TL, Ui.TL, new Vector2(20f, -116f), new Vector2(446f, 8f),
                               HudStyle.Xp, new Color(0.09f, 0.12f, 0.17f, 0.92f), 0, Color.white);
            _xpBar.SetGhostVisible(false);

            _statText = Ui.MakeText("Stats", _playerFrame, Ui.TL, Ui.TL, new Vector2(20f, -128f), new Vector2(330f, 20f),
                                    "", 16, TextAnchor.MiddleLeft, HudStyle.Muted);

            Ui.MakeImg("GoldIcon", _playerFrame, Ui.TL, Ui.TL, new Vector2(20f, -152f), new Vector2(18f, 18f), Color.white,
                       ItemIcons.Consumable(ItemIcons.ConsumableIcon.Gold));
            _goldText = Ui.MakeText("Gold", _playerFrame, Ui.TL, Ui.TL, new Vector2(44f, -152f), new Vector2(120f, 18f),
                                    "0", 16, TextAnchor.MiddleLeft, HudStyle.GoldColor);
            Ui.MakeImg("ShardIcon", _playerFrame, Ui.TL, Ui.TL, new Vector2(176f, -152f), new Vector2(18f, 18f), Color.white,
                       ItemIcons.Consumable(ItemIcons.ConsumableIcon.Shard));
            _shardText = Ui.MakeText("Shards", _playerFrame, Ui.TL, Ui.TL, new Vector2(200f, -152f), new Vector2(120f, 18f),
                                     "0", 16, TextAnchor.MiddleLeft, HudStyle.ShardColor);
        }

        // ------------------------------------------------------------------ build: consumables

        void BuildConsumables()
        {
            var panel = Ui.Panel("Consumables", _root, Ui.BR, Ui.BR, new Vector2(-30f, 30f), new Vector2(252f, 112f),
                                 HudStyle.Glass, HudStyle.CyanFaint);
            var pr = panel.rectTransform;
            Ui.Accent(pr, 0.5f, HudStyle.CyanSoft);

            var cellSize = new Vector2(68f, 68f);
            var cellBg = new Color(0.05f, 0.08f, 0.12f, 0.85f);

            _potionCell = new IconCell(pr, "Potion", Ui.TL, Ui.TL, new Vector2(12f, -10f), cellSize, 8f, cellBg);
            _potionCell.SetIcon(ItemIcons.Consumable(ItemIcons.ConsumableIcon.Potion));
            _potionCell.SetBorder(new Color(0.9f, 0.35f, 0.45f, 0.55f));
            _potionCdFill = Ui.MakeImg("PotionCd", _potionCell.Root, Ui.MC, Ui.MC, Vector2.zero, cellSize,
                                       new Color(0.02f, 0.04f, 0.07f, 0.75f));
            _potionCdFill.type = Image.Type.Filled;
            _potionCdFill.fillMethod = Image.FillMethod.Vertical;
            _potionCdFill.fillOrigin = (int)Image.OriginVertical.Top;
            _potionCdFill.fillAmount = 0f;

            _crystalCell = new IconCell(pr, "Crystal", Ui.TL, Ui.TL, new Vector2(92f, -10f), cellSize, 8f, cellBg);
            _crystalCell.SetIcon(ItemIcons.Consumable(ItemIcons.ConsumableIcon.Crystal));
            _crystalCell.SetBorder(new Color(0.4f, 0.75f, 1f, 0.55f));

            _reviveCell = new IconCell(pr, "Revive", Ui.TL, Ui.TL, new Vector2(172f, -10f), cellSize, 8f, cellBg);
            _reviveCell.SetIcon(ItemIcons.Consumable(ItemIcons.ConsumableIcon.Revive));
            _reviveCell.SetBorder(new Color(1f, 0.82f, 0.35f, 0.55f));

            _potionCount = Count(_potionCell, "0");
            _crystalCount = Count(_crystalCell, "0");
            _reviveCount = Count(_reviveCell, "0");

            Key(_potionCell, "H");
            Key(_crystalCell, "T");
            Key(_reviveCell, "*");

            Ui.MakeText("PotionLbl", pr, Ui.TL, Ui.TL, new Vector2(12f, -82f), new Vector2(68f, 18f), "POTION", 11,
                        TextAnchor.MiddleCenter, HudStyle.Faded);
            Ui.MakeText("CrystalLbl", pr, Ui.TL, Ui.TL, new Vector2(92f, -82f), new Vector2(68f, 18f), "RETURN", 11,
                        TextAnchor.MiddleCenter, HudStyle.Faded);
            Ui.MakeText("ReviveLbl", pr, Ui.TL, Ui.TL, new Vector2(172f, -82f), new Vector2(68f, 18f), "REVIVE", 11,
                        TextAnchor.MiddleCenter, HudStyle.Faded);
        }

        Text Count(IconCell cell, string value)
        {
            Ui.MakeImg("CountChip", cell.Root, Ui.BR, Ui.BR, new Vector2(-2f, 2f), new Vector2(26f, 20f),
                       new Color(0.02f, 0.05f, 0.08f, 0.9f));
            var t = Ui.MakeText("Count", cell.Root, Ui.BR, Ui.BR, new Vector2(-2f, 2f), new Vector2(26f, 20f), value, 16,
                                TextAnchor.MiddleCenter, Color.white);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        void Key(IconCell cell, string key)
        {
            Ui.MakeImg("KeyChip", cell.Root, Ui.TL, Ui.TL, new Vector2(2f, -2f), new Vector2(20f, 18f),
                       new Color(0.02f, 0.05f, 0.08f, 0.88f));
            Ui.MakeText("Key", cell.Root, Ui.TL, Ui.TL, new Vector2(2f, -2f), new Vector2(20f, 18f), key, 13,
                        TextAnchor.MiddleCenter, HudStyle.Cyan);
        }

        // ------------------------------------------------------------------ build: companion

        void BuildCompanion()
        {
            var panel = Ui.MakeImg("Companion", _root, Ui.TL, Ui.TL, new Vector2(30f, -218f), new Vector2(316f, 64f), HudStyle.Glass);
            _compFrame = Ui.Frame(panel.rectTransform, HudStyle.CyanFaint, 2f);
            _compPanel = panel.gameObject;
            var pr = panel.rectTransform;

            _compName = Ui.MakeText("CompName", pr, Ui.TL, Ui.TL, new Vector2(14f, -8f), new Vector2(150f, 24f), "MIRI", 18,
                                    TextAnchor.MiddleLeft, HudStyle.Cyan);
            _compState = Ui.MakeText("CompState", pr, Ui.TL, new Vector2(1f, 1f), new Vector2(302f, -8f), new Vector2(160f, 24f),
                                     "", 16, TextAnchor.MiddleRight, HudStyle.Muted);
            _compBar = new UiBar(pr, "CompBar", Ui.TL, Ui.TL, new Vector2(14f, -40f), new Vector2(288f, 8f),
                                 HudStyle.Cyan, new Color(0.09f, 0.12f, 0.17f, 0.92f), 0, Color.white);
            _compBar.SetGhostVisible(false);
            _compPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ build: floor / boss / target

        void BuildTopCenter()
        {
            _floorText = Ui.MakeText("Floor", _root, Ui.TC, Ui.TC, new Vector2(0f, -20f), new Vector2(760f, 46f), "FLOOR 1", 38,
                                     TextAnchor.MiddleCenter, HudStyle.Cyan);
            Ui.MakeImg("FloorLine", _root, Ui.TC, Ui.TC, new Vector2(0f, -66f), new Vector2(240f, 2f), HudStyle.CyanSoft);
            _biomeText = Ui.MakeText("Biome", _root, Ui.TC, Ui.TC, new Vector2(0f, -72f), new Vector2(760f, 22f), "", 17,
                                     TextAnchor.MiddleCenter, HudStyle.Muted);

            // boss frame
            _bossRoot = Ui.MakeRect("BossFrame", _root, Ui.TC, Ui.TC, new Vector2(0f, -104f), new Vector2(820f, 200f));
            _bossName = Ui.MakeText("BossName", _bossRoot, Ui.TC, Ui.TC, new Vector2(0f, 0f), new Vector2(820f, 32f), "", 26,
                                    TextAnchor.MiddleCenter, new Color(1f, 0.72f, 0.66f));
            _bossName.fontStyle = FontStyle.Bold;
            _bossTitle = Ui.MakeText("BossTitle", _bossRoot, Ui.TC, Ui.TC, new Vector2(0f, -32f), new Vector2(820f, 22f), "", 16,
                                     TextAnchor.MiddleCenter, new Color(0.85f, 0.66f, 0.45f));
            _bossTitle.fontStyle = FontStyle.Italic;

            _bossPips = new Image[MaxBossBars];
            const float pipW = 18f, pipGap = 5f;
            float pipTotal = MaxBossBars * pipW + (MaxBossBars - 1) * pipGap;
            for (int i = 0; i < MaxBossBars; i++)
                _bossPips[i] = Ui.MakeImg("BossPip" + i, _bossRoot, Ui.TC, Ui.TC,
                                          new Vector2(-pipTotal * 0.5f + pipW * 0.5f + i * (pipW + pipGap), -56f),
                                          new Vector2(pipW, 5f), HudStyle.BossRed);

            _bossBars = new UiBar[MaxBossBars];
            for (int i = 0; i < MaxBossBars; i++)
            {
                _bossBars[i] = new UiBar(_bossRoot, "BossBar" + i, Ui.TC, Ui.TC, new Vector2(0f, -70f - i * 20f),
                                         new Vector2(760f, 14f), HudStyle.BossRed, new Color(0.16f, 0.04f, 0.05f, 0.92f), 0, Color.white);
                _bossBars[i].SetInstant(1f);
            }
            _bossRoot.gameObject.SetActive(false);

            // lock-on target frame
            var target = Ui.MakeImg("TargetFrame", _root, Ui.TC, Ui.TC, new Vector2(0f, -104f), new Vector2(470f, 60f), HudStyle.Glass);
            Ui.Frame(target.rectTransform, HudStyle.CyanFaint, 2f);
            _targetRoot = target.rectTransform;
            _targetName = Ui.MakeText("TargetName", _targetRoot, Ui.TC, Ui.TC, new Vector2(0f, -6f), new Vector2(450f, 24f), "", 18,
                                      TextAnchor.MiddleCenter, HudStyle.Ink);
            _targetBar = new UiBar(_targetRoot, "TargetBar", Ui.TC, Ui.TC, new Vector2(0f, -34f), new Vector2(430f, 12f),
                                   new Color(0.90f, 0.35f, 0.30f, 1f), new Color(0.14f, 0.07f, 0.07f, 0.92f), 0, Color.white);
            _targetRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ build: minimap + quest tracker

        void BuildMinimap()
        {
            var frame = Ui.Panel("MapFrame", _root, Ui.TR, Ui.TR, new Vector2(-30f, -30f), new Vector2(268f, 268f),
                                 HudStyle.Glass, HudStyle.CyanFaint);
            Ui.Accent(frame.rectTransform, 0.4f, HudStyle.Cyan);

            _mapRect = Ui.MakeRect("Map", frame.rectTransform, Ui.TL, Ui.TL, new Vector2(8f, -8f), new Vector2(252f, 252f));
            _mapImage = _mapRect.gameObject.AddComponent<RawImage>();
            _mapImage.raycastTarget = false;
            _mapImage.color = new Color(1f, 1f, 1f, 0.95f);

            _stairMarker = Ui.MakeImg("Stairs", _mapRect, Ui.MC, Ui.MC, Vector2.zero, new Vector2(14f, 14f),
                                      new Color(1f, 0.90f, 0.35f, 1f), Ui.Diamond);
            _stairMarker.enabled = false;
            _playerArrow = Ui.MakeImg("PlayerArrow", _mapRect, Ui.MC, Ui.MC, Vector2.zero, new Vector2(16f, 16f),
                                      Color.white, Ui.Arrow);
            _playerArrow.enabled = false;

            var quest = Ui.MakeImg("QuestTracker", _root, Ui.TR, Ui.TR, new Vector2(-30f, -308f), new Vector2(330f, 160f), HudStyle.Glass);
            Ui.Frame(quest.rectTransform, HudStyle.CyanFaint, 2f);
            Ui.Accent(quest.rectTransform, 0.35f, HudStyle.CyanSoft);
            _questPanel = quest.gameObject;
            Ui.MakeText("QuestLabel", quest.rectTransform, Ui.TL, Ui.TL, new Vector2(14f, -8f), new Vector2(300f, 20f),
                        "OBJECTIVES", 13, TextAnchor.MiddleLeft, HudStyle.Cyan);
            _questText = Ui.Wrapped(Ui.MakeText("QuestText", quest.rectTransform, Ui.TL, Ui.TL, new Vector2(14f, -32f),
                                                new Vector2(302f, 120f), "", 16, TextAnchor.UpperLeft, HudStyle.Ink));
            _questPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ build: prompt / hint / skill bar root

        void BuildBottom()
        {
            _skillRoot = Ui.MakeRect("SkillBar", _root, Ui.BC, Ui.BC, Vector2.zero, new Vector2(1000f, 160f));

            // Soft backdrop instead of a boxed panel: prompts vary a lot in length.
            _promptBg = Ui.MakeImg("PromptBg", _root, Ui.BC, Ui.BC, new Vector2(0f, 172f), new Vector2(1500f, 92f),
                                   new Color(0f, 0f, 0f, 0.55f), Ui.Soft);
            _promptText = Ui.MakeText("Prompt", _root, Ui.BC, Ui.BC, new Vector2(0f, 200f), new Vector2(1600f, 36f), "", 24,
                                      TextAnchor.MiddleCenter, HudStyle.Ink);
            _promptBg.gameObject.SetActive(false);
            _promptText.gameObject.SetActive(false);

            _hintText = Ui.MakeText("Hint", _root, Ui.BL, Ui.BL, new Vector2(30f, 30f), new Vector2(640f, 190f),
                "WASD  move       Shift  sprint       Space  jump\n" +
                "Ctrl / RMB  dodge roll       LMB  attack combo\n" +
                "1-5  skills       H  potion       E  interact\n" +
                "Q  lock-on (Tab switches)       G  companion switch\n" +
                "I  inventory       T  return to town       Esc  free mouse",
                18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.72f));
        }

        // ------------------------------------------------------------------ build: dialogue

        void BuildDialogue()
        {
            var panel = Ui.Panel("Dialogue", _root, Ui.BC, Ui.BC, new Vector2(0f, 120f), new Vector2(1280f, 230f),
                                 HudStyle.GlassDeep, HudStyle.CyanSoft);
            Ui.Accent(panel.rectTransform, 0.3f, HudStyle.Cyan);
            _dlgPanel = panel.gameObject;
            var pr = panel.rectTransform;

            _dlgSpeaker = Ui.MakeText("Speaker", pr, Ui.TL, Ui.TL, new Vector2(28f, -16f), new Vector2(600f, 32f), "", 24,
                                      TextAnchor.MiddleLeft, HudStyle.Cyan);
            _dlgSpeaker.fontStyle = FontStyle.Bold;
            Ui.MakeImg("DlgLine", pr, Ui.TL, Ui.TL, new Vector2(28f, -52f), new Vector2(1224f, 1f), HudStyle.CyanFaint);
            _dlgBody = Ui.Wrapped(Ui.MakeText("Body", pr, Ui.TL, Ui.TL, new Vector2(28f, -64f), new Vector2(1224f, 130f), "", 21,
                                              TextAnchor.UpperLeft, HudStyle.Ink));
            _dlgHint = Ui.MakeText("DlgHint", pr, Ui.BR, Ui.BR, new Vector2(-24f, 14f), new Vector2(360f, 24f), "", 18,
                                   TextAnchor.MiddleRight, HudStyle.CyanSoft);
            _dlgPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ build: generic menu

        void BuildMenu()
        {
            var panel = Ui.Panel("Menu", _root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1140f, 780f),
                                 HudStyle.GlassDeep, HudStyle.CyanSoft);
            Ui.Accent(panel.rectTransform, 0.25f, HudStyle.Cyan);
            _menuPanel = panel.gameObject;
            var pr = panel.rectTransform;

            _menuTitle = Ui.MakeText("MenuTitle", pr, Ui.TC, Ui.TC, new Vector2(0f, -20f), new Vector2(1000f, 44f), "", 32,
                                     TextAnchor.MiddleCenter, HudStyle.Cyan);
            _menuHelp = Ui.MakeText("MenuHelp", pr, Ui.TC, Ui.TC, new Vector2(0f, -66f), new Vector2(1040f, 26f), "", 17,
                                    TextAnchor.MiddleCenter, HudStyle.Muted);

            _menuRowBg = new Image[MaxMenuRows];
            _menuRowIcon = new Image[MaxMenuRows];
            _menuRowText = new Text[MaxMenuRows];
            _menuRowFrame = new Image[MaxMenuRows][];
            for (int i = 0; i < MaxMenuRows; i++)
            {
                var bg = Ui.MakeImg("MenuRow" + i, pr, Ui.TL, Ui.TL, new Vector2(28f, -104f - i * 44f), new Vector2(604f, 40f),
                                    new Color(1f, 1f, 1f, 0.03f));
                _menuRowBg[i] = bg;
                _menuRowFrame[i] = Ui.Frame(bg.rectTransform, Color.clear, 2f);
                _menuRowIcon[i] = Ui.MakeImg("Icon", bg.rectTransform, Ui.ML, Ui.ML, new Vector2(8f, 0f), new Vector2(30f, 30f), Color.white);
                _menuRowIcon[i].preserveAspect = true;
                _menuRowIcon[i].enabled = false;
                _menuRowText[i] = Ui.MakeText("Text", bg.rectTransform, Ui.ML, Ui.ML, new Vector2(48f, 0f), new Vector2(548f, 40f),
                                              "", 20, TextAnchor.MiddleLeft, HudStyle.Ink);
                bg.gameObject.SetActive(false);
            }

            var detail = Ui.MakeImg("MenuDetail", pr, Ui.TL, Ui.TL, new Vector2(656f, -104f), new Vector2(456f, 620f),
                                    new Color(1f, 1f, 1f, 0.03f));
            Ui.Frame(detail.rectTransform, HudStyle.CyanFaint, 2f);
            _menuDetail = Ui.Wrapped(Ui.MakeText("DetailText", detail.rectTransform, Ui.TL, Ui.TL, new Vector2(16f, -14f),
                                                 new Vector2(424f, 594f), "", 18, TextAnchor.UpperLeft, HudStyle.Ink));
            _menuFooter = Ui.MakeText("MenuFooter", pr, Ui.BC, Ui.BC, new Vector2(0f, 22f), new Vector2(1080f, 30f), "", 20,
                                      TextAnchor.MiddleCenter, HudStyle.GoldColor);
            _menuPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ build: inventory

        void BuildInventory()
        {
            var panel = Ui.Panel("Inventory", _root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1480f, 880f),
                                 HudStyle.GlassDeep, HudStyle.CyanSoft);
            Ui.Accent(panel.rectTransform, 0.22f, HudStyle.Cyan);
            _invPanel = panel.gameObject;
            var pr = panel.rectTransform;

            Ui.MakeText("InvTitle", pr, Ui.TC, Ui.TC, new Vector2(0f, -18f), new Vector2(700f, 44f), "INVENTORY", 34,
                        TextAnchor.MiddleCenter, HudStyle.Cyan);
            Ui.MakeImg("InvSplit", pr, Ui.TL, Ui.TL, new Vector2(524f, -76f), new Vector2(1f, 740f), HudStyle.CyanFaint);

            // --- equipment doll ---
            Ui.MakeText("EqLabel", pr, Ui.TL, Ui.TL, new Vector2(48f, -80f), new Vector2(300f, 24f), "EQUIPPED", 17,
                        TextAnchor.MiddleLeft, HudStyle.Cyan);
            string[] slotNames = { "WEAPON", "ARMOR", "TRINKET" };
            _gearCells = new IconCell[3];
            _gearNames = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x = 48f + i * 140f;
                _gearCells[i] = new IconCell(pr, "Gear" + i, Ui.TL, Ui.TL, new Vector2(x, -112f), new Vector2(120f, 120f), 10f,
                                             new Color(0.05f, 0.08f, 0.12f, 0.9f));
                Ui.MakeText("GearLbl" + i, pr, Ui.TL, Ui.TL, new Vector2(x, -238f), new Vector2(120f, 18f), slotNames[i], 12,
                            TextAnchor.MiddleCenter, HudStyle.Faded);
                _gearNames[i] = Ui.Wrapped(Ui.MakeText("GearName" + i, pr, Ui.TL, Ui.TL, new Vector2(x - 6f, -258f),
                                                       new Vector2(132f, 46f), "", 14, TextAnchor.UpperCenter, HudStyle.Ink));
            }

            Ui.MakeText("StatLabel", pr, Ui.TL, Ui.TL, new Vector2(48f, -324f), new Vector2(300f, 24f), "CHARACTER", 17,
                        TextAnchor.MiddleLeft, HudStyle.Cyan);
            var statBg = Ui.MakeImg("StatBg", pr, Ui.TL, Ui.TL, new Vector2(48f, -352f), new Vector2(428f, 340f),
                                    new Color(1f, 1f, 1f, 0.03f));
            Ui.Frame(statBg.rectTransform, HudStyle.CyanFaint, 2f);
            _invStats = Ui.Wrapped(Ui.MakeText("StatText", statBg.rectTransform, Ui.TL, Ui.TL, new Vector2(16f, -14f),
                                               new Vector2(396f, 314f), "", 19, TextAnchor.UpperLeft, HudStyle.Ink));
            _invStats.lineSpacing = 1.25f;

            // --- backpack grid ---
            _invCount = Ui.MakeText("BagCount", pr, Ui.TL, Ui.TL, new Vector2(560f, -80f), new Vector2(500f, 24f),
                                    "BACKPACK   0 / 20", 17, TextAnchor.MiddleLeft, HudStyle.Cyan);
            _invCells = new IconCell[Inventory.Capacity];
            for (int i = 0; i < _invCells.Length; i++)
            {
                int row = i / GridCols, col = i % GridCols;
                _invCells[i] = new IconCell(pr, "Cell" + i, Ui.TL, Ui.TL,
                                            new Vector2(560f + col * 116f, -112f - row * 116f), new Vector2(104f, 104f), 10f,
                                            new Color(0.04f, 0.06f, 0.09f, 0.75f));
            }

            // --- equipped comparison card (right of the grid) ---
            var cmp = Ui.MakeImg("CompareCard", pr, Ui.TL, Ui.TL, new Vector2(1152f, -112f), new Vector2(280f, 452f),
                                 new Color(1f, 1f, 1f, 0.03f));
            Ui.Frame(cmp.rectTransform, HudStyle.CyanFaint, 2f);
            _compareTitle = Ui.MakeText("CmpTitle", cmp.rectTransform, Ui.TC, Ui.TC, new Vector2(0f, -12f), new Vector2(260f, 22f),
                                        "EQUIPPED", 14, TextAnchor.MiddleCenter, HudStyle.Faded);
            _compareCell = new IconCell(cmp.rectTransform, "CmpCell", Ui.TC, Ui.TC, new Vector2(0f, -42f), new Vector2(96f, 96f), 8f,
                                        new Color(0.05f, 0.08f, 0.12f, 0.9f));
            _compareName = Ui.Wrapped(Ui.MakeText("CmpName", cmp.rectTransform, Ui.TC, Ui.TC, new Vector2(0f, -148f),
                                                  new Vector2(252f, 60f), "", 16, TextAnchor.UpperCenter, HudStyle.Ink));
            _compareStats = Ui.Wrapped(Ui.MakeText("CmpStats", cmp.rectTransform, Ui.TC, Ui.TC, new Vector2(0f, -214f),
                                                   new Vector2(252f, 220f), "", 16, TextAnchor.UpperCenter, HudStyle.Muted));

            // --- detail card under the grid ---
            var card = Ui.MakeImg("DetailCard", pr, Ui.TL, Ui.TL, new Vector2(560f, -592f), new Vector2(872f, 236f),
                                  new Color(1f, 1f, 1f, 0.035f));
            Ui.Frame(card.rectTransform, HudStyle.CyanFaint, 2f);
            var cr = card.rectTransform;
            _detailCell = new IconCell(cr, "DetailIcon", Ui.TL, Ui.TL, new Vector2(18f, -18f), new Vector2(96f, 96f), 8f,
                                       new Color(0.05f, 0.08f, 0.12f, 0.9f));
            _detailName = Ui.MakeText("DetailName", cr, Ui.TL, Ui.TL, new Vector2(132f, -16f), new Vector2(720f, 32f), "", 24,
                                      TextAnchor.MiddleLeft, HudStyle.Ink);
            _detailName.fontStyle = FontStyle.Bold;
            _detailMeta = Ui.MakeText("DetailMeta", cr, Ui.TL, Ui.TL, new Vector2(132f, -50f), new Vector2(720f, 22f), "", 16,
                                      TextAnchor.MiddleLeft, HudStyle.Muted);
            _detailStats = Ui.MakeText("DetailStats", cr, Ui.TL, Ui.TL, new Vector2(132f, -78f), new Vector2(720f, 24f), "", 18,
                                       TextAnchor.MiddleLeft, HudStyle.Ink);
            _detailCompare = Ui.MakeText("DetailCompare", cr, Ui.TL, Ui.TL, new Vector2(132f, -108f), new Vector2(720f, 26f), "", 19,
                                         TextAnchor.MiddleLeft, HudStyle.Ink);
            _detailHint = Ui.Wrapped(Ui.MakeText("DetailHint", cr, Ui.TL, Ui.TL, new Vector2(18f, -140f), new Vector2(836f, 92f), "", 16,
                                                 TextAnchor.UpperLeft, HudStyle.Muted));

            Ui.MakeText("InvHelp", pr, Ui.BC, Ui.BC, new Vector2(0f, 16f), new Vector2(1400f, 30f),
                        "Mouse: click item = equip, right click = discard, click gear = unequip     Keys: WASD/Enter/X     I close",
                        18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.68f));

            _invPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ build: overlays

        void BuildOverlays()
        {
            // banner
            _bannerRoot = Ui.MakeRect("Banner", _root, Ui.MC, Ui.MC, new Vector2(0f, 140f), new Vector2(1300f, 220f));
            _bannerGroup = Ui.Group(_bannerRoot.gameObject);
            Ui.MakeImg("BannerGlow", _bannerRoot, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1300f, 260f),
                       new Color(0f, 0f, 0f, 0.55f), Ui.Soft);
            Ui.MakeImg("BannerLineTop", _bannerRoot, Ui.MC, Ui.MC, new Vector2(0f, 58f), new Vector2(720f, 2f), HudStyle.CyanSoft);
            Ui.MakeImg("BannerLineBot", _bannerRoot, Ui.MC, Ui.MC, new Vector2(0f, -62f), new Vector2(720f, 2f), HudStyle.CyanSoft);
            _bannerTitle = Ui.MakeText("BannerTitle", _bannerRoot, Ui.MC, Ui.MC, new Vector2(0f, 8f), new Vector2(1260f, 90f), "", 62,
                                       TextAnchor.MiddleCenter, HudStyle.Ink);
            _bannerTitle.fontStyle = FontStyle.Bold;
            _bannerSub = Ui.MakeText("BannerSub", _bannerRoot, Ui.MC, Ui.MC, new Vector2(0f, -38f), new Vector2(1260f, 32f), "", 24,
                                     TextAnchor.MiddleCenter, HudStyle.Cyan);
            _bannerRoot.gameObject.SetActive(false);

            // toast
            var toastRoot = Ui.MakeRect("Toast", _root, Ui.MC, Ui.MC, new Vector2(0f, 250f), new Vector2(1200f, 140f));
            _toastGroup = Ui.Group(toastRoot.gameObject);
            Ui.MakeImg("ToastGlow", toastRoot, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1100f, 180f),
                       new Color(0f, 0f, 0f, 0.42f), Ui.Soft);
            _toastText = Ui.MakeText("ToastText", toastRoot, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1160f, 130f), "", 40,
                                     TextAnchor.MiddleCenter, HudStyle.Cyan);
            toastRoot.gameObject.SetActive(false);

            // damage vignette
            _flash = Ui.MakeImg("Flash", _root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(4000f, 3000f),
                                new Color(1f, 0.12f, 0.12f, 0f), Ui.Vignette);
            _flash.enabled = false;

            // death overlay
            var death = Ui.MakeImg("Death", _root, Ui.MC, Ui.MC, Vector2.zero, new Vector2(4000f, 3000f),
                                   new Color(0.02f, 0f, 0f, 0.70f));
            _deathRoot = death.gameObject;
            _deathText = Ui.MakeText("DeathText", death.rectTransform, Ui.MC, Ui.MC, Vector2.zero, new Vector2(1200f, 260f),
                                     "YOU DIED", 48, TextAnchor.MiddleCenter, new Color(1f, 0.40f, 0.40f));
            _deathRoot.SetActive(false);
        }

        // ------------------------------------------------------------------ public API: messages

        public void Toast(string text, float seconds = 2.5f)
        {
            if (_toastText == null) return;
            _toastText.text = text;
            _toastStart = Time.unscaledTime;
            _toastUntil = _toastStart + Mathf.Max(0.2f, seconds);
            _toastGroup.gameObject.SetActive(true);
        }

        public void Prompt(string text)
        {
            _promptText.text = text;
            _promptTime = Time.unscaledTime;
        }

        public void Flash() { _flashAmount = 0.55f; }

        public void ShowBanner(string title, string subtitle, float seconds)
        {
            if (_bannerTitle == null) return;
            _bannerTitle.text = title == null ? "" : title;
            _bannerSub.text = subtitle == null ? "" : subtitle;
            _bannerStart = Time.unscaledTime;
            _bannerUntil = _bannerStart + Mathf.Max(0.4f, seconds);
            _bannerRoot.gameObject.SetActive(true);
        }

        public void SetBiome(string name)
        {
            if (_biomeText == null) return;
            string s = name == null ? "" : name;
            if (_biomeText.text != s) _biomeText.text = s;
            _biomeText.gameObject.SetActive(s.Length > 0);
        }

        public void SetQuestTracker(string richText)
        {
            if (_questText == null) return;
            string s = richText == null ? "" : richText;
            if (_questText.text != s) _questText.text = s;
            _questPanel.SetActive(s.Length > 0);
        }

        public void SetFloor(int floor)
        {
            _floorText.text = floor <= 0 ? "TOWN" : "FLOOR " + floor;
            if (floor <= 0) SetBiome("");
            ClearBoss();
            _hasStairs = false;
            _stairMarker.enabled = false;
        }

        public void ShowDeath(bool show)
        {
            bool hardcore = Game.Instance != null && Game.Instance.Hardcore;
            _deathText.text = hardcore
                ? "YOU DIED\nThe hardcore run ends here.\nT  start a new run"
                : "YOU DIED\nR  respawn on this floor        T  return to town";
            _deathRoot.SetActive(show);
        }

        // ------------------------------------------------------------------ public API: boss / target

        public void SetBoss(Enemy boss)
        {
            if (boss == null) { ClearBoss(); return; }
            _boss = boss;
            _bossName.text = boss.DisplayName == null ? "" : boss.DisplayName.ToUpper();
            string title = boss.BossTitle == null ? "" : boss.BossTitle;
            _bossTitle.text = title;
            _bossTitle.gameObject.SetActive(title.Length > 0);

            int bars = Mathf.Clamp(boss.BossBars, 1, MaxBossBars);
            for (int i = 0; i < MaxBossBars; i++)
            {
                _bossBars[i].SetActive(i < bars);
                if (i < bars) _bossBars[i].SetInstant(1f);
                _bossPips[i].enabled = i < bars;
                _bossPips[i].color = HudStyle.BossRed;
            }
            _bossHeight = 74f + bars * 20f;
            _bossRoot.gameObject.SetActive(true);
            LayoutTargetFrame();
        }

        public void ClearBoss()
        {
            _boss = null;
            _bossHeight = 0f;
            if (_bossRoot != null) _bossRoot.gameObject.SetActive(false);
            LayoutTargetFrame();
        }

        void LayoutTargetFrame()
        {
            if (_targetRoot == null) return;
            float y = _bossRoot != null && _bossRoot.gameObject.activeSelf ? -(104f + _bossHeight + 12f) : -104f;
            _targetRoot.anchoredPosition = new Vector2(0f, y);
        }

        // ------------------------------------------------------------------ public API: minimap

        public void MarkStairs(Vector3 worldPos)
        {
            _stairsPos = worldPos;
            _hasStairs = true;
        }

        public void SetMinimap(DungeonLayout layout)
        {
            _layout = layout;
            if (layout == null) return;
            _explored = new bool[layout.Width, layout.Height];
            if (_mapTex != null) Destroy(_mapTex);
            _mapTex = new Texture2D(layout.Width, layout.Height, TextureFormat.RGBA32, false);
            _mapTex.filterMode = FilterMode.Point;
            _mapTex.wrapMode = TextureWrapMode.Clamp;
            _mapBase = new Color32[layout.Width * layout.Height];
            _mapImage.texture = _mapTex;
            ClearBoss();

            // Drop markers left over from an earlier floor, but keep the ones registered
            // while this floor was being built (same frame as this call).
            int frame = Time.frameCount;
            for (int i = _markers.Count - 1; i >= 0; i--)
                if (_markers[i].Frame != frame) _markers.RemoveAt(i);
            SyncMarkers();

            RedrawMap();
        }

        public void ClearMapMarkers()
        {
            _markers.Clear();
            SyncMarkers();
        }

        public void AddMapMarker(Vector3 worldPos, MapMarkerKind kind)
        {
            var m = new MapMarker();
            m.Pos = worldPos;
            m.Kind = kind;
            m.Frame = Time.frameCount;
            _markers.Add(m);
            SyncMarkers();
        }

        /// <summary>Removes the first marker within ~one room-cell of the position (a looted chest, an opened gate).</summary>
        public void RemoveMapMarkerNear(Vector3 worldPos, float radius = 3f)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _markers.Count; i++)
            {
                Vector3 d = _markers[i].Pos - worldPos;
                d.y = 0f;
                if (d.sqrMagnitude <= r2)
                {
                    _markers.RemoveAt(i);
                    SyncMarkers();
                    return;
                }
            }
        }

        void SyncMarkers()
        {
            if (_mapRect == null) return;
            while (_markerImages.Count < _markers.Count)
            {
                var img = Ui.MakeImg("Marker" + _markerImages.Count, _mapRect, Ui.MC, Ui.MC, Vector2.zero,
                                     new Vector2(13f, 13f), Color.white, Ui.Diamond);
                _markerImages.Add(img);
            }
            for (int i = 0; i < _markerImages.Count; i++)
            {
                bool on = i < _markers.Count;
                _markerImages[i].enabled = on;
                if (!on) continue;
                _markerImages[i].color = MarkerColor(_markers[i].Kind);
                float s = _markers[i].Kind == MapMarkerKind.Boss ? 17f : 13f;
                _markerImages[i].rectTransform.sizeDelta = new Vector2(s, s);
            }
        }

        static Color MarkerColor(MapMarkerKind kind)
        {
            switch (kind)
            {
                case MapMarkerKind.Chest: return new Color(1f, 0.82f, 0.32f, 1f);
                case MapMarkerKind.Shrine: return new Color(0.45f, 1f, 0.82f, 1f);
                case MapMarkerKind.Boss: return new Color(1f, 0.28f, 0.28f, 1f);
                default: return new Color(0.75f, 0.55f, 1f, 1f);
            }
        }

        bool MapPoint(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            if (_layout == null || _mapRect == null) return false;
            var c = DungeonBuilder.ToCell(world);
            if (!_layout.InBounds(c.X, c.Y)) return false;
            Vector2 size = _mapRect.sizeDelta;
            local = new Vector2(((c.X + 0.5f) / _layout.Width - 0.5f) * size.x,
                                ((c.Y + 0.5f) / _layout.Height - 0.5f) * size.y);
            return true;
        }

        // ------------------------------------------------------------------ public API: dialogue

        public void ShowDialogue(string speaker, string text, Color speakerColor, bool hasMore)
        {
            if (_dlgPanel == null) return;
            _dlgSpeaker.text = speaker == null ? "" : speaker;
            _dlgSpeaker.color = speakerColor;
            _dlgBody.text = text == null ? "" : text;
            _dlgHint.text = hasMore ? "[E] continue" : "[E] close";
            _dlgPanel.SetActive(true);
        }

        public void HideDialogue()
        {
            if (_dlgPanel != null) _dlgPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ public API: menu

        public void ShowMenu(string title, string help)
        {
            if (_menuPanel == null) return;
            _menuTitle.text = title == null ? "" : title;
            _menuHelp.text = help == null ? "" : help;
            _menuPanel.SetActive(true);
        }

        public void HideMenu()
        {
            if (_menuPanel != null) _menuPanel.SetActive(false);
        }

        public void SetMenu(List<MenuRow> rows, int cursor, string detail, string footer)
        {
            if (_menuPanel == null) return;
            int n = rows == null ? 0 : Mathf.Min(rows.Count, MaxMenuRows);
            _menuRowCount = n;
            for (int i = 0; i < MaxMenuRows; i++)
            {
                bool on = i < n;
                if (_menuRowBg[i].gameObject.activeSelf != on) _menuRowBg[i].gameObject.SetActive(on);
                if (!on) continue;

                var row = rows[i];
                bool sel = i == cursor;
                _menuRowBg[i].color = sel ? new Color(0.30f, 0.72f, 1f, 0.20f) : new Color(1f, 1f, 1f, 0.03f);
                Ui.FrameColor(_menuRowFrame[i], row.Border.a > 0.01f ? row.Border : (sel ? HudStyle.CyanSoft : Color.clear));

                _menuRowIcon[i].sprite = row.Icon;
                _menuRowIcon[i].enabled = row.Icon != null;

                string text = row.Text == null ? "" : row.Text;
                _menuRowText[i].text = sel ? "<b>" + text + "</b>" : text;
                _menuRowText[i].color = row.Dim ? HudStyle.Faded : (sel ? Color.white : HudStyle.Ink);
            }
            _menuDetail.text = detail == null ? "" : detail;
            _menuFooter.text = footer == null ? "" : footer;
        }

        public int MenuRowAt(Vector2 screenPos)
        {
            if (_menuPanel == null || !_menuPanel.activeSelf) return -1;
            for (int i = 0; i < _menuRowCount && i < MaxMenuRows; i++)
                if (Ui.Hit(_menuRowBg[i].rectTransform, screenPos)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ public API: inventory

        public bool AnyPanelOpen
        {
            get
            {
                return (_invPanel != null && _invPanel.activeSelf) ||
                       (_menuPanel != null && _menuPanel.activeSelf) ||
                       (_dlgPanel != null && _dlgPanel.activeSelf);
            }
        }

        public void ShowInventory(bool show)
        {
            if (_invPanel != null) _invPanel.SetActive(show);
        }

        public int InventoryRowAt(Vector2 screenPos)
        {
            if (_invPanel == null || !_invPanel.activeSelf || _invCells == null) return -1;
            int n = Mathf.Min(_bagCount, _invCells.Length);
            for (int i = 0; i < n; i++)
                if (_invCells[i].Contains(screenPos)) return i;
            return -1;
        }

        public int InventoryGearSlotAt(Vector2 screenPos)
        {
            if (_invPanel == null || !_invPanel.activeSelf || _gearCells == null) return -1;
            for (int i = 0; i < _gearCells.Length; i++)
                if (_gearCells[i].Contains(screenPos)) return i;
            return -1;
        }

        public void RefreshInventory(PlayerController p, int cursor)
        {
            if (p == null || _invPanel == null) return;

            // --- equipment doll ---
            for (int i = 0; i < 3; i++)
            {
                var it = p.Gear.Slots[i];
                if (it != null)
                {
                    _gearCells[i].SetIcon(ItemIcons.Get(it));
                    _gearCells[i].SetIconColor(Color.white);
                    _gearCells[i].SetBorder(ItemIcons.RarityColor(it.Rarity));
                    _gearNames[i].text = "<color=" + ItemUi.Hex(it.Rarity) + ">" + it.DisplayName + "</color>";
                }
                else
                {
                    _gearCells[i].SetIcon(ItemIcons.SlotSilhouette((ItemSlot)i));
                    _gearCells[i].SetIconColor(new Color(1f, 1f, 1f, 0.30f));
                    _gearCells[i].SetBorder(new Color(1f, 1f, 1f, 0.12f));
                    _gearNames[i].text = "<color=#5A6773>empty</color>";
                }
            }

            // --- totals ---
            var wd = WeaponCatalog.Get(p.CurrentWeapon);
            _invStats.text =
                Label("LEVEL") + "  " + p.Level + "        " + Label("XP") + "  " + p.Xp + " / " + Progression.XpForNextLevel(p.Level) + "\n" +
                Label("HP") + "  " + p.Hp + " / " + p.MaxHp + "\n" +
                Label("ATK") + "  " + p.Attack + "        " + Label("DEF") + "  " + p.Defense + "        " +
                Label("CRIT") + "  " + Mathf.RoundToInt(p.CritChance * 100f) + "%\n\n" +
                "<b>" + wd.Name + "</b>\n" +
                "<color=" + HudStyle.MutedHex + ">" + wd.Blurb + "</color>\n\n" +
                "<color=" + HudStyle.GoldHex + ">GOLD  " + p.Gold + "</color>        <color=#FF8C4D>SHARDS  " + p.Shards + "</color>";

            // --- backpack grid ---
            var items = p.Bag.Items;
            _bagCount = items.Count;
            _invCount.text = "BACKPACK   " + items.Count + " / " + Inventory.Capacity;
            for (int i = 0; i < _invCells.Length; i++)
            {
                bool has = i < items.Count;
                var cell = _invCells[i];
                if (has)
                {
                    var it = items[i];
                    cell.SetIcon(ItemIcons.Get(it));
                    cell.SetIconColor(Color.white);
                    cell.SetBorder(ItemIcons.RarityColor(it.Rarity));
                    cell.SetBackground(new Color(0.06f, 0.09f, 0.14f, 0.88f));
                }
                else
                {
                    cell.SetIcon(null);
                    cell.SetBorder(new Color(1f, 1f, 1f, 0.06f));
                    cell.SetBackground(new Color(0.03f, 0.05f, 0.08f, 0.55f));
                }
                cell.SetSelected(has && i == cursor, 0f);
            }

            // --- detail + comparison ---
            Item sel = (cursor >= 0 && cursor < items.Count) ? items[cursor] : null;
            Item worn = sel != null ? p.Gear.Get(sel.Slot) : null;
            FillDetail(sel, worn);
            FillCompare(sel, worn);
        }

        static string Label(string s) { return "<color=" + HudStyle.CyanHex + ">" + s + "</color>"; }

        void FillDetail(Item sel, Item worn)
        {
            if (sel == null)
            {
                _detailCell.SetIcon(null);
                _detailCell.SetBorder(new Color(1f, 1f, 1f, 0.08f));
                _detailName.text = "<color=#5A6773>Nothing selected</color>";
                _detailMeta.text = "";
                _detailStats.text = "";
                _detailCompare.text = "";
                _detailHint.text = "<color=" + HudStyle.MutedHex + ">Defeat enemies or open chests to find gear.</color>";
                return;
            }

            _detailCell.SetIcon(ItemIcons.Get(sel));
            _detailCell.SetIconColor(Color.white);
            _detailCell.SetBorder(ItemIcons.RarityColor(sel.Rarity));
            _detailName.text = "<color=" + ItemUi.Hex(sel.Rarity) + ">" + sel.DisplayName + "</color>";
            _detailMeta.text = "<color=" + ItemUi.Hex(sel.Rarity) + ">" + sel.Rarity + "</color>" +
                               "<color=" + HudStyle.MutedHex + ">   " + SlotName(sel.Slot) +
                               "   item lv " + sel.ItemLevel + "</color>";
            string stats = ItemUi.Stats(sel);
            _detailStats.text = stats.Length > 0 ? stats : "<color=" + HudStyle.MutedHex + ">no bonuses</color>";

            float delta = sel.Score - (worn != null ? worn.Score : 0f);
            int d = Mathf.RoundToInt(delta);
            if (worn == null)
                _detailCompare.text = "power  <color=" + HudStyle.GoodHex + ">+" + Mathf.RoundToInt(sel.Score) + "</color>   (slot is empty)";
            else if (d >= 0)
                _detailCompare.text = "power  <color=" + HudStyle.GoodHex + ">+" + d + "</color>   vs equipped";
            else
                _detailCompare.text = "power  <color=" + HudStyle.BadHex + ">" + d + "</color>   vs equipped";

            if (sel.Slot == ItemSlot.Weapon)
            {
                var wd = WeaponCatalog.Get(sel.WType);
                _detailHint.text = "<b>" + wd.Name + "</b>   <color=" + HudStyle.MutedHex + ">" + wd.Blurb + "</color>";
            }
            else _detailHint.text = "<color=" + HudStyle.MutedHex + ">Enter / click equips, X / right click discards.</color>";
        }

        void FillCompare(Item sel, Item worn)
        {
            _compareTitle.text = sel == null ? "EQUIPPED" : "EQUIPPED  -  " + SlotName(sel.Slot).ToUpper();
            if (worn == null)
            {
                _compareCell.SetIcon(sel != null ? ItemIcons.SlotSilhouette(sel.Slot) : null);
                _compareCell.SetIconColor(new Color(1f, 1f, 1f, 0.30f));
                _compareCell.SetBorder(new Color(1f, 1f, 1f, 0.10f));
                _compareName.text = "<color=#5A6773>nothing equipped</color>";
                _compareStats.text = "";
                return;
            }
            _compareCell.SetIcon(ItemIcons.Get(worn));
            _compareCell.SetIconColor(Color.white);
            _compareCell.SetBorder(ItemIcons.RarityColor(worn.Rarity));
            _compareName.text = "<color=" + ItemUi.Hex(worn.Rarity) + ">" + worn.DisplayName + "</color>";
            string s = ItemUi.Stats(worn);
            _compareStats.text = "<color=" + HudStyle.MutedHex + ">" + (s.Length > 0 ? s.Replace("  ", "\n") : "no bonuses") + "</color>";
        }

        static string SlotName(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: return "Weapon";
                case ItemSlot.Armor: return "Armor";
                default: return "Trinket";
            }
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            var g = Game.Instance;
            var p = g != null ? g.Player : null;
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 5f);

            UpdatePlayerFrame(p, dt, pulse);
            UpdateSkillBar(p, pulse);
            UpdateConsumables(p, pulse);
            UpdateCompanion(g, pulse);
            UpdateBoss(dt, pulse);
            UpdateTarget(p, dt);
            UpdateMessages(now, dt);
            UpdateInventoryPulse(pulse);
            UpdateMap(dt);
        }

        void UpdatePlayerFrame(PlayerController p, float dt, float pulse)
        {
            if (p == null)
            {
                if (_playerFrame.gameObject.activeSelf) _playerFrame.gameObject.SetActive(false);
                return;
            }
            if (!_playerFrame.gameObject.activeSelf) _playerFrame.gameObject.SetActive(true);

            int maxHp = Mathf.Max(1, p.MaxHp);
            float hp01 = Mathf.Clamp01(p.Hp / (float)maxHp);
            _hpBar.Set(hp01);
            _hpBar.Tick(dt);
            if (p.Hp != _cHp || p.MaxHp != _cMaxHp)
            {
                _cHp = p.Hp;
                _cMaxHp = p.MaxHp;
                _hpBar.SetLabel(p.Hp + " / " + p.MaxHp);
            }
            Color hpColor = Color.Lerp(HudStyle.HpLow, HudStyle.HpHigh, Mathf.Clamp01(hp01 * 1.7f));
            if (hp01 < 0.3f) hpColor = Color.Lerp(hpColor, new Color(1f, 0.75f, 0.70f, 1f), pulse * 0.45f);
            _hpBar.SetFillColor(hpColor);

            int next = Progression.XpForNextLevel(p.Level);
            _xpBar.Set(next > 0 ? Mathf.Clamp01(p.Xp / (float)next) : 1f);
            _xpBar.Tick(dt);

            if (p.Level != _cLevel)
            {
                _cLevel = p.Level;
                _levelText.text = p.Level.ToString();
                _cXp = -1;
            }
            if (p.Xp != _cXp) _cXp = p.Xp;

            string hero = p.HeroName;
            if (hero == null) hero = "Wanderer";
            if (hero != _cName)
            {
                _cName = hero;
                _nameText.text = hero;
            }

            if (p.CurrentWeapon != _cWeapon)
            {
                _cWeapon = p.CurrentWeapon;
                _weaponText.text = WeaponCatalog.Get(p.CurrentWeapon).Name;
            }

            int crit = Mathf.RoundToInt(p.CritChance * 100f);
            if (p.Attack != _cAtk || p.Defense != _cDef || crit != _cCrit)
            {
                _cAtk = p.Attack;
                _cDef = p.Defense;
                _cCrit = crit;
                _statText.text = "ATK " + p.Attack + "     DEF " + p.Defense + "     CRIT " + crit + "%";
            }
            if (p.Gold != _cGold)
            {
                _cGold = p.Gold;
                _goldText.text = p.Gold.ToString();
            }
            if (p.Shards != _cShards)
            {
                _cShards = p.Shards;
                _shardText.text = p.Shards.ToString();
            }

            _badgeRing.color = new Color(0.42f, 0.88f, 1f, 0.55f + 0.35f * pulse);
        }

        void EnsureSkillSlots(int count)
        {
            if (_skillSlots != null && _skillSlots.Length == count) return;
            if (_skillSlots != null)
                for (int i = 0; i < _skillSlots.Length; i++)
                    if (_skillSlots[i] != null) Destroy(_skillSlots[i].Root.gameObject);

            _skillSlots = new SkillSlotWidget[count];
            _skillIds = new string[count];
            _skillWeapon = (WeaponType)(-1);
            if (count <= 0) return;

            const float size = 88f, gap = 12f;
            float total = count * size + (count - 1) * gap;
            for (int i = 0; i < count; i++)
            {
                float x = -total * 0.5f + size * 0.5f + i * (size + gap);
                _skillSlots[i] = new SkillSlotWidget(_skillRoot, new Vector2(x, 26f), size, i + 1);
            }
        }

        void UpdateSkillBar(PlayerController p, float pulse)
        {
            if (p == null || p.Skills == null || p.Skills.Length == 0)
            {
                if (_skillRoot.gameObject.activeSelf) _skillRoot.gameObject.SetActive(false);
                return;
            }
            if (!_skillRoot.gameObject.activeSelf) _skillRoot.gameObject.SetActive(true);

            EnsureSkillSlots(p.Skills.Length);
            bool weaponChanged = p.CurrentWeapon != _skillWeapon;

            for (int i = 0; i < _skillSlots.Length; i++)
            {
                var def = p.Skills[i];
                var slot = _skillSlots[i];
                if (def == null)
                {
                    slot.SetActive(false);
                    continue;
                }
                slot.SetActive(true);

                if (weaponChanged || _skillIds[i] != def.Id)
                {
                    _skillIds[i] = def.Id;
                    slot.SetIcon(ItemIcons.SkillIcon(def, p.CurrentWeapon));
                    slot.SetName(def.Name);
                }

                bool locked = p.Level < def.UnlockLevel;
                slot.SetLocked(locked, def.UnlockLevel);

                float norm = 0f, remaining = 0f;
                if (!locked && p.SkillCd != null && i < p.SkillCd.Length && p.SkillCd[i] != null)
                {
                    norm = p.SkillCd[i].Normalized;
                    remaining = p.SkillCd[i].Remaining;
                }
                slot.SetCooldown(locked ? 1f : norm, locked ? 0f : remaining);
                slot.SetMastery(p.SkillMasteryLevel(i));
                slot.SetGlow(!locked && norm <= 0f ? 0.10f + 0.10f * pulse : 0f);
            }
            _skillWeapon = p.CurrentWeapon;
        }

        void UpdateConsumables(PlayerController p, float pulse)
        {
            if (p == null || p.Supplies == null) return;
            var s = p.Supplies;
            if (s.Potions != _cPotion)
            {
                _cPotion = s.Potions;
                _potionCount.text = s.Potions.ToString();
                _potionCount.color = s.Potions > 0 ? Color.white : HudStyle.Faded;
                _potionCell.SetIconColor(s.Potions > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }
            if (s.ReturnCrystals != _cCrystal)
            {
                _cCrystal = s.ReturnCrystals;
                _crystalCount.text = s.ReturnCrystals.ToString();
                _crystalCount.color = s.ReturnCrystals > 0 ? Color.white : HudStyle.Faded;
                _crystalCell.SetIconColor(s.ReturnCrystals > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }
            if (s.ReviveTokens != _cRevive)
            {
                _cRevive = s.ReviveTokens;
                _reviveCount.text = s.ReviveTokens.ToString();
                _reviveCount.color = s.ReviveTokens > 0 ? Color.white : HudStyle.Faded;
                _reviveCell.SetIconColor(s.ReviveTokens > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }
            _potionCdFill.fillAmount = Mathf.Clamp01(p.PotionCooldownNormalized);
            _potionCell.SetSelected(s.Potions > 0 && p.PotionCooldownNormalized <= 0f && p.Hp < p.MaxHp * 0.5f, pulse);
        }

        void UpdateCompanion(Game g, float pulse)
        {
            var c = g != null ? g.Companion : null;
            if (c == null)
            {
                if (_compPanel.activeSelf) _compPanel.SetActive(false);
                return;
            }
            if (!_compPanel.activeSelf) _compPanel.SetActive(true);

            string n = c.Name;
            if (n == null) n = "Companion";
            n = n.ToUpper();
            if (_compName.text != n) _compName.text = n;

            bool window = c.SwitchWindowOpen && c.SwitchReady;
            if (window)
            {
                if (_compState.text != "SWITCH!  [G]") _compState.text = "SWITCH!  [G]";
                _compState.color = Color.Lerp(new Color(1f, 0.85f, 0.35f, 1f), Color.white, pulse);
                Ui.FrameColor(_compFrame, Color.Lerp(new Color(1f, 0.75f, 0.25f, 0.35f), new Color(1f, 0.9f, 0.5f, 0.95f), pulse));
                _compBar.Set(1f);
                _compBar.SetFillColor(new Color(1f, 0.82f, 0.35f, 1f));
            }
            else if (c.SwitchReady)
            {
                if (_compState.text != "ready  [G]") _compState.text = "ready  [G]";
                _compState.color = HudStyle.CyanSoft;
                Ui.FrameColor(_compFrame, HudStyle.CyanFaint);
                _compBar.Set(1f);
                _compBar.SetFillColor(HudStyle.Cyan);
            }
            else
            {
                if (_compState.text != "recovering") _compState.text = "recovering";
                _compState.color = HudStyle.Faded;
                Ui.FrameColor(_compFrame, HudStyle.CyanFaint);
                _compBar.Set(1f - Mathf.Clamp01(c.SwitchCooldownNormalized));
                _compBar.SetFillColor(new Color(0.35f, 0.55f, 0.70f, 1f));
            }
            _compBar.Tick(Time.unscaledDeltaTime);
        }

        void UpdateBoss(float dt, float pulse)
        {
            if (_boss == null) return;
            if (_boss.IsDead) { ClearBoss(); return; }

            int bars = Mathf.Clamp(_boss.BossBars, 1, MaxBossBars);
            int maxHp = Mathf.Max(1, _boss.MaxHp);
            int index = BossInfo.BarIndex(_boss.Hp, maxHp, bars);
            float fill = BossInfo.BarFill(_boss.Hp, maxHp, bars);

            for (int i = 0; i < bars; i++)
            {
                float v = i < index ? 0f : (i == index ? fill : 1f);
                _bossBars[i].Set(v);
                _bossBars[i].Tick(dt);
                _bossBars[i].SetFillColor(i == index
                    ? Color.Lerp(HudStyle.BossRed, new Color(1f, 0.55f, 0.35f, 1f), pulse * 0.35f)
                    : new Color(0.62f, 0.16f, 0.16f, 1f));
                _bossPips[i].color = i < index ? new Color(0.35f, 0.14f, 0.14f, 1f) : HudStyle.BossRed;
            }
        }

        void UpdateTarget(PlayerController p, float dt)
        {
            var t = p != null ? p.LockTarget : null;
            if (t == null || t.IsDead || (_boss != null && t == _boss))
            {
                if (_targetRoot.gameObject.activeSelf) _targetRoot.gameObject.SetActive(false);
                _lastTarget = null;
                return;
            }
            if (!_targetRoot.gameObject.activeSelf) _targetRoot.gameObject.SetActive(true);

            if (t != _lastTarget)
            {
                _lastTarget = t;
                _cTargetHp = -1;
                string label = t.DisplayName == null ? "" : t.DisplayName;
                _targetName.text = t.IsElite
                    ? "<color=#FFB840>[ELITE]</color>  " + label
                    : label;
                _targetBar.SetInstant(Mathf.Clamp01(t.Hp / (float)Mathf.Max(1, t.MaxHp)));
            }
            if (t.Hp != _cTargetHp) _cTargetHp = t.Hp;
            _targetBar.Set(Mathf.Clamp01(t.Hp / (float)Mathf.Max(1, t.MaxHp)));
            _targetBar.Tick(dt);
        }

        void UpdateMessages(float now, float dt)
        {
            bool panel = AnyPanelOpen;

            // toast: fade in, hold, fade out
            if (_toastGroup.gameObject.activeSelf)
            {
                if (now >= _toastUntil) _toastGroup.gameObject.SetActive(false);
                else
                {
                    float inA = Mathf.Clamp01((now - _toastStart) / 0.18f);
                    float outA = Mathf.Clamp01((_toastUntil - now) / 0.45f);
                    _toastGroup.alpha = Mathf.Min(inA, outA);
                    _toastGroup.transform.localScale = Vector3.one * (0.97f + 0.03f * inA);
                }
            }

            // banner
            if (_bannerRoot.gameObject.activeSelf)
            {
                if (now >= _bannerUntil) _bannerRoot.gameObject.SetActive(false);
                else
                {
                    float inA = Mathf.Clamp01((now - _bannerStart) / 0.30f);
                    float outA = Mathf.Clamp01((_bannerUntil - now) / 0.55f);
                    _bannerGroup.alpha = Mathf.Min(inA, outA);
                    _bannerRoot.localScale = Vector3.one * (0.94f + 0.06f * inA);
                }
            }

            // prompt
            bool showPrompt = !panel && now - _promptTime < 0.15f && _promptText.text.Length > 0;
            if (_promptText.gameObject.activeSelf != showPrompt)
            {
                _promptText.gameObject.SetActive(showPrompt);
                _promptBg.gameObject.SetActive(showPrompt);
            }

            // starting control hints
            bool showHint = now < _hintUntil && !panel;
            if (_hintText.gameObject.activeSelf != showHint) _hintText.gameObject.SetActive(showHint);

            // damage vignette
            if (_flashAmount > 0.001f)
            {
                _flashAmount = Mathf.MoveTowards(_flashAmount, 0f, dt * 1.5f);
                if (!_flash.enabled) _flash.enabled = true;
                _flash.color = new Color(1f, 0.12f, 0.12f, _flashAmount);
            }
            else if (_flash.enabled)
            {
                _flashAmount = 0f;
                _flash.enabled = false;
            }
        }

        void UpdateInventoryPulse(float pulse)
        {
            if (_invPanel == null || !_invPanel.activeSelf || _invCells == null) return;
            int n = Mathf.Min(_bagCount, _invCells.Length);
            for (int i = 0; i < n; i++)
                if (_invCells[i].Selected) _invCells[i].SetSelected(true, pulse);
        }

        // ------------------------------------------------------------------ minimap

        void UpdateMap(float dt)
        {
            if (_layout == null || _mapTex == null) return;

            var g = Game.Instance;
            var p = g != null ? g.Player : null;

            // markers + player arrow follow every frame (cheap, a handful of rects)
            Vector2 local;
            for (int i = 0; i < _markers.Count && i < _markerImages.Count; i++)
            {
                bool ok = MapPoint(_markers[i].Pos, out local);
                _markerImages[i].enabled = ok;
                if (ok) _markerImages[i].rectTransform.anchoredPosition = local;
            }
            if (_hasStairs && MapPoint(_stairsPos, out local))
            {
                _stairMarker.enabled = true;
                _stairMarker.rectTransform.anchoredPosition = local;
            }
            else _stairMarker.enabled = false;

            if (p != null && MapPoint(p.transform.position, out local))
            {
                _playerArrow.enabled = true;
                _playerArrow.rectTransform.anchoredPosition = local;
                _playerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -p.transform.eulerAngles.y);
            }
            else _playerArrow.enabled = false;

            _mapTimer -= dt;
            if (_mapTimer > 0f) return;
            _mapTimer = 0.1f;
            RedrawMap();
        }

        void RedrawMap()
        {
            if (_layout == null || _mapTex == null || _mapBase == null) return;
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

            var wallCol = new Color32(8, 12, 18, 210);
            var unseen = new Color32(0, 0, 0, 90);
            var floorCol = new Color32(74, 168, 200, 255);
            for (int y = 0; y < _layout.Height; y++)
                for (int x = 0; x < _layout.Width; x++)
                {
                    Color32 col = unseen;
                    if (_explored[x, y]) col = _layout.IsFloor(x, y) ? floorCol : wallCol;
                    _mapBase[y * _layout.Width + x] = col;
                }

            if (p != null)
            {
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.IsDead) continue;
                    var ec = DungeonBuilder.ToCell(e.transform.position);
                    if (_layout.InBounds(ec.X, ec.Y) && _explored[ec.X, ec.Y])
                        Dot(e.transform.position, e.Def != null && e.Def.IsBoss ? new Color32(255, 60, 60, 255)
                                                                                : new Color32(235, 110, 110, 255),
                            e.Def != null && e.Def.IsBoss ? 1 : 0);
                }
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
