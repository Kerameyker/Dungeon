using System.Collections;
using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>Story triggers and effects, save/load of the whole game state, hardcore mode and run reset.</summary>
    public partial class Game
    {
        bool _noSave;
        readonly HashSet<string> _queuedBeats = new HashSet<string>();
        static bool _freshRun;

        // ------------------------------------------------------------------ story

        void TryStory(StoryTrigger trigger, int floor = -1)
        {
            var beat = StoryCatalog.Next(trigger, floor < 0 ? Floor : floor, MaxFloor, Flags);
            if (beat == null || !_queuedBeats.Add(beat.Id)) return;
            WhenFree(() => PlayBeat(beat, trigger));
        }

        void PlayBeat(StoryBeat beat, StoryTrigger trigger)
        {
            if (Flags.Has(beat.Id)) { _queuedBeats.Remove(beat.Id); return; }
            int floor = Floor;
            if (string.IsNullOrEmpty(beat.Effect)) Flags.Add(beat.Id);   // scenes with an effect are flagged when they finish, so a quit mid-scene replays them
            PlayDialogue(beat.Lines, () =>
            {
                Flags.Add(beat.Id);
                _queuedBeats.Remove(beat.Id);
                ApplyEffect(beat.Effect);
                Save();
                TryStory(trigger, floor);   // chained scenes that unlock right after this one
            });
        }

        IEnumerator StoryAfterBoss(int floor)
        {
            yield return new WaitForSeconds(1.6f);
            while (Player != null && Player.IsDead) yield return null;
            if (Player == null) yield break;
            TryStory(StoryTrigger.BossKilled, floor);
        }

        void ApplyEffect(string effect)
        {
            if (string.IsNullOrEmpty(effect)) return;
            switch (effect)
            {
                case "join_miri":
                    EnsureCompanion();
                    Hud.Instance.Toast("<color=#FF99AA>Miri</color> joins your party\nG  -  Switch: when an enemy staggers, she finishes it", 4f);
                    Sfx.Play2D(SfxKind.Chime);
                    break;
                case "unlock_dual":
                    Player.SetDual(true);
                    Hud.Instance.Toast("NEW SKILL  -  Twin Tempest  (key 5)", 4f);
                    Sfx.Play2D(SfxKind.LevelUp);
                    GroundRing.Spawn(Player.transform.position, 0.5f, 5f, new Color(0.5f, 0.9f, 1f, 0.9f), 0.9f);
                    break;
                case "reveal":
                    Hud.Instance.Toast("The Architect waits on floor 20", 3f);
                    break;
                case "ending":
                    OpenChoice();
                    break;
            }
        }

        // ------------------------------------------------------------------ hardcore

        public void SetHardcore(bool on)
        {
            if (Hardcore == on) return;
            Hardcore = on;
            Sfx.Play2D(SfxKind.Blip, 0.7f);
            Hud.Instance.Toast(on ? "HARDCORE  on\nIf you die, the save is deleted" : "Hardcore  off", 2.5f);
            Save();
        }

        /// <summary>Wipes the save and boots a completely fresh run (used after a hardcore death).</summary>
        void RestartRun()
        {
            _noSave = true;
            SaveSystem.Delete();
            _freshRun = true;
            Time.timeScale = 1f;

            ClearFloor();
            if (Companion != null) Destroy(Companion.gameObject);
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            if (Hud.Instance != null) { Hud.Instance.gameObject.SetActive(false); Destroy(Hud.Instance.gameObject); }
            if (Rig != null) { Rig.enabled = false; Destroy(Rig); }
            var fx = GameObject.Find("PostFxVolume");
            if (fx != null) Destroy(fx);
            _pending.Clear();

            var old = gameObject;
            Player = null;
            Instance = null;
            Destroy(old);
            new GameObject("Game").AddComponent<Game>();
        }

        // ------------------------------------------------------------------ save / load

        public void Save()
        {
            if (Player == null || _noSave) return;
            SaveSystem.Write(BuildSave());
        }

        SaveData BuildSave()
        {
            var s = Player.ToSave(MaxFloor, _baseSeed);
            s.Hardcore = Hardcore;
            s.Flags = new List<string>(Flags.Set).ToArray();

            var qi = new List<string>();
            var qp = new List<int>();
            foreach (var st in Quests.Active) { qi.Add(st.Id); qp.Add(st.Progress); }
            s.QuestIds = qi.ToArray();
            s.QuestProgress = qp.ToArray();
            s.QuestsCompleted = new List<string>(Quests.Completed).ToArray();
            s.ScoutedBosses = new List<string>(Bestiary.Scouted).ToArray();
            return s;
        }

        void LoadSave(SaveData s)
        {
            if (s.Seed != 0) _baseSeed = s.Seed;
            MaxFloor = Mathf.Max(1, s.MaxFloor);
            Hardcore = s.Hardcore;
            Player.ApplySave(s);

            if (s.Flags != null) foreach (var f in s.Flags) if (!string.IsNullOrEmpty(f)) Flags.Add(f);
            if (s.QuestsCompleted != null) foreach (var c in s.QuestsCompleted) if (!string.IsNullOrEmpty(c)) Quests.Completed.Add(c);
            if (s.QuestIds != null && s.QuestProgress != null)
            {
                for (int i = 0; i < s.QuestIds.Length && i < s.QuestProgress.Length; i++)
                {
                    var def = QuestCatalog.Get(s.QuestIds[i]);
                    if (def == null || Quests.Completed.Contains(def.Id)) continue;
                    int prog = Mathf.Clamp(s.QuestProgress[i], 0, def.Count);
                    Quests.Active.Add(new QuestState { Id = def.Id, Progress = prog, Done = prog >= def.Count });
                }
            }
            if (s.ScoutedBosses != null) foreach (var b in s.ScoutedBosses) if (!string.IsNullOrEmpty(b)) Bestiary.Scouted.Add(b);
            Player.SetDual(Flags.Has("dual_memory"));
        }
    }
}
