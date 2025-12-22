using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.CoolersOverWalls
{
	[HarmonyPatch]
	static class WideVentLocationGhost
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(PlaceWorker_Cooler), nameof(PlaceWorker_Cooler.DrawGhost));
			yield return AccessTools.Method(typeof(PlaceWorker_Vent), nameof(PlaceWorker_Vent.DrawGhost));
		}

		public static IEnumerable<CodeInstruction> TranspileNorthWith(IEnumerable<CodeInstruction> instructions, OpCode paramCode)
		{
			FieldInfo NorthInfo = AccessTools.Field(typeof(IntVec3), nameof(IntVec3.North));

			MethodInfo DoubleItInfo = AccessTools.Method(typeof(WideVentLocationGhost), nameof(WideVentLocationGhost.DoubleIt));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				//IL_0019: call         valuetype Verse.IntVec3 Verse.IntVec3::get_North()
				if (i.LoadsField(NorthInfo))
				{
					yield return new CodeInstruction(paramCode);//def or thing
					yield return new CodeInstruction(OpCodes.Call, DoubleItInfo);
				}
			}
		}
		
		//public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return TranspileNorthWith(instructions, OpCodes.Ldarg_1);
		}

		public static IntVec3 DoubleIt(IntVec3 v, object o)
		{
			ThingDef thingDef = o as ThingDef ?? (o as Thing)?.def;


			return 
				thingDef == OverWallDef.Cooler_Over2W || 
				thingDef.entityDefToBuild == OverWallDef.Cooler_Over2W ||
				thingDef == OverWallDef.Vent_Over2W ||
				thingDef.entityDefToBuild == OverWallDef.Vent_Over2W
					? v * 2 : v;
		}
	}

	[HarmonyPatch(typeof(Building_Cooler), "TickRare")]
	static class WideVentLocationTemp
	{
		//public override void TickRare()
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return WideVentLocationGhost.TranspileNorthWith(instructions, OpCodes.Ldarg_0);
		}
	}

	[HarmonyPatch(typeof(Designator_Dropdown), MethodType.Constructor)]
	static class DropdownInOrder
	{
		public static void Postfix(Designator_Dropdown __instance)
		{
			__instance.Order = 20f;
		}
	}
}
