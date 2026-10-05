using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Evolutionist.Evolution;

/// <summary>以 IL 补丁扩展工匠爆炸能力的入口判定。</summary>
internal static class ExplosiveAbilityService
{
    public static void ApplyHooks()
    {
        IL.Player.ClassMechanicsArtificer += PlayerClassMechanicsArtificerIL;
    }

    public static void RemoveHooks()
    {
        IL.Player.ClassMechanicsArtificer -= PlayerClassMechanicsArtificerIL;
    }

    private static void PlayerClassMechanicsArtificerIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // 仅扩展能力入口；前一处工匠判断会更新其剧情状态，必须保持原角色专用。
        if (!cursor.TryGotoNext(
                MoveType.Before,
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdfld<Player>(nameof(Player.SlugCatClass)),
                IsArtificerFieldLoad,
                IsExtEnumEqualityCall,
                instruction => instruction.OpCode == OpCodes.Brtrue ||
                    instruction.OpCode == OpCodes.Brtrue_S))
        {
            throw new InvalidOperationException(
                "Could not locate the Artificer ability-entry check.");
        }

        cursor.Index += 4;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<bool, Player, bool>>(CanUseExplosiveAbility);
    }

    private static bool CanUseExplosiveAbility(bool isArtificer, Player player)
    {
        if (isArtificer)
        {
            return true;
        }

        return EvolutionStateService.TryGet(player, out EvolutionRunState state) &&
            state.Current.IsUnlocked(AbilityId.ExplosiveJump);
    }

    private static bool IsArtificerFieldLoad(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Ldsfld &&
            instruction.Operand is FieldReference field &&
            field.Name == "Artificer" &&
            field.DeclaringType.FullName.EndsWith(
                "MoreSlugcatsEnums/SlugcatStatsName",
                StringComparison.Ordinal);
    }

    private static bool IsExtEnumEqualityCall(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Call &&
            instruction.Operand is MethodReference method &&
            method.Name == "op_Equality" &&
            method.DeclaringType.FullName.StartsWith(
                "ExtEnum`1",
                StringComparison.Ordinal);
    }
}
