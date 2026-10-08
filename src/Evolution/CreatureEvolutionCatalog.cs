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

    public static IReadOnlyList<CreatureTemplate.Type> GetContributors(AttributeId attribute)
    {
        return GetContributors(reward =>
            attribute == AttributeId.SpearPullSpeed
                ? reward.SpearPullSpeed > 0f
                : reward.Attributes.ContainsKey(attribute));
    }

    public static IReadOnlyList<CreatureTemplate.Type> GetContributors(AbilityId ability)
    {
        return GetContributors(reward => reward.Abilities.ContainsKey(ability));
    }

    public static string GetDisplayName(CreatureTemplate.Type type, bool useChinese)
    {
        string chinese;
        string english;

        if (type == CreatureTemplate.Type.GreenLizard) (chinese, english) = ("绿蜥蜴", "Green Lizard");
        else if (type == CreatureTemplate.Type.PinkLizard) (chinese, english) = ("粉蜥蜴", "Pink Lizard");
        else if (type == CreatureTemplate.Type.BlueLizard) (chinese, english) = ("蓝蜥蜴", "Blue Lizard");
        else if (type == CreatureTemplate.Type.WhiteLizard) (chinese, english) = ("白蜥蜴", "White Lizard");
        else if (type == CreatureTemplate.Type.BlackLizard) (chinese, english) = ("黑蜥蜴", "Black Lizard");
        else if (type == CreatureTemplate.Type.YellowLizard) (chinese, english) = ("黄蜥蜴", "Yellow Lizard");
        else if (type == CreatureTemplate.Type.Salamander) (chinese, english) = ("蝾螈", "Salamander");
        else if (type == CreatureTemplate.Type.RedLizard) (chinese, english) = ("红蜥蜴", "Red Lizard");
        else if (type == CreatureTemplate.Type.CyanLizard) (chinese, english) = ("青蜥蜴", "Cyan Lizard");
        else if (type == DLCSharedEnums.CreatureTemplateType.SpitLizard) (chinese, english) = ("烈焰蜥蜴", "Caramel Lizard");
        else if (type == DLCSharedEnums.CreatureTemplateType.EelLizard) (chinese, english) = ("鳗蜥蜴", "Eel Lizard");
        else if (type == CreatureTemplate.Type.Vulture) (chinese, english) = ("秃鹫", "Vulture");
        else if (type == CreatureTemplate.Type.KingVulture) (chinese, english) = ("魔王秃鹫", "King Vulture");
        else if (type == DLCSharedEnums.CreatureTemplateType.MirosVulture) (chinese, english) = ("钢铁秃鹫", "Miros Vulture");
        else if (type == CreatureTemplate.Type.Centipede) (chinese, english) = ("蜈蚣", "Centipede");
        else if (type == CreatureTemplate.Type.RedCentipede) (chinese, english) = ("红蜈蚣", "Red Centipede");
        else if (type == CreatureTemplate.Type.Centiwing) (chinese, english) = ("飞蜈蚣", "Centiwing");
        else if (type == DLCSharedEnums.CreatureTemplateType.AquaCenti) (chinese, english) = ("水蜈蚣", "Aquapede");
        else if (type == CreatureTemplate.Type.BigSpider) (chinese, english) = ("狼蛛", "Wolf Spider");
        else if (type == CreatureTemplate.Type.SpitterSpider) (chinese, english) = ("喷吐蛛", "Spitter Spider");
        else if (type == DLCSharedEnums.CreatureTemplateType.MotherSpider) (chinese, english) = ("母蛛", "Mother Spider");
        else if (type == CreatureTemplate.Type.BigNeedleWorm) (chinese, english) = ("成年面条蝇", "Adult Noodlefly");
        else if (type == CreatureTemplate.Type.MirosBird) (chinese, english) = ("钢鸟", "Miros Bird");
        else if (type == CreatureTemplate.Type.Scavenger) (chinese, english) = ("拾荒者", "Scavenger");
        else if (type == DLCSharedEnums.CreatureTemplateType.ScavengerElite) (chinese, english) = ("精英拾荒者", "Elite Scavenger");
        else if (type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing) (chinese, english) = ("拾荒酋长", "Chieftain Scavenger");
        else if (type == CreatureTemplate.Type.LanternMouse) (chinese, english) = ("灯鼠", "Lantern Mouse");
        else if (type == CreatureTemplate.Type.JetFish) (chinese, english) = ("鲑鱼", "Jetfish");
        else if (type == CreatureTemplate.Type.TubeWorm) (chinese, english) = ("管虫", "Grappling Worm");
        else if (type == DLCSharedEnums.CreatureTemplateType.Yeek) (chinese, english) = ("跃客", "Yeek");
        else if (type == CreatureTemplate.Type.CicadaA || type == CreatureTemplate.Type.CicadaB) (chinese, english) = ("蝉乌贼", "Squidcada");
        else if (type == CreatureTemplate.Type.EggBug) (chinese, english) = ("蛋虫", "Eggbug");
        else if (type == MoreSlugcatsEnums.CreatureTemplateType.FireBug) (chinese, english) = ("火虫", "Firebug");
        else if (type == CreatureTemplate.Type.Snail) (chinese, english) = ("爆炸蜗牛", "Snail");
        else if (type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC) (chinese, english) = ("蛞蝓猫幼崽", "Slugpup");
        else (chinese, english) = (type.value, type.value);

        return useChinese ? chinese : english;
    }

    private static IReadOnlyList<CreatureTemplate.Type> GetContributors(
        Func<CreatureEvolutionReward, bool> includesReward)
    {
        if (rewards == null)
        {
            throw new InvalidOperationException("Creature evolution catalog has not been initialized.");
        }

        var contributors = new List<CreatureTemplate.Type>();
        foreach (KeyValuePair<CreatureTemplate.Type, CreatureEvolutionReward> entry in rewards)
        {
            if (includesReward(entry.Value))
            {
                contributors.Add(entry.Key);
            }
        }

        return contributors;
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
