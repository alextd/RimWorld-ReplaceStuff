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

			MethodInfo WasAcceptedInfo = AccessTools.Property(typeof(AcceptanceReport), "WasAccepted").GetGetMethod();

			bool foundFogged = false;
			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (foundFogged)  //skip the brfalse after Fogged
				{
					//IL_0518: call valuetype Verse.AcceptanceReport Verse.AcceptanceReport::get_WasAccepted()
					//IL_051d: ret
					yield return new CodeInstruction(OpCodes.Call, WasAcceptedInfo);
					yield return new CodeInstruction(OpCodes.Ret);
					foundFogged = false;
				}
				if (i.opcode == OpCodes.Call && i.operand == FoggedInfo)
					foundFogged = true;
			}
		}
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
	}

	[HarmonyPatch(typeof(FogGrid), "UnfogWorker")]
	public static class UnFogFix
	{
		//private void UnfogWorker(IntVec3 c)
		public static void Postfix(FogGrid __instance, IntVec3 c)
		{
			Map map = (Map)AccessTools.Field(typeof(FogGrid), "map").GetValue(__instance);
			if (c.GetThingList(map).FirstOrDefault(t => t.def.IsBlueprint) is Thing blueprint)
			{
				AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(blueprint.def.entityDefToBuild, blueprint.Position, blueprint.Rotation, map, false, blueprint);
				Log.Message(report + report.Reason);
				if (!report.Accepted)
					blueprint.Destroy();
			}
		}
	}
}
