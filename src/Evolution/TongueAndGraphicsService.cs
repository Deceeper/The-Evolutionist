using System;
using MoreSlugcats;
using UnityEngine;

namespace Evolutionist.Evolution;

/// <summary>实现圣徒舌头，并复用矛大师的体型与尾部孔洞绘制。</summary>
internal static class TongueAndGraphicsService
{
    private static readonly Color EyeColor = new Color(224f / 255f, 121f / 255f, 88f / 255f);
    private static readonly Color TailPatternColor = new Color(237f / 255f, 242f / 255f, 245f / 255f);
    private static readonly Color TongueColor = new Color(1f, 128f / 255f, 166f / 255f);

    public static void ApplyHooks()
    {
        On.Player.ctor += PlayerCtor;
        On.Player.Update += PlayerUpdate;
        On.Player.ClassMechanicsSaint += PlayerClassMechanicsSaint;
        On.Player.SaintTongueCheck += PlayerSaintTongueCheck;
        On.PlayerGraphics.ctor += PlayerGraphicsCtor;
        On.PlayerGraphics.Update += PlayerGraphicsUpdate;
        On.PlayerGraphics.InitiateSprites += PlayerGraphicsInitiateSprites;
        On.PlayerGraphics.DrawSprites += PlayerGraphicsDrawSprites;
        On.PlayerGraphics.ApplyPalette += PlayerGraphicsApplyPalette;
        On.PlayerGraphics.AddToContainer += PlayerGraphicsAddToContainer;
    }

    public static void RemoveHooks()
    {
        On.PlayerGraphics.AddToContainer -= PlayerGraphicsAddToContainer;
        On.PlayerGraphics.ApplyPalette -= PlayerGraphicsApplyPalette;
        On.PlayerGraphics.DrawSprites -= PlayerGraphicsDrawSprites;
        On.PlayerGraphics.InitiateSprites -= PlayerGraphicsInitiateSprites;
        On.PlayerGraphics.Update -= PlayerGraphicsUpdate;
        On.PlayerGraphics.ctor -= PlayerGraphicsCtor;
        On.Player.SaintTongueCheck -= PlayerSaintTongueCheck;
        On.Player.ClassMechanicsSaint -= PlayerClassMechanicsSaint;
        On.Player.Update -= PlayerUpdate;
        On.Player.ctor -= PlayerCtor;
    }

    private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        EnsureTongue(self);
    }

    private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        EnsureTongue(self);
        orig(self, eu);
    }

    private static bool PlayerSaintTongueCheck(On.Player.orig_SaintTongueCheck orig, Player self)
    {
        if (!EvolutionStateService.TryGet(self, out EvolutionRunState state) ||
            !state.Current.IsUnlocked(AbilityId.Tongue))
        {
            return orig(self);
        }

        EnsureTongue(self);

        return self.Consious &&
            self.tongue != null &&
            self.tongue.mode == Player.Tongue.Mode.Retracted &&
            self.bodyMode != Player.BodyModeIndex.CorridorClimb &&
            self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut &&
            self.bodyMode != Player.BodyModeIndex.WallClimb &&
            self.bodyMode != Player.BodyModeIndex.Swimming &&
            self.animation != Player.AnimationIndex.VineGrab &&
            self.animation != Player.AnimationIndex.ZeroGPoleGrab &&
            !self.monkAscension;
    }

    private static void PlayerClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
    {
        orig(self);

        if (!EvolutionStateService.TryGet(self, out EvolutionRunState state) ||
            !state.Current.IsUnlocked(AbilityId.Tongue) ||
            MMF.cfgOldTongue.Value)
        {
            return;
        }

        EnsureTongue(self);

        if (self.input[0].jmp &&
            !self.input[1].jmp &&
            !self.input[0].pckp &&
            self.canJump <= 0 &&
            self.bodyMode != Player.BodyModeIndex.Crawl &&
            self.animation != Player.AnimationIndex.ClimbOnBeam &&
            self.animation != Player.AnimationIndex.AntlerClimb &&
            self.animation != Player.AnimationIndex.HangFromBeam &&
            self.SaintTongueCheck())
        {
            Vector2 direction = new Vector2(self.flipDirection, 0.7f).normalized;

            if (self.input[0].y > 0)
            {
                direction = new Vector2(0f, 1f);
            }

            direction = (direction + self.mainBodyChunk.vel.normalized * 0.2f).normalized;

            self.tongue.Shoot(direction);
        }
    }

    private static void PlayerGraphicsCtor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject owner)
    {
        if (owner is not Player player || !EvolutionStateService.IsEvolutionist(player))
        {
            orig(self, owner);
            return;
        }

        WithSpearmasterGraphicsIdentity(player, () => orig(self, owner));
        HideLockedCosmetics(self);
    }

    private static void PlayerGraphicsUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        if (!IsEvolutionistGraphics(self))
        {
            orig(self);
            return;
        }

        WithSpearmasterGraphicsIdentity(GetPlayer(self), () => orig(self));
        HideLockedCosmetics(self);
    }

    private static void PlayerGraphicsInitiateSprites(
        On.PlayerGraphics.orig_InitiateSprites orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam)
    {
        if (!IsEvolutionistGraphics(self))
        {
            orig(self, sLeaser, rCam);
            return;
        }

        WithSpearmasterGraphicsIdentity(GetPlayer(self), () => orig(self, sLeaser, rCam));
        HideLockedCosmetics(self, sLeaser);
    }

    private static void PlayerGraphicsDrawSprites(
        On.PlayerGraphics.orig_DrawSprites orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        float timeStacker,
        UnityEngine.Vector2 camPos)
    {
        if (!IsEvolutionistGraphics(self))
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            return;
        }

        WithSpearmasterGraphicsIdentity(GetPlayer(self), () => orig(self, sLeaser, rCam, timeStacker, camPos));
        HideLockedCosmetics(self, sLeaser);
        ApplyEvolutionistDetailColors(self, sLeaser);
        EnsureUnlockedTongueVisible(self, sLeaser);
    }

    private static void PlayerGraphicsApplyPalette(
        On.PlayerGraphics.orig_ApplyPalette orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        RoomPalette palette)
    {
        if (!IsEvolutionistGraphics(self))
        {
            orig(self, sLeaser, rCam, palette);
            return;
        }

        WithSpearmasterGraphicsIdentity(GetPlayer(self), () => orig(self, sLeaser, rCam, palette));
        HideLockedCosmetics(self, sLeaser);
        ApplyEvolutionistDetailColors(self, sLeaser);
    }

    private static void PlayerGraphicsAddToContainer(
        On.PlayerGraphics.orig_AddToContainer orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        FContainer newContainer)
    {
        if (!IsEvolutionistGraphics(self))
        {
            orig(self, sLeaser, rCam, newContainer);
            return;
        }

        WithSpearmasterGraphicsIdentity(GetPlayer(self), () => orig(self, sLeaser, rCam, newContainer));
    }

    private static void EnsureTongue(Player player)
    {
        if (!EvolutionStateService.IsEvolutionist(player))
        {
            return;
        }

        if (player.tongue == null)
        {
            player.tongue = new Player.Tongue(player, 0);
        }
    }

    private static bool IsEvolutionistGraphics(PlayerGraphics graphics)
    {
        return graphics?.owner is Player player && EvolutionStateService.IsEvolutionist(player);
    }

    private static void HideLockedCosmetics(PlayerGraphics graphics)
    {
        if (graphics.bodyPearl != null)
        {
            graphics.bodyPearl.visible = false;
        }
    }

    private static void HideLockedCosmetics(PlayerGraphics graphics, RoomCamera.SpriteLeaser? sLeaser)
    {
        HideLockedCosmetics(graphics);

        if (sLeaser == null)
        {
            return;
        }

        if (graphics.bodyPearl != null)
        {
            int pearlEnd = Math.Min(
                sLeaser.sprites.Length,
                graphics.bodyPearl.startSprite + graphics.bodyPearl.numberOfSprites);

            for (int i = graphics.bodyPearl.startSprite; i < pearlEnd; i++)
            {
                if (sLeaser.sprites[i] != null)
                {
                    sLeaser.sprites[i].isVisible = false;
                }
            }
        }

        if (graphics.tailSpecks == null || graphics.owner is not Player player)
        {
            return;
        }

        bool unlocked = EvolutionStateService.TryGet(player, out EvolutionRunState state) &&
            state.Current.IsUnlocked(AbilityId.SpearGeneration);

        int end = Math.Min(
            sLeaser.sprites.Length,
            graphics.tailSpecks.startSprite + graphics.tailSpecks.numberOfSprites);

        for (int i = graphics.tailSpecks.startSprite; i < end; i++)
        {
            if (sLeaser.sprites[i] != null)
            {
                sLeaser.sprites[i].isVisible = unlocked;
            }
        }
    }

    private static void ApplyEvolutionistDetailColors(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser? sLeaser)
    {
        if (sLeaser == null)
        {
            return;
        }

        if (sLeaser.sprites.Length > 9 && sLeaser.sprites[9] != null)
        {
            sLeaser.sprites[9].color = EyeColor;
        }

        if (graphics.tailSpecks == null ||
            graphics.owner is not Player player ||
            !EvolutionStateService.TryGet(player, out EvolutionRunState state) ||
            !state.Current.IsUnlocked(AbilityId.SpearGeneration))
        {
            return;
        }

        int end = Math.Min(
            sLeaser.sprites.Length,
            graphics.tailSpecks.startSprite + graphics.tailSpecks.numberOfSprites);

        for (int i = graphics.tailSpecks.startSprite; i < end; i++)
        {
            if (sLeaser.sprites[i] != null)
            {
                sLeaser.sprites[i].color = TailPatternColor;
            }
        }
    }

    private static void EnsureUnlockedTongueVisible(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser? sLeaser)
    {
        if (sLeaser == null ||
            graphics.owner is not Player player ||
            player.tongue == null ||
            (!player.tongue.Free && !player.tongue.Attached) ||
            !EvolutionStateService.TryGet(player, out EvolutionRunState state) ||
            !state.Current.IsUnlocked(AbilityId.Tongue))
        {
            return;
        }

        // 进化者以矛大师体型绘制，舌头网格位于 gownIndex 前一位。
        int tongueSpriteIndex = graphics.gownIndex - 1;
        if (tongueSpriteIndex < 0 ||
            tongueSpriteIndex >= sLeaser.sprites.Length ||
            sLeaser.sprites[tongueSpriteIndex] is not TriangleMesh tongueMesh)
        {
            return;
        }

        tongueMesh.isVisible = true;
        for (int i = 0; i < tongueMesh.verticeColors.Length; i++)
        {
            tongueMesh.verticeColors[i] = TongueColor;
        }
    }

    private static Player GetPlayer(PlayerGraphics graphics)
    {
        return (Player)graphics.owner;
    }

    // 只在 PlayerGraphics 调用期间借用矛大师的图形分支，不向玩法、存档或剧情暴露该身份。
    private static void WithSpearmasterGraphicsClass(Player player, Action action)
    {
        SlugcatStats.Name previous = player.SlugCatClass;
        player.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Spear;
        try
        {
            action();
        }
        finally
        {
            player.SlugCatClass = previous;
        }
    }

    // 临时同步颜色身份以复用体型与尾巴绘制；不修改 StoryCharacter，并在 finally 中完整恢复。
    private static void WithSpearmasterGraphicsIdentity(Player player, Action action)
    {
        SlugcatStats.Name evolutionistClass = player.SlugCatClass;
        SlugcatStats.Name previousColorCharacter = player.playerState.slugcatCharacter;

        player.playerState.slugcatCharacter = evolutionistClass;
        try
        {
            WithSpearmasterGraphicsClass(player, action);
        }
        finally
        {
            player.playerState.slugcatCharacter = previousColorCharacter;
        }
    }
}
