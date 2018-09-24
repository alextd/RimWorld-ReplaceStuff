using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(Frame), "CompleteConstruction")]
	//public void CompleteConstruction(Pawn worker)
	public static class RememberWasNewThing
	{
		public static void Prefix(Frame __instance)
		{
			RefundDeconstruct.__STATIC_STUPID_WAS_NEW_THING = __instance.IsNewThingFrame(out Thing replacement);
		}
	}

	[HarmonyPatch(typeof(GenSpawn), "Refund")]
	//public static void Refund(Thing thing, Map map, CellRect avoidThisRect)
	public static class RefundDeconstruct
	{
		public static bool __STATIC_STUPID_WAS_NEW_THING = false;

		public static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DecideDestroyModeInfo = AccessTools.Method(typeof(RefundDeconstruct), nameof(RefundDeconstruct.DecideDestroyMode));

			foreach (CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Ldc_I4_6)  //DestroyMode.Refund
				{
					yield return new CodeInstruction(OpCodes.Call, DecideDestroyModeInfo);// DecideDestroyModeInfo(thing,map)
				}
				else
					yield return i;
			}
		}

		public static DestroyMode DecideDestroyMode()
		{
			if (__STATIC_STUPID_WAS_NEW_THING)
			{
				__STATIC_STUPID_WAS_NEW_THING = false;
				return DestroyMode.Deconstruct;
			}
			return DestroyMode.Refund;
		}
	}
}
