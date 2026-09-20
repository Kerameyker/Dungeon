using System.Collections;
using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Entry point. Builds the whole game at runtime (camera, lighting, player, HUD, dungeon floors),
    /// so the scene can be completely empty. Split in three files: Game.cs (flow, floors, events),
    /// GameMenus.cs (inventory, shop, blacksmith, quest board, dialogue) and GameStory.cs (story, save, run reset).
    /// </summary>
    public partial class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }

        public int Floor { get; private set; }
        public PlayerController Player { get; private set; }
        public CameraRig Rig { get; private set; }
        public DungeonLayout Layout { get; private set; }
        public Pathfinder Pathfinder { get; private set; }
        public Companion Companion { get; private set; }
        public QuestLog Quests { get; private set; }
        public Bestiary Bestiary { get; private set; }
        public StoryFlags Flags { get; private set; }
        public bool Hardcore { get; private set; }

        public int MaxFloor { get; private set; }
        public bool InTown { get; private set; }
        public bool Paused { get { return _mode != UiMode.None || _toggleFrame == Time.frameCount; } }
        public bool InventoryToggledThisFrame { get { return _toggleFrame == Time.frameCount; } }
        public string BiomeName { get { return InTown ? "Hollow Haven" : BiomeCatalog.ForFloor(Floor).Name; } }
        public bool BossSealed { get { return _sealRoot != null; } }

        int _baseSeed;
        GameObject _dungeonRoot;
        GameObject _sealRoot;
        Stairs _stairs;
        bool _bossDead;
        Enemy _bossEngaged;
        Vector3 _bossPos;
        float _returnT = -1f;
        Camera _cam;
        static bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _booted = false; Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!_booted)
            {
                _booted = true;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (Instance == null) new GameObject("Game").AddComponent<Game>(); };
            }
            if (Instance != null) return;
            new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _baseSeed = Random.Range(1, 100000);
            Quests = new QuestLog();
            Bestiary = new Bestiary();
            Flags = new StoryFlags();

            SetupEnvironment();
            _cam = SetupCamera();
            Rig = _cam.gameObject.AddComponent<CameraRig>();
            PostFx.Setup(_cam);

            Hud.Create();
            Player = PlayerController.Create(Rig);
            Rig.Target = Player.transform;

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = 0f;
            _music.spatialBlend = 0f;

            MaxFloor = 1;
            var save = _freshRun ? null : SaveSystem.Load();
            _freshRun = false;
            if (save != null) LoadSave(save);
            GoTown();
        }

        void OnApplicationQuit() { Save(); }

        void OnDestroy()
        {
            if (Instance == this) { Instance = null; Enemy.All.Clear(); Time.timeScale = 1f; }
        }

        // ------------------------------------------------------------------ environment

        static void SetupEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.skybox = null;
            ApplyTownEnvironment();

            // The dungeon is lit only by torches, so switch off any scene directional light.
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;
        }

        static void ApplyTownEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.74f);
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.08f, 0.09f, 0.14f);
        }

        void ApplyBiome(int floor)
        {
            if (floor <= 0) { ApplyTownEnvironment(); if (_cam != null) _cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f); return; }
            var b = BiomeCatalog.ForFloor(floor);
            RenderSettings.ambientLight = new Color(b.Ambient[0], b.Ambient[1], b.Ambient[2]);
            RenderSettings.fogColor = new Color(b.Fog[0], b.Fog[1], b.Fog[2]);
            RenderSettings.fogDensity = b.FogDensity;
            if (_cam != null) _cam.backgroundColor = new Color(b.Fog[0] * 0.4f, b.Fog[1] * 0.4f, b.Fog[2] * 0.4f);
        }

        static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 150f;
            return cam;
        }

        // ------------------------------------------------------------------ music

        AudioSource _music;
        AudioClip _musicWant;
        const float MusicVolume = 0.32f;

        void SetMusic(AudioClip clip) { _musicWant = clip; }

        void UpdateMusic()
        {
            if (_music == null || _musicWant == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_music.clip != _musicWant)
            {
                _music.volume = Mathf.MoveTowards(_music.volume, 0f, dt * 0.9f);
                if (_music.volume <= 0.01f || !_music.isPlaying)
                {
                    _music.clip = _musicWant;
                    _music.Play();
                }
            }
            else _music.volume = Mathf.MoveTowards(_music.volume, MusicVolume, dt * 0.45f);
        }

        // ------------------------------------------------------------------ town / floors

        public void GoTown()
        {
            Floor = 0;
            InTown = true;
            _bossDead = false;
            _bossEngaged = null;
            _returnT = -1f;
            CloseUi();
            ClearFloor();

            Layout = Town.CreateLayout();
            Pathfinder = new Pathfinder(Layout);
            _dungeonRoot = DungeonBuilder.Build(Layout, 0, true);
            Town.Populate(_dungeonRoot.transform);

            Player.Respawn(Town.Center + Vector3.up * 0.1f);
            Rig.Target = Player.transform;
            EnsureCompanion();
            ApplyBiome(0);
            SetMusic(Sfx.TownLoop());

            var hud = Hud.Instance;
            hud.SetFloor(0);
            hud.SetBiome("");
            hud.SetMinimap(Layout);
            hud.ClearMapMarkers();
            hud.AddMapMarker(Town.GatePosition, Hud.MapMarkerKind.Gate);
            hud.ShowDeath(false);
            hud.ShowBanner("HOLLOW HAVEN", Hardcore ? "Safe zone   -   HARDCORE" : "Safe zone", 2.5f);
            Save();
            TryStory(StoryTrigger.TownEnter);
            if (Flags.Has("ending") && !Flags.Has("ending_done")) WhenFree(OpenChoice);
        }

        public void EnterTower(int floor)
        {
            StartFloor(Mathf.Clamp(floor, 1, MaxFloor));
        }

        public void StartFloor(int floor)
        {
            Floor = floor;
            InTown = false;
            MaxFloor = Mathf.Max(MaxFloor, floor);
            _bossDead = false;
            _bossEngaged = null;
            _returnT = -1f;
            CloseUi();
            ClearFloor();

            int seed = _baseSeed + floor * 7919;
            Layout = DungeonGenerator.Generate(seed);
            Pathfinder = new Pathfinder(Layout);
            _dungeonRoot = DungeonBuilder.Build(Layout, floor);

            var startRoom = Layout.Rooms[Layout.StartRoom];
            Player.Respawn(DungeonBuilder.ToWorld(startRoom.CenterX, startRoom.CenterY) + Vector3.up * 0.1f);
            Rig.Target = Player.transform;
            EnsureCompanion();
            ApplyBiome(floor);
            SetMusic(Sfx.TowerLoop());
            Quests.OnFloorReached(floor);

            var hud = Hud.Instance;
            hud.SetFloor(floor);
            hud.SetBiome(BiomeName);
            hud.SetMinimap(Layout);
            hud.ClearMapMarkers();
            hud.ShowDeath(false);

            SpawnEnemies(floor, seed);

            bool first = (floor - 1) % BiomeCatalog.FloorsPerBiome == 0;
            string sub = BiomeName;
            if (BossInfo.IsMilestone(floor)) sub += "   -   Guardian floor";
            hud.ShowBanner("FLOOR " + floor, first ? "Entering: " + sub : sub, 2.6f);
            Save();
            TryStory(StoryTrigger.FloorEnter);
        }

        void ClearFloor()
        {
            var snapshot = new List<Enemy>(Enemy.All);
            foreach (var e in snapshot) if (e != null) Destroy(e.gameObject);
            Enemy.All.Clear();
            if (_dungeonRoot != null) Destroy(_dungeonRoot);
            if (_sealRoot != null) Destroy(_sealRoot);
            _sealRoot = null;
            if (_stairs != null) Destroy(_stairs.gameObject);
            _stairs = null;
            foreach (var o in FindObjectsByType<HpOrb>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            foreach (var o in FindObjectsByType<LootDrop>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            foreach (var o in FindObjectsByType<Chest>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            foreach (var o in FindObjectsByType<Shrine>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            foreach (var o in FindObjectsByType<EnemyBolt>(FindObjectsSortMode.None)) Destroy(o.gameObject);
        }

        void SpawnEnemies(int floor, int seed)
        {
            var rng = new System.Random(seed + 1);
            var rooms = new List<int>();
            for (int i = 0; i < Layout.Rooms.Count; i++)
                if (i != Layout.StartRoom && i != Layout.BossRoom) rooms.Add(i);

            int count = Progression.EnemyCount(floor);
            for (int n = 0; n < count && rooms.Count > 0; n++)
            {
                var room = Layout.Rooms[rooms[rng.Next(rooms.Count)]];
                int x = room.X + 1 + rng.Next(Mathf.Max(1, room.W - 2));
                int y = room.Y + 1 + rng.Next(Mathf.Max(1, room.H - 2));
                if ((x == room.CenterX && y == room.CenterY) || (x == room.X + 1 && y == room.Y + 1)) { n--; if (n < -50) break; continue; }
                var kind = EnemyCatalog.PickForFloor(floor, rng.NextDouble());
                bool elite = floor >= 2 && rng.NextDouble() < 0.10;
                Enemy.Create(kind, floor, DungeonBuilder.ToWorld(x, y) + Vector3.up * 0.05f, elite);
            }

            var boss = Layout.Rooms[Layout.BossRoom];
            _bossPos = DungeonBuilder.ToWorld(boss.CenterX, boss.CenterY);
            Enemy.Create(EnemyCatalog.BossForFloor(floor), floor, _bossPos + Vector3.up * 0.05f);
            Hud.Instance.AddMapMarker(_bossPos, Hud.MapMarkerKind.Boss);

            SpawnFeatures(floor, seed, rooms);
        }

        /// <summary>Treasure chests (up to 3) and one healing shrine per floor, in side rooms.</summary>
        void SpawnFeatures(int floor, int seed, List<int> sideRooms)
        {
            var rng = new System.Random(seed + 2);
            if (sideRooms.Count == 0) return;
            int shrineRoom = sideRooms[rng.Next(sideRooms.Count)];
            var sr = Layout.Rooms[shrineRoom];
            var shrinePos = DungeonBuilder.ToWorld(sr.CenterX, sr.CenterY);
            Shrine.Spawn(shrinePos);
            Hud.Instance.AddMapMarker(shrinePos, Hud.MapMarkerKind.Shrine);

            int chests = 0;
            foreach (int idx in sideRooms)
            {
                if (chests >= 3) break;
                if (idx == shrineRoom || rng.NextDouble() > 0.4) continue;
                var r = Layout.Rooms[idx];
                Vector3 pos = DungeonBuilder.ToWorld(r.X + 1, r.Y + 1);
                Vector3 toCenter = DungeonBuilder.ToWorld(r.CenterX, r.CenterY) - pos;
                Chest.Spawn(pos + Vector3.up * 0.02f, Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg, floor, rng.Next());
                Hud.Instance.AddMapMarker(pos, Hud.MapMarkerKind.Chest);
                chests++;
            }
        }

        public void NextFloor()
        {
            Player.Heal(Mathf.RoundToInt(Player.MaxHp * 0.3f));
            StartFloor(Floor + 1);
        }

        void EnsureCompanion()
        {
            if (!Flags.Has("meet_miri") || Player == null) return;
            Vector3 pos = Player.transform.position - Player.transform.forward * 1.6f;
            if (Companion == null) Companion = Companion.Create(pos);
            else Companion.Teleport(pos);
        }

        // ------------------------------------------------------------------ boss rooms

        public void OnBossEngaged(Enemy e)
        {
            if (e == null || _bossEngaged == e || _bossDead) return;
            _bossEngaged = e;
            Hud.Instance.SetBoss(e);
            Hud.Instance.ShowBanner(e.DisplayName, string.IsNullOrEmpty(e.BossTitle) ? "Floor " + Floor + " guardian" : e.BossTitle, 2.6f);
            Sfx.Play2D(SfxKind.BossRoar);
            SetMusic(e.Def.Kind == EnemyKind.Architect ? Sfx.FinalBossLoop() : Sfx.BossLoop());
            if (Bestiary.IsScouted(e.Def.Kind)) Hud.Instance.Toast("Studied foe:  +10% damage", 2f);
        }

        bool PlayerInBossRoom()
        {
            if (Layout == null || Layout.BossRoom < 0 || Player == null) return false;
            var r = Layout.Rooms[Layout.BossRoom];
            var c = DungeonBuilder.ToCell(Player.transform.position);
            return c.X >= r.X && c.X < r.X + r.W && c.Y >= r.Y && c.Y < r.Y + r.H;
        }

        void UpdateBossSeal()
        {
            if (_bossEngaged == null || _bossDead || _sealRoot != null || Player.IsDead) return;
            if (_bossEngaged.IsDead) return;
            if (PlayerInBossRoom()) SealBossRoom();
        }

        void SealBossRoom()
        {
            var r = Layout.Rooms[Layout.BossRoom];
            _sealRoot = new GameObject("BossSeal");
            if (_dungeonRoot != null) _sealRoot.transform.SetParent(_dungeonRoot.transform, false);
            var mat = ProcAssets.Unlit(new Color(0.9f, 0.12f, 0.12f));
            int placed = 0;
            for (int x = r.X - 1; x <= r.X + r.W; x++)
            {
                for (int y = r.Y - 1; y <= r.Y + r.H; y++)
                {
                    bool ring = x == r.X - 1 || x == r.X + r.W || y == r.Y - 1 || y == r.Y + r.H;
                    if (!ring || !Layout.IsFloor(x, y)) continue;
                    Vector3 c = DungeonBuilder.ToWorld(x, y);
                    ProcAssets.Prim(PrimitiveType.Cube, _sealRoot.transform, c + Vector3.up * 2.5f,
                                    new Vector3(DungeonBuilder.CellSize * 0.9f, 5f, DungeonBuilder.CellSize * 0.9f), mat, true, "SealDoor");
                    Spark.Burst(c + Vector3.up * 1.5f, new Color(1f, 0.25f, 0.2f), 10, 3f, 0.12f, 0.6f);
                    placed++;
                }
            }
            if (placed > 0)
            {
                if (Companion != null) Companion.Teleport(Player.transform.position - Player.transform.forward * 1.2f);
                Sfx.Play2D(SfxKind.Door);
                Hud.Instance.Toast("THE DOORS SEAL\nDefeat the guardian to leave", 3f);
            }
            else { Destroy(_sealRoot); _sealRoot = null; }
        }

        void OpenDoors()
        {
            if (_sealRoot == null) return;
            Destroy(_sealRoot);
            _sealRoot = null;
            Sfx.Play2D(SfxKind.Door);
        }

        // ------------------------------------------------------------------ events

        public void OnEnemyKilled(Enemy e)
        {
            Bestiary.RecordKill(e.Def.Kind);
            Quests.OnKill(e.Def.Kind, e.IsElite, e.Def.IsBoss);
            if (!e.Def.IsBoss || _bossDead) return;

            _bossDead = true;
            _bossEngaged = null;
            OpenDoors();
            Hud.Instance.ClearBoss();
            Hud.Instance.RemoveMapMarkerNear(_bossPos, 8f);
            SetMusic(Sfx.TowerLoop());
            Sfx.Play2D(SfxKind.Chime);

            string msg = e.DisplayName.ToUpper() + " DEFEATED";
            if (Bestiary.Scout(e.Def.Kind)) msg += "\nStudied: +10% damage against this foe";
            var pos = e.transform.position;
            pos.y = 0f;

            bool finalBoss = Floor == BossInfo.FinalFloor && !Flags.Has("ending_stay");
            if (!finalBoss)
            {
                msg += "\nA stairway has appeared";
                _stairs = Stairs.Spawn(pos);
                Hud.Instance.MarkStairs(pos);
            }
            Hud.Instance.Toast(msg, 4f);
            _bossPos = pos;
            Save();
            StartCoroutine(StoryAfterBoss(Floor));
        }

        public void OnPlayerDied()
        {
            CloseUi();
            _returnT = -1f;
            if (Hardcore)
            {
                _noSave = true;
                SaveSystem.Delete();
            }
            Hud.Instance.ShowDeath(true);
        }

        // ------------------------------------------------------------------ return crystal

        public void CancelReturn()
        {
            if (_returnT < 0f) return;
            _returnT = -1f;
            if (Hud.Instance != null) Hud.Instance.Toast("Return interrupted!", 1.5f);
        }

        bool InCombat()
        {
            foreach (var e in Enemy.All) if (e != null && !e.IsDead && e.Aggro) return true;
            return false;
        }

        void UpdateReturn()
        {
            if (Player.IsDead || InTown) { _returnT = -1f; return; }
            if (_returnT >= 0f)
            {
                if (BossSealed) { _returnT = -1f; Hud.Instance.Toast("The doors are sealed", 1.6f); return; }
                _returnT -= Time.deltaTime;
                Hud.Instance.Prompt("Return Crystal:  teleporting in " + Mathf.Max(0f, _returnT).ToString("0.0") + "s   (taking damage interrupts)");
                if (_returnT <= 0f)
                {
                    _returnT = -1f;
                    if (Player.Supplies.UseCrystal()) { Sfx.Play2D(SfxKind.Crystal); GoTown(); }
                }
                return;
            }
            if (!GameInput.ReturnPressed) return;
            if (BossSealed) { Hud.Instance.Toast("The doors are sealed", 1.6f); return; }
            if (!InCombat()) { GoTown(); return; }
            if (Player.Supplies.ReturnCrystals > 0)
            {
                _returnT = 1.5f;
                Sfx.Play2D(SfxKind.Crystal, 0.8f);
                GroundRing.Spawn(Player.transform.position, 0.5f, 3f, new Color(0.6f, 0.9f, 1f, 0.9f), 1.5f);
                Hud.Instance.Toast("Return Crystal:  channeling...", 1.5f);
            }
            else Hud.Instance.Toast("Can't return during combat  (no Return Crystal)", 1.8f);
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (Player == null) return;
            UpdateMusic();

            if (!Player.IsDead && GameInput.InventoryPressed)
            {
                if (_mode == UiMode.Inventory) CloseUi();
                else if (_mode == UiMode.None) OpenInventory();
            }
            if (_mode != UiMode.None && _modeFrame != Time.frameCount) UpdateUi();

            if (_mode == UiMode.None)
            {
                RunPending();
                UpdateBossSeal();
                UpdateReturn();
                UpdateQuestTracker();
            }

            if (Player.IsDead)
            {
                if (GameInput.ReturnPressed)
                {
                    if (Hardcore) RestartRun(); else GoTown();
                }
                else if (GameInput.RestartPressed && !Hardcore)
                {
                    if (Floor > 0) StartFloor(Floor); else GoTown();   // same seed => same layout, enemies reset; XP and level are kept
                }
            }
        }
    }
}
