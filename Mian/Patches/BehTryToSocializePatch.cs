using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ai;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(BehTryToSocialize))]
public class BehTryToSocializePatch
{

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(BehTryToSocialize.execute))]
    static IEnumerable<CodeInstruction> SocializePatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);

        try
        {
            codeMatcher = codeMatcher
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(Actor), nameof(Actor.canFallInLoveWith), new[] { typeof(Actor) })))
                .ThrowIfInvalid("Could not find canFallInLoveWith call!")
                .Advance(-2) // go back to first instruction
                .RemoveInstructions(7); // remove the whole thingy ma jig
            codeMatcher.Instruction.labels.Clear(); // remove the label here since we removed the branch
        }
        catch (InvalidOperationException e)
        {
            TolUtil.LogInfo("Failed to remove canFallInLoveWith call! Perhaps another mod already removed it\n"+e.Message);
        }
        
        codeMatcher = codeMatcher.Start().MatchForward(false, new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Actor), nameof(Actor.beh_actor_target))))
            .ThrowIfInvalid("Could not find beh_actor_target setter! Did a mod remove this..")
            .Advance(1); // moves forward
        
        // we are inserting our code after the resetSocialize methods

        var labelElse = generator.DefineLabel();
        var labelContinue = generator.DefineLabel();
        
        codeMatcher = codeMatcher.InsertAndAdvance(
            // if (TolUtil.IsOrientationSystemEnabledFor(pActor))
            new CodeInstruction(OpCodes.Ldarg_1), // loads actor from argument 1
            CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.IsOrientationSystemEnabledFor)), // calls TolUtil.IsOrientationSystemEnabledFor,
            new CodeInstruction(OpCodes.Brfalse_S, labelElse), // go to else block
            
            // if(TolUtil.SocializedLoveCheck(__instance, pActor, randomActorAround))
            new CodeInstruction(OpCodes.Ldarg_0), // loads instance from argument 0
            new CodeInstruction(OpCodes.Ldarg_1), // loads actor from argument 1
            new CodeInstruction(OpCodes.Ldloc_0), // gets stored randomActorAround
            new CodeInstruction(OpCodes.Ldc_I4_0), // pushes 0 integer which is just false in this case (BINARY WHOA)
            CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.SocializedLoveCheck)), // calls SocializedLoveCheck
            
            new CodeInstruction(OpCodes.Brfalse_S, labelContinue), // skips the rest of the code and goes back onto the original code if SocializedLoveCheck is false
            
            // if true..
            // return BehResult.Continue;
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ret),
            
            // continue
            new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labelElse), // load actor (and is the destination)
            new CodeInstruction(OpCodes.Ldloc_0), // load randomActorAround
            CodeInstruction.Call(typeof(ActorTool), nameof(ActorTool.checkFallInLove)) // calls checkFallInLove
        );
        codeMatcher = codeMatcher.AddLabels(List.Of(labelContinue)); // destination
        
        return codeMatcher.InstructionEnumeration();
    }
}