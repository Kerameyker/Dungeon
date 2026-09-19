using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>Colors and text helpers for items (rich text is used in the HUD).</summary>
    public static class ItemUi
    {
        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.4f, 1f, 0.5f);
                case Rarity.Rare: return new Color(0.35f, 0.6f, 1f);
                case Rarity.Epic: return new Color(0.75f, 0.4f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.65f, 0.2f);
                default: return new Color(0.82f, 0.82f, 0.82f);
            }
        }

        public static string Hex(Rarity r) { return "#" + ColorUtility.ToHtmlStringRGB(RarityColor(r)); }

        public static string Colored(Item i)
        {
            return "<color=" + Hex(i.Rarity) + ">[" + i.Rarity + "] " + i.Name + "</color>";
        }

        public static string Stats(Item i)
        {
            string s = "";
            if (i.Attack > 0) s += "ATK +" + i.Attack + "  ";
            if (i.Defense > 0) s += "DEF +" + i.Defense + "  ";
            if (i.MaxHp > 0) s += "HP +" + i.MaxHp + "  ";
            if (i.Crit > 0f) s += "CRIT +" + Mathf.RoundToInt(i.Crit * 100f) + "%  ";
            return s.TrimEnd();
        }
    }

    /// <summary>A gear drop lying on the ground. Walk over it to pick it up.</summary>
    public class LootDrop : MonoBehaviour
    {
        Item _item;
        Vector3 _basePos;
        Transform _gem;
        float _t;
        static float _nextFullToast;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _nextFullToast = 0f; }

        public static void Spawn(Vector3 pos, Item item)
        {
            var go = new GameObject("Loot_" + item.Name.Replace(" ", ""));
            pos.y = 0f;
            go.transform.position = pos;
            Color c = ItemUi.RarityColor(item.Rarity);
            var mat = ProcAssets.Unlit(c);

            var gem = ProcAssets.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.7f, 0f), Vector3.one * 0.32f, mat, false, "Gem");
            gem.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);

            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 1f, 0f);
            l.type = LightType.Point;
            l.color = c;
            l.range = 4f + (int)item.Rarity;
            l.intensity = 1.2f + 0.4f * (int)item.Rarity;

            if (item.Rarity >= Rarity.Rare)
                ProcAssets.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 2.2f, 0f), new Vector3(0.07f, 2.2f, 0.07f), mat, false, "Beam");

            var drop = go.AddComponent<LootDrop>();
            drop._item = item;
            drop._basePos = go.transform.position;
            drop._gem = gem.transform;
        }

        void Update()
        {
            _t += Time.deltaTime;
            _gem.localPosition = new Vector3(0f, 0.7f + Mathf.Sin(_t * 3f) * 0.12f, 0f);
            _gem.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);

            var g = Game.Instance;
            if (g == null || g.Player == null || g.Player.IsDead || g.Paused) return;
            Vector3 d = g.Player.transform.position - _basePos;
            d.y = 0f;
            if (d.magnitude > 1.9f) return;

            if (g.Player.AddItem(_item))
            {
                Spark.Burst(_basePos + Vector3.up * 0.7f, ItemUi.RarityColor(_item.Rarity), 10, 4f);
                Destroy(gameObject);
            }
            else if (Time.time > _nextFullToast)
            {
                _nextFullToast = Time.time + 3f;
                Hud.Instance.Toast("Backpack full!  Press I to discard something (X)", 2f);
            }
        }
    }
}
