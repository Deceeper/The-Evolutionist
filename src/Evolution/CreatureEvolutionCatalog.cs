using System;
using System.Collections.Generic;
using MoreSlugcats;

namespace Evolutionist.Evolution;

// 登记每种可食用生物提供的属性与能力进度。
internal static class CreatureEvolutionCatalog
{
    private static Dictionary<CreatureTemplate.Type, CreatureEvolutionReward>? rewards;

    public static void Initialize()
    {
        if (rewards != null)
        {
            return;
        }

        rewards = new();

        // 蜥蜴类
        Add(CreatureTemplate.Type.GreenLizard, A(AttributeId.StunResistance, 15, AttributeId.SpearDamage, 5));
        Add(CreatureTemplate.Type.PinkLizard, A(AttributeId.PoleClimbSpeed, 10, AttributeId.RunSpeed, 5));
        Add(CreatureTemplate.Type.BlueLizard,
            A(AttributeId.PoleClimbSpeed, 15, AttributeId.CorridorClimbSpeed, 5),
            B(AbilityId.Tongue, 10));
        Add(CreatureTemplate.Type.WhiteLizard,
            A(AttributeId.PoleClimbSpeed, 10, AttributeId.StunResistance, 5),
            B(AbilityId.Tongue, 20));
        Add(CreatureTemplate.Type.BlackLizard, A(AttributeId.CorridorClimbSpeed, 15, AttributeId.StunResistance, 5));
        Add(CreatureTemplate.Type.YellowLizard, A(AttributeId.RunSpeed, 10, AttributeId.PoleClimbSpeed, 5));
        Add(CreatureTemplate.Type.Salamander, A(AttributeId.SwimSpeed, 15, AttributeId.LungCapacity, 10));
        Add(CreatureTemplate.Type.RedLizard,
            A(AttributeId.RunSpeed, 25, AttributeId.SpearDamage, 15, AttributeId.StunResistance, 10),
            B(AbilityId.Tongue, 40));
        Add(CreatureTemplate.Type.CyanLizard,
            A(AttributeId.PounceDistance, 25, AttributeId.JumpHeight, 15, AttributeId.BackflipHeight, 10),
            B(AbilityId.Tongue, 15, AbilityId.ExplosiveJump, 20));
        Add(DLCSharedEnums.CreatureTemplateType.SpitLizard,
            A(AttributeId.PounceDistance, 15, AttributeId.StunResistance, 10, AttributeId.SpearDamage, 5),
            B(AbilityId.ExplosiveJump, 25));
        Add(DLCSharedEnums.CreatureTemplateType.EelLizard,
            A(AttributeId.SwimSpeed, 25, AttributeId.LungCapacity, 15, AttributeId.CorridorClimbSpeed, 5));

        // 秃鹫与钢鸟
        Add(CreatureTemplate.Type.Vulture,
            A(AttributeId.JumpHeight, 15, AttributeId.LungCapacity, 10, AttributeId.BackflipHeight, 5));
        Add(CreatureTemplate.Type.KingVulture,
            A(AttributeId.SpearPerformance, 25, AttributeId.SpearDamage, 15, AttributeId.StunResistance, 10),
            B(AbilityId.SpearGeneration, 50, AbilityId.BackSpear, 25),
            spearPullSpeed: 25);
        Add(DLCSharedEnums.CreatureTemplateType.MirosVulture,
            A(AttributeId.JumpHeight, 25, AttributeId.PounceDistance, 15, AttributeId.LungCapacity, 10),
            B(AbilityId.SpearGeneration, 40),
            spearPullSpeed: 20);

        // 蜈蚣类
        Add(CreatureTemplate.Type.Centipede,
            A(AttributeId.StunResistance, 15, AttributeId.CorridorClimbSpeed, 10, AttributeId.RunSpeed, 5));
        Add(CreatureTemplate.Type.RedCentipede,
            A(AttributeId.StunResistance, 25, AttributeId.SpearDamage, 15, AttributeId.CorridorClimbSpeed, 10),
            B(AbilityId.ExplosiveJump, 30));
        Add(CreatureTemplate.Type.Centiwing,
            A(AttributeId.JumpHeight, 15, AttributeId.BackflipHeight, 10, AttributeId.PounceDistance, 5));
        Add(DLCSharedEnums.CreatureTemplateType.AquaCenti,
            A(AttributeId.SwimSpeed, 25, AttributeId.LungCapacity, 15, AttributeId.StunResistance, 10));

        // 蜘蛛、面条蝇与钢鸟
        Add(CreatureTemplate.Type.BigSpider,
            A(AttributeId.CorridorClimbSpeed, 15, AttributeId.PoleClimbSpeed, 10, AttributeId.RunSpeed, 5));
        Add(CreatureTemplate.Type.SpitterSpider,
            A(AttributeId.SpearPerformance, 15, AttributeId.CorridorClimbSpeed, 10, AttributeId.RunSpeed, 5),
            B(AbilityId.ExplosiveJump, 15, AbilityId.SpearGeneration, 20),
            spearPullSpeed: 10);
        Add(DLCSharedEnums.CreatureTemplateType.MotherSpider,
            A(AttributeId.PounceDistance, 15, AttributeId.JumpHeight, 10, AttributeId.CorridorClimbSpeed, 5));
        Add(CreatureTemplate.Type.BigNeedleWorm,
            A(AttributeId.SpearPerformance, 15, AttributeId.PounceDistance, 10, AttributeId.JumpHeight, 5),
            B(AbilityId.SpearGeneration, 30),
            spearPullSpeed: 15);
        Add(CreatureTemplate.Type.MirosBird,
            A(AttributeId.RunSpeed, 25, AttributeId.SlideSpeed, 15, AttributeId.StunResistance, 10));

        // 拾荒者
        Add(CreatureTemplate.Type.Scavenger,
            A(AttributeId.SpearPerformance, 15, AttributeId.SpearDamage, 10, AttributeId.RollDistance, 5),
            B(AbilityId.BackSpear, 20));
        Add(DLCSharedEnums.CreatureTemplateType.ScavengerElite,
            A(AttributeId.SpearPerformance, 25, AttributeId.SpearDamage, 15, AttributeId.RollDistance, 10),
            B(AbilityId.BackSpear, 40));
        Add(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing,
            A(AttributeId.SpearPerformance, 25, AttributeId.SpearDamage, 15, AttributeId.StunResistance, 10));

        // 其他可食用生物
        Add(CreatureTemplate.Type.LanternMouse, A(AttributeId.PoleClimbSpeed, 10, AttributeId.CorridorClimbSpeed, 5));
        Add(CreatureTemplate.Type.JetFish, A(AttributeId.SwimSpeed, 15, AttributeId.LungCapacity, 5));
        Add(CreatureTemplate.Type.TubeWorm,
            A(AttributeId.PoleClimbSpeed, 10, AttributeId.PounceDistance, 5),
            B(AbilityId.Tongue, 35));
        Add(DLCSharedEnums.CreatureTemplateType.Yeek,
            A(AttributeId.JumpHeight, 15, AttributeId.PounceDistance, 10, AttributeId.BackflipHeight, 5));
        Add(CreatureTemplate.Type.CicadaA, A(AttributeId.JumpHeight, 10, AttributeId.BackflipHeight, 5));
        Add(CreatureTemplate.Type.CicadaB, A(AttributeId.JumpHeight, 10, AttributeId.BackflipHeight, 5));
        Add(CreatureTemplate.Type.EggBug,
            A(AttributeId.RunSpeed, 10, AttributeId.JumpHeight, 5, AttributeId.BackflipHeight, 2));
        Add(MoreSlugcatsEnums.CreatureTemplateType.FireBug,
            A(AttributeId.RunSpeed, 15, AttributeId.JumpHeight, 10, AttributeId.PoleClimbSpeed, 5),
            B(AbilityId.ExplosiveJump, 50));
        Add(CreatureTemplate.Type.Snail, A(AttributeId.StunResistance, 15, AttributeId.PoleClimbSpeed, 5));
        Add(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC,
            A(AttributeId.JumpHeight, 10, AttributeId.RunSpeed, 5, AttributeId.LungCapacity, 2));

        Validate();
    }

    public static bool TryGet(CreatureTemplate.Type type, out CreatureEvolutionReward reward)
    {
        if (rewards == null)
        {
            throw new InvalidOperationException("Creature evolution catalog has not been initialized.");
        }

        return rewards.TryGetValue(type, out reward!);
    }

    private static void Add(
        CreatureTemplate.Type type,
        Dictionary<AttributeId, float>? attributes = null,
        Dictionary<AbilityId, float>? abilities = null,
        float spearPullSpeed = 0f)
    {
        if (rewards == null || type == null)
        {
            throw new InvalidOperationException("A creature evolution type is unavailable.");
        }

        rewards.Add(type, new CreatureEvolutionReward(attributes, abilities, spearPullSpeed));
    }

    private static Dictionary<AttributeId, float> A(params object[] values)
    {
        var result = new Dictionary<AttributeId, float>();
        for (int i = 0; i < values.Length; i += 2)
        {
            result.Add((AttributeId)values[i], Convert.ToSingle(values[i + 1]));
        }

        return result;
    }

    private static Dictionary<AbilityId, float> B(params object[] values)
    {
        var result = new Dictionary<AbilityId, float>();
        for (int i = 0; i < values.Length; i += 2)
        {
            result.Add((AbilityId)values[i], Convert.ToSingle(values[i + 1]));
        }

        return result;
    }

    private static void Validate()
    {
        if (rewards == null)
        {
            throw new InvalidOperationException("Creature evolution catalog is unavailable.");
        }

        foreach (KeyValuePair<CreatureTemplate.Type, CreatureEvolutionReward> entry in rewards)
        {
            ValidateValues(entry.Key.value, entry.Value.Attributes);
            ValidateValues(entry.Key.value, entry.Value.Abilities);
            if (entry.Value.SpearPullSpeed < 0f || entry.Value.SpearPullSpeed > EvolutionProgress.Maximum)
            {
                throw new InvalidOperationException($"Invalid spear pull speed reward for {entry.Key.value}.");
            }
        }
    }

    private static void ValidateValues<T>(string creature, IReadOnlyDictionary<T, float> values)
    {
        foreach (KeyValuePair<T, float> value in values)
        {
            if (value.Value <= 0f || value.Value > EvolutionProgress.Maximum)
            {
                throw new InvalidOperationException(
                    $"Invalid {typeof(T).Name} reward for {creature}: {value.Key}={value.Value}.");
            }
        }
    }
}

