using System;
using System.Collections.Generic;

namespace Hollow.Core
{
    public enum StoryTrigger { TownEnter, FloorEnter, BossKilled }

    public sealed class DialogueLine
    {
        public string Speaker;
        public string Text;
        public DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    public sealed class StoryBeat
    {
        public string Id;                 // also the flag set once the beat has been seen
        public StoryTrigger Trigger;
        public int Floor;                 // FloorEnter/BossKilled: exact floor. TownEnter: minimum highest-floor reached
        public string Requires;           // flag that must already be set (or null)
        public string Effect;             // game effect applied after the scene (or null)
        public DialogueLine[] Lines;
    }

    /// <summary>Story flags (saved as a list of strings).</summary>
    public sealed class StoryFlags
    {
        public readonly HashSet<string> Set = new HashSet<string>();
        public bool Has(string f) { return Set.Contains(f); }
        public void Add(string f) { Set.Add(f); }
    }

    public static class StoryCatalog
    {
        static DialogueLine L(string who, string text) { return new DialogueLine(who, text); }

        public static readonly StoryBeat[] Beats =
        {
            new StoryBeat { Id = "prologue", Trigger = StoryTrigger.TownEnter, Floor = 0, Lines = new[]
            {
                L("Architect", "Welcome to Hollow Spire. You cannot leave the way you came in."),
                L("Architect", "The door is gone. There is one exit: the top of the tower."),
                L("Architect", "Twenty floors of the first season lie between you and it. Each one ends with a guardian."),
                L("Architect", "If your health reaches zero, the Spire keeps you. There is no second chance. Climb well."),
                L("Wanderer", "So that's how it is. Then I'll start with the stairs."),
            }},
            new StoryBeat { Id = "first_steps", Trigger = StoryTrigger.FloorEnter, Floor = 1, Requires = "prologue", Lines = new[]
            {
                L("Wanderer", "Same old halls as the beta. Except now nobody is going to reset me."),
                L("Wanderer", "Stay sharp. Dodge with Ctrl, use skills, and don't fight at low health."),
            }},
            new StoryBeat { Id = "meet_miri", Trigger = StoryTrigger.BossKilled, Floor = 2, Effect = "join_miri", Lines = new[]
            {
                L("Miri", "You handled that guardian like you'd seen it before."),
                L("Wanderer", "Who are you?"),
                L("Miri", "Miri. Fencer. I've been watching the crowds panic for two days, and you're the first person who wasn't."),
                L("Miri", "Let me come along. I'll take the flank; you take the front. When something staggers, call for a switch and I'll finish it."),
                L("Wanderer", "...Fine. Keep up."),
            }},
            new StoryBeat { Id = "clerk_intro", Trigger = StoryTrigger.TownEnter, Floor = 3, Requires = "meet_miri", Lines = new[]
            {
                L("Tessa", "Wanderer! The guild board is open. Small jobs, real pay."),
                L("Tessa", "Kill count, chests, floors reached. Bring me proof and I'll hand over gold and shards."),
                L("Miri", "Shards feed the blacksmith. Dorn can push your gear up to +10, but the last levels can fail."),
            }},
            new StoryBeat { Id = "act1_end", Trigger = StoryTrigger.BossKilled, Floor = 5, Requires = "meet_miri", Lines = new[]
            {
                L("Corvin", "Impressive. The fifth guardian fell in under a minute."),
                L("Corvin", "I lead the Ashen Oath, the largest guild in the Spire. We could use blades like yours."),
                L("Miri", "Careful. Big guilds mean big promises."),
                L("Corvin", "Come to the town when you're ready. My people will have a place for you."),
            }},
            new StoryBeat { Id = "ashen_oath", Trigger = StoryTrigger.TownEnter, Floor = 6, Requires = "act1_end", Lines = new[]
            {
                L("Corvin", "The Ashen Oath keeps its members alive. That is the whole creed."),
                L("Corvin", "Most players are hiding in the towns. My guild fights so they don't have to."),
                L("Wanderer", "And what do you want in return?"),
                L("Corvin", "A strong sword on the front line. That's all."),
                L("Miri", "He's charming. I don't trust it."),
            }},
            new StoryBeat { Id = "green_ruin", Trigger = StoryTrigger.FloorEnter, Floor = 7, Requires = "ashen_oath", Lines = new[]
            {
                L("Miri", "The stone here is overgrown. Somebody built this place and then let it rot on purpose."),
                L("Wanderer", "Or the Spire is growing on its own."),
            }},
            new StoryBeat { Id = "dual_memory", Trigger = StoryTrigger.BossKilled, Floor = 10, Requires = "meet_miri", Effect = "unlock_dual", Lines = new[]
            {
                L("Wanderer", "That last blow... my left hand moved on its own."),
                L("Miri", "You're holding two blades."),
                L("Wanderer", "In the beta there was a hidden technique nobody found. Two swords, ten strikes, one breath."),
                L("Wanderer", "It's called Twin Tempest. Key 5."),
                L("Miri", "Then no more hiding it. Everyone will know what you are."),
            }},
            new StoryBeat { Id = "vault_warning", Trigger = StoryTrigger.FloorEnter, Floor = 12, Requires = "dual_memory", Lines = new[]
            {
                L("Corvin", "The Oath found a sealed vault on this floor. We're going in tonight."),
                L("Miri", "A vault nobody has opened, on the day the Spire turned on us. That's bait."),
                L("Wanderer", "I'll watch the entrance."),
            }},
            new StoryBeat { Id = "guild_lost", Trigger = StoryTrigger.BossKilled, Floor = 12, Requires = "vault_warning", Lines = new[]
            {
                L("Wanderer", "The vault doors are open. There is nobody left inside."),
                L("Miri", "Forty members. All at once."),
                L("Corvin", "A trap. I told them to wait. I told them."),
                L("Wanderer", "You weren't inside with them."),
                L("Corvin", "Someone had to lead from behind. Remember that, Wanderer."),
            }},
            new StoryBeat { Id = "trust", Trigger = StoryTrigger.FloorEnter, Floor = 14, Requires = "guild_lost", Lines = new[]
            {
                L("Miri", "You've stopped talking to me since the vault."),
                L("Wanderer", "I should have gone in with them."),
                L("Miri", "You'd be dead too. Stop counting the ones you couldn't save; count the ones you're still saving."),
                L("Wanderer", "...Thanks."),
            }},
            new StoryBeat { Id = "reveal", Trigger = StoryTrigger.BossKilled, Floor = 15, Requires = "guild_lost", Effect = "reveal", Lines = new[]
            {
                L("Corvin", "Well fought. Twin Tempest, the very move I cut from the beta."),
                L("Wanderer", "You cut it?"),
                L("Corvin", "I am the Architect. Corvin was a name I wore to walk among you."),
                L("Miri", "The vault. The guild. That was you!"),
                L("Corvin", "A test. I had to know who was worth the top floor. Come to floor twenty."),
                L("Wanderer", "I'll be there."),
            }},
            new StoryBeat { Id = "final_climb", Trigger = StoryTrigger.FloorEnter, Floor = 16, Requires = "reveal", Lines = new[]
            {
                L("Miri", "Five floors left."),
                L("Wanderer", "Then let's make them count."),
            }},
            new StoryBeat { Id = "eve_of_end", Trigger = StoryTrigger.FloorEnter, Floor = 20, Requires = "reveal", Lines = new[]
            {
                L("Architect", "Here at last. The stair ends in this room."),
                L("Architect", "I built a world that could not be left, and you learned to live in it. Show me that was worth it."),
            }},
            new StoryBeat { Id = "ending", Trigger = StoryTrigger.BossKilled, Floor = 20, Requires = "reveal", Effect = "ending", Lines = new[]
            {
                L("Architect", "The seal is broken. You may go, all of you. Or stay, and finish what I started."),
                L("Miri", "Whatever you pick, I'm with you."),
                L("Wanderer", "..."),
            }},
        };

        public static StoryBeat Get(string id)
        {
            for (int i = 0; i < Beats.Length; i++) if (Beats[i].Id == id) return Beats[i];
            return null;
        }

        /// <summary>The first unseen beat for this trigger (beats are listed in story order).</summary>
        public static StoryBeat Next(StoryTrigger trigger, int floor, int maxFloor, StoryFlags flags)
        {
            for (int i = 0; i < Beats.Length; i++)
            {
                var b = Beats[i];
                if (b.Trigger != trigger || flags.Has(b.Id)) continue;
                if (b.Requires != null && !flags.Has(b.Requires)) continue;
                if (trigger == StoryTrigger.TownEnter) { if (maxFloor < b.Floor) continue; }
                else if (b.Floor != floor) continue;
                return b;
            }
            return null;
        }

        public static readonly DialogueLine[] EndingLeave =
        {
            new DialogueLine("Miri", "The door is real. Light on the other side..."),
            new DialogueLine("Wanderer", "Go. I'll be right behind you."),
            new DialogueLine("Architect", "Season one is complete. Those who stay may keep climbing."),
        };

        public static readonly DialogueLine[] EndingStay =
        {
            new DialogueLine("Wanderer", "There are still people inside the Spire. I'm not leaving them."),
            new DialogueLine("Miri", "Then we rebuild it together. Floor by floor."),
            new DialogueLine("Architect", "Season one is complete. The Spire is yours now."),
        };
    }
}
