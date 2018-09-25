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

		public static void Postfix()
		{
			__STATIC_STUPID_WAS_NEW_THING = false;
		}

		public static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
		{ 
			MethodInfo MinifiableInfo = AccessTools.Property(typeof(ThingDef), "Minifiable").GetGetMethod();

			MethodInfo DecideDestroyModeInfo = AccessTools.Method(typeof(RefundDeconstruct), nameof(RefundDeconstruct.DecideDestroyMode));
			MethodInfo NevermindAboutMinifiableInfo = AccessTools.Method(typeof(RefundDeconstruct), nameof(RefundDeconstruct.NevermindAboutMinifiable));

			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Ldc_I4_6)  //DestroyMode.Refund
					yield return new CodeInstruction(OpCodes.Call, DecideDestroyModeInfo);
				else
					yield return i;

				if (i.opcode == OpCodes.Callvirt && i.operand == MinifiableInfo)
					yield return new CodeInstruction(OpCodes.Call, NevermindAboutMinifiableInfo);
			}
		}

		public static DestroyMode DecideDestroyMode()
		{
			return __STATIC_STUPID_WAS_NEW_THING ? DestroyMode.Deconstruct : DestroyMode.Refund;
		}

		public static bool NevermindAboutMinifiable(bool minifiable)
		{
			return __STATIC_STUPID_WAS_NEW_THING ? false : minifiable;
		}
	}
}
