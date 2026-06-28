using HarmonyLib;
using Verse;

namespace NoTerrainEdgeFade
{
    [StaticConstructorOnStartup]
    public static class NoTerrainEdgeFadeMod
    {
        static NoTerrainEdgeFadeMod()
        {
            var harmony = new Harmony("yourname.noterrainedgefade");
            harmony.PatchAll();
            Log.Message("[No Terrain Edge Fade] Harmony patches applied.");
        }
    }
}
