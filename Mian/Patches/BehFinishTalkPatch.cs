using System.Collections.Generic;
using System.Reflection.Emit;
using ai;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(BehFinishTalk))]
public class BehFinishTalkPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(BehFinishTalk.finishTalk))]
    static IEnumerable<CodeInstruction> BehFinishTalkTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Call,
            AccessTools.Method(typeof(ActorTool), nameof(ActorTool.checkFallInLove), new[]{typeof(Actor), typeof(Actor)})))
            .ThrowIfInvalid("Could not find checkFallInLove.. damn game update :(")
            .Advance(-3);

        var goPastLove = (Label) codeMatcher.Instruction.operand;

        codeMatcher.Advance(1);
        var higherChanceBranch = generator.DefineLabel();
        var fallInLoveBranch = generator.DefineLabel();
        
        codeMatcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Actor), nameof(Actor.getBestFriend))),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ceq),
            new CodeInstruction(OpCodes.Brtrue, higherChanceBranch),
            new CodeInstruction(OpCodes.Ldc_R4, 0.25f),
            new CodeInstruction(OpCodes.Br, fallInLoveBranch),
            new CodeInstruction(OpCodes.Ldc_R4, 0.75f).WithLabels(higherChanceBranch),
            
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Randy), nameof(Randy.randomChance)))
                .WithLabels(fallInLoveBranch),
            new CodeInstruction(OpCodes.Brfalse, goPastLove)
        );
        
        return codeMatcher.InstructionEnumeration();
    }
    
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)] // dugga doo dugga doo dugga dugag dugga doo
    [HarmonyPatch(nameof(BehFinishTalk.finishTalk))]
    static void FinishTalkPatch(Actor pActor, Actor pTarget)
    {
        TolUtil.ChangeIntimacyHappinessBy(pActor, 25);
        TolUtil.ChangeIntimacyHappinessBy(pTarget, 25);
    }
}