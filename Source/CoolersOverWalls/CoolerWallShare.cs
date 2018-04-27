using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	[DefOf]
	public static class OverDefOf
	{
		public static ThingDef Cooler_Over;
		public static ThingDef Vent_Over;
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class CoolerWallShare_Blocks
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (!__result) return;

			ThingDef thingDef = constructible is Blueprint ? constructible.def
				: constructible is Frame ? constructible.def.entityDefToBuild.blueprintDef
				: constructible.def.blueprintDef;

			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if (thingDef.entityDefToBuild is ThingDef def
				&& (def == ThingDefOf.Wall && (t.def == OverDefOf.Cooler_Over || t.def == OverDefOf.Vent_Over)
				|| t.def == ThingDefOf.Wall && (def == OverDefOf.Cooler_Over || def == OverDefOf.Vent_Over)))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver")]
	class CoolerWallShare_Blueprint
	{
		//public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
		public static void Postfix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
		{
			if (__result) return;

			BuildableDef oldBuildDef = GenConstruct.BuiltDefOf(oldDef);
			if (oldDef.category == ThingCategory.Building || oldDef.IsBlueprint || oldDef.IsFrame)
			{
				//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
				if ((newDef == OverDefOf.Cooler_Over && oldBuildDef == ThingDefOf.Wall) || (newDef == OverDefOf.Vent_Over && oldBuildDef == ThingDefOf.Wall)
					|| (newDef == ThingDefOf.Wall && oldBuildDef == OverDefOf.Cooler_Over) || (newDef == ThingDefOf.Wall && oldBuildDef == OverDefOf.Vent_Over))
				{
					__result = true;
				}
			}
		}
	}


	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	class CoolerWallShare_Wipes
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (!__result) return;

			ThingDef newDef = newEntDef as ThingDef;
			ThingDef oldDef = oldEntDef as ThingDef;
			BuildableDef newBuiltDef = GenConstruct.BuiltDefOf(newDef);
			BuildableDef oldBuiltDef = GenConstruct.BuiltDefOf(oldDef);
			
			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if ((newBuiltDef == OverDefOf.Cooler_Over && oldBuiltDef == ThingDefOf.Wall) || (newBuiltDef == OverDefOf.Vent_Over && oldBuiltDef == ThingDefOf.Wall)
				|| (newBuiltDef == ThingDefOf.Wall && oldBuiltDef == OverDefOf.Cooler_Over) || (newBuiltDef == ThingDefOf.Wall && oldBuiltDef == OverDefOf.Vent_Over))
			{
				__result = false;	
			}
		}
	}
}
