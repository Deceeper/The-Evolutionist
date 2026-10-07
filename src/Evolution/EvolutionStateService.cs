using System;
using System.Runtime.CompilerServices;
using SlugBase.SaveData;

namespace Evolutionist.Evolution;

// 创建、提交、回滚并保存进化进度。
internal static class EvolutionStateService
{
    public const string CharacterId = "Evolutionist";

    private const string SaveKey = Plugin.ModId + ".progress.v1";

    private static readonly ConditionalWeakTable<RainWorldGame, EvolutionRunState> States =
        new();

    public static void ApplyHooks()
    {
        On.SaveState.SessionEnded += SaveStateSessionEnded;
    }

    public static void RemoveHooks()
    {
        On.SaveState.SessionEnded -= SaveStateSessionEnded;
    }

    public static bool IsEvolutionist(Player player)
    {
        return player?.SlugCatClass?.value == CharacterId;
    }

    public static bool TryGet(Player player, out EvolutionRunState state)
    {
        state = null!;
        if (!IsEvolutionist(player))
        {
            return false;
        }

        RainWorldGame? game = player.abstractCreature?.world?.game;
        if (game == null)
        {
            return false;
        }

        state = States.GetValue(game, CreateState);
        return true;
    }

    public static bool TryGet(RainWorldGame game, out EvolutionRunState state)
    {
        state = null!;
        if (!IsEvolutionistStory(game))
        {
            return false;
        }

        state = States.GetValue(game, CreateState);
        return true;
    }

    private static EvolutionRunState CreateState(RainWorldGame game)
    {
        var saved = new EvolutionProgress();
        // 非剧情模式始终从空进度开始，避免竞技场局次之间继承属性。
        if (IsEvolutionistStory(game))
        {
            DeathPersistentSaveData persistent = game.GetStorySession.saveState.deathPersistentSaveData;
            if (persistent.GetSlugBaseData().TryGet(SaveKey, out EvolutionProgress loaded) && loaded != null)
            {
                loaded.Normalize();
                saved = loaded;
            }
        }

        return new EvolutionRunState(saved);
    }

    private static bool IsEvolutionistStory(RainWorldGame game)
    {
        return game != null && game.IsStorySession && game.StoryCharacter?.value == CharacterId;
    }

    private static void SaveStateSessionEnded(
        On.SaveState.orig_SessionEnded orig,
        SaveState self,
        RainWorldGame game,
        bool survived,
        bool newMalnourished)
    {
        if (IsEvolutionistStory(game) && TryGet(game, out EvolutionRunState state))
        {
            // 雨眠提交本循环进度，死亡则回滚到上一次成功雨眠。
            if (survived)
            {
                state.CommitCycle();
                self.deathPersistentSaveData.GetSlugBaseData().Set(SaveKey, state.Saved);
            }
            else
            {
                state.RollbackCycle();
            }
        }

        orig(self, game, survived, newMalnourished);
    }
}

