namespace Evolutionist.Evolution;

/// <summary>禁用进化者的胃袋存储功能。</summary>
internal static class StomachStorageService
{
    public static void ApplyHooks()
    {
        On.Player.CanBeSwallowed += PlayerCanBeSwallowed;
        On.Player.SwallowObject += PlayerSwallowObject;
    }

    public static void RemoveHooks()
    {
        On.Player.CanBeSwallowed -= PlayerCanBeSwallowed;
        On.Player.SwallowObject -= PlayerSwallowObject;
    }

    private static bool PlayerCanBeSwallowed(
        On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObject)
    {
        // 进化者完全禁用胃袋存储，其他角色仍走原版判定。
        return !EvolutionStateService.IsEvolutionist(self) && orig(self, testObject);
    }

    private static void PlayerSwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (!EvolutionStateService.IsEvolutionist(self))
        {
            orig(self, grasp);
        }
    }
}
