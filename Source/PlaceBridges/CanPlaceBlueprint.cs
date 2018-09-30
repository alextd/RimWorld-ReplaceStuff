using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.PlaceBridges
{
	[HarmonyPatch(typeof(GenConstruct), "CanBuildOnTerrain")]
	class CanPlaceBlueprint
	{
		//public static bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null)
		public static bool Prefix(ref bool __result, BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null)
		{
			CellRect cellRect = GenAdj.OccupiedRect(c, rot, entDef.Size);
			cellRect.ClipInsideMap(map);
			CellRect.CellRectIterator iterator = cellRect.GetIterator();
			while (!iterator.Done())
			{
				if (iterator.Current.SupportsStructureType(map, TerrainDefOf.Bridge.terrainAffordanceNeeded) &&
					TerrainDefOf.Bridge.affordances.Contains(entDef.terrainAffordanceNeeded))
				{
					__result = true;
					return false;
				}
				iterator.MoveNext();
			}

			return true;
		}
	}

	//This should technically go inside Designator_Build.DesignateSingleCell, but this is easier.
	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForBuild")]
	class InterceptBlueprintPlaceBridgeFrame
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static void Prefix(ref Blueprint_Build __result, BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer || sourceDef == TerrainDefOf.Bridge) return;

			TerrainAffordanceDef affNeeded = sourceDef.terrainAffordanceNeeded;

			foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(center, rotation, sourceDef.Size))
			{
				if (cell.SupportsStructureType(map, affNeeded))
					continue;


				if (cell.SupportsStructureType(map, TerrainDefOf.Bridge.terrainAffordanceNeeded) && 
					TerrainDefOf.Bridge.affordances.Contains(affNeeded))
				{
					GenConstruct.PlaceBlueprintForBuild(TerrainDefOf.Bridge, cell, map, rotation, faction, null);
				}
			}
		}
	}

	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	public static class DontWipeBridgeBlueprints
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static bool Prefix(BuildableDef oldEntDef, bool __result)
		{
			if (oldEntDef as ThingDef == null)
				return true;

			if ((GenConstruct.BuiltDefOf(oldEntDef as ThingDef) ?? oldEntDef) == TerrainDefOf.Bridge)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
