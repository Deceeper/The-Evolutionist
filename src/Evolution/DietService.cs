using SlugBase.DataTypes;
using SlugBase.Features;

namespace Evolutionist.Evolution;

/// <summary>使食性与当前产矛能力的解锁状态保持同步。</summary>
internal static class DietService
{
    public static void ApplyHooks()
    {
        On.Player.Update += PlayerUpdate;
    }

    public static void RemoveHooks()
    {
        On.Player.Update -= PlayerUpdate;
    }

    private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        ApplyCurrentDiet(self);
        orig(self, eu);
    }

    private static void ApplyCurrentDiet(Player player)
    {
        if (!EvolutionStateService.TryGet(player, out EvolutionRunState state) ||
            !PlayerFeatures.Diet.TryGet(player, out Diet diet))
        {
            return;
        }

        bool carnivore = state.Current.IsUnlocked(AbilityId.SpearGeneration);
        // 食性直接跟随当前雨循环的能力状态，死亡回滚时会同步恢复。
        diet.Corpses = 1f;
        diet.Meat = 1f;
        diet.Plants = carnivore ? 0.25f : 1f;
        diet.CreatureOverrides[CreatureTemplate.Type.Fly] = carnivore ? 0.25f : 1f;
    }
}
