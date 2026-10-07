using System;
using System.Collections.Generic;

namespace Evolutionist.Evolution;

// 可序列化的属性与特殊能力进度。
internal sealed class EvolutionProgress
{
    public const float Maximum = 100f;

    public int DataVersion { get; set; } = 1;

    public Dictionary<string, float> Attributes { get; set; } = new();

    public Dictionary<string, float> Abilities { get; set; } = new();

    public float Get(AttributeId attribute)
    {
        return GetValue(Attributes, attribute.ToString());
    }

    public float Get(AbilityId ability)
    {
        return GetValue(Abilities, ability.ToString());
    }

    public bool IsUnlocked(AbilityId ability)
    {
        return Get(ability) >= Maximum;
    }

    public float Add(AttributeId attribute, float amount)
    {
        return AddValue(Attributes, attribute.ToString(), amount);
    }

    public float Add(AbilityId ability, float amount)
    {
        return AddValue(Abilities, ability.ToString(), amount);
    }

    public EvolutionProgress Clone()
    {
        return new EvolutionProgress
        {
            DataVersion = DataVersion,
            Attributes = new Dictionary<string, float>(Attributes),
            Abilities = new Dictionary<string, float>(Abilities)
        };
    }

    public void Normalize()
    {
        NormalizeValues(Attributes);
        NormalizeValues(Abilities);
    }

    private static float GetValue(Dictionary<string, float> values, string key)
    {
        return values.TryGetValue(key, out float value) ? ClampAndRound(value) : 0f;
    }

    private static float AddValue(Dictionary<string, float> values, string key, float amount)
    {
        float oldValue = GetValue(values, key);
        float newValue = ClampAndRound(oldValue + Math.Max(0f, amount));
        values[key] = newValue;
        return newValue - oldValue;
    }

    private static void NormalizeValues(Dictionary<string, float> values)
    {
        var keys = new List<string>(values.Keys);
        foreach (string key in keys)
        {
            values[key] = ClampAndRound(values[key]);
        }
    }

    private static float ClampAndRound(float value)
    {
        float clamped = Math.Max(0f, Math.Min(Maximum, value));
        return (float)Math.Round(clamped, 1, MidpointRounding.AwayFromZero);
    }
}

