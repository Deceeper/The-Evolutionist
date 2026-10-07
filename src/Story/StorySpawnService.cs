using Evolutionist.Evolution;

namespace Evolutionist.Story;

// 将剧情模式初始位置调整到垃圾堆安全出生点。
internal static class StorySpawnService
{
    private const string StartRoom = "GW_C10";
    private const int StartTileX = 145;
    private const int StartTileY = 11;

    public static void ApplyHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += SpawnPlayers;
    }

    public static void RemoveHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate -= SpawnPlayers;
    }

    private static AbstractCreature SpawnPlayers(
        On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig,
        RainWorldGame self,
        bool player1,
        bool player2,
        bool player3,
        bool player4,
        WorldCoordinate location)
    {
        if (self.IsStorySession &&
            self.StoryCharacter?.value == EvolutionStateService.CharacterId &&
            self.world.GetAbstractRoom(location.room)?.name == StartRoom)
        {
            location = new WorldCoordinate(location.room, StartTileX, StartTileY, -1);
        }

        return orig(self, player1, player2, player3, player4, location);
    }
}
