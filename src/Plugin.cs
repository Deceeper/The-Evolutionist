using BepInEx;
using Evolutionist.Evolution;
using Evolutionist.Story;

namespace Evolutionist;

/// 注册模组并集中管理所有运行时钩子。
[BepInPlugin(ModId, ModName, ModVersion)]
[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string ModId = "decper.evolutionist";
    public const string ModName = "The Evolutionist";
    public const string ModVersion = "0.1.2";

    private bool initialized;

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnModsInit;
        StoryStateService.ApplyHooks();
        StorySpawnService.ApplyHooks();
        PebblesStoryService.ApplyHooks();
        MoonStoryService.ApplyHooks();
        StoryEndingService.ApplyHooks();
        EvolutionStateService.ApplyHooks();
        CorpseConsumptionService.ApplyHooks();
        AttributeApplicationService.ApplyHooks();
        StomachStorageService.ApplyHooks();
        DietService.ApplyHooks();
        BackSpearService.ApplyHooks();
        ExplosiveAbilityService.ApplyHooks();
        SpearGenerationService.ApplyHooks();
        TongueAndGraphicsService.ApplyHooks();
        PauseProgressService.ApplyHooks();
        ScavengerBehaviorService.ApplyHooks();
    }

    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorldOnModsInit;
        PauseProgressService.RemoveHooks();
        SpearGenerationService.RemoveHooks();
        TongueAndGraphicsService.RemoveHooks();
        ScavengerBehaviorService.RemoveHooks();
        ExplosiveAbilityService.RemoveHooks();
        BackSpearService.RemoveHooks();
        DietService.RemoveHooks();
        StomachStorageService.RemoveHooks();
        AttributeApplicationService.RemoveHooks();
        CorpseConsumptionService.RemoveHooks();
        EvolutionStateService.RemoveHooks();
        StoryEndingService.RemoveHooks();
        MoonStoryService.RemoveHooks();
        PebblesStoryService.RemoveHooks();
        StorySpawnService.RemoveHooks();
        StoryStateService.RemoveHooks();
    }

    private void RainWorldOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (initialized)
        {
            return;
        }

        CreatureEvolutionCatalog.Initialize();
        initialized = true;
        Logger.LogInfo($"{ModName} {ModVersion} initialized.");
    }
}

