using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	//public override AcceptanceReport CanDesignateCell(IntVec3 c)
	[HarmonyPatch(typeof(Designator_Build), "CanDesignateCell")]
	static class Designator_Build_Stuff
	{
		public static ThingDef stuffDef;
		public static void Prefix(Designator_Build __instance)
		{
			stuffDef = (ThingDef)AccessTools.Field(typeof(Designator_Build), "stuffDef").GetValue(__instance);
		}
		public static void Postfix()
		{
			stuffDef = null;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	public static class NormalBuildReplace
	{
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
		public static void Postfix(ref AcceptanceReport __result, BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode, Thing thingToIgnore)
		{
			if (__result.Reason != "IdenticalThingExists".Translate() &&
				__result.Reason != "IdenticalBlueprintExists".Translate()) return;

			if (!entDef.MadeFromStuff) return;

			ThingDef newStuff = Designator_Build_Stuff.stuffDef;

			foreach (Thing thing in center.GetThingList(map))
				if (thing != thingToIgnore && thing.Position == center && thing.Rotation == rot &&
					GenConstruct.BuiltDefOf(thing.def) == entDef)
				{
					ThingDef oldStuff = thing is Blueprint bp ? bp.UIStuff() : thing.Stuff;
					if (thing is ReplaceFrame rf && oldStuff == newStuff)
					{
						__result = false;
						return;
					}
					if (newStuff != oldStuff)
						__result = true;
				}
		}
	}
}