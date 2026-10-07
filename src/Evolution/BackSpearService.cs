namespace Evolutionist.Evolution;

// 在解锁后启用猎手式背部存矛。
internal static class BackSpearService
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
        // 原版背矛对象可以延后创建，因此解锁后在下一次玩家更新中补建即可。
        if (self.spearOnBack == null &&
            EvolutionStateService.TryGet(self, out EvolutionRunState state) &&
            state.Current.IsUnlocked(AbilityId.BackSpear))
        {
            self.spearOnBack = new Player.SpearOnBack(self);
        }

        orig(self, eu);
    }
}
