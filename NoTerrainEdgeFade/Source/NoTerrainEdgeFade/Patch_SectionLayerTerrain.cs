using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace NoTerrainEdgeFade
{
    // Verse.SectionLayer_Terrain.Regenerate() builds a soft alpha-blended "fade" mesh
    // over each cell for every neighboring cell whose terrain has:
    //   - a non-default TerrainDef.edgeType (Fade / FadeRough / Water), AND
    //   - a TerrainDef.renderPrecedence >= this cell's terrain's renderPrecedence, AND
    //   - neither cell has a FoundationAt() override.
    //
    // Man-made floors (TerrainDef.edgeType == TerrainEdgeType.Hard, the default value)
    // are supposed to render with crisp edges, but many floor defs never set
    // renderPrecedence (defaults to 0), so any terrain with a higher renderPrecedence -
    // most notably water (renderPrecedence ~394-399) - still paints its fade mesh on
    // top of them. This patch makes the fade unconditionally skip whenever the
    // *current* cell's terrain is a Hard-edged floor, regardless of renderPrecedence,
    // while leaving natural Fade/FadeRough terrain blending (soil, sand, gravel, etc.)
    // completely untouched.
    [HarmonyPatch(typeof(SectionLayer_Terrain), "Regenerate")]
    public static class SectionLayer_Terrain_Regenerate_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            FieldInfo cellTerrainDefField = AccessTools.Field(typeof(CellTerrain), "def");
            FieldInfo renderPrecedenceField = AccessTools.Field(typeof(TerrainDef), "renderPrecedence");
            FieldInfo edgeTypeField = AccessTools.Field(typeof(TerrainDef), "edgeType");

            if (cellTerrainDefField == null || renderPrecedenceField == null || edgeTypeField == null)
            {
                Log.Error("[No Terrain Edge Fade] Could not find expected fields via reflection; skipping patch.");
                return codes;
            }

            bool patched = false;

            for (int i = 0; i < codes.Count - 5; i++)
            {
                // Looking for:
                //   ldloc.s   neighborTerrainLocal
                //   ldfld     CellTerrain.def
                //   ldfld     TerrainDef.renderPrecedence
                //   ldloc.s   thisTerrainLocal
                //   ldfld     CellTerrain.def
                //   ldfld     TerrainDef.renderPrecedence
                //   blt.s     skipLabel
                if (IsLdlocAny(codes[i]) &&
                    codes[i + 1].opcode == OpCodes.Ldfld && Equals(codes[i + 1].operand, cellTerrainDefField) &&
                    codes[i + 2].opcode == OpCodes.Ldfld && Equals(codes[i + 2].operand, renderPrecedenceField) &&
                    IsLdlocAny(codes[i + 3]) &&
                    codes[i + 4].opcode == OpCodes.Ldfld && Equals(codes[i + 4].operand, cellTerrainDefField) &&
                    codes[i + 5].opcode == OpCodes.Ldfld && Equals(codes[i + 5].operand, renderPrecedenceField))
                {
                    int branchIdx = i + 6;
                    if (branchIdx >= codes.Count) continue;
                    var branchInstr = codes[branchIdx];
                    if (branchInstr.opcode != OpCodes.Blt && branchInstr.opcode != OpCodes.Blt_S) continue;

                    var skipLabel = (Label)branchInstr.operand;

                    // thisTerrainLocal is whatever local the THIRD ldloc in the
                    // pattern (codes[i+3]) loads - clone it so we can re-load it.
                    var thisTerrainLoad = codes[i + 3].Clone();

                    var extraCheck = new List<CodeInstruction>
                    {
                        thisTerrainLoad,
                        new CodeInstruction(OpCodes.Ldfld, cellTerrainDefField),
                        new CodeInstruction(OpCodes.Ldfld, edgeTypeField),
                        // TerrainEdgeType.Hard == 0
                        new CodeInstruction(OpCodes.Brfalse, skipLabel),
                    };

                    codes.InsertRange(i, extraCheck);
                    patched = true;
                    break;
                }
            }

            if (!patched)
            {
                Log.Error("[No Terrain Edge Fade] Failed to locate target IL pattern in " +
                    "SectionLayer_Terrain.Regenerate - the method may have changed in this game version. " +
                    "No changes were applied; terrain fading will behave as in vanilla.");
            }

            return codes;
        }

        private static bool IsLdlocAny(CodeInstruction instr)
        {
            return instr.opcode == OpCodes.Ldloc ||
                   instr.opcode == OpCodes.Ldloc_S ||
                   instr.opcode == OpCodes.Ldloc_0 ||
                   instr.opcode == OpCodes.Ldloc_1 ||
                   instr.opcode == OpCodes.Ldloc_2 ||
                   instr.opcode == OpCodes.Ldloc_3;
        }
    }
}
