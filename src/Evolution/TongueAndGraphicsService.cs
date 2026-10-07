using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace Evolutionist.Evolution;

// 以进化者自身身份实现肥大尾巴、尾部孔洞与圣徒舌头。
internal static class TongueAndGraphicsService
{
    private const int RopeSegmentCount = 20;

    private static readonly Color EyeColor = new(224f / 255f, 121f / 255f, 88f / 255f);
    private static readonly Color TailPatternColor = new(237f / 255f, 242f / 255f, 245f / 255f);
    private static readonly Color TongueColor = new(1f, 128f / 255f, 166f / 255f);

    private static readonly ConditionalWeakTable<RoomCamera.SpriteLeaser, SpriteLayout> SpriteLayouts =
        new();
    private static readonly ConditionalWeakTable<PlayerGraphics, RopeVisualState> RopeVisualStates =
        new();

    public static void ApplyHooks()
    {
        On.Player.ctor += PlayerCtor;
        On.Player.Update += PlayerUpdate;
        On.Player.ClassMechanicsSaint += PlayerClassMechanicsSaint;
        On.Player.SaintTongueCheck += PlayerSaintTongueCheck;
        On.PlayerGraphics.ctor += PlayerGraphicsCtor;
        On.PlayerGraphics.Reset += PlayerGraphicsReset;
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
        On.PlayerGraphics.Reset -= PlayerGraphicsReset;
        On.PlayerGraphics.ctor -= PlayerGraphicsCtor;
        On.Player.SaintTongueCheck -= PlayerSaintTongueCheck;
        On.Player.ClassMechanicsSaint -= PlayerClassMechanicsSaint;
        On.Player.Update -= PlayerUpdate;
        On.Player.ctor -= PlayerCtor;
    }

    private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        UpdateTongueAvailability(self);
    }

    private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        UpdateTongueAvailability(self);
        ReleaseTongueForShortcut(self);
        orig(self, eu);
        ReleaseTongueForShortcut(self);
    }

    private static bool PlayerSaintTongueCheck(On.Player.orig_SaintTongueCheck orig, Player self)
    {
        if (!HasTongueAbility(self))
        {
            return orig(self);
        }

        UpdateTongueAvailability(self);
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

        if (!HasTongueAbility(self) || MMF.cfgOldTongue.Value)
        {
            return;
        }

        UpdateTongueAvailability(self);
        if (!self.input[0].jmp ||
            self.input[1].jmp ||
            self.input[0].pckp ||
            self.canJump > 0 ||
            self.bodyMode == Player.BodyModeIndex.Crawl ||
            self.animation == Player.AnimationIndex.ClimbOnBeam ||
            self.animation == Player.AnimationIndex.AntlerClimb ||
            self.animation == Player.AnimationIndex.HangFromBeam ||
            !self.SaintTongueCheck())
        {
            return;
        }

        Vector2 direction = self.input[0].y > 0
            ? Vector2.up
            : new Vector2(self.flipDirection, 0.7f).normalized;
        direction = (direction + self.mainBodyChunk.vel.normalized * 0.2f).normalized;
        self.tongue.Shoot(direction);
    }

    private static void PlayerGraphicsCtor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject owner)
    {
        // 原版构造函数始终使用真实的 Evolutionist 身份。
        orig(self, owner);
        if (!IsEvolutionistGraphics(self))
        {
            return;
        }

        UseSpearmasterTailShape(self);
        EnsureRopeSegments(self);
        self.tailSpecks = new PlayerGraphics.TailSpeckles(self, 0);
        self.bodyPearl = null;
    }

    private static void PlayerGraphicsReset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
    {
        orig(self);
        if (IsEvolutionistGraphics(self))
        {
            ResetRopeSegments(self);
        }
    }

    private static void PlayerGraphicsUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        if (IsEvolutionistGraphics(self))
        {
            UpdateEvolutionistRope(self);
        }
    }

    private static void PlayerGraphicsInitiateSprites(
        On.PlayerGraphics.orig_InitiateSprites orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam)
    {
        SpriteLayouts.Remove(sLeaser);
        orig(self, sLeaser, rCam);
        if (!IsEvolutionistGraphics(self))
        {
            return;
        }

        EnsureAuxiliaryGraphics(self);

        int tailStart = sLeaser.sprites.Length;
        self.tailSpecks.startSprite = tailStart;
        int tongueSprite = tailStart + self.tailSpecks.numberOfSprites;

        FSprite[] expanded = new FSprite[tongueSprite + 1];
        Array.Copy(sLeaser.sprites, expanded, sLeaser.sprites.Length);
        sLeaser.sprites = expanded;

        self.tailSpecks.InitiateSprites(sLeaser, rCam);
        sLeaser.sprites[tongueSprite] = TriangleMesh.MakeLongMesh(
            self.ropeSegments.Length - 1,
            pointyTip: false,
            customColor: true);

        SpriteLayouts.Add(sLeaser, new SpriteLayout(tailStart, tongueSprite));
        AddCustomSpritesToContainer(self, sLeaser, rCam);
        ApplyEvolutionistDetailColors(self, sLeaser);
    }

    private static void PlayerGraphicsDrawSprites(
        On.PlayerGraphics.orig_DrawSprites orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        float timeStacker,
        Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (!IsEvolutionistGraphics(self) ||
            !TryGetLayout(self, sLeaser, out SpriteLayout layout))
        {
            return;
        }

        self.tailSpecks.startSprite = layout.TailStart;
        self.tailSpecks.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        UpdateTailPatternVisibility(self, sLeaser, layout);
        ApplyEvolutionistDetailColors(self, sLeaser);
        DrawTongue(self, sLeaser, layout.TongueSprite, timeStacker, camPos);
    }

    private static void PlayerGraphicsApplyPalette(
        On.PlayerGraphics.orig_ApplyPalette orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (IsEvolutionistGraphics(self))
        {
            ApplyEvolutionistDetailColors(self, sLeaser);
        }
    }

    private static void PlayerGraphicsAddToContainer(
        On.PlayerGraphics.orig_AddToContainer orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        FContainer newContainer)
    {
        orig(self, sLeaser, rCam, newContainer);
        if (IsEvolutionistGraphics(self))
        {
            AddCustomSpritesToContainer(self, sLeaser, rCam);
        }
    }

    private static void UpdateTongueAvailability(Player player)
    {
        if (!EvolutionStateService.IsEvolutionist(player))
        {
            return;
        }

        if (HasTongueAbility(player))
        {
            player.tongue ??= new Player.Tongue(player, 0);
            return;
        }

        if (player.tongue != null)
        {
            if (player.tongue.mode != Player.Tongue.Mode.Retracted)
            {
                player.tongue.Release();
            }

            player.tongue = null;
        }
    }

    private static bool HasTongueAbility(Player player)
    {
        return EvolutionStateService.TryGet(player, out EvolutionRunState state) &&
            state.Current.IsUnlocked(AbilityId.Tongue);
    }

    // 管道状态可能在 Player.Update 内才刚被设置，因此更新前后都检查一次。
    private static void ReleaseTongueForShortcut(Player player)
    {
        if (EvolutionStateService.IsEvolutionist(player) &&
            player.tongue != null &&
            player.tongue.mode != Player.Tongue.Mode.Retracted &&
            (player.enteringShortCut.HasValue || player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut))
        {
            player.tongue.Release();
        }
    }

    private static bool IsEvolutionistGraphics(PlayerGraphics graphics)
    {
        return graphics?.owner is Player player && EvolutionStateService.IsEvolutionist(player);
    }

    private static void EnsureAuxiliaryGraphics(PlayerGraphics graphics)
    {
        EnsureRopeSegments(graphics);
        graphics.tailSpecks ??= new PlayerGraphics.TailSpeckles(graphics, 0);
    }

    private static void EnsureRopeSegments(PlayerGraphics graphics)
    {
        if (graphics.ropeSegments?.Length == RopeSegmentCount)
        {
            return;
        }

        graphics.ropeSegments = new PlayerGraphics.RopeSegment[RopeSegmentCount];
        for (int i = 0; i < graphics.ropeSegments.Length; i++)
        {
            graphics.ropeSegments[i] = new PlayerGraphics.RopeSegment(i, graphics);
        }

        ResetRopeSegments(graphics);
    }

    private static void ResetRopeSegments(PlayerGraphics graphics)
    {
        if (graphics.ropeSegments == null || graphics.owner is not Player player)
        {
            return;
        }

        Vector2 position = player.mainBodyChunk.pos;
        foreach (PlayerGraphics.RopeSegment segment in graphics.ropeSegments)
        {
            segment.pos = position;
            segment.lastPos = position;
            segment.vel = Vector2.zero;
            segment.claimedForBend = false;
        }

        RopeVisualState visualState = RopeVisualStates.GetOrCreateValue(graphics);
        visualState.LastStretch = player.tongue != null ? graphics.RopeStretchFac : 0f;
        visualState.Stretch = visualState.LastStretch;
    }

    private static void UseSpearmasterTailShape(PlayerGraphics graphics)
    {
        Player player = (Player)graphics.owner;
        TailSegment[] oldTail = graphics.tail;
        float length = player.playerState.isPup ? 3.5f : 7f;
        float firstLength = player.playerState.isPup ? 2f : 4f;

        TailSegment[] newTail = new TailSegment[4];
        newTail[0] = new TailSegment(graphics, 8f, firstLength, null, 0.85f, 1f, 1f, true);
        newTail[1] = new TailSegment(graphics, 6f, length, newTail[0], 0.85f, 1f, 0.5f, true);
        newTail[2] = new TailSegment(graphics, 4f, length, newTail[1], 0.85f, 1f, 0.5f, true);
        newTail[3] = new TailSegment(graphics, 2f, length, newTail[2], 0.85f, 1f, 0.5f, true);

        graphics.tail = newTail;
        for (int bodyPartIndex = 0; bodyPartIndex < graphics.bodyParts.Length; bodyPartIndex++)
        {
            for (int tailIndex = 0; tailIndex < oldTail.Length && tailIndex < newTail.Length; tailIndex++)
            {
                if (ReferenceEquals(graphics.bodyParts[bodyPartIndex], oldTail[tailIndex]))
                {
                    graphics.bodyParts[bodyPartIndex] = newTail[tailIndex];
                    break;
                }
            }
        }

        Vector2 resetPosition = player.bodyChunks[1].pos;
        foreach (TailSegment segment in newTail)
        {
            segment.Reset(resetPosition);
        }
    }

    // 复制圣徒的绳段对齐与距离约束，但不进入圣徒的原版角色分支。
    private static void UpdateEvolutionistRope(PlayerGraphics graphics)
    {
        if (graphics.owner is not Player player ||
            player.room == null ||
            player.tongue == null ||
            !HasTongueAbility(player))
        {
            return;
        }

        EnsureRopeSegments(graphics);
        RopeVisualState visualState = RopeVisualStates.GetOrCreateValue(graphics);
        visualState.LastStretch = visualState.Stretch;
        visualState.Stretch = graphics.RopeStretchFac;

        var ropePoints = new List<Vector2>();
        for (int i = player.tongue.rope.TotalPositions - 1; i > 0; i--)
        {
            ropePoints.Add(player.tongue.rope.GetPosition(i));
        }

        ropePoints.Add(player.mainBodyChunk.pos);
        float totalLength = 0f;
        for (int i = 1; i < ropePoints.Count; i++)
        {
            totalLength += Vector2.Distance(ropePoints[i - 1], ropePoints[i]);
        }

        if (totalLength <= 0.001f)
        {
            AlignRopeSegment(graphics, 0f, player.mainBodyChunk.pos);
        }
        else
        {
            float coveredLength = 0f;
            for (int i = 0; i < ropePoints.Count; i++)
            {
                if (i > 0)
                {
                    coveredLength += Vector2.Distance(ropePoints[i - 1], ropePoints[i]);
                }

                AlignRopeSegment(graphics, coveredLength / totalLength, ropePoints[i]);
            }
        }

        foreach (PlayerGraphics.RopeSegment segment in graphics.ropeSegments)
        {
            segment.Update();
        }

        for (int i = 1; i < graphics.ropeSegments.Length; i++)
        {
            ConnectRopeSegments(graphics, i, i - 1);
        }

        foreach (PlayerGraphics.RopeSegment segment in graphics.ropeSegments)
        {
            segment.claimedForBend = false;
        }
    }

    private static void AlignRopeSegment(PlayerGraphics graphics, float position, Vector2 target)
    {
        int index = Custom.IntClamp(
            (int)(position * graphics.ropeSegments.Length),
            0,
            graphics.ropeSegments.Length - 1);
        PlayerGraphics.RopeSegment segment = graphics.ropeSegments[index];
        segment.lastPos = segment.pos;
        segment.pos = target;
        segment.vel = Vector2.zero;
        segment.claimedForBend = true;
    }

    private static void ConnectRopeSegments(PlayerGraphics graphics, int firstIndex, int secondIndex)
    {
        PlayerGraphics.RopeSegment first = graphics.ropeSegments[firstIndex];
        PlayerGraphics.RopeSegment second = graphics.ropeSegments[secondIndex];
        Vector2 direction = Direction(first.pos, second.pos);
        float distance = Vector2.Distance(first.pos, second.pos);
        Player player = (Player)graphics.owner;
        float targetDistance = player.tongue.rope.totalLength /
            graphics.ropeSegments.Length * 0.1f;
        Vector2 correction = direction * ((distance - targetDistance) * 0.5f);

        if (!first.claimedForBend)
        {
            first.pos += correction;
            first.vel += correction;
        }

        if (!second.claimedForBend)
        {
            second.pos -= correction;
            second.vel -= correction;
        }
    }

    private static bool TryGetLayout(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser sLeaser,
        out SpriteLayout layout)
    {
        if (SpriteLayouts.TryGetValue(sLeaser, out SpriteLayout? found) &&
            graphics.tailSpecks != null &&
            found.TailStart >= 0 &&
            found.TongueSprite < sLeaser.sprites.Length)
        {
            layout = found;
            return true;
        }

        layout = null!;
        return false;
    }

    private static void AddCustomSpritesToContainer(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam)
    {
        if (!TryGetLayout(graphics, sLeaser, out SpriteLayout layout))
        {
            return;
        }

        FContainer midground = rCam.ReturnFContainer("Midground");
        graphics.tailSpecks.startSprite = layout.TailStart;
        graphics.tailSpecks.AddToContainer(sLeaser, rCam, midground);
        midground.AddChild(sLeaser.sprites[layout.TongueSprite]);
    }

    private static void UpdateTailPatternVisibility(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser sLeaser,
        SpriteLayout layout)
    {
        bool unlocked = graphics.owner is Player player &&
            EvolutionStateService.TryGet(player, out EvolutionRunState state) &&
            state.Current.IsUnlocked(AbilityId.SpearGeneration);

        int end = Math.Min(
            sLeaser.sprites.Length,
            layout.TailStart + graphics.tailSpecks.numberOfSprites);
        for (int i = layout.TailStart; i < end; i++)
        {
            if (sLeaser.sprites[i] != null)
            {
                sLeaser.sprites[i].isVisible = unlocked;
            }
        }
    }

    private static void ApplyEvolutionistDetailColors(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser sLeaser)
    {
        if (sLeaser.sprites.Length > 9 && sLeaser.sprites[9] != null)
        {
            sLeaser.sprites[9].color = EyeColor;
        }

        if (!TryGetLayout(graphics, sLeaser, out SpriteLayout layout))
        {
            return;
        }

        int end = Math.Min(
            sLeaser.sprites.Length,
            layout.TailStart + graphics.tailSpecks.numberOfSprites);
        for (int i = layout.TailStart; i < end; i++)
        {
            if (sLeaser.sprites[i] != null)
            {
                sLeaser.sprites[i].color = TailPatternColor;
            }
        }

        if (sLeaser.sprites[layout.TongueSprite] is TriangleMesh tongueMesh)
        {
            tongueMesh.color = TongueColor;
            for (int i = 0; i < tongueMesh.verticeColors.Length; i++)
            {
                tongueMesh.verticeColors[i] = TongueColor;
            }
        }
    }

    private static void DrawTongue(
        PlayerGraphics graphics,
        RoomCamera.SpriteLeaser sLeaser,
        int tongueSpriteIndex,
        float timeStacker,
        Vector2 camPos)
    {
        if (sLeaser.sprites[tongueSpriteIndex] is not TriangleMesh mesh ||
            graphics.owner is not Player player)
        {
            return;
        }

        bool visible = player.room != null &&
            player.tongue != null &&
            HasTongueAbility(player) &&
            (player.tongue.Free || player.tongue.Attached);
        mesh.isVisible = visible;
        if (!visible || graphics.ropeSegments.Length < 2)
        {
            return;
        }

        RopeVisualState visualState = RopeVisualStates.GetOrCreateValue(graphics);
        float stretch = Mathf.Lerp(visualState.LastStretch, visualState.Stretch, timeStacker);
        Vector2 previous = Vector2.Lerp(
            graphics.ropeSegments[0].lastPos,
            graphics.ropeSegments[0].pos,
            timeStacker);
        Vector2 second = Vector2.Lerp(
            graphics.ropeSegments[1].lastPos,
            graphics.ropeSegments[1].pos,
            timeStacker);
        previous += Direction(previous, second);

        for (int i = 1; i < graphics.ropeSegments.Length; i++)
        {
            float position = (float)i / (graphics.ropeSegments.Length - 1);
            Vector2 next = i >= graphics.ropeSegments.Length - 2
                ? new Vector2(sLeaser.sprites[9].x, sLeaser.sprites[9].y) + camPos
                : Vector2.Lerp(
                    graphics.ropeSegments[i].lastPos,
                    graphics.ropeSegments[i].pos,
                    timeStacker);
            Vector2 direction = (previous - next).normalized;
            Vector2 perpendicular = new(-direction.y, direction.x);
            float width = 0.2f + 1.6f * Mathf.Lerp(
                1f,
                stretch,
                Mathf.Pow(Mathf.Sin(position * Mathf.PI), 0.7f));

            int vertex = (i - 1) * 4;
            mesh.MoveVertice(vertex, previous - perpendicular * width - camPos);
            mesh.MoveVertice(vertex + 1, previous + perpendicular * width - camPos);
            mesh.MoveVertice(vertex + 2, next - perpendicular * width - camPos);
            mesh.MoveVertice(vertex + 3, next + perpendicular * width - camPos);
            previous = next;
        }
    }

    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 difference = to - from;
        return difference.sqrMagnitude > 0.0001f ? difference.normalized : Vector2.zero;
    }

    private sealed class SpriteLayout
    {
        public SpriteLayout(int tailStart, int tongueSprite)
        {
            TailStart = tailStart;
            TongueSprite = tongueSprite;
        }

        public int TailStart { get; }

        public int TongueSprite { get; }
    }

    private sealed class RopeVisualState
    {
        public float LastStretch { get; set; }

        public float Stretch { get; set; }
    }
}
