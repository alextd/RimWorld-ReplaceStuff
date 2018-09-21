using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;

namespace Replace_Stuff.Replace
{
	static class DisableThing
	{
		public static bool IsReplacing(Thing thing)
		{
			return thing.Spawned &&
				thing.Position.GetThingList(thing.Map)
				.Any(t => t is ReplaceFrame f && f.workDone > 0);
		}
	}

	[HarmonyPatch(typeof(Building_WorkTable), "UsableForBillsAfterFueling")]
	class DisableWorkbench
	{
		//public virtual bool UsableNow
		public static void Postfix(ref bool __result, Building_WorkTable __instance)
		{
			if (DisableThing.IsReplacing(__instance))	__result = false;
		}
	}
}
