using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Entry point. Builds the whole game at runtime (camera, lighting, player, HUD, dungeon floors),
    /// so the scene can be completely empty.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }

        public int Floor { get; private set; }
        public PlayerController Player { get; private set; }
        public CameraRig Rig { get; private set; }
        public DungeonLayout Layout { get; private set; }
        public Pathfinder Pathfinder { get; private set; }

        public bool Paused { get; private set; }
        int _toggleFrame = -1;
        public bool InventoryToggledThisFrame { get { return _toggleFrame == Time.frameCount; } }

        int _baseSeed;
        int _invCursor;
        GameObject _dungeonRoot;
        Stairs _stairs;
        bool _bossDead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _baseSeed = Random.Range(1, 100000);

            SetupEnvironment();
            var cam = SetupCamera();
            Rig = cam.gameObject.AddComponent<CameraRig>();

            Hud.Create();
            Player = PlayerController.Create(Rig);
            Rig.Target = Player.transform;

            var music = gameObject.AddComponent<AudioSource>();
            music.clip = Sfx.AmbientLoop();
            music.loop = true;
            music.volume = 0.35f;
            music.spatialBlend = 0f;
            music.Play();

            StartFloor(1);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        void SetInventory(bool open)
        {
            if (open && (Player == null || Player.IsDead)) return;
            Paused = open;
            _toggleFrame = Time.frameCount;
            Time.timeScale = open ? 0f : 1f;
            Hud.Instance.ShowInventory(open);
            if (open)
            {
                _invCursor = 0;
                Hud.Instance.RefreshInventory(Player, _invCursor);
            }
        }

        void UpdateInventory()
        {
            var items = Player.Bag.Items;
            bool changed = false;
            if (GameInput.MenuUpPressed && items.Count > 0) { _invCursor = (_invCursor - 1 + items.Count) % items.Count; changed = true; }
            if (GameInput.MenuDownPressed && items.Count > 0) { _invCursor = (_invCursor + 1) % items.Count; changed = true; }
            if (GameInput.ConfirmPressed && items.Count > 0) { Player.EquipFromBag(_invCursor); changed = true; }
            else if (GameInput.DiscardPressed && items.Count > 0) { Player.DiscardFromBag(_invCursor); changed = true; }
            if (changed)
            {
                _invCursor = Mathf.Clamp(_invCursor, 0, Mathf.Max(0, items.Count - 1));
                Hud.Instance.RefreshInventory(Player, _invCursor);
            }
        }

        static void SetupEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.016f;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
            RenderSettings.skybox = null;

            // The dungeon is lit only by torches, so switch off any scene directional light.
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;
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

        // ------------------------------------------------------------------ floors

        public void StartFloor(int floor)
        {
            Floor = floor;
            _bossDead = false;
            if (Paused) SetInventory(false);
            ClearFloor();

            int seed = _baseSeed + floor * 7919;
            Layout = DungeonGenerator.Generate(seed);
            Pathfinder = new Pathfinder(Layout);
            _dungeonRoot = DungeonBuilder.Build(Layout, floor);

            var startRoom = Layout.Rooms[Layout.StartRoom];
            Player.Respawn(DungeonBuilder.ToWorld(startRoom.CenterX, startRoom.CenterY) + Vector3.up * 0.1f);
            Rig.Target = Player.transform;

            SpawnEnemies(floor, seed);

            Hud.Instance.SetFloor(floor);
            Hud.Instance.SetMinimap(Layout);
            Hud.Instance.ShowDeath(false);
            Hud.Instance.Toast("FLOOR " + floor, 2.5f);
        }

        void ClearFloor()
        {
            var snapshot = new List<Enemy>(Enemy.All);
            foreach (var e in snapshot) if (e != null) Destroy(e.gameObject);
            Enemy.All.Clear();
            if (_dungeonRoot != null) Destroy(_dungeonRoot);
            if (_stairs != null) Destroy(_stairs.gameObject);
            _stairs = null;
            foreach (var o in FindObjectsByType<HpOrb>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            foreach (var o in FindObjectsByType<LootDrop>(FindObjectsSortMode.None)) Destroy(o.gameObject);
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
                var kind = EnemyCatalog.PickForFloor(floor, rng.NextDouble());
                Enemy.Create(kind, floor, DungeonBuilder.ToWorld(x, y) + Vector3.up * 0.05f);
            }

            var boss = Layout.Rooms[Layout.BossRoom];
            Enemy.Create(EnemyKind.FloorGuardian, floor, DungeonBuilder.ToWorld(boss.CenterX, boss.CenterY) + Vector3.up * 0.05f);
        }

        public void NextFloor()
        {
            Player.Heal(Mathf.RoundToInt(Player.MaxHp * 0.3f));
            StartFloor(Floor + 1);
        }

        // ------------------------------------------------------------------ events

        public void OnEnemyKilled(Enemy e)
        {
            if (!e.Def.IsBoss || _bossDead) return;
            _bossDead = true;
            Hud.Instance.Toast("FLOOR GUARDIAN DEFEATED\nA stairway has appeared", 4f);
            var pos = e.transform.position;
            pos.y = 0f;
            _stairs = Stairs.Spawn(pos);
            Hud.Instance.MarkStairs(pos);
        }

        public void OnPlayerDied()
        {
            if (Paused) SetInventory(false);
            Hud.Instance.ShowDeath(true);
        }

        void Update()
        {
            if (Player != null && !Player.IsDead && GameInput.InventoryPressed) SetInventory(!Paused);
            if (Paused && GameInput.EscapePressed) SetInventory(false);
            if (Paused && Player != null) UpdateInventory();

            if (Player != null && Player.IsDead && GameInput.RestartPressed)
                StartFloor(Floor);   // same seed => same layout, enemies reset; XP and level are kept
        }
    }
}
