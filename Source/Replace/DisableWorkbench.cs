using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(Building_WorkTable))]
	[HarmonyPatch("UsableNow", PropertyMethod.Getter)]
	class DisableWorkbench
	{
		//public virtual bool UsableNow
		public static void Postfix(ref bool __result, Building_WorkTable __instance)
		{
			if (__instance.Position.GetThingList(__instance.Map).FirstOrDefault(t => t is ReplaceFrame) is ReplaceFrame frame
				&& frame.workDone > 0)
				__result = false;
		}
	}
}
