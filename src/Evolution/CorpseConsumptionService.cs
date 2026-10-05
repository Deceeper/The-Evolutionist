using System;
using UnityEngine;

namespace Evolutionist.Evolution;

/// <summary>检测尸体最后一口，并处理满腹状态下的完整进食。</summary>
internal static class CorpseConsumptionService
{
    [ThreadStatic]
    private static Player? foodSuppressedFor;

    public static void ApplyHooks()
    {
        On.Player.EatMeatUpdate += PlayerEatMeatUpdate;
        On.Player.AddFood += PlayerAddFood;
    }

    public static void RemoveHooks()
    {
        On.Player.EatMeatUpdate -= PlayerEatMeatUpdate;
        On.Player.AddFood -= PlayerAddFood;
    }

    private static void PlayerEatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        Creature? corpse = GetHeldCreature(self, graspIndex);
        if (!EvolutionStateService.IsEvolutionist(self) || corpse == null)
        {
            orig(self, graspIndex);
            return;
        }

        int meatBefore = corpse.State.meatLeft;
        bool bypassFullStomach = meatBefore > 0 && self.FoodInStomach >= self.MaxFoodInStomach;
        int foodBefore = self.playerState.foodInStomach;
        int quarterFoodBefore = self.playerState.quarterFoodPoints;

        // 临时腾出一格只为允许继续啃食，finally 会恢复原饱食度且不额外加食物。
        try
        {
            if (bypassFullStomach)
            {
                self.playerState.foodInStomach = Math.Max(0, self.MaxFoodInStomach - 1);
                foodSuppressedFor = self;
            }

            orig(self, graspIndex);
        }
        finally
        {
            if (bypassFullStomach)
            {
                foodSuppressedFor = null;
                self.playerState.foodInStomach = foodBefore;
                self.playerState.quarterFoodPoints = quarterFoodBefore;
            }
        }

        int meatAfter = corpse.State.meatLeft;
        // 仅最后一口由进化者完成且尸体已死亡时结算一次进化收益。
        if (meatBefore > 0 && meatAfter == 0 && corpse.dead)
        {
            AwardCompletedCorpse(self, corpse);
        }

        // 原版会让满腹玩家每 80 帧丢下未吃完的尸体，此处只跳过该次强制丢弃。
        if (bypassFullStomach && meatAfter > 0 && self.eatMeat % 80 == 0)
        {
            self.eatMeat++;
        }
    }

    private static void PlayerAddFood(On.Player.orig_AddFood orig, Player self, int add)
    {
        if (ReferenceEquals(foodSuppressedFor, self))
        {
            return;
        }

        orig(self, add);
    }

    private static Creature? GetHeldCreature(Player player, int graspIndex)
    {
        if (graspIndex < 0 || graspIndex >= player.grasps.Length)
        {
            return null;
        }

        return player.grasps[graspIndex]?.grabbed as Creature;
    }

    private static void AwardCompletedCorpse(Player player, Creature corpse)
    {
        if (!CreatureEvolutionCatalog.TryGet(corpse.Template.type, out CreatureEvolutionReward reward) ||
            !EvolutionStateService.TryGet(player, out EvolutionRunState state))
        {
            return;
        }

        float templateYield = Math.Max(1, corpse.Template.meatPoints);
        float intrinsicYield = GetIntrinsicMeatYield(corpse);
        state.ApplyCorpseReward(reward, intrinsicYield / templateYield);
    }

    private static float GetIntrinsicMeatYield(Creature corpse)
    {
        if (corpse is not Centipede centipede)
        {
            return Math.Max(1, corpse.Template.meatPoints);
        }

        if (centipede.Centiwing)
        {
            return 3f;
        }

        if (centipede.Red)
        {
            return 9f;
        }

        return Mathf.RoundToInt(Mathf.Lerp(2.3f, 7f, centipede.size));
    }
}
