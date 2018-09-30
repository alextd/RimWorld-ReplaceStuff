using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	public static class NormalBuildReplace
	{
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
		public static void Postfix(ref AcceptanceReport __result, BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode, Thing thingToIgnore)
		{
			if (__result.Reason != "IdenticalThingExists".Translate() &&
				__result.Reason != "IdenticalBlueprintExists".Translate()) return;

			if (!entDef.MadeFromStuff) return;

			//Would love to check stuff here
			foreach (Thing thing in center.GetThingList(map))
				if (thing != thingToIgnore && thing.Position == center && thing.Rotation == rot &&
					GenConstruct.BuiltDefOf(thing.def) == entDef)
				{
					__result = true;
					return;
				}
		}
	}
}