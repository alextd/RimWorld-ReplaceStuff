using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	[DefOf]
	public static class OverWallDef
	{
		public static bool IsWall(this BuildableDef bdef)
		{
			//return bdef == ThingDefOf.Wall || bdef.IsSmoothed;//Just IsSmoothed doesn't account for modded walls
			return bdef is ThingDef def && def.coversFloor && def.holdsRoof && def.passability == Traversability.Impassable &&
				(def.building?.canBuildNonEdificesUnder ?? true);
		}

		public static ThingDef Cooler_Over;
		public static ThingDef Cooler_Over2W;
		public static ThingDef Vent_Over;
		public static ThingDef Vent_Over2W;
		public static ThingDef Vent;//not in vanilla
		public static bool IsOverWall(this BuildableDef bdef)
		{
			return bdef == Cooler_Over || bdef == Cooler_Over2W || bdef == Vent_Over || bdef == Vent_Over2W;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class CoolerWallShare_Blocks
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (!__result) return;

			BuildableDef cDef = constructible.def.entityDefToBuild ?? constructible.def;
			BuildableDef tDef = t.def.entityDefToBuild ?? t.def;

			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if ((cDef.IsWall() && tDef.IsOverWall()) || 
					(tDef.IsWall() && cDef.IsOverWall()))
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
				if ((newDef.IsOverWall() && oldBuildDef.IsWall())	|| (newDef.IsWall() && oldBuildDef.IsOverWall()))
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
			if ((newBuiltDef.IsOverWall() && oldBuiltDef.IsWall())
				|| (newBuiltDef.IsWall() && oldBuiltDef.IsOverWall()))
			{
				__result = false;	
			}
		}
	}


	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	class CoolerWipesCooler
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (__result) return;
			
			else if (newEntDef is ThingDef newDef && newDef.thingClass == typeof(Building_Cooler) &&
				oldEntDef is ThingDef oldDef && oldDef.thingClass == typeof(Building_Cooler))
				__result = true;
		}
	}
}
