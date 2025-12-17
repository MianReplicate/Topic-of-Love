using HarmonyLib;

namespace Topic_of_Love.Mian;

public class CustomStatsRows : StatsRowsContainer
{
    public void OnEnable()
    {
        
    }

    public void beginShow()
    {
        this.StartCoroutine(this.showRows());
    }
}

public static class grr
{
    
    // [HarmonyPref]
    [HarmonyPatch(typeof(StatsRowsContainer), nameof(StatsRowsContainer.getStatRow))]
    static void DoPatch()
    {
        
    }
}