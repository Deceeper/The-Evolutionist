using System;
using System.Runtime.CompilerServices;
using Menu;
using UnityEngine;

namespace Evolutionist.Evolution;

/// <summary>在单人暂停菜单左侧绘制无数字进度条。</summary>
internal static class PauseProgressService
{
    private const float BarWidth = 170f;
    private const float BarHeight = 8f;
    private const float LabelToBarOffset = 218f;

    private static readonly ConditionalWeakTable<PauseMenu, ProgressPanel> Panels =
        new();

    private static readonly ProgressRow[] Rows =
    {
        ProgressRow.ForAttribute("奔跑速度：", "Run Speed:", AttributeId.RunSpeed),
        ProgressRow.ForAttribute("爬杆速度：", "Pole Climb Speed:", AttributeId.PoleClimbSpeed),
        ProgressRow.ForAttribute("管道移动速度：", "Corridor Speed:", AttributeId.CorridorClimbSpeed),
        ProgressRow.ForAttribute("跳跃高度：", "Jump Height:", AttributeId.JumpHeight),
        ProgressRow.ForAttribute("翻滚距离：", "Roll Distance:", AttributeId.RollDistance),
        ProgressRow.ForAttribute("蓄力跳距离：", "Pounce Distance:", AttributeId.PounceDistance),
        ProgressRow.ForAttribute("滑行速度：", "Slide Speed:", AttributeId.SlideSpeed),
        ProgressRow.ForAttribute("后空翻高度：", "Backflip Height:", AttributeId.BackflipHeight),
        ProgressRow.ForAttribute("游泳速度：", "Swim Speed:", AttributeId.SwimSpeed),
        ProgressRow.ForAttribute("肺活量：", "Lung Capacity:", AttributeId.LungCapacity),
        ProgressRow.ForAttribute("抗眩晕能力：", "Stun Resistance:", AttributeId.StunResistance),
        ProgressRow.ForAttribute("投矛伤害：", "Spear Damage:", AttributeId.SpearDamage),
        ProgressRow.ForAttribute("投矛性能：", "Spear Performance:", AttributeId.SpearPerformance),
        ProgressRow.ForAttribute("拔矛速度：", "Spear Pull Speed:", AttributeId.SpearPullSpeed),
        ProgressRow.ForAbility("圣徒舌头：", "Saint's Tongue:", AbilityId.Tongue),
        ProgressRow.ForAbility("爆炸跳与爆炸格挡：", "Explosion Jump & Parry:", AbilityId.ExplosiveJump),
        ProgressRow.ForAbility("活体针矛生成：", "Needle Spear Generation:", AbilityId.SpearGeneration),
        ProgressRow.ForAbility("背部存矛：", "Back Spear:", AbilityId.BackSpear)
    };

    public static void ApplyHooks()
    {
        On.Menu.PauseMenu.ctor += PauseMenuCtor;
        On.Menu.PauseMenu.GrafUpdate += PauseMenuGrafUpdate;
        On.Menu.PauseMenu.ShutDownProcess += PauseMenuShutDownProcess;
    }

    public static void RemoveHooks()
    {
        On.Menu.PauseMenu.ShutDownProcess -= PauseMenuShutDownProcess;
        On.Menu.PauseMenu.GrafUpdate -= PauseMenuGrafUpdate;
        On.Menu.PauseMenu.ctor -= PauseMenuCtor;
    }

    private static void PauseMenuCtor(
        On.Menu.PauseMenu.orig_ctor orig,
        PauseMenu self,
        ProcessManager manager,
        RainWorldGame game)
    {
        orig(self, manager, game);

        if (!TryGetSingleEvolutionist(game, out Player player) ||
            !EvolutionStateService.TryGet(player, out EvolutionRunState state))
        {
            return;
        }

        try
        {
            bool useChinese = IsChineseLanguage(
                manager.rainWorld.inGameTranslator.currentLanguage);
            string? fontName = useChinese
                ? ChineseFontService.EnsureLoaded()
                : RWCustom.Custom.GetFont();

            if (fontName == null)
            {
                UnityEngine.Debug.LogError(
                    "[The Evolutionist] Pause-menu font could not be loaded.");
                return;
            }

            var panel = new ProgressPanel(self, state.Current, fontName, useChinese);
            self.pages[0].Container.AddChild(panel.Container);
            Panels.Add(self, panel);
        }
        catch (Exception exception)
        {
            // 界面绘制失败时仍须保证原版暂停菜单可以打开。
            UnityEngine.Debug.LogException(exception);
        }
    }

    private static void PauseMenuGrafUpdate(On.Menu.PauseMenu.orig_GrafUpdate orig, PauseMenu self, float timeStacker)
    {
        orig(self, timeStacker);

        if (Panels.TryGetValue(self, out ProgressPanel panel))
        {
            // 键盘操作图也在页面容器中绘制，将面板置顶可避免进度条被遮挡。
            panel.Container.MoveToFront();
        }
    }

    private static void PauseMenuShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
    {
        if (Panels.TryGetValue(self, out ProgressPanel panel))
        {
            panel.Remove();
            Panels.Remove(self);
        }

        orig(self);
    }

    private static bool TryGetSingleEvolutionist(RainWorldGame game, out Player player)
    {
        player = null!;
        int controlledPlayerCount = 0;

        foreach (AbstractCreature abstractCreature in game.Players)
        {
            if (abstractCreature.realizedCreature is not Player candidate || candidate.isNPC)
            {
                continue;
            }

            controlledPlayerCount++;
            player = candidate;
        }

        return controlledPlayerCount == 1 && EvolutionStateService.IsEvolutionist(player);
    }

    private static bool IsChineseLanguage(InGameTranslator.LanguageID language)
    {
        return language == InGameTranslator.LanguageID.Chinese ||
            language == InGameTranslator.LanguageID.TraditionalChinese;
    }

    private sealed class ProgressPanel
    {
        private static readonly Color TrackColor = new Color(0.12f, 0.14f, 0.16f);
        private static readonly Color AttributeFillColor = new Color(0.72f, 0.88f, 0.9f);
        private static readonly Color AbilityFillColor = new Color(0.95f, 0.72f, 0.38f);

        public ProgressPanel(
            PauseMenu menu,
            EvolutionProgress progress,
            string fontName,
            bool useChinese)
        {
            Container = new FContainer();

            float screenWidth = menu.manager.rainWorld.options.ScreenSize.x;
            float screenHeight = menu.manager.rainWorld.options.ScreenSize.y;
            float safeOffsetX = menu.manager.rainWorld.options.SafeScreenOffset.x;
            float safeOffsetY = menu.manager.rainWorld.options.SafeScreenOffset.y;
            float left = (1366f - screenWidth) / 2f + safeOffsetX + 28f;
            float top = screenHeight - safeOffsetY - 64f;
            float spacing = Mathf.Clamp((screenHeight - 150f) / (Rows.Length - 1), 24f, 31f);

            for (int i = 0; i < Rows.Length; i++)
            {
                ProgressRow row = Rows[i];
                float y = top - i * spacing;

                var label = new FLabel(fontName, row.GetName(useChinese))
                {
                    alignment = FLabelAlignment.Left,
                    anchorX = 0f,
                    anchorY = 0.5f,
                    x = left,
                    y = y,
                    color = Color.white
                };

                var track = new FSprite("pixel")
                {
                    anchorX = 0f,
                    anchorY = 0.5f,
                    x = left + LabelToBarOffset,
                    y = y,
                    scaleX = BarWidth,
                    scaleY = BarHeight,
                    color = TrackColor,
                    alpha = 0.9f
                };

                float fillAmount = Mathf.Clamp01(row.GetValue(progress) / EvolutionProgress.Maximum);
                var fill = new FSprite("pixel")
                {
                    anchorX = 0f,
                    anchorY = 0.5f,
                    x = track.x,
                    y = y,
                    scaleX = BarWidth * fillAmount,
                    scaleY = BarHeight,
                    color = row.IsAbility ? AbilityFillColor : AttributeFillColor
                };

                Container.AddChild(label);
                Container.AddChild(track);
                Container.AddChild(fill);
            }
        }

        public FContainer Container { get; }

        public void Remove()
        {
            Container.RemoveFromContainer();
        }
    }

    private sealed class ProgressRow
    {
        private readonly AttributeId? attribute;
        private readonly AbilityId? ability;

        private ProgressRow(
            string chineseName,
            string englishName,
            AttributeId? attribute,
            AbilityId? ability)
        {
            ChineseName = chineseName;
            EnglishName = englishName;
            this.attribute = attribute;
            this.ability = ability;
        }

        public string ChineseName { get; }

        public string EnglishName { get; }

        public bool IsAbility => ability.HasValue;

        public static ProgressRow ForAttribute(
            string chineseName,
            string englishName,
            AttributeId attribute)
        {
            return new ProgressRow(chineseName, englishName, attribute, null);
        }

        public static ProgressRow ForAbility(
            string chineseName,
            string englishName,
            AbilityId ability)
        {
            return new ProgressRow(chineseName, englishName, null, ability);
        }

        public string GetName(bool useChinese)
        {
            return useChinese ? ChineseName : EnglishName;
        }

        public float GetValue(EvolutionProgress progress)
        {
            if (attribute.HasValue)
            {
                return progress.Get(attribute.Value);
            }

            return ability.HasValue ? progress.Get(ability.Value) : 0f;
        }
    }
}
