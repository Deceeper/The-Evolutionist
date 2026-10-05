using System;
using System.IO;
using System.Runtime.CompilerServices;
using Evolutionist.Evolution;
using Menu;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace Evolutionist.Story;

/// <summary>控制外层空间业力门、结局演出与存档封存。</summary>
internal static class StoryEndingService
{
    private const string OuterExpanseGateRoom = "GATE_SB_OE";
    private const string OuterExpanseEntryRoom = "OE_RAIL04";
    private const string EndingIllustrationName = "evolutionist_messenger_ending";
    private const int EndingPauseDuration = 80;
    private const float EndingFadeDuration = 80f;
    private const int EndingBlackHoldDuration = 40;
    private const int EndingIllustrationFadeInDuration = 60;
    private const int EndingIllustrationHoldDuration = 320;
    private const int EndingIllustrationFadeOutDuration = 60;
    private const int EndingFinalBlackHoldDuration = 40;
    private const float EndingIllustrationWidth = 1366f;
    private const float EndingIllustrationHeight = 768f;

    private static readonly ConditionalWeakTable<RainWorldGame, MessengerEndingController>
        MessengerEndings = new();

    private static readonly ConditionalWeakTable<SlugcatSelectMenu, SaveState>
        SealedStatisticsStates = new();

    public static void ApplyHooks()
    {
        On.RegionGate.customOEGateRequirements += RegionGateCustomOEGateRequirements;
        On.RegionGate.Update += RegionGateUpdate;
        On.Player.NewRoom += PlayerNewRoom;
        On.RainWorldGame.BeatGameMode += RainWorldGameBeatGameMode;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenuUpdateStartButtonText;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenuContinueStartedGame;
        On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess +=
            SlugcatSelectMenuCommunicateWithUpcomingProcess;
    }

    public static void RemoveHooks()
    {
        On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess -=
            SlugcatSelectMenuCommunicateWithUpcomingProcess;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame -= SlugcatSelectMenuContinueStartedGame;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText -= SlugcatSelectMenuUpdateStartButtonText;
        On.RainWorldGame.BeatGameMode -= RainWorldGameBeatGameMode;
        On.Player.NewRoom -= PlayerNewRoom;
        On.RegionGate.Update -= RegionGateUpdate;
        On.RegionGate.customOEGateRequirements -= RegionGateCustomOEGateRequirements;
    }

    private static bool RegionGateCustomOEGateRequirements(
        On.RegionGate.orig_customOEGateRequirements orig,
        RegionGate self)
    {
        if (!IsEvolutionistOuterExpanseGate(self, out StoryProgress? progress))
        {
            return orig(self);
        }

        // 业力门构造时会用此结果选择图标；这里有意忽略全局饕餮通关标记。
        return progress!.AllDirectivesRemoved;
    }

    private static void RegionGateUpdate(
        On.RegionGate.orig_Update orig,
        RegionGate self,
        bool eu)
    {
        bool lockedForEvolutionist =
            IsEvolutionistOuterExpanseGate(self, out StoryProgress? progress) &&
            !progress!.AllDirectivesRemoved;

        if (lockedForEvolutionist)
        {
            self.dontOpen = true;
            self.startCounter = 0;
        }

        orig(self, eu);

        if (lockedForEvolutionist)
        {
            // 原版会在激活区无人时清除 dontOpen，需恢复剧情专用锁以防全局数据绕过。
            self.dontOpen = true;
            self.startCounter = 0;
        }
    }

    private static bool IsEvolutionistOuterExpanseGate(RegionGate gate, out StoryProgress? progress)
    {
        progress = null;
        Room? room = gate.room;
        RainWorldGame? game = room?.game;
        return game != null && room!.abstractRoom.name == OuterExpanseGateRoom &&
               StoryStateService.TryGet(game, out progress);
    }

    private static void PlayerNewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
    {
        orig(self, newRoom);

        RainWorldGame game = newRoom.game;
        if (newRoom.abstractRoom.name != OuterExpanseEntryRoom ||
            game.FirstAlivePlayer?.realizedCreature != self ||
            !StoryStateService.TryGet(game, out StoryProgress progress) ||
            !progress.AllDirectivesRemoved || progress.SaveSealed ||
            MessengerEndings.TryGetValue(game, out _))
        {
            return;
        }

        var ending = new MessengerEndingController(newRoom, self);
        MessengerEndings.Add(game, ending);
        newRoom.AddObject(ending);
    }

    private static void RainWorldGameBeatGameMode(
        On.RainWorldGame.orig_BeatGameMode orig,
        RainWorldGame game,
        bool standardVoidSea)
    {
        if (standardVoidSea &&
            StoryStateService.TryGet(game, out StoryProgress progress) &&
            !progress.SaveSealed)
        {
            string endingType = !progress.MetPebblesFirstTime
                ? StoryEndingType.UnknowingAscension
                : progress.AllDirectivesRemoved
                    ? StoryEndingType.ChosenAscension
                    : StoryEndingType.ObedientAscension;

            game.GetStorySession.saveState.deathPersistentSaveData.ascended = true;
            StoryStateService.UpdateImmediately(
                game,
                value => value.CompleteEnding(endingType));
        }

        orig(game, standardVoidSea);
    }

    private static void SlugcatSelectMenuUpdateStartButtonText(
        On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig,
        SlugcatSelectMenu self)
    {
        orig(self);

        if (!IsSelectedSealedEvolutionist(self) ||
            self.startButton.menuLabel.text != self.Translate("CONTINUE"))
        {
            return;
        }

        self.startButton.menuLabel.text = self.Translate("STATISTICS");
    }

    private static void SlugcatSelectMenuContinueStartedGame(
        On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig,
        SlugcatSelectMenu self,
        SlugcatStats.Name storyGameCharacter)
    {
        if (storyGameCharacter?.value != EvolutionStateService.CharacterId ||
            !self.saveGameData.TryGetValue(storyGameCharacter, out SlugcatSelectMenu.SaveGameData? data) ||
            data == null || !data.ascended)
        {
            orig(self, storyGameCharacter);
            return;
        }

        SaveState state = self.manager.rainWorld.progression.GetOrInitiateSaveState(
            storyGameCharacter,
            null,
            self.manager.menuSetup,
            saveAsDeathOrQuit: false);

        SealedStatisticsStates.Remove(self);
        SealedStatisticsStates.Add(self, state);
        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
        self.PlaySound(SoundID.MENU_Switch_Page_Out);
    }

    private static void SlugcatSelectMenuCommunicateWithUpcomingProcess(
        On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig,
        SlugcatSelectMenu self,
        MainLoopProcess nextProcess)
    {
        if (nextProcess.ID != ProcessManager.ProcessID.Statistics ||
            !SealedStatisticsStates.TryGetValue(self, out SaveState? state) ||
            nextProcess is not StoryGameStatisticsScreen statistics)
        {
            orig(self, nextProcess);
            return;
        }

        var package = new KarmaLadderScreen.SleepDeathScreenDataPackage(
            state.food,
            new IntVector2(
                state.deathPersistentSaveData.karma,
                state.deathPersistentSaveData.karmaCap),
            state.deathPersistentSaveData.reinforcedKarma,
            -1,
            Vector2.zero,
            null,
            state,
            new SlugcatStats(state.saveStateNumber, malnourished: false),
            null,
            startMalnourished: false,
            goalMalnourished: false);

        statistics.GetDataFromGame(package);
        SealedStatisticsStates.Remove(self);
    }

    private static bool IsSelectedSealedEvolutionist(SlugcatSelectMenu menu)
    {
        if (menu.slugcatPageIndex < 0 || menu.slugcatPageIndex >= menu.slugcatPages.Count ||
            menu.slugcatPages[menu.slugcatPageIndex].slugcatNumber?.value !=
            EvolutionStateService.CharacterId)
        {
            return false;
        }

        SlugcatSelectMenu.SaveGameData? data = menu.GetSaveGameData(menu.slugcatPageIndex);
        return data != null && data.ascended;
    }

    private sealed class MessengerEndingController : UpdatableAndDeletable
    {
        private readonly Player player;
        private int pauseFrame;
        private bool started;
        private bool installedController;
        private bool enteredCutscene;
        private bool completed;
        private bool illustrationLoadAttempted;
        private int presentationFrame;
        private FadeOut? fadeOut;
        private FSprite? illustration;

        public MessengerEndingController(Room room, Player player)
        {
            this.room = room;
            this.player = player;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (completed)
            {
                return;
            }

            if (player.dead || player.room != room)
            {
                Cancel();
                return;
            }

            if (!started)
            {
                if (player.inShortcut)
                {
                    return;
                }

                started = true;
                RainWorld.lockGameTimer = true;
                if (player.controller == null)
                {
                    player.controller = new Player.NullController();
                    installedController = true;
                }

                if (!room.game.cameras[0].InCutscene)
                {
                    room.game.cameras[0].EnterCutsceneMode(
                        player.abstractCreature,
                        RoomCamera.CameraCutsceneType.HideJollyHudAndArrows);
                    enteredCutscene = true;
                }
            }

            player.standing = false;
            if (player.graphicsModule is PlayerGraphics graphics)
            {
                graphics.blink = Math.Max(graphics.blink, 5);
            }

            if (pauseFrame < EndingPauseDuration)
            {
                pauseFrame++;
                return;
            }

            if (fadeOut == null)
            {
                fadeOut = new FadeOut(room, Color.black, EndingFadeDuration, fadeIn: false);
                room.AddObject(fadeOut);
                return;
            }

            if (!fadeOut.IsDoneFading())
            {
                return;
            }

            fadeOut.freezeFade = true;
            UpdateIllustrationPresentation();
        }

        private void UpdateIllustrationPresentation()
        {
            presentationFrame++;

            if (presentationFrame <= EndingBlackHoldDuration)
            {
                return;
            }

            if (!illustrationLoadAttempted)
            {
                CreateIllustration();
            }

            int illustrationFrame = presentationFrame - EndingBlackHoldDuration;
            int fadeInEnd = EndingIllustrationFadeInDuration;
            int holdEnd = fadeInEnd + EndingIllustrationHoldDuration;
            int fadeOutEnd = holdEnd + EndingIllustrationFadeOutDuration;
            int presentationEnd = fadeOutEnd + EndingFinalBlackHoldDuration;

            if (illustration != null)
            {
                if (illustrationFrame <= fadeInEnd)
                {
                    illustration.alpha = Mathf.InverseLerp(0f, fadeInEnd, illustrationFrame);
                }
                else if (illustrationFrame <= holdEnd)
                {
                    illustration.alpha = 1f;
                }
                else
                {
                    illustration.alpha = 1f - Mathf.InverseLerp(holdEnd, fadeOutEnd, illustrationFrame);
                }

                PositionIllustration();
            }

            if (illustrationFrame >= presentationEnd)
            {
                CompleteEnding();
            }
        }

        private void CreateIllustration()
        {
            illustrationLoadAttempted = true;

            try
            {
                if (Futile.atlasManager.GetAtlasWithName(EndingIllustrationName) == null)
                {
                    string path = AssetManager.ResolveFilePath(
                        "illustrations" + Path.DirectorySeparatorChar +
                        EndingIllustrationName + ".png");
                    if (!File.Exists(path))
                    {
                        Custom.LogWarning("Evolutionist ending illustration was not found:", path);
                        return;
                    }

                    var texture = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
                    AssetManager.SafeWWWLoadTexture(
                        ref texture,
                        "file:///" + path,
                        clampWrapMode: true,
                        crispPixels: false);
                    HeavyTexturesCache.LoadAndCacheAtlasFromTexture(
                        EndingIllustrationName,
                        texture,
                        textureFromAsset: false);
                }

                illustration = new FSprite(EndingIllustrationName)
                {
                    anchorX = 0.5f,
                    anchorY = 0.5f,
                    alpha = 0f
                };
                PositionIllustration();
                room.game.cameras[0].ReturnFContainer("HUD2").AddChild(illustration);
            }
            catch (Exception exception)
            {
                Custom.LogWarning(
                    "Evolutionist ending illustration could not be loaded:",
                    exception.ToString());
                illustration?.RemoveFromContainer();
                illustration = null;
            }
        }

        private void PositionIllustration()
        {
            if (illustration == null)
            {
                return;
            }

            RoomCamera camera = room.game.cameras[0];
            float scale = Mathf.Min(
                camera.sSize.x / EndingIllustrationWidth,
                camera.sSize.y / EndingIllustrationHeight);
            illustration.x = camera.sSize.x * 0.5f;
            illustration.y = camera.sSize.y * 0.5f;
            illustration.scaleX = scale;
            illustration.scaleY = scale;
        }

        private void CompleteEnding()
        {
            completed = true;
            RainWorldGame game = room.game;
            game.GetStorySession.saveState.deathPersistentSaveData.ascended = true;
            StoryStateService.UpdateImmediately(
                game,
                progress => progress.CompleteEnding(StoryEndingType.MessengerWithoutMessage));

            illustration?.RemoveFromContainer();
            illustration = null;
            if (enteredCutscene)
            {
                room.game.cameras[0].ExitCutsceneMode();
            }

            RainWorld.lockGameTimer = false;
            RainWorldGame.BeatGameMode(game, standardVoidSea: false);
            game.GoToRedsGameOver();
        }

        private void Cancel()
        {
            if (installedController && player.controller is Player.NullController)
            {
                player.controller = null;
            }

            illustration?.RemoveFromContainer();
            illustration = null;
            fadeOut?.Destroy();
            if (enteredCutscene)
            {
                room.game.cameras[0].ExitCutsceneMode();
            }

            RainWorld.lockGameTimer = false;
            MessengerEndings.Remove(room.game);
            Destroy();
        }
    }
}
