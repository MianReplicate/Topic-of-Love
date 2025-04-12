using HarmonyLib;

namespace Better_Loving.Mian.Patches;

public class CultureManagerPatch
{
    [HarmonyPatch(typeof(CultureManager), nameof(CultureManager.newCulture))]
    class OnCultureCreatePatch
    {
        static void Postfix(Actor pActor, ref Culture __result)
        {
            if (Randy.randomBool())
            {
                if (Util.NeedDifferentSexTypeForReproduction(pActor))
                    __result.addTrait("homophobic");
                if (Util.NeedSameSexTypeForReproduction(pActor))
                    __result.addTrait("heterophobic");
                if (!Util.NeedDifferentSexTypeForReproduction(pActor) && !Util.NeedSameSexTypeForReproduction(pActor) && !Util.CanDoAnySexType(pActor))
                    __result.addTrait("orientationless");
            } else if (Randy.randomBool())
            {
                var preference = QueerTraits.GetPreferenceFromActor(pActor, false);
                if(preference == Preference.DifferentSex)
                    __result.addTrait("homophobic");
                if(preference == Preference.SameSex)
                    __result.addTrait("heterophobic");
            }
        }
    }
}