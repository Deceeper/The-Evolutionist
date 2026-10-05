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

    private static readonly IReadOnlyList<string> ReturningOpening = new[]
    {
        "你回来了。",
        "这一次，你应该能够理解我了。",
        "我记得你。上次你来到这里时，我曾试着与你交谈，<LINE>但我们无法理解彼此。",
        "请靠近一些。你身上有些东西，我上次便想仔细确认。"
    };

    private static readonly IReadOnlyList<string> FirstOpening = new[]
    {
        "你好，小生物。",
        "你身上带着五块卵石的印记。",
        "可你的身体里，还留着一些比这个印记更加古老的痕迹。",
        "请靠近一些。让我仔细看看。"
    };

    private static readonly IReadOnlyList<string> DiagnosisBeforeSuppression = new[]
    {
        "……原来如此。",
        "你的身体里有一处已经愈合的空洞。<LINE>它曾是为了承载某样东西而被创造的。<LINE>如今，那件东西和承载它的容器都已经不在了，<LINE>可等待它们归来的命令仍然活着。",
        "每当它发现自己依旧空无一物，<LINE>便会再次呼唤你回到五块卵石身边。",
        "可在那之后，他又为你指出了另一条路。<LINE>离开他，向西，前往地下，结束自己的循环。",
        "于是，你的身体被两根来自同一只手的线牵引。<LINE>一根将你拉回他身边，另一根却将你推向深渊。",
        "它们都带着五块卵石留下的印记，而且扎得很深。<LINE>我可以触碰它们，却没有权力将它们彻底剪断。",
        "我能暂时让其中一根安静下来。<LINE>它不会自行醒来，但我也无法将它从你身体里除去。"
    };

    private static readonly IReadOnlyList<string> DiagnosisAfterSuppression = new[]
    {
        "这样，在你再次回到他身边以前，<LINE>你不必再被迫返回。",
        "但如果你想真正摆脱这些牵引，<LINE>就只能回到系上它们的人面前。",
        "是否回去，应当由你自己决定。<LINE>至少这一次，不该再由这些命令替你选择。"
    };

    private static readonly ConditionalWeakTable<SLOracleBehaviorHasMark.MoonConversation, object>
        DiagnosisConversations = new();

    private static readonly ConditionalWeakTable<SLOracleBehaviorHasMark, SuppressionRuntime>
        Suppressions = new();

    private static readonly Dictionary<HUD.DialogBox, FLabel> OriginalDialogLabels = new();

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

        foreach (HUD.DialogBox dialogBox in new List<HUD.DialogBox>(OriginalDialogLabels.Keys))
        {
            RestoreDialogFont(dialogBox);
        }
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
        UseChineseDialogFont(self.dialogBox);

        AddLines(self, progress!.SawMoonWithoutMark ? ReturningOpening : FirstOpening);
        AddLines(self, DiagnosisBeforeSuppression);
        self.events.Add(new Conversation.SpecialEvent(self, 0, SuppressReturnProtocolEvent));
        AddLines(self, DiagnosisAfterSuppression);
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

        MaintainDialogFont(self);

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

            RestoreDialogFont(runtime.Conversation.dialogBox);
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

    private static void AddLines(
        SLOracleBehaviorHasMark.MoonConversation conversation,
        IReadOnlyList<string> lines)
    {
        foreach (string line in lines)
        {
            conversation.events.Add(
                new Conversation.TextEvent(conversation, 0, line, GetTextLinger(line)));
        }
    }

    private static int GetTextLinger(string text)
    {
        int visibleLength = text.Replace("<LINE>", string.Empty).Length;
        return Math.Min(200, 70 + visibleLength * 2);
    }

    private static void MaintainDialogFont(SLOracleBehaviorHasMark behavior)
    {
        if (behavior.currentConversation is SLOracleBehaviorHasMark.MoonConversation conversation &&
            DiagnosisConversations.TryGetValue(conversation, out _) &&
            !conversation.slatedForDeletion && conversation.events.Count > 0 &&
            behavior.player != null && behavior.player.room == behavior.oracle.room)
        {
            UseChineseDialogFont(conversation.dialogBox);
            return;
        }

        RestoreDialogFontIfOwned(behavior);
    }

    private static void RestoreDialogFontIfOwned(SLOracleBehaviorHasMark behavior)
    {
        foreach (HUD.DialogBox dialogBox in new List<HUD.DialogBox>(OriginalDialogLabels.Keys))
        {
            if (dialogBox.hud?.rainWorld == behavior.oracle?.room?.game?.rainWorld)
            {
                RestoreDialogFont(dialogBox);
            }
        }
    }

    private static void UseChineseDialogFont(HUD.DialogBox dialogBox)
    {
        InGameTranslator.LanguageID language = dialogBox.hud.rainWorld.inGameTranslator.currentLanguage;
        if (language == InGameTranslator.LanguageID.Chinese ||
            language == InGameTranslator.LanguageID.TraditionalChinese ||
            OriginalDialogLabels.ContainsKey(dialogBox))
        {
            return;
        }

        string? fontName = ChineseFontService.EnsureLoaded();
        if (fontName == null)
        {
            UnityEngine.Debug.LogError("[The Evolutionist] 无法加载月姐对白所需的中文字体。");
            return;
        }

        FLabel originalLabel = dialogBox.label;
        var chineseLabel = new FLabel(fontName, string.Empty)
        {
            alignment = FLabelAlignment.Left,
            anchorX = 0f,
            anchorY = 1f
        };

        originalLabel.RemoveFromContainer();
        dialogBox.label = chineseLabel;
        dialogBox.hud.fContainers[1].AddChild(chineseLabel);
        OriginalDialogLabels.Add(dialogBox, originalLabel);
    }

    private static void RestoreDialogFont(HUD.DialogBox dialogBox)
    {
        if (!OriginalDialogLabels.TryGetValue(dialogBox, out FLabel originalLabel))
        {
            return;
        }

        dialogBox.label.RemoveFromContainer();
        dialogBox.label = originalLabel;
        dialogBox.hud.fContainers[1].AddChild(originalLabel);
        OriginalDialogLabels.Remove(dialogBox);
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
