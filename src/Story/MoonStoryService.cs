using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace Evolutionist.Story;

/// <summary>处理月姐初见记录、诊断对白与归巢指令压制。</summary>
internal static class MoonStoryService
{
    private const string SuppressReturnProtocolEvent = "EVOLUTIONIST_SUPPRESS_RETURN";
    private const int SuppressionDuration = 100;

    private static readonly IReadOnlyList<LocalizedDialogueLine> ReturningOpening = new LocalizedDialogueLine[]
    {
        new("你回来了。", "You have returned."),
        new(
            "这一次，你应该能够理解我了。",
            "This time, you should be able to understand me."),
        new(
            "我记得你。上次你来到这里时，我曾试着与你交谈，但我们无法理解彼此。",
            "I remember you. When you came here before, I tried to speak with you, but neither of us could understand the other."),
        new(
            "请靠近一些。你身上有些东西，我上次便想仔细确认。",
            "Please come closer. There is something in you that I wanted to examine the last time.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> FirstOpening = new LocalizedDialogueLine[]
    {
        new("你好，小生物。", "Hello, little creature."),
        new("你身上带着五块卵石的印记。", "You bear Five Pebbles' mark."),
        new(
            "可你的身体里，还留着一些比这个印记更加古老的痕迹。",
            "But your body also carries traces far older than that mark."),
        new("请靠近一些。让我仔细看看。", "Please come closer. Let me take a careful look.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> DiagnosisBeforeSuppression = new LocalizedDialogueLine[]
    {
        new("……原来如此。", "...I see."),
        new(
            "你的身体里有一处已经愈合的空洞。它曾是为了承载某样东西而被创造的。如今，那件东西和承载它的容器都已经不在了，可等待它们归来的命令仍然活着。",
            "Inside you is a hollow that has already healed. It was created to carry something. Now both that object and the vessel that held it are gone, yet the command waiting for their return remains alive."),
        new(
            "每当它发现自己依旧空无一物，便会再次呼唤你回到五块卵石身边。",
            "Whenever it finds itself empty once more, it calls you back to Five Pebbles."),
        new(
            "可在那之后，他又为你指出了另一条路。离开他，向西，前往地下，结束自己的循环。",
            "But afterward, he pointed you toward another path. Leave him, travel west, descend underground, and end your cycle."),
        new(
            "于是，你的身体被两根来自同一只手的线牵引。一根将你拉回他身边，另一根却将你推向深渊。",
            "Thus your body is pulled by two threads tied by the same hand. One draws you back to him; the other drives you toward the Depths."),
        new(
            "它们都带着五块卵石留下的印记，而且扎得很深。我可以触碰它们，却没有权力将它们彻底剪断。",
            "Both bear Five Pebbles' mark, and both are rooted deeply. I can touch them, but I do not have the authority to cut them away completely."),
        new(
            "我能暂时让其中一根安静下来。它不会自行醒来，但我也无法将它从你身体里除去。",
            "I can quiet one of them for a time. It will not awaken on its own, but I cannot remove it from your body.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> DiagnosisAfterSuppression = new LocalizedDialogueLine[]
    {
        new(
            "这样，在你再次回到他身边以前，你不必再被迫返回。",
            "Until you return to him again, you will no longer be forced back."),
        new(
            "但如果你想真正摆脱这些牵引，就只能回到系上它们的人面前。",
            "But if you wish to be truly free of these threads, you must return to the one who tied them."),
        new(
            "是否回去，应当由你自己决定。至少这一次，不该再由这些命令替你选择。",
            "Whether you go back should be your choice. This time, at least, those commands should not choose for you.")
    };

    private static readonly ConditionalWeakTable<SLOracleBehaviorHasMark.MoonConversation, object>
        DiagnosisConversations = new();

    private static readonly ConditionalWeakTable<SLOracleBehaviorHasMark, SuppressionRuntime>
        Suppressions = new();

    public static void ApplyHooks()
    {
        On.SLOracleBehaviorNoMark.Update += SLOracleBehaviorNoMarkUpdate;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversationAddEvents;
        On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMarkSpecialEvent;
        On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMarkUpdate;
    }

    public static void RemoveHooks()
    {
        On.SLOracleBehaviorHasMark.Update -= SLOracleBehaviorHasMarkUpdate;
        On.SLOracleBehaviorHasMark.SpecialEvent -= SLOracleBehaviorHasMarkSpecialEvent;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= MoonConversationAddEvents;
        On.SLOracleBehaviorNoMark.Update -= SLOracleBehaviorNoMarkUpdate;
    }

    private static void SLOracleBehaviorNoMarkUpdate(
        On.SLOracleBehaviorNoMark.orig_Update orig,
        SLOracleBehaviorNoMark self,
        bool eu)
    {
        orig(self, eu);

        Player? player = self.player;
        Oracle? oracle = self.oracle;
        Room? room = oracle?.room;
        RainWorldGame? game = room?.game;
        if (game == null || room == null || oracle == null ||
            oracle.ID != Oracle.OracleID.SL || player == null ||
            player.room != room || !self.hasNoticedPlayer || !oracle.Alive ||
            self.State.neuronsLeft <= 0 ||
            !StoryStateService.TryGet(game, out StoryProgress progress) ||
            progress.SawMoonWithoutMark)
        {
            return;
        }

        StoryStateService.UpdateImmediately(game, progress => progress.MarkMoonVisitWithoutMark());
    }

    private static void MoonConversationAddEvents(
        On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig,
        SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (!TryGetDiagnosisContext(self, out StoryProgress? progress))
        {
            orig(self);
            return;
        }

        DiagnosisConversations.Add(self, new object());
        LocalizedDialogue.AddLines(
            self,
            progress!.SawMoonWithoutMark ? ReturningOpening : FirstOpening);
        LocalizedDialogue.AddLines(self, DiagnosisBeforeSuppression);
        self.events.Add(new Conversation.SpecialEvent(self, 0, SuppressReturnProtocolEvent));
        LocalizedDialogue.AddLines(self, DiagnosisAfterSuppression);
    }

    private static void SLOracleBehaviorHasMarkSpecialEvent(
        On.SLOracleBehaviorHasMark.orig_SpecialEvent orig,
        SLOracleBehaviorHasMark self,
        string eventName)
    {
        if (eventName != SuppressReturnProtocolEvent ||
            self.currentConversation is not SLOracleBehaviorHasMark.MoonConversation conversation ||
            !DiagnosisConversations.TryGetValue(conversation, out _) ||
            Suppressions.TryGetValue(self, out _))
        {
            orig(self, eventName);
            return;
        }

        Player? player = self.player;
        if (!CanContinueSuppression(self, player, conversation))
        {
            return;
        }

        var runtime = new SuppressionRuntime(
            conversation,
            player!,
            self.movementBehavior);
        if (player!.controller == null)
        {
            player.controller = new Player.NullController();
            runtime.InstalledController = true;
        }

        conversation.paused = true;
        self.setMovementBehavior(SLOracleBehavior.MovementBehavior.InvestigateSlugcat);
        self.lookPoint = player.bodyChunks[1].pos;
        self.oracle.room.PlaySound(
            SoundID.SS_AI_Give_The_Mark_Boom,
            player.mainBodyChunk,
            loop: false,
            0.35f,
            0.7f);
        Suppressions.Add(self, runtime);
    }

    private static void SLOracleBehaviorHasMarkUpdate(
        On.SLOracleBehaviorHasMark.orig_Update orig,
        SLOracleBehaviorHasMark self,
        bool eu)
    {
        orig(self, eu);

        if (!Suppressions.TryGetValue(self, out SuppressionRuntime? runtime))
        {
            return;
        }

        if (!CanContinueSuppression(self, runtime.Player, runtime.Conversation))
        {
            EndSuppression(self, runtime, completed: false);
            return;
        }

        runtime.Frame++;
        Player player = runtime.Player;
        self.lookPoint = player.bodyChunks[1].pos;
        player.standing = false;

        if (player.graphicsModule is PlayerGraphics graphics)
        {
            graphics.blink = Math.Max(graphics.blink, 5);
        }

        if (runtime.Frame % 12 == 0)
        {
            SpawnSuppressionSpark(player);
        }

        if (runtime.Frame == SuppressionDuration / 2)
        {
            player.room.AddObject(new ElectricDeath.SparkFlash(player.bodyChunks[1].pos, 0.55f));
        }

        if (runtime.Frame >= SuppressionDuration)
        {
            EndSuppression(self, runtime, completed: true);
        }
    }

    private static void EndSuppression(
        SLOracleBehaviorHasMark self, SuppressionRuntime runtime, bool completed)
    {
        if (runtime.InstalledController && runtime.Player.controller is Player.NullController)
        {
            runtime.Player.controller = null;
        }

        self.setMovementBehavior(runtime.PreviousMovementBehavior);
        Suppressions.Remove(self);

        if (!completed)
        {
            runtime.Conversation.Destroy();
            if (self.currentConversation == runtime.Conversation)
            {
                self.currentConversation = null;
            }

            return;
        }

        RainWorldGame game = self.oracle.room.game;
        StoryStateService.UpdateImmediately(game, progress => progress.CompleteMoonDiagnosis());

        if (self.currentConversation == runtime.Conversation &&
            !runtime.Conversation.slatedForDeletion)
        {
            runtime.Conversation.paused = false;
        }
    }

    private static bool CanContinueSuppression(
        SLOracleBehaviorHasMark self,
        Player? player,
        SLOracleBehaviorHasMark.MoonConversation conversation)
    {
        return player != null && !player.dead && player.room == self.oracle.room &&
               self.oracle.Alive && self.oracle.Consious && self.State.neuronsLeft > 0 &&
               self.State.SpeakingTerms && self.currentConversation == conversation &&
               !conversation.slatedForDeletion;
    }

    private static void SpawnSuppressionSpark(Player player)
    {
        Vector2 center = player.bodyChunks[1].pos;
        Vector2 offset = Custom.RNV() * UnityEngine.Random.Range(4f, 13f);
        Vector2 velocity = Custom.RNV() * UnityEngine.Random.Range(2f, 7f);
        player.room.AddObject(new Spark(
            center + offset,
            velocity,
            new Color(0.72f, 0.9f, 1f),
            null,
            5,
            16));
    }

    private static bool TryGetDiagnosisContext(
        SLOracleBehaviorHasMark.MoonConversation conversation,
        out StoryProgress? progress)
    {
        progress = null;
        if (conversation.id != Conversation.ID.MoonFirstPostMarkConversation &&
            conversation.id != Conversation.ID.MoonSecondPostMarkConversation)
        {
            return false;
        }

        var behavior = conversation.myBehavior as SLOracleBehaviorHasMark;
        RainWorldGame? game = behavior?.oracle?.room?.game;
        if (game == null || behavior == null ||
            behavior.oracle.ID != Oracle.OracleID.SL || !behavior.oracle.Alive ||
            behavior.State.neuronsLeft <= 0 || !behavior.State.SpeakingTerms ||
            !StoryStateService.TryGet(game, out StoryProgress loaded))
        {
            return false;
        }

        progress = loaded;
        return loaded.MetPebblesFirstTime &&
               !loaded.MoonDiagnosisComplete &&
               !loaded.AllDirectivesRemoved;
    }

    private sealed class SuppressionRuntime
    {
        public SuppressionRuntime(
            SLOracleBehaviorHasMark.MoonConversation conversation,
            Player player,
            SLOracleBehavior.MovementBehavior previousMovementBehavior)
        {
            Conversation = conversation;
            Player = player;
            PreviousMovementBehavior = previousMovementBehavior;
        }

        public SLOracleBehaviorHasMark.MoonConversation Conversation { get; }

        public Player Player { get; }

        public SLOracleBehavior.MovementBehavior PreviousMovementBehavior { get; }

        public int Frame { get; set; }

        public bool InstalledController { get; set; }
    }
}
