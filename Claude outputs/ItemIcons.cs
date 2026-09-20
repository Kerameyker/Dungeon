using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Procedurally drawn 48x48 pixel-art icons for gear, empty slots, consumables and skills.
    /// Everything is painted into a small RGBA buffer with a tiny drawing helper, then cached as a Sprite.
    /// No imported art: shapes are described by polygons, discs and lines in icon space (y grows upwards).
    /// </summary>
    public static class ItemIcons
    {
        public const int Size = 48;

        public enum ConsumableIcon { Potion, Crystal, Revive, Shard, Gold }

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Cache.Clear(); }

        /// <summary>Common grey-white, Uncommon green, Rare blue, Epic purple, Legendary orange-gold.</summary>
        public static Color RarityColor(Rarity r) { return ItemUi.RarityColor(r); }

        // ================================================================== public API

        /// <summary>Icon for a piece of gear. Cached by slot / weapon type / style / rarity / upgrade tier / seed variant.</summary>
        public static Sprite Get(Item item)
        {
            if (item == null) return null;
            int variant = Mathf.Abs(item.Seed) % 4;
            int upTier = item.Upgrade <= 0 ? 0 : (item.Upgrade >= 10 ? 3 : (item.Upgrade >= 5 ? 2 : 1));
            string key = "g" + (int)item.Slot + "_" + (int)item.WType + "_" + item.Style + "_" +
                         (int)item.Rarity + "_" + upTier + "_" + variant;
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;

            var c = new IconCanvas();
            Color rc = RarityColor(item.Rarity);
            Panel(c, rc, item.Rarity, variant);
            switch (item.Slot)
            {
                case ItemSlot.Weapon: DrawWeapon(c, item.WType, item.Style, item.Rarity, variant); break;
                case ItemSlot.Armor: DrawArmor(c, item.Style, item.Rarity, variant); break;
                default: DrawTrinket(c, item.Style, item.Rarity, variant); break;
            }
            if (upTier > 0) UpgradePips(c, rc, upTier);
            s = c.ToSprite(key);
            Cache[key] = s;
            return s;
        }

        /// <summary>Dim glyph for an empty equipment slot.</summary>
        public static Sprite SlotSilhouette(ItemSlot slot)
        {
            string key = "sil" + (int)slot;
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;

            var c = new IconCanvas();
            Color grey = new Color(0.55f, 0.58f, 0.64f);
            c.Clear(new Color(0.055f, 0.06f, 0.075f, 1f));
            c.Frame(0, 0, Size, Size, new Color(0f, 0f, 0f, 0.85f));
            c.Frame(1, 1, Size - 2, Size - 2, new Color(grey.r, grey.g, grey.b, 0.30f));

            Color dim = new Color(grey.r, grey.g, grey.b, 0.42f);
            if (slot == ItemSlot.Weapon)
            {
                var a = Axis(44f);
                c.Poly(BladePoly(a, 0.34f, 1f, 2.2f, 0f, 0.85f, 0f, 3), dim);
                c.Poly(Bar(a, 0.315f, 6.5f, 0.035f), dim);
                c.Poly(Bar(a, 0.22f, 1.3f, 0.10f), dim);
            }
            else if (slot == ItemSlot.Armor)
            {
                c.Poly(TorsoPoly(), dim);
            }
            else
            {
                c.Ring(24, 22, 10.5f, 3f, dim);
                c.Disc(24, 35, 3.2f, dim);
            }
            s = c.ToSprite(key);
            Cache[key] = s;
            return s;
        }

        /// <summary>Potion flask, return crystal, revive token, ember shard, gold coin.</summary>
        public static Sprite Consumable(ConsumableIcon kind)
        {
            string key = "c" + (int)kind;
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;

            var c = new IconCanvas();
            Color accent;
            switch (kind)
            {
                case ConsumableIcon.Crystal: accent = new Color(0.45f, 0.75f, 1f); break;
                case ConsumableIcon.Revive: accent = new Color(1f, 0.92f, 0.62f); break;
                case ConsumableIcon.Shard: accent = new Color(1f, 0.6f, 0.2f); break;
                case ConsumableIcon.Gold: accent = new Color(1f, 0.82f, 0.32f); break;
                default: accent = new Color(1f, 0.35f, 0.4f); break;
            }
            Panel(c, accent, Rarity.Common, 0);
            // the frame follows the item's own colour instead of a rarity
            c.Frame(1, 1, Size - 2, Size - 2, new Color(accent.r, accent.g, accent.b, 0.85f));

            switch (kind)
            {
                case ConsumableIcon.Potion: DrawPotion(c, accent); break;
                case ConsumableIcon.Crystal: DrawCrystal(c, accent); break;
                case ConsumableIcon.Revive: DrawRevive(c, accent); break;
                case ConsumableIcon.Shard: DrawShard(c, accent); break;
                default: DrawCoin(c, accent); break;
            }
            s = c.ToSprite(key);
            Cache[key] = s;
            return s;
        }

        /// <summary>Skill glyph by SkillKind, tinted per weapon type. "twin_tempest" gets crossed blades.</summary>
        public static Sprite SkillIcon(SkillDef skill, WeaponType weapon)
        {
            bool unique = skill != null && skill.Id == "twin_tempest";
            SkillKind kind = skill != null ? skill.Kind : SkillKind.Arc;
            string key = "s" + (unique ? "u" : ((int)kind).ToString()) + "_" + (int)weapon;
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;

            Color tint = WeaponTint(weapon);
            var c = new IconCanvas();
            Color deep = new Color(tint.r * 0.10f + 0.035f, tint.g * 0.10f + 0.04f, tint.b * 0.10f + 0.055f, 1f);
            c.Clear(deep);
            c.RadialGlow(24f, 24f, 24f, tint, 0.14f);
            c.Frame(0, 0, Size, Size, new Color(0f, 0f, 0f, 0.85f));
            c.Frame(1, 1, Size - 2, Size - 2, new Color(tint.r, tint.g, tint.b, 0.55f));

            Color bright = Color.Lerp(tint, Color.white, 0.45f);
            Color soft = new Color(tint.r, tint.g, tint.b, 0.35f);

            if (unique)
            {
                // crossed blades
                c.Line(11f, 11f, 37f, 37f, 5f, soft);
                c.Line(37f, 11f, 11f, 37f, 5f, soft);
                c.Line(12f, 12f, 36f, 36f, 3f, bright);
                c.Line(36f, 12f, 12f, 36f, 3f, bright);
                c.Line(9f, 17f, 17f, 9f, 2f, tint);       // guard of the left blade
                c.Line(31f, 9f, 39f, 17f, 2f, tint);      // guard of the right blade
                c.Disc(24f, 24f, 3.2f, new Color(1f, 1f, 1f, 0.85f));
                c.Disc(24f, 24f, 5.5f, new Color(bright.r, bright.g, bright.b, 0.25f));
            }
            else
            {
                switch (kind)
                {
                    case SkillKind.Arc:
                        c.Arc(24f, 14f, 17f, 25f, 155f, 6.5f, soft);
                        c.Arc(24f, 14f, 17f, 30f, 150f, 3.5f, bright);
                        c.Arc(24f, 14f, 13f, 45f, 135f, 1.6f, new Color(1f, 1f, 1f, 0.55f));
                        break;
                    case SkillKind.Vertical:
                        c.Poly(new[] { V(18f, 44f), V(26f, 44f), V(28f, 12f), V(24f, 7f), V(20f, 12f) }, soft);
                        c.Poly(new[] { V(20f, 42f), V(25f, 42f), V(26f, 13f), V(23.5f, 9f), V(21f, 13f) }, bright);
                        c.Line(11f, 9f, 18f, 12f, 2f, tint);
                        c.Line(37f, 9f, 30f, 12f, 2f, tint);
                        c.Line(24f, 5f, 24f, 3f, 2f, bright);
                        break;
                    case SkillKind.Thrust:
                        c.Poly(new[] { V(41f, 24f), V(25f, 33f), V(25f, 15f) }, soft);
                        c.Poly(new[] { V(39f, 24f), V(26f, 31f), V(26f, 17f) }, bright);
                        c.Line(6f, 24f, 24f, 24f, 3f, bright);
                        c.Line(8f, 31f, 21f, 29f, 2f, soft);
                        c.Line(8f, 17f, 21f, 19f, 2f, soft);
                        break;
                    default: // Flurry: star of slashes
                        for (int i = 0; i < 4; i++)
                        {
                            float ang = i * 45f * Mathf.Deg2Rad;
                            float dx = Mathf.Cos(ang) * 17f, dy = Mathf.Sin(ang) * 17f;
                            c.Line(24f - dx, 24f - dy, 24f + dx, 24f + dy, 4f, soft);
                            c.Line(24f - dx * 0.92f, 24f - dy * 0.92f, 24f + dx * 0.92f, 24f + dy * 0.92f, 2f, bright);
                        }
                        c.Disc(24f, 24f, 4.5f, new Color(bright.r, bright.g, bright.b, 0.5f));
                        c.Disc(24f, 24f, 2.4f, new Color(1f, 1f, 1f, 0.9f));
                        break;
                }
            }
            s = c.ToSprite(key);
            Cache[key] = s;
            return s;
        }

        // ================================================================== shared look

        static Color WeaponTint(WeaponType w)
        {
            switch (w)
            {
                case WeaponType.Rapier: return new Color(0.72f, 0.88f, 1f);
                case WeaponType.GreatBlade: return new Color(1f, 0.62f, 0.28f);
                case WeaponType.Dagger: return new Color(0.55f, 1f, 0.66f);
                default: return new Color(0.42f, 0.85f, 1f);
            }
        }

        /// <summary>Dark tinted panel, rarity border and a subtle inner glow.</summary>
        static void Panel(IconCanvas c, Color rc, Rarity rarity, int variant)
        {
            Color deep = new Color(rc.r * 0.13f + 0.035f, rc.g * 0.13f + 0.04f, rc.b * 0.13f + 0.055f, 1f);
            c.Clear(deep);
            c.RadialGlow(24f, 24f, 26f, rc, 0.13f + 0.035f * (int)rarity);

            // faint diagonal weave so the panel is not flat (varies with the item seed)
            float step = 5f + variant;
            for (int y = 2; y < Size - 2; y++)
                for (int x = 2; x < Size - 2; x++)
                    if (((x + y) % (int)step) == 0)
                        c.Blend(x, y, new Color(1f, 1f, 1f, 0.025f));

            // corner shading
            c.RadialGlow(24f, 24f, 34f, Color.black, 0f);

            c.Frame(0, 0, Size, Size, new Color(0f, 0f, 0f, 0.9f));
            c.Frame(1, 1, Size - 2, Size - 2, new Color(rc.r, rc.g, rc.b, 0.95f));
            c.Frame(2, 2, Size - 4, Size - 4, new Color(rc.r, rc.g, rc.b, 0.20f));
            c.Rect(2, Size - 3, Size - 4, 1, new Color(1f, 1f, 1f, 0.12f));   // top highlight

            if (rarity >= Rarity.Epic)
            {
                Color bright = Color.Lerp(rc, Color.white, 0.5f);
                // corner brackets
                c.Rect(1, 1, 6, 2, bright); c.Rect(1, 1, 2, 6, bright);
                c.Rect(Size - 7, 1, 6, 2, bright); c.Rect(Size - 3, 1, 2, 6, bright);
                c.Rect(1, Size - 3, 6, 2, bright); c.Rect(1, Size - 7, 2, 6, bright);
                c.Rect(Size - 7, Size - 3, 6, 2, bright); c.Rect(Size - 3, Size - 7, 2, 6, bright);
            }
            if (rarity == Rarity.Legendary)
            {
                Color bright = Color.Lerp(rc, Color.white, 0.7f);
                c.Disc(4f, 4f, 1.6f, bright);
                c.Disc(Size - 4f, 4f, 1.6f, bright);
                c.Disc(4f, Size - 4f, 1.6f, bright);
                c.Disc(Size - 4f, Size - 4f, 1.6f, bright);
            }
        }

        static void UpgradePips(IconCanvas c, Color rc, int tier)
        {
            Color pip = Color.Lerp(rc, Color.white, 0.25f + 0.25f * tier);
            for (int i = 0; i < tier; i++)
                c.Rect(Size - 6 - i * 4, 4, 3, 3, pip);
        }

        // ================================================================== weapons

        struct IconAxis
        {
            public Vector2 Org, Fwd, Side;
            public float L;
            public Vector2 P(float w, float t) { return Org + Fwd * ((t - 0.5f) * L) + Side * w; }
        }

        /// <summary>Diagonal weapon axis: t = 0 pommel .. 1 tip, w = offset across the blade.</summary>
        static IconAxis Axis(float length)
        {
            var a = new IconAxis();
            a.Org = new Vector2(24f, 24f);
            a.Fwd = new Vector2(0.7071f, 0.7071f);
            a.Side = new Vector2(-0.7071f, 0.7071f);
            a.L = length;
            return a;
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        /// <summary>Blade outline: base t0 to tip t1, half width hw, optional bend, taper and wave.</summary>
        static Vector2[] BladePoly(IconAxis a, float t0, float t1, float hw, float curve, float taper, float wave, int segs)
        {
            if (segs < 2) segs = 2;
            var pts = new Vector2[(segs + 1) * 2];
            for (int i = 0; i <= segs; i++)
            {
                float k = i / (float)segs;
                float t = Mathf.Lerp(t0, t1, k);
                float off = curve * k * k + wave * Mathf.Sin(k * Mathf.PI * 2f);
                float w = hw * (1f - taper * k);
                pts[i] = a.P(off - w, t);
                pts[pts.Length - 1 - i] = a.P(off + w, t);
            }
            return pts;
        }

        /// <summary>Rectangle across the blade axis (guard, grip, ricasso).</summary>
        static Vector2[] Bar(IconAxis a, float t, float halfWidth, float halfLen)
        {
            return new[]
            {
                a.P(-halfWidth, t - halfLen), a.P(halfWidth, t - halfLen),
                a.P(halfWidth, t + halfLen), a.P(-halfWidth, t + halfLen)
            };
        }

        static void DrawWeapon(IconCanvas c, WeaponType w, int style, Rarity r, int variant)
        {
            style = ((style % 4) + 4) % 4;
            Color rc = RarityColor(r);
            Color steel = new Color(0.80f, 0.86f, 0.94f);
            Color metal = Color.Lerp(steel, rc, 0.10f + 0.10f * (int)r);
            Color edge = Color.Lerp(metal, Color.white, 0.55f);
            Color shade = new Color(metal.r * 0.45f, metal.g * 0.45f, metal.b * 0.5f, 1f);
            Color grip = new Color(0.30f, 0.19f, 0.12f);
            Color gold = Color.Lerp(new Color(0.85f, 0.70f, 0.32f), rc, 0.35f);
            Color gem = Color.Lerp(rc, Color.white, 0.15f + 0.08f * variant);

            float len, t0, hw, curve, taper, wave, guardW, gripT, pommelT;
            int segs = 4;
            switch (w)
            {
                case WeaponType.Rapier:
                    len = 50f; t0 = 0.34f; hw = 1.15f; curve = 0f; taper = 0.6f; wave = 0f; guardW = 4.2f;
                    gripT = 0.22f; pommelT = 0.10f;
                    if (style == 1) { hw = 0.85f; taper = 0.9f; }
                    if (style == 2) { hw = 1.3f; curve = 1.4f; taper = 0.7f; segs = 8; }
                    if (style == 3) { hw = 1.0f; taper = 0.45f; }
                    break;
                case WeaponType.GreatBlade:
                    len = 52f; t0 = 0.30f; hw = 5.2f; curve = 0f; taper = 0.45f; wave = 0f; guardW = 9f;
                    gripT = 0.17f; pommelT = 0.05f;
                    if (style == 0) { hw = 5.6f; curve = 2.2f; taper = 0.55f; segs = 6; }
                    if (style == 1) { hw = 5.0f; taper = 0.12f; }
                    if (style == 2) { hw = 4.6f; taper = 0.8f; }
                    if (style == 3) { hw = 6.0f; taper = 0.5f; wave = 0.7f; segs = 8; }
                    break;
                case WeaponType.Dagger:
                    len = 36f; t0 = 0.36f; hw = 1.9f; curve = 0f; taper = 0.9f; wave = 0f; guardW = 3.6f;
                    gripT = 0.22f; pommelT = 0.10f;
                    if (style == 1) { curve = 2.6f; taper = 0.85f; segs = 8; }
                    if (style == 2) { wave = 1.7f; taper = 0.85f; segs = 12; }
                    if (style == 3) { hw = 1.4f; taper = 0.55f; }
                    break;
                default: // Sword
                    len = 46f; t0 = 0.34f; hw = 2.4f; curve = 0f; taper = 0.85f; wave = 0f; guardW = 7f;
                    gripT = 0.22f; pommelT = 0.10f;
                    if (style == 1) { hw = 2.2f; curve = 3.6f; taper = 0.8f; segs = 8; }      // saber
                    if (style == 2) { hw = 3.0f; t0 = 0.30f; taper = 0.7f; }                   // longsword
                    if (style == 3) { hw = 2.0f; curve = -2.2f; taper = 0.95f; wave = 0.6f; segs = 8; }
                    break;
            }

            var a = Axis(len);
            var blade = BladePoly(a, t0, 1f, hw, curve, taper, wave, segs);

            // rarity halo behind the blade
            if (r >= Rarity.Rare)
            {
                var halo = BladePoly(a, t0 - 0.02f, 1.02f, hw + 2.2f, curve, taper * 0.8f, wave, segs);
                c.Poly(halo, new Color(rc.r, rc.g, rc.b, r == Rarity.Legendary ? 0.34f : 0.22f));
            }

            // grip + pommel first so the guard overlaps them
            c.Poly(Bar(a, gripT, w == WeaponType.GreatBlade ? 1.7f : 1.35f, (t0 - gripT) * 0.92f), grip);
            c.Disc(a.P(0f, pommelT).x, a.P(0f, pommelT).y, w == WeaponType.GreatBlade ? 2.4f : 1.9f, gold);
            if (w == WeaponType.GreatBlade)
                c.Poly(Bar(a, (gripT + t0) * 0.5f, 2.0f, 0.02f), gold);   // second hand wrap

            // guard
            if (w == WeaponType.Rapier)
            {
                Vector2 cup = a.P(0f, t0 - 0.045f);
                c.Disc(cup.x, cup.y, 5f, gold);
                c.Disc(cup.x, cup.y, 3.1f, new Color(gold.r * 0.4f, gold.g * 0.4f, gold.b * 0.4f, 1f));
                if (style == 2 || style == 3)
                {
                    Vector2 s0 = a.P(-5f, t0 - 0.04f), s1 = a.P(-2.5f, t0 + 0.10f);
                    Vector2 s2 = a.P(5f, t0 - 0.04f), s3 = a.P(2.5f, t0 + 0.10f);
                    c.Line(s0.x, s0.y, s1.x, s1.y, 1.6f, gold);
                    c.Line(s2.x, s2.y, s3.x, s3.y, 1.6f, gold);
                }
            }
            else
            {
                c.Poly(Bar(a, t0 - 0.015f, guardW, 0.035f), gold);
                if (w == WeaponType.Sword && style == 2)
                {
                    Vector2 g0 = a.P(-guardW, t0 - 0.02f), g1 = a.P(-guardW * 0.85f, t0 - 0.10f);
                    Vector2 g2 = a.P(guardW, t0 - 0.02f), g3 = a.P(guardW * 0.85f, t0 - 0.10f);
                    c.Line(g0.x, g0.y, g1.x, g1.y, 1.6f, gold);
                    c.Line(g2.x, g2.y, g3.x, g3.y, 1.6f, gold);
                }
            }

            // blade body + shading
            c.Poly(blade, metal);
            var spine = BladePoly(a, t0, 1f, hw * 0.42f, curve, taper, wave, segs);
            c.Poly(spine, Color.Lerp(metal, shade, 0.55f));

            // bright cutting edge along one side
            for (int i = 0; i <= segs; i++)
            {
                float k = i / (float)segs;
                if (k >= 1f) break;
                float k2 = (i + 1) / (float)segs;
                float ta = Mathf.Lerp(t0, 1f, k), tb = Mathf.Lerp(t0, 1f, k2);
                float wa = hw * (1f - taper * k), wb = hw * (1f - taper * k2);
                float oa = curve * k * k + wave * Mathf.Sin(k * Mathf.PI * 2f);
                float ob = curve * k2 * k2 + wave * Mathf.Sin(k2 * Mathf.PI * 2f);
                Vector2 p0 = a.P(oa - wa + 0.5f, ta), p1 = a.P(ob - wb + 0.5f, tb);
                c.Line(p0.x, p0.y, p1.x, p1.y, 1f, edge);
                if (r >= Rarity.Rare)
                {
                    Vector2 q0 = a.P(oa + wa - 0.4f, ta), q1 = a.P(ob + wb - 0.4f, tb);
                    c.Line(q0.x, q0.y, q1.x, q1.y, 1.2f, Color.Lerp(rc, Color.white, 0.35f));
                }
            }

            // per-style blade details
            if (w == WeaponType.Sword && style == 2)
                for (int i = 0; i < 3; i++)
                {
                    Vector2 p = a.P(0f, Mathf.Lerp(t0 + 0.10f, 0.85f, i / 2f));
                    c.Disc(p.x, p.y, 1.1f, gem);
                }
            if (w == WeaponType.GreatBlade && style == 2)
            {
                Vector2 f0 = a.P(0f, t0 + 0.06f), f1 = a.P(0f, 0.82f);
                c.Line(f0.x, f0.y, f1.x, f1.y, 1.6f, shade);
            }
            if (w == WeaponType.GreatBlade && style == 3)
            {
                Vector2 f0 = a.P(0f, t0 + 0.06f), f1 = a.P(0f, 0.86f);
                c.Line(f0.x, f0.y, f1.x, f1.y, 2.6f, new Color(0.10f, 0.08f, 0.16f, 0.9f));
            }
            if (w == WeaponType.Dagger && style == 0)
            {
                Vector2 p = a.P(0f, t0 + 0.08f);
                c.Disc(p.x, p.y, 1.3f, gem);
            }

            // pommel gem + legendary sparkle
            Vector2 pom = a.P(0f, pommelT);
            c.Disc(pom.x, pom.y, 1.0f, gem);
            if (r == Rarity.Legendary)
            {
                Vector2 tip = a.P(0f, 1f);
                c.Disc(tip.x, tip.y, 2.6f, new Color(rc.r, rc.g, rc.b, 0.45f));
                c.Disc(tip.x, tip.y, 1.2f, new Color(1f, 1f, 1f, 0.9f));
            }
        }

        // ================================================================== armor

        static Vector2[] TorsoPoly()
        {
            return new[]
            {
                V(13f, 35f), V(20f, 36f), V(24f, 34f), V(28f, 36f), V(35f, 35f),
                V(37f, 29f), V(34f, 11f), V(14f, 11f), V(11f, 29f)
            };
        }

        static void DrawArmor(IconCanvas c, int style, Rarity r, int variant)
        {
            style = ((style % 5) + 5) % 5;
            Color rc = RarityColor(r);
            Color baseCol;
            switch (style)
            {
                case 1: baseCol = new Color(0.55f, 0.58f, 0.64f); break;   // chain
                case 2: baseCol = new Color(0.72f, 0.76f, 0.82f); break;   // plate
                case 3: baseCol = new Color(0.20f, 0.36f, 0.32f); break;   // cloak
                case 4: baseCol = new Color(0.40f, 0.48f, 0.38f); break;   // scale
                default: baseCol = new Color(0.45f, 0.29f, 0.17f); break;  // leather
            }
            Color body = Color.Lerp(baseCol, rc, 0.18f + 0.06f * (int)r);
            Color dark = new Color(body.r * 0.5f, body.g * 0.5f, body.b * 0.55f, 1f);
            Color light = Color.Lerp(body, Color.white, 0.35f);
            Color trim = Color.Lerp(rc, Color.white, 0.2f);

            if (r >= Rarity.Rare)
            {
                // soft glow behind the silhouette
                c.Disc(24f, 24f, 17f, new Color(rc.r, rc.g, rc.b, r == Rarity.Legendary ? 0.20f : 0.12f));
            }

            if (style == 3)
            {
                // cape behind the hood
                c.Poly(new[] { V(24f, 36f), V(39f, 10f), V(34f, 8f), V(24f, 12f), V(14f, 8f), V(9f, 10f) }, dark);
                c.Poly(new[] { V(24f, 34f), V(35f, 12f), V(24f, 15f), V(13f, 12f) }, body);
                // hood
                c.Disc(24f, 34f, 9.5f, body);
                c.Disc(24f, 33f, 6.6f, new Color(0.05f, 0.05f, 0.08f, 1f));
                c.Disc(24f, 32f, 3.0f, new Color(rc.r, rc.g, rc.b, 0.75f));   // glowing eyes area
                c.Rect(20, 22, 8, 2, trim);
            }
            else
            {
                var torso = TorsoPoly();
                c.Poly(torso, body);
                c.PolyEdge(torso, dark);

                if (style == 0)
                {
                    // coat lapels + belt
                    c.Poly(new[] { V(24f, 34f), V(18f, 33f), V(21f, 16f), V(24f, 16f) }, dark);
                    c.Poly(new[] { V(24f, 34f), V(30f, 33f), V(27f, 16f), V(24f, 16f) }, light);
                    c.Rect(13, 13, 22, 4, dark);
                    c.Rect(22, 13, 4, 4, trim);
                }
                else if (style == 1)
                {
                    // chain mesh
                    for (int y = 13; y < 35; y += 2)
                        for (int x = 12 + ((y / 2) % 2); x < 37; x += 2)
                            c.Blend(x, y, new Color(dark.r, dark.g, dark.b, 0.75f));
                    c.Rect(17, 33, 14, 3, light);   // collar
                    c.Rect(13, 13, 22, 3, dark);
                }
                else if (style == 2)
                {
                    // plate: pauldrons, central ridge, rivets
                    c.Disc(11f, 32f, 6.5f, light);
                    c.Disc(37f, 32f, 6.5f, light);
                    c.Disc(11f, 32f, 4.2f, body);
                    c.Disc(37f, 32f, 4.2f, body);
                    c.Rect(23, 12, 2, 22, light);
                    c.Rect(17, 33, 14, 3, light);
                    for (int i = 0; i < 4; i++)
                    {
                        c.Disc(16f, 16f + i * 5f, 1.1f, dark);
                        c.Disc(32f, 16f + i * 5f, 1.1f, dark);
                    }
                }
                else
                {
                    // scale vest
                    for (int row = 0; row < 5; row++)
                    {
                        float y = 14f + row * 4.2f;
                        float xo = (row % 2 == 0) ? 0f : 2.4f;
                        for (int i = 0; i < 5; i++)
                        {
                            float x = 14f + xo + i * 4.8f;
                            if (x > 35f) continue;
                            c.Disc(x, y, 2.4f, row % 2 == 0 ? light : body);
                            c.Disc(x, y + 0.9f, 1.5f, dark);
                        }
                    }
                    c.Rect(13, 12, 22, 3, dark);
                }
            }

            // rarity treatment
            if (r >= Rarity.Uncommon)
            {
                c.Rect(13, 35, 22, 1, trim);
                c.Rect(13, 11, 22, 1, trim);
            }
            if (r >= Rarity.Rare)
            {
                c.Line(17f, 33f, 19f, 13f, 1f, new Color(trim.r, trim.g, trim.b, 0.9f));
                c.Line(31f, 33f, 29f, 13f, 1f, new Color(trim.r, trim.g, trim.b, 0.9f));
            }
            if (r >= Rarity.Epic)
            {
                // shoulder spikes
                c.Poly(new[] { V(9f, 33f), V(3f, 41f), V(13f, 37f) }, trim);
                c.Poly(new[] { V(39f, 33f), V(45f, 41f), V(35f, 37f) }, trim);
            }
            if (r == Rarity.Legendary)
            {
                // wings of light
                for (int i = 0; i < 4; i++)
                {
                    float k = i / 3f;
                    c.Line(12f - k * 2f, 30f - k * 6f, 2f, 36f - k * 9f, 1.4f, new Color(rc.r, rc.g, rc.b, 0.55f - 0.1f * i));
                    c.Line(36f + k * 2f, 30f - k * 6f, 46f, 36f - k * 9f, 1.4f, new Color(rc.r, rc.g, rc.b, 0.55f - 0.1f * i));
                }
                c.Ring(24f, 24f, 20f, 1.4f, new Color(rc.r, rc.g, rc.b, 0.35f));
            }
        }

        // ================================================================== trinkets

        static void DrawTrinket(IconCanvas c, int style, Rarity r, int variant)
        {
            style = ((style % 5) + 5) % 5;
            Color rc = RarityColor(r);
            Color gold = Color.Lerp(new Color(0.88f, 0.72f, 0.33f), rc, 0.25f);
            Color silver = Color.Lerp(new Color(0.80f, 0.84f, 0.90f), rc, 0.25f);
            Color gem = Color.Lerp(rc, Color.white, 0.15f + 0.06f * variant);
            Color glow = new Color(rc.r, rc.g, rc.b, 0.10f + 0.08f * (int)r);

            c.Disc(24f, 24f, 16f, glow);

            switch (style)
            {
                case 0: // copper ring
                    c.Ring(24f, 20f, 11f, 4.2f, gold);
                    c.Ring(24f, 20f, 11f, 1.6f, Color.Lerp(gold, Color.white, 0.4f));
                    c.Poly(new[] { V(24f, 40f), V(30f, 33f), V(24f, 27f), V(18f, 33f) }, gem);
                    c.PolyEdge(new[] { V(24f, 40f), V(30f, 33f), V(24f, 27f), V(18f, 33f) }, Color.Lerp(gem, Color.black, 0.4f));
                    break;
                case 1: // silver amulet
                    c.Arc(24f, 30f, 13f, 20f, 160f, 2.2f, silver);
                    c.Disc(24f, 18f, 8.5f, silver);
                    c.Disc(24f, 18f, 5.5f, gem);
                    c.Disc(22f, 20f, 1.8f, new Color(1f, 1f, 1f, 0.7f));
                    break;
                case 2: // lucky charm
                    c.Line(24f, 42f, 24f, 32f, 1.6f, silver);
                    c.Disc(19f, 26f, 5.2f, gem);
                    c.Disc(29f, 26f, 5.2f, gem);
                    c.Disc(24f, 18f, 5.2f, gem);
                    c.Disc(24f, 27f, 4.2f, Color.Lerp(gem, Color.white, 0.35f));
                    c.Disc(24f, 27f, 1.6f, gold);
                    break;
                case 3: // ember pendant
                    c.Line(16f, 42f, 24f, 32f, 1.6f, gold);
                    c.Line(32f, 42f, 24f, 32f, 1.6f, gold);
                    c.Poly(new[] { V(24f, 34f), V(32f, 22f), V(24f, 8f), V(16f, 22f) }, new Color(0.85f, 0.35f, 0.12f, 1f));
                    c.Poly(new[] { V(24f, 29f), V(28f, 21f), V(24f, 13f), V(20f, 21f) }, new Color(1f, 0.78f, 0.3f, 1f));
                    c.Disc(24f, 20f, 2.2f, new Color(1f, 1f, 0.85f, 0.95f));
                    break;
                default: // spire sigil
                    c.Ring(24f, 24f, 14f, 2f, gold);
                    c.Poly(new[] { V(24f, 40f), V(34f, 24f), V(24f, 8f), V(14f, 24f) }, new Color(gem.r, gem.g, gem.b, 0.55f));
                    c.Poly(new[] { V(24f, 36f), V(24f, 12f), V(29f, 22f) }, gem);
                    c.Poly(new[] { V(24f, 36f), V(24f, 12f), V(19f, 22f) }, Color.Lerp(gem, Color.black, 0.35f));
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = (45f + i * 90f) * Mathf.Deg2Rad;
                        c.Line(24f + Mathf.Cos(ang) * 14f, 24f + Mathf.Sin(ang) * 14f,
                               24f + Mathf.Cos(ang) * 19f, 24f + Mathf.Sin(ang) * 19f, 1.4f, gold);
                    }
                    break;
            }

            if (r >= Rarity.Rare) c.Ring(24f, 24f, 19f, 1.2f, new Color(rc.r, rc.g, rc.b, 0.45f));
            if (r == Rarity.Legendary)
            {
                c.Disc(9f, 38f, 1.5f, new Color(1f, 1f, 1f, 0.85f));
                c.Disc(39f, 12f, 1.5f, new Color(1f, 1f, 1f, 0.85f));
            }
        }

        // ================================================================== consumables

        static void DrawPotion(IconCanvas c, Color accent)
        {
            Color glass = new Color(0.72f, 0.82f, 0.88f);
            c.Rect(20, 30, 8, 8, glass);                       // neck
            c.Rect(19, 37, 10, 5, new Color(0.45f, 0.30f, 0.18f, 1f));   // cork
            c.Disc(24f, 19f, 12f, glass);
            c.Disc(24f, 19f, 9.6f, new Color(accent.r * 0.9f, accent.g * 0.9f, accent.b * 0.9f, 1f));
            c.Rect(21, 27, 6, 6, new Color(accent.r * 0.9f, accent.g * 0.9f, accent.b * 0.9f, 1f));
            c.Disc(21f, 22f, 3f, new Color(1f, 1f, 1f, 0.35f));           // highlight
            c.Disc(27f, 15f, 1.6f, new Color(1f, 1f, 1f, 0.5f));
            c.Ring(24f, 19f, 12f, 1.2f, new Color(1f, 1f, 1f, 0.35f));
        }

        static void DrawCrystal(IconCanvas c, Color accent)
        {
            var outer = new[] { V(24f, 44f), V(37f, 27f), V(31f, 6f), V(17f, 6f), V(11f, 27f) };
            c.Poly(outer, new Color(accent.r, accent.g, accent.b, 0.35f));
            var inner = new[] { V(24f, 41f), V(34f, 27f), V(29f, 9f), V(19f, 9f), V(14f, 27f) };
            c.Poly(inner, accent);
            c.Poly(new[] { V(24f, 41f), V(24f, 9f), V(19f, 9f), V(14f, 27f) }, Color.Lerp(accent, Color.black, 0.35f));
            c.Line(24f, 41f, 24f, 9f, 1f, new Color(1f, 1f, 1f, 0.6f));
            c.Line(14f, 27f, 34f, 27f, 1f, new Color(1f, 1f, 1f, 0.35f));
            c.PolyEdge(inner, new Color(1f, 1f, 1f, 0.5f));
        }

        static void DrawRevive(IconCanvas c, Color accent)
        {
            Color soft = new Color(accent.r, accent.g, accent.b, 0.25f);
            c.Disc(24f, 24f, 17f, soft);
            c.Ring(24f, 35f, 7f, 3.2f, accent);
            c.Rect(21, 6, 6, 27, accent);
            c.Rect(10, 23, 28, 5, accent);
            c.Rect(22, 7, 2, 25, new Color(1f, 1f, 1f, 0.55f));
            c.Rect(11, 24, 26, 1, new Color(1f, 1f, 1f, 0.45f));
            c.Ring(24f, 35f, 7f, 1.1f, new Color(1f, 1f, 1f, 0.5f));
        }

        static void DrawShard(IconCanvas c, Color accent)
        {
            c.Disc(24f, 24f, 16f, new Color(accent.r, accent.g, accent.b, 0.22f));
            var shard = new[] { V(24f, 43f), V(33f, 24f), V(29f, 6f), V(20f, 12f), V(15f, 26f) };
            c.Poly(shard, accent);
            c.Poly(new[] { V(24f, 43f), V(24f, 10f), V(20f, 12f), V(15f, 26f) }, Color.Lerp(accent, Color.black, 0.4f));
            c.PolyEdge(shard, Color.Lerp(accent, Color.white, 0.6f));
            c.Disc(25f, 30f, 2.2f, new Color(1f, 0.95f, 0.75f, 0.85f));
        }

        static void DrawCoin(IconCanvas c, Color accent)
        {
            Color dark = Color.Lerp(accent, Color.black, 0.45f);
            c.Disc(24f, 22f, 16f, dark);
            c.Disc(24f, 24f, 16f, accent);
            c.Ring(24f, 24f, 13f, 1.4f, dark);
            c.Disc(21f, 29f, 4f, new Color(1f, 1f, 1f, 0.3f));
            // star mark
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 45f) * Mathf.Deg2Rad;
                float dx = Mathf.Cos(ang) * 7f, dy = Mathf.Sin(ang) * 7f;
                c.Line(24f - dx, 24f - dy, 24f + dx, 24f + dy, 1.6f, dark);
            }
            c.Disc(24f, 24f, 2.2f, Color.Lerp(accent, Color.white, 0.5f));
        }

        // ================================================================== tiny drawing helper

        /// <summary>A 48x48 RGBA pixel buffer with alpha-blended fill/line/disc/polygon primitives (y grows up).</summary>
        sealed class IconCanvas
        {
            readonly Color[] _px = new Color[Size * Size];

            public void Clear(Color c)
            {
                for (int i = 0; i < _px.Length; i++) _px[i] = c;
            }

            public void Blend(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= Size || y >= Size || c.a <= 0f) return;
                int i = y * Size + x;
                Color d = _px[i];
                float a = c.a > 1f ? 1f : c.a;
                _px[i] = new Color(d.r + (c.r - d.r) * a,
                                   d.g + (c.g - d.g) * a,
                                   d.b + (c.b - d.b) * a,
                                   d.a + (1f - d.a) * a);
            }

            public void Rect(int x, int y, int w, int h, Color c)
            {
                for (int yy = y; yy < y + h; yy++)
                    for (int xx = x; xx < x + w; xx++)
                        Blend(xx, yy, c);
            }

            /// <summary>1px outline of a rectangle.</summary>
            public void Frame(int x, int y, int w, int h, Color c)
            {
                if (w <= 0 || h <= 0) return;
                Rect(x, y, w, 1, c);
                Rect(x, y + h - 1, w, 1, c);
                Rect(x, y, 1, h, c);
                Rect(x + w - 1, y, 1, h, c);
            }

            public void Disc(float cx, float cy, float r, Color c)
            {
                if (r <= 0f) return;
                int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
                int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
                float r2 = r * r;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy <= r2) Blend(x, y, c);
                    }
            }

            public void Ring(float cx, float cy, float r, float thick, Color c)
            {
                float half = thick * 0.5f;
                int x0 = Mathf.FloorToInt(cx - r - half), x1 = Mathf.CeilToInt(cx + r + half);
                int y0 = Mathf.FloorToInt(cy - r - half), y1 = Mathf.CeilToInt(cy + r + half);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (Mathf.Abs(d - r) <= half) Blend(x, y, c);
                    }
            }

            /// <summary>Stroked circular arc (angles in degrees, counter-clockwise from +X).</summary>
            public void Arc(float cx, float cy, float r, float startDeg, float endDeg, float thick, Color c)
            {
                float span = endDeg - startDeg;
                int steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(span) * r / 45f));
                float half = Mathf.Max(0.5f, thick * 0.5f);
                for (int i = 0; i <= steps; i++)
                {
                    float ang = (startDeg + span * (i / (float)steps)) * Mathf.Deg2Rad;
                    Disc(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r, half, c);
                }
            }

            public void Line(float x0, float y0, float x1, float y1, float thick, Color c)
            {
                float dx = x1 - x0, dy = y1 - y0;
                int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 2f));
                float half = Mathf.Max(0.5f, thick * 0.5f);
                for (int i = 0; i <= steps; i++)
                {
                    float k = i / (float)steps;
                    Disc(x0 + dx * k, y0 + dy * k, half, c);
                }
            }

            /// <summary>Even-odd polygon fill (points in pixel space).</summary>
            public void Poly(Vector2[] p, Color c)
            {
                if (p == null || p.Length < 3) return;
                float minX = p[0].x, maxX = p[0].x, minY = p[0].y, maxY = p[0].y;
                for (int i = 1; i < p.Length; i++)
                {
                    if (p[i].x < minX) minX = p[i].x;
                    if (p[i].x > maxX) maxX = p[i].x;
                    if (p[i].y < minY) minY = p[i].y;
                    if (p[i].y > maxY) maxY = p[i].y;
                }
                int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(maxX));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(maxY));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        if (Inside(p, x + 0.5f, y + 0.5f)) Blend(x, y, c);
            }

            /// <summary>1px outline along the polygon edges.</summary>
            public void PolyEdge(Vector2[] p, Color c)
            {
                if (p == null || p.Length < 2) return;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                    Line(p[j].x, p[j].y, p[i].x, p[i].y, 1f, c);
            }

            static bool Inside(Vector2[] p, float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    if ((p[i].y > y) != (p[j].y > y) &&
                        x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                        inside = !inside;
                }
                return inside;
            }

            /// <summary>Adds a soft radial tint (strength 0 darkens the corners instead).</summary>
            public void RadialGlow(float cx, float cy, float radius, Color c, float strength)
            {
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(0.001f, radius);
                        if (strength > 0f)
                        {
                            float k = Mathf.Clamp01(1f - d);
                            Blend(x, y, new Color(c.r, c.g, c.b, strength * k * k));
                        }
                        else
                        {
                            float k = Mathf.Clamp01(d - 0.55f) * 0.55f;
                            Blend(x, y, new Color(0f, 0f, 0f, k));
                        }
                    }
            }

            public Sprite ToSprite(string name)
            {
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                tex.name = "Icon_" + name;
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels(_px);
                tex.Apply(false, false);
                var sp = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
                sp.name = "Icon_" + name;
                return sp;
            }
        }
    }
}
