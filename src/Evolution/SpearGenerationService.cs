using System.Runtime.CompilerServices;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace Evolutionist.Evolution;

/// <summary>实现活体针矛生成、拔取动画和成长速度。</summary>
internal static class SpearGenerationService
{
    private const int InitialPullFrames = 114;
    private const int FastestPullFrames = 44;

    private static readonly ConditionalWeakTable<Player, PullState> PullStates =
        new();

    public static void ApplyHooks()
    {
        On.Player.GrabUpdate += PlayerGrabUpdate;
    }

    public static void RemoveHooks()
    {
        On.Player.GrabUpdate -= PlayerGrabUpdate;
    }

    private static void PlayerGrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        PullState pull = PullStates.GetOrCreateValue(self);
        if (!CanGenerateSpear(self, out EvolutionRunState state))
        {
            ResetPullVisual(self);
            pull.Reset();
            return;
        }

        int requiredFrames = GetRequiredFrames(state.Current.Get(AttributeId.SpearPullSpeed));
        // 持续满足产矛条件时逐帧推进，松键或状态中断会清空本次进度。
        pull.Frames++;
        if (pull.Frames == 1)
        {
            pull.SpearType = Random.Range(0, 3);
            self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull,
                0f, 1f, 1f + Random.value * 0.5f);
        }

        float progress = Mathf.Clamp01((float)pull.Frames / requiredFrames);

        if (self.graphicsModule is PlayerGraphics graphics && graphics.tailSpecks != null)
        {
            if (pull.Frames == 1)
            {
                graphics.tailSpecks.newSpearSlot();
            }

            graphics.tailSpecks.spearType = pull.SpearType;
            graphics.tailSpecks.setSpearProgress(progress);

            graphics.blink = Mathf.Max(graphics.blink, 5);

            if (progress > 0.6f)
            {
                graphics.head.vel += Custom.RNV() * ((progress - 0.6f) / 0.4f * 2f);
            }
        }

        if (pull.Frames < requiredFrames)
        {
            return;
        }

        CreateNeedleSpear(self, pull.SpearType);
        ResetPullVisual(self);
        pull.Reset();
    }

    private static bool CanGenerateSpear(Player player, out EvolutionRunState state)
    {
        state = null!;
        if (!EvolutionStateService.TryGet(player, out state) ||
            !state.Current.IsUnlocked(AbilityId.SpearGeneration) ||
            player.room == null ||
            player.graphicsModule == null ||
            !player.Consious ||
            !player.input[0].pckp ||
            player.input[0].y != 0 ||
            player.FreeHand() < 0 ||
            player.eatMeat >= 20 ||
            player.maulTimer >= 15 ||
            player.spearOnBack?.increment == true)
        {
            return false;
        }

        for (int i = 0; i < player.grasps.Length; i++)
        {
            PhysicalObject? held = player.grasps[i]?.grabbed;
            // 与原版矛大师一致，手持矛或可食用物时不能开始产矛。
            if (held is Spear || held is IPlayerEdible { Edible: true })
            {
                return false;
            }
        }

        return true;
    }

    private static void ResetPullVisual(Player player)
    {
        if (player.graphicsModule is PlayerGraphics graphics &&
            graphics.tailSpecks != null)
        {
            graphics.tailSpecks.setSpearProgress(0f);
        }
    }

    private static int GetRequiredFrames(float pullSpeedProgress)
    {
        float normalized = Mathf.Clamp01(pullSpeedProgress / EvolutionProgress.Maximum);
        return Mathf.RoundToInt(Mathf.Lerp(InitialPullFrames, FastestPullFrames, normalized));
    }

    private static void CreateNeedleSpear(Player player, int spearType)
    {
        player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab,
            0f, 1f, 0.5f + Random.value * 1.5f);

        PlayerGraphics graphics = (PlayerGraphics)player.graphicsModule;
        Vector2 tailPosition = graphics.tail[graphics.tail.Length / 2].pos;
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = (player.bodyChunks[1].pos - tailPosition).normalized;
            player.room.AddObject(new WaterDrip(
                tailPosition + Custom.RNV() * (Random.value * 1.5f),
                Custom.RNV() * (3f * Random.value) + direction * Mathf.Lerp(2f, 6f, Random.value),
                waterColor: false));
        }

        for (int i = 0; i < 5; i++)
        {
            Vector2 direction = Custom.RNV();
            player.room.AddObject(new Spark(
                tailPosition + direction * (Random.value * 40f),
                direction * Mathf.Lerp(4f, 30f, Random.value),
                Color.white,
                null,
                4,
                18));
        }

        var abstractSpear = new AbstractSpear(
            player.room.world,
            null,
            player.room.GetWorldCoordinate(player.mainBodyChunk.pos),
            player.room.game.GetNewID(),
            explosive: false);
        player.room.abstractRoom.AddEntity(abstractSpear);
        abstractSpear.pos = player.abstractCreature.pos;
        abstractSpear.RealizeInRoom();

        Spear spear = (Spear)abstractSpear.realizedObject;
        Vector2 spawnPosition = player.bodyChunks[0].pos;
        Vector2 ejectDirection = (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized;
        if (Mathf.Abs(player.bodyChunks[0].pos.y - player.bodyChunks[1].pos.y) >
                Mathf.Abs(player.bodyChunks[0].pos.x - player.bodyChunks[1].pos.x) &&
            player.bodyChunks[0].pos.y > player.bodyChunks[1].pos.y)
        {
            spawnPosition += ejectDirection * 5f;
            ejectDirection *= -1f;
            ejectDirection.x += 0.4f * player.flipDirection;
            ejectDirection.Normalize();
        }

        spear.firstChunk.HardSetPosition(spawnPosition);
        spear.firstChunk.vel = Vector2.ClampMagnitude(
            (ejectDirection * 2f + Custom.RNV() * Random.value) / spear.firstChunk.mass,
            6f);
        spear.Spear_makeNeedle(spearType, active: true);
        player.SlugcatGrab(spear, player.FreeHand());
    }

    private sealed class PullState
    {
        public int Frames { get; set; }

        public int SpearType { get; set; }

        public void Reset()
        {
            Frames = 0;
            SpearType = 0;
        }
    }
}
