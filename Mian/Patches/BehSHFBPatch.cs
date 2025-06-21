using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ai.behaviours;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(BehSpawnHeartsFromBuilding))]
public class BehSHFBPatch
{

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BehSpawnHeartsFromBuilding.execute))]
    static bool ExecutePatch(Actor pActor, ref BehResult __result)
    {
        if (!pActor.hasLover() && pActor.beh_actor_target == null) // who tf are you having sex with
        {
            __result = BehResult.Stop;
            return false;
        }

        return true;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(BehSpawnHeartsFromBuilding.execute))]
    static IEnumerable<CodeInstruction> AllowForTargets(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);
        
        codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt,
            AccessTools.Method(typeof(Actor), nameof(Actor.hasLover)))).Advance(1);
        var operandLabel = (Label)codeMatcher.Operand;
        codeMatcher.RemoveInstructionsInRange(codeMatcher.Pos - 2, codeMatcher.Pos);
        
        try
        {
            var afterglowPos = codeMatcher.Start().MatchForward(false, new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(Actor), nameof(Actor.addAfterglowStatus))))
                .Pos;
            codeMatcher.RemoveInstructionsInRange(afterglowPos - 1, afterglowPos + 4);
        }catch (InvalidOperationException e)
        {
            TolUtil.LogInfo("Did someone remove addAfterGlowStatus..? It's not here :(\n"+e.Message);
        }

        var listOfInstructions = codeMatcher.Instructions();
        for(var i = 0; i < listOfInstructions.Count; i++)
        {
            if (listOfInstructions[i].labels.Contains(operandLabel))
            {
                codeMatcher.RemoveInstructionsInRange(i, i + 1);
                break;
            }
        }

        return codeMatcher.InstructionEnumeration();
    }
}