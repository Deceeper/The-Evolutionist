using System.Collections.Generic;

namespace Evolutionist.Evolution;

// 一具完整尸体对应的进化收益。
internal sealed class CreatureEvolutionReward
{
    public CreatureEvolutionReward(
        Dictionary<AttributeId, float>? attributes = null,
        Dictionary<AbilityId, float>? abilities = null,
        float spearPullSpeed = 0f)
    {
        Attributes = attributes ?? new();
        Abilities = abilities ?? new();
        SpearPullSpeed = spearPullSpeed;
    }

    public IReadOnlyDictionary<AttributeId, float> Attributes { get; }

    public IReadOnlyDictionary<AbilityId, float> Abilities { get; }

    public float SpearPullSpeed { get; }
}

