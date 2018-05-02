using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	public static class BlueprintOverFogged
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo FoggedInfo = AccessTools.Method(typeof(GridsUtility), "Fogged");

			foreach(CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Call && i.operand == FoggedInfo)
				{
					yield return new CodeInstruction(OpCodes.Pop);//center
					yield return new CodeInstruction(OpCodes.Pop);//map
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);//false
				}
				else
					yield return i;
			}
		}
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
	}
}
