using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.CoolersOverWalls
{
	[HarmonyPatch(typeof(PlaceWorker_Cooler), "DrawGhost")]
	static class WideVentLocationGhost
	{
		public static IEnumerable<CodeInstruction> TranspileNorthWith(IEnumerable<CodeInstruction> instructions, OpCode paramCode)
		{
			MethodInfo NorthInfo = AccessTools.Property(typeof(IntVec3), "North").GetGetMethod();

			MethodInfo DoubleItInfo = AccessTools.Method(typeof(WideVentLocationGhost), nameof(WideVentLocationGhost.DoubleIt));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				//IL_0019: call         valuetype Verse.IntVec3 Verse.IntVec3::get_North()
				if (i.opcode == OpCodes.Call && i.operand == NorthInfo)
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
			ThingDef d = o is Thing t ? t.def : o as ThingDef;
			return d == OverWallDef.Cooler_Over2W ? v * 2 : v;
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
}
