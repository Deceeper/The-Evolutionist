using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Menu;
using UnityEngine;

namespace Evolutionist.Evolution;

// 在单人暂停菜单中提供可切换的进化属性页面。
internal static class PauseProgressService
{
    private const string ToggleSignal = "EVOLUTIONIST_ATTRIBUTES";
    private const float BarWidth = 180f;
    private const float BarHeight = 8f;
    private const float LabelToBarOffset = 220f;
    private const float BarToCreatureOffset = 210f;

    private static readonly ConditionalWeakTable<PauseMenu, ProgressMenu> Menus = new();

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
        On.Menu.PauseMenu.Singal += PauseMenuSingal;
        On.Menu.PauseMenu.GrafUpdate += PauseMenuGrafUpdate;
        On.Menu.PauseMenu.ShutDownProcess += PauseMenuShutDownProcess;
    }

    public static void RemoveHooks()
    {
        On.Menu.PauseMenu.ShutDownProcess -= PauseMenuShutDownProcess;
        On.Menu.PauseMenu.GrafUpdate -= PauseMenuGrafUpdate;
        On.Menu.PauseMenu.Singal -= PauseMenuSingal;
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
            bool useChinese = IsChineseLanguage(manager.rainWorld.inGameTranslator.currentLanguage);
            string? fontName = useChinese ? ChineseFontService.EnsureLoaded() : RWCustom.Custom.GetFont();
            if (fontName == null)
            {
                Debug.LogError("[The Evolutionist] Pause-menu font could not be loaded.");
                return;
            }

            var progressMenu = new ProgressMenu(self, state.Current, fontName, useChinese);
            Menus.Add(self, progressMenu);
        }
        catch (Exception exception)
        {
            // 自定义页面失败时仍须保证原版暂停菜单可以打开。
            Debug.LogException(exception);
        }
    }

    private static void PauseMenuSingal(
        On.Menu.PauseMenu.orig_Singal orig,
        PauseMenu self,
        MenuObject sender,
        string message)
    {
        if (message == ToggleSignal && Menus.TryGetValue(self, out ProgressMenu progressMenu))
        {
            progressMenu.Toggle();
            self.selectedObject = progressMenu.Button;
            self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            return;
        }

        orig(self, sender, message);
    }

    private static void PauseMenuGrafUpdate(On.Menu.PauseMenu.orig_GrafUpdate orig, PauseMenu self, float timeStacker)
    {
        orig(self, timeStacker);

        if (Menus.TryGetValue(self, out ProgressMenu progressMenu))
        {
            progressMenu.BringToFront();
        }
    }

    private static void PauseMenuShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
    {
        if (Menus.TryGetValue(self, out ProgressMenu progressMenu))
        {
            progressMenu.Remove();
            Menus.Remove(self);
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

    private sealed class ProgressMenu
    {
        private static readonly Color BackdropColor = new(0.025f, 0.03f, 0.035f);
        private static readonly Color TrackColor = new(0.12f, 0.14f, 0.16f);
        private static readonly Color AttributeFillColor = new(0.72f, 0.88f, 0.9f);
        private static readonly Color AbilityFillColor = new(0.95f, 0.72f, 0.38f);
        private static readonly Color CreatureColor = new(0.78f, 0.8f, 0.82f);

        private readonly PauseMenu menu;
        private readonly bool useChinese;
        private readonly bool controlMapWasInactive;
        private readonly List<ControlMapNodeState> controlMapNodes = new();
        private bool isOpen;

        public ProgressMenu(
            PauseMenu menu,
            EvolutionProgress progress,
            string fontName,
            bool useChinese)
        {
            this.menu = menu;
            this.useChinese = useChinese;
            Container = new FContainer();

            float screenWidth = menu.manager.rainWorld.options.ScreenSize.x;
            float screenHeight = menu.manager.rainWorld.options.ScreenSize.y;
            float safeOffsetX = menu.manager.rainWorld.options.SafeScreenOffset.x;
            float safeOffsetY = menu.manager.rainWorld.options.SafeScreenOffset.y;
            float left = (1366f - screenWidth) / 2f + safeOffsetX + 32f;
            float right = 1366f - left;
            float top = screenHeight - safeOffsetY - 48f;
            float bottom = Math.Max(safeOffsetY + 78f, 125f);
            float spacing = (top - bottom) / (Rows.Length - 1);

            var backdrop = new FSprite("pixel")
            {
                anchorX = 0.5f,
                anchorY = 0.5f,
                x = (left + right) * 0.5f,
                y = (top + bottom) * 0.5f,
                scaleX = right - left + 24f,
                scaleY = top - bottom + spacing + 24f,
                color = BackdropColor,
                alpha = 0.94f
            };
            Container.AddChild(backdrop);

            float labelX = left + 14f;
            float barX = labelX + LabelToBarOffset;
            float creatureX = barX + BarToCreatureOffset;
            float creatureWidth = Math.Max(120f, right - creatureX - 14f);

            for (int i = 0; i < Rows.Length; i++)
            {
                ProgressRow row = Rows[i];
                float y = top - i * spacing;

                var label = CreateLabel(fontName, row.GetName(useChinese), labelX, y, Color.white);
                var track = new FSprite("pixel")
                {
                    anchorX = 0f,
                    anchorY = 0.5f,
                    x = barX,
                    y = y,
                    scaleX = BarWidth,
                    scaleY = BarHeight,
                    color = TrackColor,
                    alpha = 0.95f
                };

                float fillAmount = Mathf.Clamp01(row.GetValue(progress) / EvolutionProgress.Maximum);
                var fill = new FSprite("pixel")
                {
                    anchorX = 0f,
                    anchorY = 0.5f,
                    x = barX,
                    y = y,
                    scaleX = BarWidth * fillAmount,
                    scaleY = BarHeight,
                    color = row.IsAbility ? AbilityFillColor : AttributeFillColor,
                    isVisible = fillAmount > 0f
                };

                string creatures = row.GetDiscoveredCreatureNames(progress, useChinese);
                FLabel creatureLabel = CreateLabel(fontName, creatures, creatureX, y, CreatureColor);
                if (creatureLabel.textRect.width > creatureWidth && creatureLabel.textRect.width > 0f)
                {
                    creatureLabel.scale = creatureWidth / creatureLabel.textRect.width;
                }

                Container.AddChild(label);
                Container.AddChild(track);
                Container.AddChild(fill);
                Container.AddChild(creatureLabel);
            }

            menu.pages[0].Container.AddChild(Container);

            float buttonY = Math.Max(safeOffsetY, 15f);
            Button = new SimpleButton(
                menu,
                menu.pages[0],
                useChinese ? "属性" : "Attributes",
                ToggleSignal,
                new Vector2(left, buttonY),
                new Vector2(110f, 30f));
            menu.pages[0].subObjects.Add(Button);
            Button.nextSelectable[1] = Button;
            Button.nextSelectable[3] = Button;

            if (menu.controlMap != null)
            {
                controlMapWasInactive = menu.controlMap.inactive;
                CaptureControlMapNodes(menu.controlMap);
            }

            SetOpen(false);
        }

        public FContainer Container { get; }

        public SimpleButton Button { get; }

        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        public void BringToFront()
        {
            if (isOpen)
            {
                SetControlMapNodesVisible(false);
                Container.MoveToFront();
            }

            Button.Container.MoveToFront();
        }

        public void Remove()
        {
            RestoreControlMap();
            Container.RemoveFromContainer();
        }

        private static FLabel CreateLabel(string fontName, string text, float x, float y, Color color)
        {
            return new FLabel(fontName, text)
            {
                alignment = FLabelAlignment.Left,
                anchorX = 0f,
                anchorY = 0.5f,
                x = x,
                y = y,
                color = color
            };
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            Container.isVisible = open;
            Button.menuLabel.text = open
                ? useChinese ? "退出" : "Exit"
                : useChinese ? "属性" : "Attributes";

            if (menu.controlMap != null)
            {
                menu.controlMap.inactive = open || controlMapWasInactive;
            }

            if (open)
            {
                SetControlMapNodesVisible(false);
            }
            else
            {
                RestoreControlMapNodes();
            }
        }

        private void RestoreControlMap()
        {
            if (menu.controlMap != null)
            {
                menu.controlMap.inactive = controlMapWasInactive;
            }

            RestoreControlMapNodes();
        }

        private void CaptureControlMapNodes(ControlMap controlMap)
        {
            AddControlMapNode(controlMap.controlsMap?.sprite);
            AddControlMapNode(controlMap.controlsMap2?.sprite);
            AddControlMapNode(controlMap.controlsMap3?.sprite);
            AddControlMapNode(controlMap.controlsMapBlackFade);
            AddControlMapNode(controlMap.pickupButtonInstructionsFade);
            AddControlMapNode(controlMap.pickupButtonInstructions?.label);

            if (controlMap.controlLabels == null)
            {
                return;
            }

            foreach (MenuLabel label in controlMap.controlLabels)
            {
                AddControlMapNode(label?.label);
            }
        }

        private void AddControlMapNode(FNode? node)
        {
            if (node != null)
            {
                controlMapNodes.Add(new ControlMapNodeState(node, node.isVisible));
            }
        }

        private void SetControlMapNodesVisible(bool visible)
        {
            foreach (ControlMapNodeState state in controlMapNodes)
            {
                state.Node.isVisible = visible;
            }
        }

        private void RestoreControlMapNodes()
        {
            foreach (ControlMapNodeState state in controlMapNodes)
            {
                state.Node.isVisible = state.WasVisible;
            }
        }

        private sealed class ControlMapNodeState
        {
            public ControlMapNodeState(FNode node, bool wasVisible)
            {
                Node = node;
                WasVisible = wasVisible;
            }

            public FNode Node { get; }

            public bool WasVisible { get; }
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

        public string GetDiscoveredCreatureNames(EvolutionProgress progress, bool useChinese)
        {
            IReadOnlyList<CreatureTemplate.Type> contributors = attribute.HasValue
                ? CreatureEvolutionCatalog.GetContributors(attribute.Value)
                : CreatureEvolutionCatalog.GetContributors(ability!.Value);
            var names = new List<string>();
            var uniqueNames = new HashSet<string>();

            foreach (CreatureTemplate.Type creatureType in contributors)
            {
                if (!progress.HasDiscovered(creatureType))
                {
                    continue;
                }

                string name = CreatureEvolutionCatalog.GetDisplayName(creatureType, useChinese);
                if (uniqueNames.Add(name))
                {
                    names.Add(name);
                }
            }

            return string.Join(useChinese ? "、" : ", ", names);
        }
    }
}
