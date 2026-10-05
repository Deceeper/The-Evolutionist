using UnityEngine;

namespace Evolutionist.Evolution;

/// <summary>将当前进化进度转换为玩家属性和动作补偿。</summary>
internal static class AttributeApplicationService
{
    private const float MonkRunSpeed = 1f;
    private const float MonkPoleClimbSpeed = 1f;
    private const float MonkCorridorClimbSpeed = 1f;
    private const float MonkLungsFactor = 1.2f;
    private const float MonkSwimForceFactor = 1f;

    private const float MaximumRunSpeed = 1.75f;
    private const float MaximumPoleClimbSpeed = 1.8f;
    private const float MaximumCorridorClimbSpeed = 1.6f;
    private const float MaximumLungsFactor = 0.15f;
    private const float MaximumSwimForceFactor = 1f;

    private const float MonkRollPropulsion = 1.1f;
    private const float MaximumRollPropulsion = 2.2f;
    private const float MonkShortSlidePropulsion = 18.1f;
    private const float MonkLongSlidePropulsion = 14f;
    private const float MaximumShortSlidePropulsion = 25f;
    private const float MaximumLongSlidePropulsion = 20f;

    public static void ApplyHooks()
    {
        On.Player.Update += PlayerUpdate;
        On.Player.MovementUpdate += PlayerMovementUpdate;
        On.Player.Jump += PlayerJump;
        On.Player.Stun += PlayerStun;
        On.Player.ThrowObject += PlayerThrowObject;
        On.Weapon.Update += WeaponUpdate;
    }

    public static void RemoveHooks()
    {
        On.Weapon.Update -= WeaponUpdate;
        On.Player.ThrowObject -= PlayerThrowObject;
        On.Player.Stun -= PlayerStun;
        On.Player.Jump -= PlayerJump;
        On.Player.MovementUpdate -= PlayerMovementUpdate;
        On.Player.Update -= PlayerUpdate;
    }

    private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        ApplyDirectStats(self);
        orig(self, eu);
        ApplyDirectStats(self);
    }

    private static void PlayerMovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (!EvolutionStateService.TryGet(self, out EvolutionRunState state))
        {
            orig(self, eu);
            return;
        }

        Player.AnimationIndex animation = self.animation;
        int rollDirection = self.rollDirection;
        int rollCounter = self.rollCounter;
        bool longBellySlide = self.longBellySlide;

        orig(self, eu);

        EvolutionProgress progress = state.Current;
        if (animation == Player.AnimationIndex.Roll && rollDirection != 0)
        {
            float extra = Interpolate(MonkRollPropulsion, MaximumRollPropulsion,
                progress.Get(AttributeId.RollDistance)) - MonkRollPropulsion;
            self.bodyChunks[0].vel.x += extra * rollDirection;
            self.bodyChunks[1].vel.x += extra * rollDirection;
        }
        else if (animation == Player.AnimationIndex.BellySlide && rollDirection != 0)
        {
            float amount = Progress01(progress, AttributeId.SlideSpeed);
            float initial = longBellySlide ? MonkLongSlidePropulsion : MonkShortSlidePropulsion;
            float maximum = longBellySlide ? MaximumLongSlidePropulsion : MaximumShortSlidePropulsion;
            float phaseLength = longBellySlide ? 39f : 15f;

            float extra = Mathf.Lerp(initial, maximum, amount) - initial;
            self.bodyChunks[0].vel.x += extra * rollDirection *
                Mathf.Sin(rollCounter / phaseLength * Mathf.PI);

            if (rollCounter < 6)
            {
                self.bodyChunks[1].vel.y -= 2.7f * amount;
                self.bodyChunks[1].vel.x += 9.1f * rollDirection * amount;
            }
        }
        if (animation == Player.AnimationIndex.SurfaceSwim)
        {
            float swimProgress = Progress01(progress, AttributeId.SwimSpeed);

            if (self.input[0].x != 0)
            {
                float baseImpulse = Mathf.Lerp(0.2f, 0.3f, self.Adrenaline);

                float surfaceMultiplier = Mathf.Lerp(1f, 1.5f, swimProgress);

                self.bodyChunks[1].vel.x -= self.input[0].x * baseImpulse * (surfaceMultiplier - 1f);
            }
        }
    }

    private static void PlayerJump(On.Player.orig_Jump orig, Player self)
    {
        if (!EvolutionStateService.TryGet(self, out EvolutionRunState state))
        {
            orig(self);
            return;
        }

        Player.AnimationIndex animation = self.animation;
        Player.BodyModeIndex bodyMode = self.bodyMode;
        bool standing = self.standing;
        int slideCounter = self.slideCounter;
        int superLaunchJump = self.superLaunchJump;
        bool runningBackflip = IsRunningBackflip(standing, slideCounter);
        float velocity0X = self.bodyChunks[0].vel.x;
        float velocity1X = self.bodyChunks[1].vel.x;

        orig(self);

        EvolutionProgress progress = state.Current;
        if (runningBackflip &&
            self.animation == Player.AnimationIndex.Flip)
        {
            float amount = Progress01(progress, AttributeId.BackflipHeight);

            if (self.bodyChunks[0].vel.y > 0f)
            {
                self.bodyChunks[0].vel.y *= Mathf.Lerp(1f, 12f / 9f, amount);
            }

            if (self.bodyChunks[1].vel.y > 0f)
            {
                self.bodyChunks[1].vel.y *= Mathf.Lerp(1f, 10f / 7f, amount);
            }

            self.jumpBoost = Mathf.Lerp(self.jumpBoost, 9f, amount);
            return;
        }

        if (!standing && superLaunchJump >= 20)
        {
            float multiplier = Mathf.Lerp(1f, 12f / 9f, Progress01(progress, AttributeId.PounceDistance));
            ScaleSignedDelta(ref self.bodyChunks[0].vel.x, velocity0X, multiplier);
            ScaleSignedDelta(ref self.bodyChunks[1].vel.x, velocity1X, multiplier);
            return;
        }

        if (IsOrdinaryGroundJump(standing, animation, bodyMode, slideCounter, self))
        {
            float amount = Progress01(progress, AttributeId.JumpHeight);

            if (self.bodyChunks[0].vel.y > 0f)
            {
                self.bodyChunks[0].vel.y *= Mathf.Lerp(1f, 6f / 4f, amount);
            }

            if (self.bodyChunks[1].vel.y > 0f)
            {
                self.bodyChunks[1].vel.y *= Mathf.Lerp(1f, 5f / 3f, amount);
            }
        }
    }

    private static void PlayerStun(On.Player.orig_Stun orig, Player self, int st)
    {
        if (st > 0 && !self.dead && EvolutionStateService.TryGet(self, out EvolutionRunState state))
        {
            float recoveryMultiplier = Mathf.Lerp(
                1f, 2f, Progress01(state.Current, AttributeId.StunResistance));
            st = Mathf.Max(1, Mathf.CeilToInt(st / recoveryMultiplier));
        }

        orig(self, st);
    }

    private static void PlayerThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        Spear? spear = null;
        if (grasp >= 0 && grasp < self.grasps.Length && self.grasps[grasp]?.grabbed is Spear heldSpear)
        {
            spear = heldSpear;
        }

        orig(self, grasp, eu);

        if (spear == null || spear.thrownBy != self ||
            !EvolutionStateService.TryGet(self, out EvolutionRunState state))
        {
            return;
        }

        EvolutionProgress progress = state.Current;
        float damageProgress = Progress01(progress, AttributeId.SpearDamage);
        spear.spearDamageBonus = Mathf.Lerp(spear.spearDamageBonus, 1.25f, damageProgress);

        float performanceProgress = Progress01(progress, AttributeId.SpearPerformance);
        spear.firstChunk.vel *= Mathf.Lerp(1f, 1.2f / 0.77f, performanceProgress);
        // 取消原版按飞行时间失效的限制，实际落地后再由 WeaponUpdate 解除投掷状态。
        spear.throwModeFrames = -1;
        spear.doNotTumbleAtLowSpeed = true;
    }

    private static void WeaponUpdate(On.Weapon.orig_Update orig, Weapon self, bool eu)
    {
        bool protectedEvolutionistSpear =
            self is Spear &&
            self.mode == Weapon.Mode.Thrown &&
            self.doNotTumbleAtLowSpeed &&
            self.thrownBy is Player thrower &&
            EvolutionStateService.TryGet(thrower, out _);

        orig(self, eu);

        if (!protectedEvolutionistSpear)
        {
            return;
        }

        // 矛在空中保持有效；接触地面且弹跳结束后才转为可拾取的自由状态。
        if (self.mode == Weapon.Mode.Thrown &&
            self.firstChunk.ContactPoint.y < 0 &&
            self.floorBounceFrames <= 0)
        {
            self.doNotTumbleAtLowSpeed = false;
            self.SetRandomSpin();
            self.ChangeMode(Weapon.Mode.Free);
            return;
        }

        if (self.mode != Weapon.Mode.Thrown)
        {
            self.doNotTumbleAtLowSpeed = false;
        }
    }

    private static void ApplyDirectStats(Player player)
    {
        if (!EvolutionStateService.TryGet(player, out EvolutionRunState state))
        {
            return;
        }

        EvolutionProgress progress = state.Current;
        player.slugcatStats.runspeedFac = Interpolate(MonkRunSpeed, MaximumRunSpeed,
            progress.Get(AttributeId.RunSpeed));
        player.initRunSpeedFac = player.slugcatStats.runspeedFac;
        player.slugcatStats.poleClimbSpeedFac = Interpolate(MonkPoleClimbSpeed, MaximumPoleClimbSpeed,
            progress.Get(AttributeId.PoleClimbSpeed));
        player.slugcatStats.corridorClimbSpeedFac = Interpolate(MonkCorridorClimbSpeed,
            MaximumCorridorClimbSpeed, progress.Get(AttributeId.CorridorClimbSpeed));
        player.slugcatStats.lungsFac = Interpolate(MonkLungsFactor, MaximumLungsFactor,
            progress.Get(AttributeId.LungCapacity));
        player.slugcatStats.swimForceFac = Interpolate(MonkSwimForceFactor, MaximumSwimForceFactor,
            progress.Get(AttributeId.SwimSpeed));
    }

    private static bool IsRunningBackflip(bool standing, int slideCounter)
    {
        return standing &&
            slideCounter > 0 &&
            slideCounter < 10;
    }

    private static bool IsOrdinaryGroundJump(
        bool standing,
        Player.AnimationIndex animation,
        Player.BodyModeIndex bodyMode,
        int slideCounter,
        Player player)
    {
        return standing &&
            (slideCounter <= 0 || slideCounter >= 10) &&
            IsGroundBodyMode(bodyMode) &&
            animation != Player.AnimationIndex.Roll &&
            animation != Player.AnimationIndex.BellySlide &&
            animation != Player.AnimationIndex.Flip &&
            animation != Player.AnimationIndex.RocketJump &&
            player.animation != Player.AnimationIndex.BellySlide;
    }

    private static bool IsGroundBodyMode(Player.BodyModeIndex bodyMode)
    {
        return bodyMode == Player.BodyModeIndex.Default ||
            bodyMode == Player.BodyModeIndex.Stand ||
            bodyMode == Player.BodyModeIndex.Crawl;
    }

    private static void ScaleSignedDelta(ref float value, float original, float multiplier)
    {
        value = original + (value - original) * multiplier;
    }

    private static float Progress01(EvolutionProgress progress, AttributeId attribute)
    {
        return Mathf.Clamp01(progress.Get(attribute) / EvolutionProgress.Maximum);
    }

    private static float Interpolate(float initial, float maximum, float progress)
    {
        return Mathf.Lerp(initial, maximum, Mathf.Clamp01(progress / EvolutionProgress.Maximum));
    }
}
