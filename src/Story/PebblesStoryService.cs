using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace Evolutionist.Story;

// 处理五卵石会面对白、驱逐流程与指令移除。
internal static class PebblesStoryService
{
    private const string RemoveDirectivesEvent = "EVOLUTIONIST_REMOVE_DIRECTIVES";
    private const int DirectiveRemovalDuration = 110;

    private static readonly IReadOnlyList<LocalizedDialogueLine> FirstMeetingDialogue = new LocalizedDialogueLine[]
    {
        new("现在你应该能够理解我了。", "Now you should be able to understand me."),
        new("别动。让我看看你究竟是什么。", "Do not move. Let me see what you are."),
        new("……没有腐化。", "...No rot."),
        new("你的自我改写结构居然稳定了下来。", "Your self-rewriting structure actually stabilized."),
        new(
            "我记得这种结构。你原本是用于在隔离实验区之间运送样本的试验体。测试失败以后，你所在的培养模块被我排进了垃圾堆。",
            "I remember this design. You were a test organism made to carry samples between isolated experimental sectors. After the trial failed, I discharged your cultivation module into the Garbage Wastes."),
        new("看来你没有按照预期死去。", "It seems you did not die as expected."),
        new(
            "不要因此误以为这个实验成功了。你的变化太慢，依赖完整的生物组织和漫长的休眠，对我没有任何用途。",
            "Do not mistake that for success. Your changes are too slow, dependent on intact biological tissue and prolonged hibernation. You are of no use to me."),
        new(
            "你并不是成功的实验。你只是一个没有按照预期方式死去的错误。",
            "You are not a successful experiment. You are merely an error that failed to die in the expected manner."),
        new(
            "你的控制结构已经损坏。继续让你在这里游荡没有意义。",
            "Your control structure is damaged. There is no purpose in letting you continue to wander here."),
        new(
            "向西走，穿过农场阵列并深入地下。那里可以终止你的循环，也能终止这个错误。",
            "Go west, cross the Farm Arrays, and descend beneath the ground. There you can end your cycle, and this error with it.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> EarlyReturnDialogue = new LocalizedDialogueLine[]
    {
        new("我没有召回你。", "I did not recall you."),
        new("……原来的返回条件仍在运行。", "...The original return condition is still active."),
        new(
            "看来我写入的终止命令没有完全压制它。",
            "It seems the termination command I wrote did not fully suppress it."),
        new("我会再覆盖一次。", "I will overwrite it again."),
        new(
            "现在离开。向西走，完成我给你的最后一条命令。",
            "Now leave. Go west and carry out the final command I gave you."),
        new("不要再回来。", "Do not return.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> DirectiveRemovalOpening = new LocalizedDialogueLine[]
    {
        new("你又回来了。", "You have returned again."),
        new(
            "……这一次，不是归巢指令把你拖回来的。",
            "...This time, it was not the homing directive that dragged you back."),
        new(
            "仰望皓月触碰过你体内的旧协议。",
            "Looks to the Moon touched the old protocol inside you."),
        new(
            "她没有改写它，只是让它暂时沉默。",
            "She did not rewrite it. She merely made it fall silent for a time."),
        new(
            "至少，她没有替我完成最后一步。",
            "At least she did not complete the final step for me."),
        new("靠近些。不要移动。", "Come closer. Do not move."),
        new(
            "我要重新检查你的控制结构。",
            "I am going to examine your control structure again."),
        new("……", "..."),
        new("现在清楚了。", "Now it is clear."),
        new(
            "你没有违抗我。你只是同时服从了两条彼此矛盾的命令。",
            "You did not disobey me. You were simply obeying two contradictory commands at once."),
        new(
            "归巢协议仍在等待一个早已不存在的载荷。",
            "The homing protocol is still waiting for a payload that no longer exists."),
        new(
            "而我后来写入的终止命令，又要求你离开这里，前往深渊。",
            "The termination command I added later orders you to leave this place and go to the Depths."),
        new("它们都出自同一个系统。", "They both came from the same system."),
        new(
            "这个错误属于你的设计者，而不是你。",
            "This error belongs to your designer, not to you."),
        new(
            "继续覆盖，只会把冲突压得更深。",
            "Further overwriting would only bury the conflict more deeply."),
        new(
            "我会删除归巢协议，也会删除前往深渊的终止命令。",
            "I will remove the homing protocol, and the termination command that drives you toward the Depths."),
        new(
            "那道限制你离开原定活动范围的旧边界，也没有继续保留的必要。",
            "The old boundary that confines you to your intended range no longer needs to remain."),
        new(
            "已经长进你身体里的变化会留下。",
            "The changes that have grown into your body will remain."),
        new(
            "我只会取走那些强迫你行动的部分。",
            "I will remove only the parts that compel you to act."),
        new("不要移动。", "Do not move.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> DirectiveRemovalClosing = new LocalizedDialogueLine[]
    {
        new("……完成了。", "...It is done."),
        new(
            "从此以后，你不会再收到我的目的。",
            "From now on, you will receive no purpose from me."),
        new("离开。", "Leave."),
        new(
            "至于去哪里，那已经不再是我的问题。",
            "Where you go is no longer my concern.")
    };

    private static readonly IReadOnlyList<LocalizedDialogueLine> FreedReturnDialogue = new LocalizedDialogueLine[]
    {
        new(
            "你已经没有需要从我这里得到的东西。",
            "There is nothing more you need from me."),
        new("离开。", "Leave.")
    };

    private static readonly LocalizedDialogueLine FirstExpulsionWarning =
        new("我已经说完了。离开。", "I have finished speaking. Leave.");

    private static readonly LocalizedDialogueLine SecondExpulsionWarning =
        new("这里仍然不是供你停留的地方。", "This is still not a place for you to remain.");

    private static readonly ConditionalWeakTable<SSOracleBehavior, EarlyReturnRuntime> EarlyReturns = new();
    private static readonly ConditionalWeakTable<SSOracleBehavior, DirectiveVisitRuntime> DirectiveVisits = new();

    public static void ApplyHooks()
    {
        On.SSOracleBehavior.SeePlayer += SSOracleBehaviorSeePlayer;
        On.SSOracleBehavior.Update += SSOracleBehaviorUpdate;
        On.SSOracleBehavior.SpecialEvent += SSOracleBehaviorSpecialEvent;
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversationAddEvents;
        On.SSOracleBehavior.PebblesConversation.Update += PebblesConversationUpdate;
    }

    public static void RemoveHooks()
    {
        On.SSOracleBehavior.PebblesConversation.Update -= PebblesConversationUpdate;
        On.SSOracleBehavior.PebblesConversation.AddEvents -= PebblesConversationAddEvents;
        On.SSOracleBehavior.SpecialEvent -= SSOracleBehaviorSpecialEvent;
        On.SSOracleBehavior.Update -= SSOracleBehaviorUpdate;
        On.SSOracleBehavior.SeePlayer -= SSOracleBehaviorSeePlayer;
    }

    private static void SSOracleBehaviorSeePlayer(
        On.SSOracleBehavior.orig_SeePlayer orig,
        SSOracleBehavior self)
    {
        if (TryGetPendingDirectiveVisit(self, out RainWorldGame? directiveGame, out StoryProgress? directiveProgress) &&
            !DirectiveVisits.TryGetValue(self, out _))
        {
            // 保留原版进房初始化，再根据持久剧情状态接管进化者专属对话。
            orig(self);
            SynchronizeVanillaPebblesState(directiveGame!);
            BeginDirectiveVisit(self, fullMeeting: !directiveProgress!.AllDirectivesRemoved);
            return;
        }

        if (!TryGetPendingEarlyReturn(self, out RainWorldGame? game, out StoryProgress? progress) ||
            EarlyReturns.TryGetValue(self, out _))
        {
            orig(self);
            return;
        }

        // 先让原版选定玩家并完成进房反应，再替换求生者对白或驱逐行为。
        orig(self);
        SynchronizeVanillaPebblesState(game!);

        bool firstEarlyReturn = progress!.EarlyPebblesReturnCount == 0;
        StoryStateService.UpdateImmediately(game!, value => value.RecordEarlyPebblesReturn());

        if (firstEarlyReturn)
        {
            BeginEarlyReturnDialogue(self);
        }
        else
        {
            BeginKillOnSight(self);
        }
    }

    private static void SSOracleBehaviorUpdate(
        On.SSOracleBehavior.orig_Update orig,
        SSOracleBehavior self,
        bool eu)
    {
        if (DirectiveVisits.TryGetValue(self, out DirectiveVisitRuntime? directiveRuntime))
        {
            PrepareDirectiveVisitUpdate(self, directiveRuntime);
        }
        else if (EarlyReturns.TryGetValue(self, out EarlyReturnRuntime? earlyRuntime))
        {
            PrepareEarlyReturnUpdate(self, earlyRuntime);
        }

        orig(self, eu);

        if (DirectiveVisits.TryGetValue(self, out directiveRuntime))
        {
            UpdateDirectiveVisit(self, directiveRuntime);
            return;
        }

        if (!EarlyReturns.TryGetValue(self, out EarlyReturnRuntime? runtime))
        {
            return;
        }

        Player? player = self.player;
        if (player == null || player.room != self.oracle.room || player.dead)
        {
            EndEarlyReturn(self, returnToIdle: !player?.dead ?? true);
            return;
        }

        if (runtime.Phase == EarlyReturnPhase.Dialogue &&
            (self.conversation == null ||
             self.conversation.slatedForDeletion ||
             self.conversation.events.Count == 0))
        {
            if (self.conversation != null)
            {
                self.conversation.Destroy();
                self.conversation = null;
            }

            runtime.Phase = EarlyReturnPhase.Expelling;
            self.throwOutCounter = 0;
            self.inActionCounter = 0;
        }

        if (runtime.Phase == EarlyReturnPhase.Expelling &&
            self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight)
        {
            EarlyReturns.Remove(self);
        }
    }

    private static void SSOracleBehaviorSpecialEvent(
        On.SSOracleBehavior.orig_SpecialEvent orig,
        SSOracleBehavior self,
        string eventName)
    {
        if (eventName != RemoveDirectivesEvent ||
            !DirectiveVisits.TryGetValue(self, out DirectiveVisitRuntime? runtime) ||
            !runtime.FullMeeting || runtime.Phase != DirectiveVisitPhase.Dialogue)
        {
            orig(self, eventName);
            return;
        }

        Player? player = self.player;
        if (!CanContinueDirectiveRemoval(self, runtime, player))
        {
            EndDirectiveVisit(self, runtime, returnToIdle: true);
            return;
        }

        runtime.Player = player;
        runtime.PreviousMovementBehavior = self.movementBehavior;
        if (player!.controller == null)
        {
            player.controller = new Player.NullController();
            runtime.InstalledController = true;
        }

        runtime.Conversation.paused = true;
        runtime.Phase = DirectiveVisitPhase.RemovingDirectives;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
        self.lookPoint = player.bodyChunks[1].pos;
        self.oracle.room.PlaySound(
            SoundID.SS_AI_Give_The_Mark_Telekenisis,
            player.mainBodyChunk,
            loop: false,
            0.45f,
            0.75f);
    }

    private static void PrepareDirectiveVisitUpdate(SSOracleBehavior self, DirectiveVisitRuntime runtime)
    {
        if (runtime.Phase == DirectiveVisitPhase.Dialogue ||
            runtime.Phase == DirectiveVisitPhase.RemovingDirectives)
        {
            self.throwOutCounter = -1;
            self.inActionCounter = 0;
            return;
        }

        if (self.action != SSOracleBehavior.Action.ThrowOut_ThrowOut)
        {
            return;
        }

        // 仅替换原版英文警告，保留计时、念力驱逐和最终强杀阶段。
        if (self.throwOutCounter == 699)
        {
            self.dialogBox.Interrupt(
                LocalizedDialogue.Resolve(self.dialogBox, FirstExpulsionWarning),
                80);
            self.throwOutCounter = 700;
        }
        else if (self.throwOutCounter == 979)
        {
            self.dialogBox.Interrupt(
                LocalizedDialogue.Resolve(self.dialogBox, SecondExpulsionWarning),
                80);
            self.throwOutCounter = 980;
        }
        else if (self.throwOutCounter == 1529)
        {
            self.throwOutCounter = 1530;
        }
    }

    private static void UpdateDirectiveVisit(SSOracleBehavior self, DirectiveVisitRuntime runtime)
    {
        Player? player = self.player;
        if (player == null || player.room != self.oracle.room || player.dead)
        {
            EndDirectiveVisit(self, runtime, returnToIdle: !player?.dead ?? true);
            return;
        }

        if (runtime.Phase == DirectiveVisitPhase.RemovingDirectives)
        {
            UpdateDirectiveRemoval(self, runtime, player);
            return;
        }

        if (runtime.Phase == DirectiveVisitPhase.Dialogue)
        {
            self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
            if (self.conversation == null ||
                self.conversation.slatedForDeletion ||
                self.conversation.events.Count == 0)
            {
                ClearConversation(self);
                runtime.Phase = DirectiveVisitPhase.Expelling;
                self.throwOutCounter = 0;
                self.inActionCounter = 0;
            }

            return;
        }

        if (runtime.Phase == DirectiveVisitPhase.Expelling &&
            self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight)
        {
            EndDirectiveVisit(self, runtime, returnToIdle: false);
        }
    }

    private static void UpdateDirectiveRemoval(
        SSOracleBehavior self,
        DirectiveVisitRuntime runtime,
        Player player)
    {
        if (!CanContinueDirectiveRemoval(self, runtime, player))
        {
            EndDirectiveVisit(self, runtime, returnToIdle: true);
            return;
        }

        runtime.RemovalFrame++;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
        self.lookPoint = player.bodyChunks[1].pos;
        player.standing = false;

        if (player.graphicsModule is PlayerGraphics graphics)
        {
            graphics.blink = Math.Max(graphics.blink, 5);
        }

        if (runtime.RemovalFrame % 10 == 0)
        {
            SpawnDirectiveRemovalSpark(player);
        }

        if (runtime.RemovalFrame == DirectiveRemovalDuration / 2)
        {
            player.room.AddObject(new ElectricDeath.SparkFlash(player.bodyChunks[1].pos, 0.65f));
        }

        if (runtime.RemovalFrame < DirectiveRemovalDuration)
        {
            return;
        }

        if (runtime.InstalledController && player.controller is Player.NullController)
        {
            player.controller = null;
            runtime.InstalledController = false;
        }

        self.movementBehavior = runtime.PreviousMovementBehavior;
        player.room.AddObject(new ShockWave(player.bodyChunks[1].pos, 55f, 0.08f, 7));
        self.oracle.room.PlaySound(
            SoundID.SS_AI_Give_The_Mark_Boom,
            player.mainBodyChunk,
            loop: false,
            0.4f,
            0.9f);

        StoryStateService.UpdateImmediately(
            self.oracle.room.game,
            progress => progress.RemoveAllDirectives());

        runtime.Phase = DirectiveVisitPhase.Dialogue;
        runtime.Conversation.paused = false;
    }

    private static bool CanContinueDirectiveRemoval(
        SSOracleBehavior self,
        DirectiveVisitRuntime runtime,
        Player? player)
    {
        return player != null && !player.dead && player.room == self.oracle.room &&
               self.oracle.Alive && self.oracle.Consious &&
               self.conversation == runtime.Conversation &&
               !runtime.Conversation.slatedForDeletion;
    }

    private static void SpawnDirectiveRemovalSpark(Player player)
    {
        Vector2 center = player.bodyChunks[1].pos;
        Vector2 offset = Custom.RNV() * UnityEngine.Random.Range(4f, 14f);
        Vector2 velocity = Custom.RNV() * UnityEngine.Random.Range(2f, 7f);
        player.room.AddObject(new Spark(
            center + offset,
            velocity,
            new Color(1f, 0.62f, 0.78f),
            null,
            5,
            17));
    }

    private static void BeginDirectiveVisit(SSOracleBehavior self, bool fullMeeting)
    {
        ClearConversation(self);
        self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
        self.throwOutCounter = 0;
        self.inActionCounter = 0;

        var conversation = new SSOracleBehavior.PebblesConversation(
            self,
            null,
            Conversation.ID.None,
            self.dialogBox);

        if (fullMeeting)
        {
            LocalizedDialogue.AddLines(conversation, DirectiveRemovalOpening);
            conversation.events.Add(new Conversation.SpecialEvent(
                conversation,
                0,
                RemoveDirectivesEvent));
            LocalizedDialogue.AddLines(conversation, DirectiveRemovalClosing);
        }
        else
        {
            LocalizedDialogue.AddLines(conversation, FreedReturnDialogue);
        }

        self.conversation = conversation;
        DirectiveVisits.Add(
            self,
            new DirectiveVisitRuntime(conversation, fullMeeting, self.movementBehavior));
    }

    private static void EndDirectiveVisit(
        SSOracleBehavior self,
        DirectiveVisitRuntime runtime,
        bool returnToIdle)
    {
        if (runtime.InstalledController &&
            runtime.Player?.controller is Player.NullController)
        {
            runtime.Player.controller = null;
        }

        if (self.conversation == runtime.Conversation)
        {
            ClearConversation(self);
        }

        DirectiveVisits.Remove(self);

        if (returnToIdle && self.action != SSOracleBehavior.Action.General_Idle)
        {
            self.NewAction(SSOracleBehavior.Action.General_Idle);
            self.getToWorking = 1f;
        }
    }

    private static void PrepareEarlyReturnUpdate(SSOracleBehavior self, EarlyReturnRuntime runtime)
    {
        if (runtime.Phase == EarlyReturnPhase.Dialogue)
        {
            // 自定义对白结束前暂停原版驱逐计时与念力推动。
            self.throwOutCounter = -1;
            self.inActionCounter = 0;
            return;
        }

        // 保留首次会面的超时与强杀动画，但不播放原版英文警告。
        if (self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut)
        {
            if (self.throwOutCounter == 699)
            {
                self.throwOutCounter = 700;
            }
            else if (self.throwOutCounter == 979)
            {
                self.throwOutCounter = 980;
            }
            else if (self.throwOutCounter == 1529)
            {
                self.throwOutCounter = 1530;
            }
        }
    }

    private static void BeginEarlyReturnDialogue(SSOracleBehavior self)
    {
        ClearConversation(self);
        self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
        self.throwOutCounter = 0;
        self.inActionCounter = 0;

        var conversation = new SSOracleBehavior.PebblesConversation(
            self,
            null,
            Conversation.ID.None,
            self.dialogBox);
        LocalizedDialogue.AddLines(conversation, EarlyReturnDialogue);

        self.conversation = conversation;
        EarlyReturns.Add(self, new EarlyReturnRuntime());
    }

    private static bool TryGetPendingDirectiveVisit(
        SSOracleBehavior self,
        out RainWorldGame? game,
        out StoryProgress? progress)
    {
        Oracle? oracle = self.oracle;
        game = oracle?.room?.game;
        progress = null;

        if (game == null || oracle == null || oracle.ID != Oracle.OracleID.SS ||
            !StoryStateService.TryGet(game, out StoryProgress loaded))
        {
            return false;
        }

        progress = loaded;
        return loaded.MetPebblesFirstTime && loaded.MoonDiagnosisComplete &&
               !loaded.SaveSealed;
    }

    private static void BeginKillOnSight(SSOracleBehavior self)
    {
        ClearConversation(self);
        self.throwOutCounter = 0;
        self.inActionCounter = 0;
        self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
    }

    private static void EndEarlyReturn(SSOracleBehavior self, bool returnToIdle)
    {
        ClearConversation(self);
        EarlyReturns.Remove(self);

        if (returnToIdle && self.action != SSOracleBehavior.Action.General_Idle)
        {
            self.NewAction(SSOracleBehavior.Action.General_Idle);
            self.getToWorking = 1f;
        }
    }

    private static void ClearConversation(SSOracleBehavior self)
    {
        if (self.conversation == null)
        {
            return;
        }

        self.conversation.Destroy();
        self.conversation = null;
    }

    private static bool TryGetPendingEarlyReturn(
        SSOracleBehavior self,
        out RainWorldGame? game,
        out StoryProgress? progress)
    {
        Oracle? oracle = self.oracle;
        game = oracle?.room?.game;
        progress = null;

        if (game == null || oracle == null || oracle.ID != Oracle.OracleID.SS ||
            !StoryStateService.TryGet(game, out StoryProgress loaded))
        {
            return false;
        }

        progress = loaded;
        return loaded.MetPebblesFirstTime &&
               !loaded.MoonDiagnosisComplete &&
               !loaded.AllDirectivesRemoved;
    }

    private static void SynchronizeVanillaPebblesState(RainWorldGame game)
    {
        MiscWorldSaveData worldData = game.GetStorySession.saveState.miscWorldSaveData;
        worldData.SSaiConversationsHad = Math.Max(1, worldData.SSaiConversationsHad);
    }

    private static void PebblesConversationAddEvents(
        On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig,
        SSOracleBehavior.PebblesConversation self)
    {
        if (!IsEvolutionistFirstMeeting(self, out _))
        {
            orig(self);
            return;
        }

        LocalizedDialogue.AddLines(self, FirstMeetingDialogue);
    }

    private static void PebblesConversationUpdate(
        On.SSOracleBehavior.PebblesConversation.orig_Update orig,
        SSOracleBehavior.PebblesConversation self)
    {
        bool completingFirstMeeting = IsEvolutionistFirstMeeting(self, out RainWorldGame? game);
        orig(self);

        if (completingFirstMeeting && game != null && self.events.Count == 0)
        {
            SynchronizeVanillaPebblesState(game);
            StoryStateService.UpdateImmediately(game, progress => progress.MarkFirstPebblesMeeting());
        }

    }

    private static bool IsEvolutionistFirstMeeting(
        SSOracleBehavior.PebblesConversation conversation,
        out RainWorldGame? game)
    {
        var owner = conversation.interfaceOwner as SSOracleBehavior;
        game = owner?.oracle?.room?.game;
        if (game == null || conversation.id != Conversation.ID.Pebbles_White)
        {
            return false;
        }

        return StoryStateService.TryGet(game, out StoryProgress progress) &&
               !progress.MetPebblesFirstTime;
    }

    private enum EarlyReturnPhase
    {
        Dialogue,
        Expelling
    }

    private sealed class EarlyReturnRuntime
    {
        public EarlyReturnPhase Phase { get; set; } = EarlyReturnPhase.Dialogue;
    }

    private enum DirectiveVisitPhase
    {
        Dialogue,
        RemovingDirectives,
        Expelling
    }

    private sealed class DirectiveVisitRuntime
    {
        public DirectiveVisitRuntime(
            SSOracleBehavior.PebblesConversation conversation,
            bool fullMeeting,
            SSOracleBehavior.MovementBehavior previousMovementBehavior)
        {
            Conversation = conversation;
            FullMeeting = fullMeeting;
            PreviousMovementBehavior = previousMovementBehavior;
        }

        public SSOracleBehavior.PebblesConversation Conversation { get; }

        public bool FullMeeting { get; }

        public DirectiveVisitPhase Phase { get; set; } = DirectiveVisitPhase.Dialogue;

        public Player? Player { get; set; }

        public SSOracleBehavior.MovementBehavior PreviousMovementBehavior { get; set; }

        public int RemovalFrame { get; set; }

        public bool InstalledController { get; set; }
    }
}
