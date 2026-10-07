namespace Evolutionist.Evolution;

// 控制拾荒者追杀机制在产矛能力永久解锁后生效。
internal static class ScavengerBehaviorService
{
    public static void ApplyHooks()
    {
        On.ScavengerOutpost.ScavengerReportTransgression += ScavengerReportTransgression;
    }

    public static void RemoveHooks()
    {
        On.ScavengerOutpost.ScavengerReportTransgression -= ScavengerReportTransgression;
    }

    private static void ScavengerReportTransgression(
        On.ScavengerOutpost.orig_ScavengerReportTransgression orig,
        ScavengerOutpost self,
        Player player)
    {
        // 产矛能力在成功雨眠前不算永久解锁，因此追杀条件读取 Saved。
        if (EvolutionStateService.TryGet(player, out EvolutionRunState state) &&
            !state.Saved.IsUnlocked(AbilityId.SpearGeneration))
        {
            return;
        }

        orig(self, player);
    }
}
