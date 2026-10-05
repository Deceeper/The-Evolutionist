using System;
using System.Runtime.CompilerServices;
using Evolutionist.Evolution;
using SlugBase.SaveData;

namespace Evolutionist.Story;

/// <summary>读取并立即保存不会随死亡回滚的剧情节点。</summary>
internal static class StoryStateService
{
    private const string SaveKey = Plugin.ModId + ".story.v1";

    private static readonly ConditionalWeakTable<RainWorldGame, StoryProgress> States =
        new();

    public static void ApplyHooks()
    {
        On.SaveState.SessionEnded += SaveStateSessionEnded;
    }

    public static void RemoveHooks()
    {
        On.SaveState.SessionEnded -= SaveStateSessionEnded;
    }

    public static bool TryGet(Player player, out StoryProgress progress)
    {
        progress = null!;
        if (!EvolutionStateService.IsEvolutionist(player))
        {
            return false;
        }

        RainWorldGame? game = player.abstractCreature?.world?.game;
        return game != null && TryGet(game, out progress);
    }

    public static bool TryGet(RainWorldGame game, out StoryProgress progress)
    {
        progress = null!;
        if (!IsEvolutionistStory(game))
        {
            return false;
        }

        progress = States.GetValue(game, CreateState);
        return true;
    }

    public static bool UpdateImmediately(RainWorldGame game, Func<StoryProgress, bool> update)
    {
        if (update == null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        if (!TryGet(game, out StoryProgress progress))
        {
            return false;
        }

        bool changed = update(progress);
        progress.Normalize();
        // 剧情节点立即写盘，避免玩家在同一循环死亡后重复触发。
        return !changed || SaveImmediately(game, progress);
    }

    public static bool SaveImmediately(RainWorldGame game)
    {
        return TryGet(game, out StoryProgress progress) && SaveImmediately(game, progress);
    }

    private static StoryProgress CreateState(RainWorldGame game)
    {
        var progress = new StoryProgress();
        DeathPersistentSaveData persistent = game.GetStorySession.saveState.deathPersistentSaveData;

        if (persistent.GetSlugBaseData().TryGet(SaveKey, out StoryProgress loaded) && loaded != null)
        {
            loaded.Normalize();
            progress = loaded;
        }

        return progress;
    }

    private static bool SaveImmediately(RainWorldGame game, StoryProgress progress)
    {
        if (!IsEvolutionistStory(game))
        {
            return false;
        }

        progress.Normalize();
        DeathPersistentSaveData persistent = game.GetStorySession.saveState.deathPersistentSaveData;
        persistent.GetSlugBaseData().Set(SaveKey, progress.Clone());

        PlayerProgression? progression = game.rainWorld?.progression;
        return progression != null &&
               progression.SaveProgressionAndDeathPersistentDataOfCurrentState(false, false);
    }

    private static bool IsEvolutionistStory(RainWorldGame game)
    {
        return game != null && game.IsStorySession && game.StoryCharacter?.value == EvolutionStateService.CharacterId;
    }

    private static void SaveStateSessionEnded(
        On.SaveState.orig_SessionEnded orig,
        SaveState self,
        RainWorldGame game,
        bool survived,
        bool newMalnourished)
    {
        if (IsEvolutionistStory(game) && TryGet(game, out StoryProgress progress))
        {
            progress.Normalize();
            self.deathPersistentSaveData.GetSlugBaseData().Set(SaveKey, progress.Clone());
        }

        orig(self, game, survived, newMalnourished);
    }
}
