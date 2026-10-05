namespace Evolutionist.Evolution;

/// <summary>区分上次雨眠进度与当前雨循环的临时进度。</summary>
internal sealed class EvolutionRunState
{
    public EvolutionRunState(EvolutionProgress savedProgress)
    {
        Saved = savedProgress.Clone();
        Current = savedProgress.Clone();
    }

    public EvolutionProgress Saved { get; private set; }

    public EvolutionProgress Current { get; private set; }

    public void ApplyCorpseReward(CreatureEvolutionReward reward, float ordinaryRewardScale)
    {
        bool hadSpearGeneration = Current.IsUnlocked(AbilityId.SpearGeneration);

        foreach (var attribute in reward.Attributes)
        {
            Current.Add(attribute.Key, attribute.Value * ordinaryRewardScale);
        }

        foreach (var ability in reward.Abilities)
        {
            Current.Add(ability.Key, ability.Value);
        }

        // 首次解锁产矛的尸体不追溯计算拔矛速度收益。
        if (hadSpearGeneration && reward.SpearPullSpeed > 0f)
        {
            Current.Add(AttributeId.SpearPullSpeed, reward.SpearPullSpeed * ordinaryRewardScale);
        }
    }

    public void CommitCycle()
    {
        Current.Normalize();
        Saved = Current.Clone();
    }

    public void RollbackCycle()
    {
        Current = Saved.Clone();
    }
}

