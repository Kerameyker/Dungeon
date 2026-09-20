using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>All modal UI flows: inventory, merchant, blacksmith, quest board, dialogue and the ending choice.</summary>
    public partial class Game
    {
        enum UiMode { None, Inventory, Shop, Smith, Quests, Dialogue, Choice }

        UiMode _mode = UiMode.None;
        int _modeFrame = -1, _toggleFrame = -1;
        int _cursor, _scroll;
        bool _shopSell, _mouseArmed;
        Vector2 _lastPtr;
        const int VisibleRows = 14;

        readonly List<MenuRow> _rows = new List<MenuRow>();
        readonly List<System.Action> _acts = new List<System.Action>();
        readonly List<string> _details = new List<string>();
        string _footer = "";
        readonly Queue<DialogueLine> _lines = new Queue<DialogueLine>();
        System.Action _afterDialogue;
        readonly Queue<System.Action> _pending = new Queue<System.Action>();
        readonly System.Random _shopRng = new System.Random();

        // ------------------------------------------------------------------ mode plumbing

        void SetMode(UiMode m)
        {
            _mode = m;
            _modeFrame = Time.frameCount;
            _toggleFrame = Time.frameCount;
            Time.timeScale = m == UiMode.None ? 1f : 0f;
            CameraRig.SetCursorLocked(m == UiMode.None || m == UiMode.Dialogue);
            _cursor = 0;
            _scroll = 0;
        }

        /// <summary>Closes any open panel and resumes the game.</summary>
        void CloseUi()
        {
            var hud = Hud.Instance;
            if (hud != null)
            {
                hud.ShowInventory(false);
                hud.HideMenu();
                hud.HideDialogue();
            }
            _lines.Clear();
            _afterDialogue = null;
            if (_mode != UiMode.None)
            {
                Sfx.Play2D(SfxKind.UiClose, 0.6f);
                SetMode(UiMode.None);
            }
        }

        /// <summary>Runs an action as soon as no panel is open.</summary>
        void WhenFree(System.Action a)
        {
            if (_mode == UiMode.None && !Player.IsDead) a();
            else _pending.Enqueue(a);
        }

        void RunPending()
        {
            if (_pending.Count > 0 && !Player.IsDead) _pending.Dequeue()();
        }

        void UpdateUi()
        {
            switch (_mode)
            {
                case UiMode.Inventory: UpdateInventory(); break;
                case UiMode.Shop:
                case UiMode.Smith:
                case UiMode.Quests:
                case UiMode.Choice: UpdateMenu(); break;
                case UiMode.Dialogue: UpdateDialogue(); break;
            }
        }

        // ------------------------------------------------------------------ inventory

        void OpenInventory()
        {
            if (Player == null || Player.IsDead) return;
            SetMode(UiMode.Inventory);
            Sfx.Play2D(SfxKind.UiOpen, 0.6f);
            Hud.Instance.ShowInventory(true);
            Hud.Instance.RefreshInventory(Player, _cursor);
        }

        void UpdateInventory()
        {
            if (GameInput.MenuBackPressed || GameInput.EscapePressed) { CloseUi(); return; }
            var items = Player.Bag.Items;
            int n = items.Count;
            bool changed = false;
            Vector2 ptr = GameInput.PointerPosition;
            bool moved = (ptr - _lastPtr).sqrMagnitude > 1f;
            _lastPtr = ptr;

            if (n > 0)
            {
                if (GameInput.MenuLeftPressed) { _cursor = (_cursor - 1 + n) % n; changed = true; }
                if (GameInput.MenuRightPressed) { _cursor = (_cursor + 1) % n; changed = true; }
                if (GameInput.MenuUpPressed && _cursor - 5 >= 0) { _cursor -= 5; changed = true; }
                if (GameInput.MenuDownPressed && _cursor + 5 < n) { _cursor += 5; changed = true; }
                int scroll = GameInput.ScrollStep;
                if (scroll != 0) { _cursor = (_cursor - scroll + n) % n; changed = true; }
            }

            bool mouse = !GameInput.UsingGamepad;
            int hover = mouse ? Hud.Instance.InventoryRowAt(ptr) : -1;
            if (hover >= 0 && hover < n)
            {
                if (moved && hover != _cursor) { _cursor = hover; changed = true; }
                if (GameInput.MenuClickPressed) { _cursor = hover; Player.EquipFromBag(hover); changed = true; }
                else if (GameInput.RightClickPressed) { Player.DiscardFromBag(hover); changed = true; }
            }
            else if (mouse && GameInput.MenuClickPressed)
            {
                int slot = Hud.Instance.InventoryGearSlotAt(ptr);
                if (slot >= 0) { Player.UnequipToBag((ItemSlot)slot); changed = true; }
            }

            n = items.Count;
            if (n > 0 && GameInput.ConfirmPressed) { Player.EquipFromBag(Mathf.Clamp(_cursor, 0, n - 1)); changed = true; }
            else if (n > 0 && GameInput.DiscardPressed) { Player.DiscardFromBag(Mathf.Clamp(_cursor, 0, n - 1)); changed = true; }

            if (changed)
            {
                _cursor = Mathf.Clamp(_cursor, 0, Mathf.Max(0, Player.Bag.Items.Count - 1));
                Hud.Instance.RefreshInventory(Player, _cursor);
            }
        }

        // ------------------------------------------------------------------ generic list menu

        void OpenMenu(UiMode mode, string title, string help)
        {
            SetMode(mode);
            Sfx.Play2D(SfxKind.UiOpen, 0.6f);
            Hud.Instance.ShowMenu(title, help);
            RebuildMenu();
        }

        void AddRow(string text, Sprite icon, Color border, bool dim, string detail, System.Action act)
        {
            _rows.Add(new MenuRow { Text = text, Icon = icon, Border = border, Dim = dim });
            _details.Add(detail ?? "");
            _acts.Add(act);
        }

        void RebuildMenu()
        {
            _rows.Clear();
            _acts.Clear();
            _details.Clear();
            _footer = "";
            switch (_mode)
            {
                case UiMode.Shop: BuildShop(); break;
                case UiMode.Smith: BuildSmith(); break;
                case UiMode.Quests: BuildQuests(); break;
                case UiMode.Choice: BuildChoice(); break;
            }
            _cursor = Mathf.Clamp(_cursor, 0, Mathf.Max(0, _rows.Count - 1));
            _scroll = Mathf.Clamp(_scroll, 0, Mathf.Max(0, _rows.Count - VisibleRows));
            if (_cursor < _scroll) _scroll = _cursor;
            if (_cursor >= _scroll + VisibleRows) _scroll = _cursor - VisibleRows + 1;
            _scroll = Mathf.Max(0, _scroll);
            int count = Mathf.Min(VisibleRows, _rows.Count - _scroll);
            var slice = count > 0 ? _rows.GetRange(_scroll, count) : new List<MenuRow>();
            string detail = _cursor < _details.Count ? _details[_cursor] : "";
            Hud.Instance.SetMenu(slice, _cursor - _scroll, detail, _footer);
        }

        void UpdateMenu()
        {
            if (_mode != UiMode.Choice && (GameInput.MenuBackPressed || GameInput.EscapePressed)) { CloseUi(); return; }
            if (_mode == UiMode.Shop && (GameInput.MenuLeftPressed || GameInput.MenuRightPressed))
            {
                _shopSell = !_shopSell;
                _cursor = 0;
                _scroll = 0;
                Sfx.Play2D(SfxKind.UiClick, 0.6f);
                RebuildMenu();
                return;
            }

            int n = _rows.Count;
            bool changed = false, activate = false;
            if (n > 0)
            {
                if (GameInput.MenuUpPressed) { _cursor = (_cursor - 1 + n) % n; changed = true; }
                if (GameInput.MenuDownPressed) { _cursor = (_cursor + 1) % n; changed = true; }
                int wheel = GameInput.ScrollStep;
                if (wheel != 0) { _cursor = Mathf.Clamp(_cursor - wheel, 0, n - 1); changed = true; }
                if (changed) _mouseArmed = false;   // the list moved under a resting pointer: clicks act on the highlighted row until the mouse moves
            }
            Vector2 ptr = GameInput.PointerPosition;
            bool moved = (ptr - _lastPtr).sqrMagnitude > 1f;
            _lastPtr = ptr;
            if (!GameInput.UsingGamepad)
            {
                int hv = Hud.Instance.MenuRowAt(ptr);
                if (hv >= 0 && hv + _scroll < n)
                {
                    int abs = hv + _scroll;
                    if (moved) _mouseArmed = true;
                    if (moved && abs != _cursor) { _cursor = abs; changed = true; }
                    if (GameInput.MenuClickPressed && _mouseArmed) { _cursor = abs; activate = true; }
                }
            }
            if (GameInput.ConfirmPressed || GameInput.InteractPressed) activate = true;

            if (activate && _cursor >= 0 && _cursor < _acts.Count && _acts[_cursor] != null)
            {
                _acts[_cursor]();
                if (_mode != UiMode.None && _mode != UiMode.Dialogue) RebuildMenu();
                return;
            }
            if (changed) { Sfx.Play2D(SfxKind.UiClick, 0.4f); RebuildMenu(); }
        }

        // ------------------------------------------------------------------ merchant

        public void OpenShop()
        {
            if (Player == null || Player.IsDead || _mode != UiMode.None) return;
            _shopSell = false;
            OpenMenu(UiMode.Shop, "MERCHANT  BRANNOC", "Left / Right: Buy - Sell      Enter / click: confirm      Esc: close");
        }

        void BuildShop()
        {
            var p = Player;
            _footer = "<color=#FFD24A>Gold  " + p.Gold + "</color>     <color=#FF8C4D>Shards  " + p.Shards + "</color>     " +
                      (_shopSell ? "<b>[ SELL ]</b>  buy" : "<b>[ BUY ]</b>  sell");
            if (!_shopSell)
            {
                AddRow("Health Potion  -  " + Consumables.PotionPrice + " g     (" + p.Supplies.Potions + " / " + Consumables.PotionCap + ")",
                       ItemIcons.Consumable(ItemIcons.ConsumableIcon.Potion), Color.clear, p.Gold < Consumables.PotionPrice,
                       "Restores 40% of your health.\nPress H in the tower. 6 second cooldown.", () => BuySupply(0));
                AddRow("Return Crystal  -  " + Consumables.CrystalPrice + " g     (" + p.Supplies.ReturnCrystals + " / " + Consumables.CrystalCap + ")",
                       ItemIcons.Consumable(ItemIcons.ConsumableIcon.Crystal), Color.clear, p.Gold < Consumables.CrystalPrice,
                       "Teleports you to town even in combat.\nPress T; channels for 1.5 s and breaks if you are hit.\nDoes not work in a sealed boss room.", () => BuySupply(1));
                AddRow("Revive Token  -  " + Consumables.RevivePrice + " g     (" + p.Supplies.ReviveTokens + " / " + Consumables.ReviveCap + ")",
                       ItemIcons.Consumable(ItemIcons.ConsumableIcon.Revive), Color.clear, p.Gold < Consumables.RevivePrice,
                       "Used automatically when your health reaches zero.\nRevives you with 50% health.", () => BuySupply(2));
                int price = Economy.BuyPrice(MaxFloor);
                AddRow("Random gear  -  " + price + " g", ItemIcons.Consumable(ItemIcons.ConsumableIcon.Gold), Color.clear, p.Gold < price,
                       "A random Uncommon-or-better piece scaled to your highest floor.\nCould be any slot and any weapon type.", () => BuyGear(price));
            }
            else
            {
                int cheap = 0, cheapGold = 0;
                foreach (var it in p.Bag.Items)
                    if (it.Rarity <= Rarity.Uncommon) { cheap++; cheapGold += Economy.SellValue(it); }
                AddRow("Sell all Common / Uncommon  (" + cheap + " items,  +" + cheapGold + " g)", ItemIcons.Consumable(ItemIcons.ConsumableIcon.Gold),
                       Color.clear, cheap == 0, "Quickly clears out the junk. Equipped gear is never sold.", SellCheap);
                foreach (var it in new List<Item>(p.Bag.Items))
                {
                    var item = it;
                    int v = Economy.SellValue(item);
                    AddRow(item.DisplayName + "   +" + v + " g", ItemIcons.Get(item), ItemIcons.RarityColor(item.Rarity), false,
                           "<color=" + ItemUi.Hex(item.Rarity) + ">" + item.Rarity + "</color>   " + ItemUi.Stats(item) + "\nSell value  " + v + " g",
                           () => SellOne(item, v));
                }
                if (p.Bag.Items.Count == 0) AddRow("Your backpack is empty.", null, Color.clear, true, "", null);
            }
        }

        void BuySupply(int kind)
        {
            var p = Player;
            int price = kind == 0 ? Consumables.PotionPrice : (kind == 1 ? Consumables.CrystalPrice : Consumables.RevivePrice);
            if (p.Gold < price) { Hud.Instance.Toast("Merchant:  Not enough gold.", 1.5f); Sfx.Play2D(SfxKind.Blip, 0.5f); return; }
            bool ok = kind == 0 ? p.Supplies.AddPotion() : (kind == 1 ? p.Supplies.AddCrystal() : p.Supplies.AddRevive());
            if (!ok) { Hud.Instance.Toast("You can't carry any more of those.", 1.5f); return; }
            p.Gold -= price;
            Sfx.Play2D(SfxKind.Coin);
            Save();
        }

        void BuyGear(int price)
        {
            var p = Player;
            if (p.Gold < price) { Hud.Instance.Toast("Merchant:  Not enough gold.", 1.5f); Sfx.Play2D(SfxKind.Blip, 0.5f); return; }
            if (p.Bag.IsFull) { Hud.Instance.Toast("Backpack is full.", 1.5f); return; }
            p.Gold -= price;
            var item = LootGenerator.RollOf(_shopRng, MaxFloor, LootGenerator.RollSlot(_shopRng), LootGenerator.RollEliteRarity(_shopRng));
            p.Bag.Add(item);
            Sfx.Play2D(SfxKind.Coin);
            Hud.Instance.Toast("Bought  " + ItemUi.Colored(item), 2.2f);
            Save();
        }

        void SellOne(Item item, int value)
        {
            if (!Player.Bag.Items.Remove(item)) return;
            Player.Gold += value;
            Sfx.Play2D(SfxKind.Coin);
            Save();
        }

        void SellCheap()
        {
            var items = Player.Bag.Items;
            int gold = 0, n = 0;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].Rarity > Rarity.Uncommon) continue;
                gold += Economy.SellValue(items[i]);
                items.RemoveAt(i);
                n++;
            }
            if (n == 0) return;
            Player.Gold += gold;
            Sfx.Play2D(SfxKind.Coin);
            Hud.Instance.Toast("Sold " + n + " items  +" + gold + " g", 2f);
            Save();
        }

        // ------------------------------------------------------------------ blacksmith

        public void OpenSmith()
        {
            if (Player == null || Player.IsDead || _mode != UiMode.None) return;
            OpenMenu(UiMode.Smith, "BLACKSMITH  DORN", "Enter / click: upgrade the selected item      Esc: close");
        }

        void BuildSmith()
        {
            var p = Player;
            _footer = "<color=#FFD24A>Gold  " + p.Gold + "</color>     <color=#FF8C4D>Shards  " + p.Shards + "</color>";
            for (int i = 0; i < 3; i++)
                if (p.Gear.Slots[i] != null) AddSmithRow(p.Gear.Slots[i], true);
            foreach (var it in p.Bag.Items) AddSmithRow(it, false);
            if (_rows.Count == 0) AddRow("Nothing to upgrade yet.", null, Color.clear, true, "Bring gear to the forge. Each +1 adds 8% to its stats.", null);
        }

        void AddSmithRow(Item item, bool worn)
        {
            bool max = item.Upgrade >= Smithing.MaxUpgrade;
            int gold = Smithing.GoldCost(item), shards = Smithing.ShardCost(item);
            bool afford = Player.Gold >= gold && Player.Shards >= shards;
            string text = (worn ? "[worn]  " : "") + item.DisplayName + (max ? "    MAX" : "    +" + item.Upgrade + " > +" + (item.Upgrade + 1));
            string detail = "<color=" + ItemUi.Hex(item.Rarity) + "><b>" + item.DisplayName + "</b></color>   " + item.Rarity + "\n" + ItemUi.Stats(item) + "\n\n";
            if (max) detail += "Fully upgraded.";
            else
            {
                detail += "Next level  +" + (item.Upgrade + 1) + "\n";
                detail += "Success chance  <b>" + Mathf.RoundToInt(Smithing.SuccessChance(item.Upgrade) * 100f) + "%</b>\n";
                detail += "Cost  <color=" + (Player.Gold >= gold ? "#FFD24A" : "#FF5555") + ">" + gold + " g</color>   " +
                          "<color=" + (Player.Shards >= shards ? "#FF8C4D" : "#FF5555") + ">" + shards + " shards</color>\n";
                if (item.Upgrade >= 5) detail += "\n<color=#FF7777>A failure lowers this item by one level.</color>";
                else detail += "\nA failure only costs the materials.";
            }
            AddRow(text, ItemIcons.Get(item), ItemIcons.RarityColor(item.Rarity), max || !afford, detail, () => SmithAttempt(item));
        }

        void SmithAttempt(Item item)
        {
            var p = Player;
            int gold = p.Gold, shards = p.Shards;
            var result = Smithing.Attempt(item, _shopRng, ref gold, ref shards);
            switch (result)
            {
                case SmithResult.MaxLevel: Hud.Instance.Toast("Already at maximum level.", 1.4f); return;
                case SmithResult.NotEnoughGold: Hud.Instance.Toast("Dorn:  Not enough gold.", 1.4f); Sfx.Play2D(SfxKind.Blip, 0.5f); return;
                case SmithResult.NotEnoughShards: Hud.Instance.Toast("Dorn:  Not enough Ember Shards.", 1.4f); Sfx.Play2D(SfxKind.Blip, 0.5f); return;
            }
            p.Gold = gold;
            p.Shards = shards;
            if (result == SmithResult.Success)
            {
                Sfx.Play2D(SfxKind.Upgrade);
                Hud.Instance.Toast("<color=#7CFF8A>SUCCESS</color>   " + item.DisplayName, 2f);
                Spark.Burst(p.transform.position + Vector3.up, new Color(1f, 0.8f, 0.3f), 24, 5f, 0.12f, 0.7f);
            }
            else
            {
                Sfx.Play2D(SfxKind.UpgradeFail);
                Hud.Instance.Toast(result == SmithResult.FailedDowngrade
                    ? "<color=#FF7777>FAILED</color>   the item dropped to " + item.DisplayName
                    : "<color=#FF7777>FAILED</color>   the materials are lost", 2.2f);
            }
            // equipped items change the hero's stats and look
            for (int i = 0; i < 3; i++)
            {
                if (p.Gear.Slots[i] != item) continue;
                p.RecalcStats(false);
                p.RefreshLoadout();
                break;
            }
            Save();
        }

        // ------------------------------------------------------------------ quest board

        public void OpenQuestBoard()
        {
            if (Player == null || Player.IsDead || _mode != UiMode.None) return;
            OpenMenu(UiMode.Quests, "GUILD BOARD  -  TESSA", "Enter / click: accept or hand in      Esc: close      (max " + QuestLog.MaxActive + " active)");
        }

        void BuildQuests()
        {
            _footer = "<color=#FFD24A>Gold  " + Player.Gold + "</color>     <color=#FF8C4D>Shards  " + Player.Shards + "</color>";
            foreach (var st in new List<QuestState>(Quests.Active))
            {
                var def = QuestCatalog.Get(st.Id);
                if (def == null) continue;
                var state = st;
                string prog = def.Kind == QuestKind.EarnGold ? state.Progress + " / " + def.Count + " g" : state.Progress + " / " + def.Count;
                AddRow((state.Done ? "<color=#7CFF8A>[READY]</color>  " : "[active]  ") + def.Title + "    " + prog, null,
                       state.Done ? new Color(0.4f, 1f, 0.5f) : new Color(0.3f, 0.7f, 1f), false,
                       QuestDetail(def) + "\n\nProgress  " + prog + (state.Done ? "\n<color=#7CFF8A>Press Enter to hand in.</color>" : ""),
                       state.Done ? (System.Action)(() => ClaimQuest(def.Id)) : null);
            }
            foreach (var def in Quests.Available(MaxFloor))
            {
                var d = def;
                bool full = Quests.Active.Count >= QuestLog.MaxActive;
                AddRow("[new]  " + d.Title, null, Color.clear, full,
                       QuestDetail(d) + (full ? "\n\n<color=#FF7777>You already carry " + QuestLog.MaxActive + " quests.</color>" : "\n\nPress Enter to accept."),
                       () => AcceptQuest(d.Id));
            }
            if (_rows.Count == 0) AddRow("No quests right now.", null, Color.clear, true, "Reach higher floors to unlock more jobs.", null);
        }

        static string QuestDetail(QuestDef d)
        {
            string r = "<b>" + d.Title + "</b>\n" + d.Description + "\n\nReward:  " + d.RewardGold + " gold";
            if (d.RewardShards > 0) r += ",  " + d.RewardShards + " shards";
            if (d.RewardPotions > 0) r += ",  " + d.RewardPotions + " potions";
            return r;
        }

        void AcceptQuest(string id)
        {
            if (!Quests.Accept(id, MaxFloor)) { Hud.Instance.Toast("You can't take another quest.", 1.4f); return; }
            Sfx.Play2D(SfxKind.UiClick);
            Hud.Instance.Toast("Quest accepted", 1.4f);
            Save();
        }

        void ClaimQuest(string id)
        {
            var def = Quests.Claim(id);
            if (def == null) return;
            Player.Gold += def.RewardGold;
            if (def.RewardShards > 0) Player.Shards += def.RewardShards;
            if (def.RewardPotions > 0) Player.Supplies.AddPotion(Mathf.Min(def.RewardPotions, Consumables.PotionCap - Player.Supplies.Potions));
            _notified.Remove(id);
            Sfx.Play2D(SfxKind.Chime);
            Hud.Instance.Toast("Quest complete:  " + def.Title + "\n+" + def.RewardGold + " g", 2.5f);
            Save();
        }

        // ------------------------------------------------------------------ quest tracker

        readonly HashSet<string> _notified = new HashSet<string>();
        float _trackerAt;
        string _trackerCache = "";

        void UpdateQuestTracker()
        {
            if (Time.unscaledTime < _trackerAt) return;
            _trackerAt = Time.unscaledTime + 0.4f;
            var sb = new System.Text.StringBuilder();
            foreach (var st in Quests.Active)
            {
                var def = QuestCatalog.Get(st.Id);
                if (def == null) continue;
                if (sb.Length == 0) sb.Append("<b>QUESTS</b>");
                sb.Append("\n");
                if (st.Done) sb.Append("<color=#7CFF8A>").Append(def.Title).Append("  (hand in)</color>");
                else sb.Append(def.Title).Append("  ").Append(st.Progress).Append("/").Append(def.Count);
                if (st.Done && _notified.Add(st.Id))
                {
                    Hud.Instance.Toast("Quest ready:  " + def.Title + "\nReport to the guild board in town", 3f);
                    Sfx.Play2D(SfxKind.Chime, 0.7f);
                }
            }
            string s = sb.ToString();
            if (s != _trackerCache)
            {
                _trackerCache = s;
                Hud.Instance.SetQuestTracker(s);
            }
        }

        // ------------------------------------------------------------------ dialogue

        static Color SpeakerColor(string who)
        {
            switch (who)
            {
                case "Miri": return new Color(1f, 0.6f, 0.68f);
                case "Architect": return new Color(1f, 0.82f, 0.35f);
                case "Corvin": return new Color(1f, 0.6f, 0.3f);
                case "Tessa": return new Color(0.6f, 1f, 0.7f);
                case "Wanderer": return new Color(0.5f, 0.9f, 1f);
                default: return new Color(0.85f, 0.9f, 1f);
            }
        }

        void PlayDialogue(DialogueLine[] lines, System.Action after)
        {
            if (lines == null || lines.Length == 0) { if (after != null) after(); return; }
            _lines.Clear();
            foreach (var l in lines) _lines.Enqueue(l);
            _afterDialogue = after;
            SetMode(UiMode.Dialogue);
            Sfx.Play2D(SfxKind.UiOpen, 0.5f);
            ShowLine();
        }

        void ShowLine()
        {
            var l = _lines.Dequeue();
            Hud.Instance.ShowDialogue(l.Speaker, l.Text, SpeakerColor(l.Speaker), _lines.Count > 0);
            Sfx.Play2D(SfxKind.Blip, 0.35f);
        }

        void UpdateDialogue()
        {
            if (!(GameInput.ConfirmPressed || GameInput.InteractPressed || GameInput.MenuClickPressed)) return;
            if (_lines.Count > 0) { ShowLine(); return; }
            Hud.Instance.HideDialogue();
            var after = _afterDialogue;
            _afterDialogue = null;
            SetMode(UiMode.None);
            if (after != null) after();
        }

        // ------------------------------------------------------------------ ending choice

        void OpenChoice()
        {
            OpenMenu(UiMode.Choice, "THE SEAL IS BROKEN", "Up / Down: choose      Enter / click: confirm");
        }

        void BuildChoice()
        {
            AddRow("Leave the Spire", null, new Color(0.5f, 0.9f, 1f), false,
                   "Step through the door into the light.\nThe season ends here. You keep your character and can still return to the tower.", () => FinishEnding(true));
            AddRow("Stay and keep climbing", null, new Color(1f, 0.7f, 0.3f), false,
                   "There are still people inside the Spire.\nA stairway opens: floors beyond twenty follow.", () => FinishEnding(false));
        }

        void FinishEnding(bool leave)
        {
            Hud.Instance.HideMenu();
            Flags.Add(leave ? "ending_leave" : "ending_stay");
            SetMode(UiMode.None);
            PlayDialogue(leave ? StoryCatalog.EndingLeave : StoryCatalog.EndingStay, () =>
            {
                Flags.Add("ending_done");
                Hud.Instance.ShowBanner("SEASON ONE COMPLETE", leave ? "You walked out of the Spire" : "The climb goes on", 4f);
                Sfx.Play2D(SfxKind.Chime);
                if (leave) GoTown();
                else
                {
                    if (!InTown)
                    {
                        _stairs = Stairs.Spawn(_bossPos);
                        Hud.Instance.MarkStairs(_bossPos);
                    }
                }
                Save();
            });
        }
    }
}
