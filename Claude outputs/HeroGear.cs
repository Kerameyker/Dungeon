using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Builds the visible equipment of the hero out of primitives so the silhouette changes with the gear.
    /// Everything it spawns lives in children whose name starts with "Gear" (GearWeapon / GearOffhand / Gear_*),
    /// so <see cref="Refresh"/> can wipe and rebuild the whole look at any time.
    /// No colliders, no lights, cheap particles only, ~40 primitives at most.
    /// </summary>
    public class HeroGear : MonoBehaviour
    {
        /// <summary>Hard cap on spawned renderers (keeps a Legendary full set affordable).</summary>
        const int Budget = 42;

        static readonly Color DefaultBody = new Color(0.10f, 0.13f, 0.24f);

        // rig references (see PlayerController.Build)
        Transform _visual, _bodyRoot, _legL, _legR, _armL, _swordPivot;
        Material _bodyMat;
        Renderer _phBlade, _phGuard, _phHilt;

        // spawned state
        readonly List<Material> _owned = new List<Material>();
        Transform _sigil, _auraRing;
        Material _auraMat;
        Color _auraColor = Color.white;
        int _count;

        /// <summary>Base color for the coat / legs; the player tints its shared body material with it.</summary>
        public Color BodyColor { get; private set; }

        /// <summary>Local Z of the weapon tip inside the sword pivot (trail anchor).</summary>
        public float TipZ { get; private set; }

        // ------------------------------------------------------------------ attach

        public static HeroGear Attach(GameObject playerRoot, Transform visual, Transform bodyRoot,
                                      Transform legL, Transform legR, Transform armL,
                                      Transform swordPivot, Material bodyMat)
        {
            if (playerRoot == null) return null;
            var hg = playerRoot.GetComponent<HeroGear>();
            if (hg == null) hg = playerRoot.AddComponent<HeroGear>();

            hg._visual = visual;
            hg._bodyRoot = bodyRoot;
            hg._legL = legL;
            hg._legR = legR;
            hg._armL = armL;
            hg._swordPivot = swordPivot;
            hg._bodyMat = bodyMat;
            hg.BodyColor = DefaultBody;
            hg.TipZ = 1.25f;
            hg.CachePlaceholder();
            hg.Refresh(null, false);
            return hg;
        }

        void CachePlaceholder()
        {
            _phBlade = FindRenderer(_swordPivot, "Blade");
            _phGuard = FindRenderer(_swordPivot, "Guard");
            _phHilt = FindRenderer(_swordPivot, "Hilt");
        }

        static Renderer FindRenderer(Transform parent, string child)
        {
            if (parent == null) return null;
            var t = parent.Find(child);
            return t == null ? null : t.GetComponent<Renderer>();
        }

        // ------------------------------------------------------------------ rebuild

        /// <summary>Destroys every gear visual and builds the current set again. Safe to call on every equip.</summary>
        public void Refresh(Equipment gear, bool dualUnlocked)
        {
            Cleanup();
            _count = 0;

            Item weapon = gear != null ? gear.Get(ItemSlot.Weapon) : null;
            Item armor = gear != null ? gear.Get(ItemSlot.Armor) : null;
            Item trinket = gear != null ? gear.Get(ItemSlot.Trinket) : null;

            BodyColor = ArmorBodyColor(armor);

            // the starting kit is the placeholder sword built by PlayerController
            SetPlaceholder(weapon == null);
            TipZ = weapon == null ? 1.25f : BuildWeapon(weapon);

            if (dualUnlocked) BuildOffhand(weapon);
            if (armor != null) BuildArmor(armor);
            if (trinket != null) BuildTrinket(trinket);
        }

        void SetPlaceholder(bool on)
        {
            if (_phBlade != null) _phBlade.enabled = on;
            if (_phGuard != null) _phGuard.enabled = on;
            if (_phHilt != null) _phHilt.enabled = on;
        }

        void Cleanup()
        {
            DestroyGear(_swordPivot);
            DestroyGear(_armL);
            DestroyGear(_bodyRoot);
            DestroyGear(_legL);
            DestroyGear(_legR);
            DestroyGear(_visual);
            DestroyGear(transform);

            for (int i = 0; i < _owned.Count; i++)
                if (_owned[i] != null) Destroy(_owned[i]);
            _owned.Clear();

            _sigil = null;
            _auraRing = null;
            _auraMat = null;
        }

        static void DestroyGear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var ch = t.GetChild(i);
                if (ch == null || !ch.name.StartsWith("Gear")) continue;
                ch.gameObject.SetActive(false);
                ch.SetParent(null, false);   // detach so a same-frame rebuild never sees it
                Destroy(ch.gameObject);
            }
        }

        // ------------------------------------------------------------------ weapon

        float BuildWeapon(Item w)
        {
            var root = MakeRoot(_swordPivot, "GearWeapon", Vector3.zero);
            if (root == null) return 1.25f;

            int style = ((w.Style % 4) + 4) % 4;
            int ri = (int)w.Rarity;
            Color rc = ItemIcons.RarityColor(w.Rarity);

            Color steel = new Color(0.82f, 0.88f, 0.95f);
            Color bladeCol = Color.Lerp(steel, rc, 0.14f + 0.12f * ri);
            Color emis = ri >= 2 ? rc * (0.20f + 0.22f * (ri - 1)) : new Color(0.04f, 0.10f, 0.16f);
            Material bladeMat = Solid(bladeCol, 0.85f, emis);
            Material guardMat = Solid(Color.Lerp(new Color(0.76f, 0.60f, 0.28f), rc, 0.35f), 0.6f, null);
            Material gripMat = Solid(new Color(0.26f, 0.16f, 0.11f), 0.15f, null);
            Material glowMat = ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.35f));

            float len, width, thick, guardW, gripLen;
            int segCount = 1;
            float bend = 0f;
            bool flatTip = false;

            switch (w.WType)
            {
                case WeaponType.Rapier:
                    len = 1.45f; width = 0.05f; thick = 0.022f; guardW = 0.20f; gripLen = 0.24f;
                    if (style == 1) { width = 0.035f; len = 1.50f; }
                    if (style == 2) { width = 0.055f; len = 1.40f; segCount = 2; bend = 6f; }
                    if (style == 3) { width = 0.045f; }
                    break;
                case WeaponType.GreatBlade:
                    len = 1.60f; width = 0.30f; thick = 0.045f; guardW = 0.52f; gripLen = 0.40f;
                    if (style == 1) { width = 0.28f; len = 1.55f; flatTip = true; }
                    if (style == 2) { width = 0.24f; len = 1.75f; }
                    if (style == 3) { width = 0.34f; len = 1.70f; }
                    break;
                case WeaponType.Dagger:
                    len = 0.55f; width = 0.085f; thick = 0.024f; guardW = 0.20f; gripLen = 0.18f;
                    if (style == 1) { width = 0.08f; segCount = 2; bend = 12f; }
                    if (style == 2) { width = 0.075f; len = 0.58f; segCount = 3; bend = 15f; }
                    if (style == 3) { width = 0.065f; len = 0.50f; }
                    break;
                default: // Sword
                    len = 1.15f; width = 0.10f; thick = 0.028f; guardW = 0.34f; gripLen = 0.26f;
                    if (style == 1) { width = 0.095f; segCount = 2; bend = 9f; }
                    if (style == 2) { width = 0.13f; len = 1.30f; guardW = 0.40f; }
                    if (style == 3) { width = 0.085f; len = 1.22f; segCount = 2; bend = -7f; }
                    break;
            }

            const float guardZ = 0.05f;
            float bladeStart = guardZ + 0.04f;

            // --- blade, optionally bent into 2-3 segments (sabers, kris, swept rapiers)
            float segLen = len / segCount;
            float ang = 0f;
            float sign = 1f;
            Vector3 cursor = new Vector3(0f, 0f, bladeStart);
            Vector3 dir = Vector3.forward;
            for (int i = 0; i < segCount; i++)
            {
                if (i > 0) { ang += bend * sign; sign = -sign; }
                float rad = ang * Mathf.Deg2Rad;
                dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 center = cursor + dir * (segLen * 0.5f);
                Vector3 euler = new Vector3(0f, ang, 0f);
                Prim(PrimitiveType.Cube, root, center, new Vector3(width, thick, segLen), bladeMat, "Blade" + i, euler);
                if (ri >= 2)   // Rare+: glowing edge halo around the blade
                    Prim(PrimitiveType.Cube, root, center,
                         new Vector3(width * 1.22f, thick * 0.5f, segLen * 0.98f), glowMat, "Edge" + i, euler);
                cursor += dir * segLen;
            }

            // --- tip
            if (!flatTip)
            {
                float tipLen = Mathf.Min(0.20f, len * 0.16f);
                Prim(PrimitiveType.Cube, root, cursor + dir * (tipLen * 0.5f),
                     new Vector3(width * 0.52f, thick, tipLen), bladeMat, "Tip", new Vector3(0f, ang, 0f));
                cursor += dir * tipLen;
            }
            float tipZ = cursor.z;

            // --- guard
            if (w.WType == WeaponType.Rapier)
            {
                Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0f, guardZ + 0.03f),
                     new Vector3(0.17f, 0.015f, 0.17f), guardMat, "Cup", new Vector3(90f, 0f, 0f));
                Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.06f, guardZ - 0.04f),
                     new Vector3(0.03f, 0.14f, 0.03f), guardMat, "Knuckle", Vector3.zero);
            }
            else
            {
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, guardZ),
                     new Vector3(guardW, 0.055f, 0.07f), guardMat, "Guard", Vector3.zero);
                if (w.WType == WeaponType.GreatBlade)
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, guardZ + 0.10f),
                         new Vector3(guardW * 0.45f, 0.04f, 0.05f), guardMat, "Guard2", Vector3.zero);
            }

            // --- grip + pommel
            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0f, -gripLen * 0.5f),
                 new Vector3(0.05f, gripLen * 0.5f, 0.05f), gripMat, "Grip", new Vector3(90f, 0f, 0f));
            Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0f, -gripLen - 0.04f),
                 Vector3.one * 0.085f, guardMat, "Pommel", Vector3.zero);

            // --- rarity flourishes
            if (ri >= 3)
                CheapFlame(root, new Vector3(0f, 0.02f, bladeStart + len * 0.55f), rc,
                           w.WType == WeaponType.GreatBlade ? 0.16f : 0.10f, ri == 4 ? 12f : 7f, ri == 4 ? 18 : 10);

            if (ri == 4)
            {
                var rot = Quaternion.Euler(0f, 90f, 0f);
                Vector3 mid = new Vector3(0f, 0f, bladeStart + len * 0.5f);
                Vector3 sc = new Vector3(len * 1.05f, width * 7f, 1f);
                GlowQuad(root, mid, sc, rot, new Color(rc.r, rc.g, rc.b, 0.42f), ProcAssets.SoftCircle(), "Gear_Aura1");
                GlowQuad(root, mid, sc, rot * Quaternion.Euler(90f, 0f, 0f),
                         new Color(rc.r, rc.g, rc.b, 0.42f), ProcAssets.SoftCircle(), "Gear_Aura2");
            }

            // --- blacksmith accents
            if (w.Upgrade > 0)
            {
                Color up = Color.Lerp(rc, Color.white, Mathf.Clamp01(w.Upgrade / 10f));
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.045f, guardZ),
                     Vector3.one * 0.055f, ProcAssets.Unlit(up), "Rune", new Vector3(45f, 0f, 45f));
                if (w.Upgrade >= 5)
                    GlowQuad(root, new Vector3(0f, 0f, guardZ), new Vector3(0.45f, 0.45f, 1f),
                             Quaternion.identity, new Color(up.r, up.g, up.b, 0.35f), ProcAssets.SoftCircle(), "Gear_UpGlow");
            }

            return tipZ;
        }

        /// <summary>Second blade in the free hand once the twin-blade skill is unlocked.</summary>
        void BuildOffhand(Item mainHand)
        {
            var root = MakeRoot(_armL, "GearOffhand", new Vector3(0f, -0.62f, 0.02f));
            if (root == null) return;
            root.localRotation = Quaternion.Euler(18f, 0f, 12f);

            Rarity rar = mainHand != null ? mainHand.Rarity : Rarity.Common;
            int ri = (int)rar;
            Color rc = ItemIcons.RarityColor(rar);
            Color bladeCol = Color.Lerp(new Color(0.82f, 0.88f, 0.95f), rc, 0.14f + 0.12f * ri);
            Material bladeMat = Solid(bladeCol, 0.85f, ri >= 2 ? rc * 0.35f : new Color(0.04f, 0.10f, 0.16f));
            Material trimMat = Solid(Color.Lerp(new Color(0.76f, 0.60f, 0.28f), rc, 0.35f), 0.6f, null);

            float len = mainHand != null && mainHand.WType == WeaponType.Dagger ? 0.45f : 0.62f;
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, 0.10f + len * 0.5f),
                 new Vector3(0.075f, 0.022f, len), bladeMat, "Blade", Vector3.zero);
            if (ri >= 2)
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, 0.10f + len * 0.5f),
                     new Vector3(0.092f, 0.011f, len * 0.98f), ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.35f)),
                     "Edge", Vector3.zero);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, 0.07f),
                 new Vector3(0.2f, 0.045f, 0.05f), trimMat, "Guard", Vector3.zero);
            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0f, -0.07f),
                 new Vector3(0.045f, 0.09f, 0.045f), Solid(new Color(0.26f, 0.16f, 0.11f), 0.15f, null),
                 "Grip", new Vector3(90f, 0f, 0f));
        }

        // ------------------------------------------------------------------ armor

        static Color ArmorBase(int style)
        {
            switch (style)
            {
                case 1: return new Color(0.52f, 0.55f, 0.62f);   // chain mail
                case 2: return new Color(0.70f, 0.74f, 0.82f);   // knight's plate
                case 3: return new Color(0.18f, 0.34f, 0.31f);   // warden cloak
                case 4: return new Color(0.38f, 0.47f, 0.37f);   // scale vest
                default: return new Color(0.44f, 0.28f, 0.16f);  // leather coat
            }
        }

        Color ArmorBodyColor(Item armor)
        {
            if (armor == null) return DefaultBody;
            int style = ((armor.Style % 5) + 5) % 5;
            Color b;
            switch (style)
            {
                case 1: b = new Color(0.28f, 0.30f, 0.35f); break;
                case 2: b = new Color(0.38f, 0.41f, 0.48f); break;
                case 3: b = new Color(0.09f, 0.19f, 0.18f); break;
                case 4: b = new Color(0.19f, 0.25f, 0.20f); break;
                default: b = new Color(0.29f, 0.19f, 0.12f); break;
            }
            return Color.Lerp(b, ItemIcons.RarityColor(armor.Rarity), 0.10f + 0.05f * (int)armor.Rarity);
        }

        void BuildArmor(Item a)
        {
            var root = MakeRoot(_bodyRoot, "Gear_Armor", Vector3.zero);
            if (root == null) return;

            int style = ((a.Style % 5) + 5) % 5;
            int ri = (int)a.Rarity;
            Color rc = ItemIcons.RarityColor(a.Rarity);
            Color plate = Color.Lerp(ArmorBase(style), rc, 0.12f + 0.05f * ri);
            Color darkCol = new Color(plate.r * 0.55f, plate.g * 0.55f, plate.b * 0.6f, 1f);
            Color trimCol = Color.Lerp(rc, Color.white, 0.25f);

            Material mMain = Solid(plate, style == 2 ? 0.6f : 0.25f, null);
            Material mDark = Solid(darkCol, 0.2f, null);
            Material mTrim = ri >= 2 ? ProcAssets.Unlit(trimCol) : Solid(trimCol, 0.55f, null);
            Material mCloth = _bodyMat != null ? _bodyMat : mMain;

            switch (style)
            {
                case 1: // chain mail
                    Prim(PrimitiveType.Capsule, root, new Vector3(0f, 0.06f, 0f),
                         new Vector3(0.68f, 0.52f, 0.56f), mMain, "Mail", Vector3.zero);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.50f, 0f),
                         new Vector3(0.34f, 0.04f, 0.30f), mDark, "Collar", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(-0.36f, 0.34f, 0f),
                         new Vector3(0.18f, 0.24f, 0.26f), mMain, "SleeveL", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0.36f, 0.34f, 0f),
                         new Vector3(0.18f, 0.24f, 0.26f), mMain, "SleeveR", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.30f, 0f),
                         new Vector3(0.64f, 0.22f, 0.40f), mDark, "Skirt", Vector3.zero);
                    break;
                case 2: // knight's plate
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.12f, 0f),
                         new Vector3(0.70f, 0.50f, 0.46f), mMain, "Cuirass", Vector3.zero);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.46f, 0f),
                         new Vector3(0.32f, 0.05f, 0.30f), mDark, "Gorget", Vector3.zero);
                    Prim(PrimitiveType.Sphere, root, new Vector3(-0.36f, 0.40f, 0f),
                         new Vector3(0.34f, 0.26f, 0.34f), mMain, "PauldronL", Vector3.zero);
                    Prim(PrimitiveType.Sphere, root, new Vector3(0.36f, 0.40f, 0f),
                         new Vector3(0.34f, 0.26f, 0.34f), mMain, "PauldronR", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.12f, 0.23f),
                         new Vector3(0.07f, 0.48f, 0.03f), mTrim, "Ridge", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.32f, 0f),
                         new Vector3(0.72f, 0.20f, 0.42f), mDark, "Fauld", Vector3.zero);
                    break;
                case 3: // warden cloak (hood + cape)
                    Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.78f, -0.03f),
                         new Vector3(0.50f, 0.46f, 0.50f), mMain, "Hood", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.42f, -0.02f),
                         new Vector3(0.74f, 0.16f, 0.46f), mDark, "Mantle", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.08f, -0.28f),
                         new Vector3(0.58f, 0.96f, 0.04f), mCloth, "Cape", new Vector3(-6f, 0f, 0f));
                    Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.45f, 0.22f),
                         Vector3.one * 0.10f, mTrim, "Clasp", Vector3.zero);
                    break;
                case 4: // scale vest
                    for (int i = 0; i < 3; i++)
                        Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.30f - i * 0.20f, 0f),
                             new Vector3(0.68f, 0.17f, 0.46f), i % 2 == 0 ? mMain : mDark,
                             "Scales" + i, new Vector3(i % 2 == 0 ? 7f : -7f, 0f, 0f));
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.28f, 0f),
                         new Vector3(0.72f, 0.10f, 0.42f), mDark, "Belt", Vector3.zero);
                    break;
                default: // leather coat
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.10f, 0f),
                         new Vector3(0.68f, 0.52f, 0.44f), mMain, "Chest", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(-0.13f, 0.12f, 0.23f),
                         new Vector3(0.09f, 0.60f, 0.03f), mDark, "StrapL", new Vector3(0f, 0f, 13f));
                    Prim(PrimitiveType.Cube, root, new Vector3(0.13f, 0.12f, 0.23f),
                         new Vector3(0.09f, 0.60f, 0.03f), mDark, "StrapR", new Vector3(0f, 0f, -13f));
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.28f, 0f),
                         new Vector3(0.74f, 0.12f, 0.40f), mDark, "Belt", Vector3.zero);
                    break;
            }

            // greaves for the metal sets
            if (style == 1 || style == 2 || style == 4)
            {
                var gl = MakeRoot(_legL, "Gear_GreaveL", Vector3.zero);
                var gr = MakeRoot(_legR, "Gear_GreaveR", Vector3.zero);
                Prim(PrimitiveType.Cube, gl, new Vector3(0f, -0.44f, 0.01f), new Vector3(0.27f, 0.24f, 0.30f), mMain, "Greave", Vector3.zero);
                Prim(PrimitiveType.Cube, gr, new Vector3(0f, -0.44f, 0.01f), new Vector3(0.27f, 0.24f, 0.30f), mMain, "Greave", Vector3.zero);
            }

            // rarity treatment
            if (ri >= 1)
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.34f, 0f),
                     new Vector3(0.70f, 0.045f, 0.45f), mTrim, "Trim", Vector3.zero);
            if (ri >= 2)
            {
                Material seam = ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.45f));
                Prim(PrimitiveType.Cube, root, new Vector3(-0.20f, 0.10f, 0.23f),
                     new Vector3(0.035f, 0.46f, 0.02f), seam, "SeamL", Vector3.zero);
                Prim(PrimitiveType.Cube, root, new Vector3(0.20f, 0.10f, 0.23f),
                     new Vector3(0.035f, 0.46f, 0.02f), seam, "SeamR", Vector3.zero);
            }
            if (ri >= 3)
            {
                Material spike = ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.3f));
                Prim(PrimitiveType.Cube, root, new Vector3(-0.42f, 0.56f, 0f),
                     new Vector3(0.07f, 0.26f, 0.07f), spike, "SpikeL", new Vector3(0f, 0f, 28f));
                Prim(PrimitiveType.Cube, root, new Vector3(0.42f, 0.56f, 0f),
                     new Vector3(0.07f, 0.26f, 0.07f), spike, "SpikeR", new Vector3(0f, 0f, -28f));
            }
            if (ri == 4)
            {
                // wings of light + a slow aura ring at the feet
                Color wing = new Color(rc.r, rc.g, rc.b, 0.40f);
                GlowQuad(root, new Vector3(-0.42f, 0.28f, -0.30f), new Vector3(1.0f, 0.75f, 1f),
                         Quaternion.Euler(0f, 40f, 24f), wing, ProcAssets.SoftCircle(), "Gear_WingL");
                GlowQuad(root, new Vector3(0.42f, 0.28f, -0.30f), new Vector3(1.0f, 0.75f, 1f),
                         Quaternion.Euler(0f, -40f, -24f), wing, ProcAssets.SoftCircle(), "Gear_WingR");

                _auraColor = new Color(rc.r, rc.g, rc.b, 0.55f);
                var ring = GlowQuad(_visual, new Vector3(0f, 0.06f, 0f), new Vector3(2.1f, 2.1f, 1f),
                                    Quaternion.Euler(90f, 0f, 0f), _auraColor, ProcAssets.RingTexture(), "Gear_AuraRing");
                if (ring != null)
                {
                    _auraRing = ring.transform;
                    var rend = ring.GetComponent<Renderer>();
                    if (rend != null) _auraMat = rend.sharedMaterial;
                }
            }
            if (a.Upgrade > 0)
            {
                Color up = Color.Lerp(rc, Color.white, Mathf.Clamp01(a.Upgrade / 10f));
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.30f, 0.24f),
                     Vector3.one * 0.06f, ProcAssets.Unlit(up), "UpRune", new Vector3(0f, 0f, 45f));
            }
        }

        // ------------------------------------------------------------------ trinket

        void BuildTrinket(Item t)
        {
            int style = ((t.Style % 5) + 5) % 5;
            int ri = (int)t.Rarity;
            Color rc = ItemIcons.RarityColor(t.Rarity);
            Material gem = ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.2f));
            Material metal = Solid(Color.Lerp(new Color(0.80f, 0.70f, 0.38f), rc, 0.3f), 0.7f, null);

            Transform root;
            switch (style)
            {
                case 0: // ring on the free hand
                    root = MakeRoot(_armL, "Gear_Trinket", new Vector3(0f, -0.60f, 0f));
                    Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.11f, 0.012f, 0.11f), metal, "Band", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.02f, 0.06f), Vector3.one * 0.05f, gem, "Stone", new Vector3(0f, 45f, 45f));
                    break;
                case 1: // amulet on the chest
                    root = MakeRoot(_bodyRoot, "Gear_Trinket", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(-0.10f, 0.52f, 0.14f), new Vector3(0.02f, 0.22f, 0.02f), metal, "ChainL", new Vector3(0f, 0f, -14f));
                    Prim(PrimitiveType.Cube, root, new Vector3(0.10f, 0.52f, 0.14f), new Vector3(0.02f, 0.22f, 0.02f), metal, "ChainR", new Vector3(0f, 0f, 14f));
                    Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.40f, 0.21f), Vector3.one * 0.10f, gem, "Pendant", Vector3.zero);
                    break;
                case 2: // charm on the belt
                    root = MakeRoot(_bodyRoot, "Gear_Trinket", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0.26f, -0.24f, 0.14f), new Vector3(0.015f, 0.12f, 0.015f), metal, "Cord", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0.26f, -0.34f, 0.14f), Vector3.one * 0.09f, gem, "Charm", new Vector3(0f, 0f, 45f));
                    break;
                case 3: // ember pendant
                    root = MakeRoot(_bodyRoot, "Gear_Trinket", Vector3.zero);
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.52f, 0.14f), new Vector3(0.02f, 0.22f, 0.02f), metal, "Cord", Vector3.zero);
                    Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.40f, 0.21f), Vector3.one * 0.11f,
                         ProcAssets.Unlit(new Color(1f, 0.55f, 0.18f)), "Ember", Vector3.zero);
                    CheapFlame(root, new Vector3(0f, 0.44f, 0.21f), new Color(1f, 0.6f, 0.2f), 0.10f, 8f, 12);
                    break;
                default: // spire sigil floating behind the back
                    root = MakeRoot(_bodyRoot, "Gear_Trinket", new Vector3(0f, 0.42f, -0.42f));
                    _sigil = root;
                    Prim(PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.16f, 0.16f, 0.02f), gem, "Sigil", new Vector3(0f, 0f, 45f));
                    Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.02f, 0f), new Vector3(0.05f, 0.34f, 0.02f), metal, "Spire", Vector3.zero);
                    break;
            }

            if (ri >= 2 && root != null)
            {
                Vector3 p = style == 0 ? Vector3.zero : (style == 4 ? Vector3.zero : new Vector3(0f, 0.40f, 0.22f));
                GlowQuad(root, p, Vector3.one * (0.4f + 0.1f * ri), Quaternion.identity,
                         new Color(rc.r, rc.g, rc.b, 0.35f), ProcAssets.SoftCircle(), "Gear_TrinketGlow");
            }
            if (t.Upgrade >= 5 && root != null)
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.30f, 0.22f), Vector3.one * 0.04f,
                     ProcAssets.Unlit(Color.Lerp(rc, Color.white, 0.6f)), "UpRune", new Vector3(0f, 0f, 45f));
        }

        // ------------------------------------------------------------------ idle animation

        void Update()
        {
            float t = Time.time;
            if (_sigil != null)
                _sigil.localRotation = Quaternion.Euler(0f, t * 55f, 18f + Mathf.Sin(t * 1.7f) * 6f);

            if (_auraRing != null)
            {
                float s = 2.1f + Mathf.Sin(t * 2f) * 0.12f;
                _auraRing.localScale = new Vector3(s, s, 1f);
                _auraRing.localRotation = Quaternion.Euler(90f, 0f, t * 25f);
            }
            if (_auraMat != null)
            {
                Color c = _auraColor;
                c.a = _auraColor.a * (0.7f + 0.3f * Mathf.Sin(t * 2.4f));
                _auraMat.color = c;
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < _owned.Count; i++)
                if (_owned[i] != null) Destroy(_owned[i]);
            _owned.Clear();
        }

        // ------------------------------------------------------------------ small builders

        static Transform MakeRoot(Transform parent, string name, Vector3 localPos)
        {
            if (parent == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale,
                        Material mat, string name, Vector3 euler)
        {
            if (parent == null || mat == null || _count >= Budget) return null;
            _count++;
            var go = ProcAssets.Prim(type, parent, pos, scale, mat, false, name);
            if (euler != Vector3.zero) go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        GameObject GlowQuad(Transform parent, Vector3 pos, Vector3 scale, Quaternion rot,
                            Color color, Texture2D tex, string name)
        {
            if (parent == null || _count >= Budget) return null;
            var mat = ProcAssets.SpriteMat(tex, color);
            if (mat == null) return null;          // Sprites/Default unavailable: skip the effect
            _owned.Add(mat);
            _count++;
            var go = ProcAssets.Prim(PrimitiveType.Quad, parent, pos, scale, mat, false, name);
            go.transform.localRotation = rot;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go;
        }

        void CheapFlame(Transform parent, Vector3 pos, Color color, float size, float rate, int max)
        {
            if (parent == null || _count >= Budget) return;
            _count++;
            var ps = Particles.Flame(parent, pos, color, size);
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = rate;
            var main = ps.main;
            main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        }

        // ------------------------------------------------------------------ shared materials

        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { MatCache.Clear(); }

        /// <summary>Cached lit material (emission is baked in at creation, never toggled at runtime).</summary>
        static Material Solid(Color c, float smooth, Color? emission)
        {
            string key = ColorKey(c) + "|" + Mathf.RoundToInt(smooth * 20f) + "|" +
                         (emission.HasValue ? ColorKey(emission.Value) : "-");
            Material m;
            if (MatCache.TryGetValue(key, out m) && m != null) return m;
            m = ProcAssets.Lit(c, null, smooth, emission);
            MatCache[key] = m;
            return m;
        }

        static string ColorKey(Color c)
        {
            Color32 c32 = c;
            return c32.r + "," + c32.g + "," + c32.b;
        }
    }
}
