using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.PlaceBridges
{
	public static class PlaceBridges
	{
		public static bool NeedsBridge(BuildableDef def, IntVec3 pos, Map map, ThingDef stuff)
		{
			TerrainAffordanceDef needed = def.GetTerrainAffordanceNeed(stuff);
			return pos.InBounds(map) &&
				!pos.SupportsStructureType(map, needed) &&
				pos.SupportsStructureType(map, TerrainDefOf.Bridge.terrainAffordanceNeeded) &&
				TerrainDefOf.Bridge.affordances.Contains(needed);
		}

		public static bool CantEvenBridge(IntVec3 pos, Map map) =>
			!pos.SupportsStructureType(map, TerrainDefOf.Bridge.terrainAffordanceNeeded);
	}

	[HarmonyPatch(typeof(GenConstruct), "CanBuildOnTerrain")]
	class CanPlaceBlueprint
	{
		//public static bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ContainsInfo = AccessTools.Method(typeof(List<TerrainAffordanceDef>), nameof(List<TerrainAffordanceDef>.Contains));

			bool firstOnly = true;
			foreach(CodeInstruction i in instructions)
			{
				if(i.Calls(ContainsInfo) && firstOnly)
				{
					firstOnly = false;
					
					i.operand = AccessTools.Method(typeof(CanPlaceBlueprint), nameof(TerrainOrBridgesCanDo));
				}

				yield return i;
			}
		}

		public static bool TerrainOrBridgesCanDo(List<TerrainAffordanceDef> affordances, TerrainAffordanceDef neededDef)
		{
			if (affordances.Contains(neededDef))  return true;

			if (DesignatorContext.designating)
				return affordances.Contains(TerrainDefOf.Bridge.terrainAffordanceNeeded)	//terrain can support bridges
				&& TerrainDefOf.Bridge.affordances.Contains(neededDef);//bridges can support building

			return false;
		}
	}

	//This should technically go inside Designator's DesignateSingleCell, but this is easier.
	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForBuild")]
	class InterceptBlueprintPlaceBridgeFrame
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static void Prefix(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer || sourceDef == TerrainDefOf.Bridge) return;

			TerrainAffordanceDef affNeeded = sourceDef.GetTerrainAffordanceNeed(stuff);

			foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(center, rotation, sourceDef.Size))
				if (PlaceBridges.NeedsBridge(sourceDef, pos, map, stuff))
					GenConstruct.PlaceBlueprintForBuild(TerrainDefOf.Bridge, pos, map, rotation, faction, null);
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForInstall")]
	class InterceptBlueprintPlaceBridgeFrame_Install
	{
		//public static Blueprint_Install PlaceBlueprintForInstall(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction)
		public static void Prefix(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction)
		{
			ThingDef def = itemToInstall.InnerThing.def;
			InterceptBlueprintPlaceBridgeFrame.Prefix(def, center, map, rotation, faction, itemToInstall.InnerThing.Stuff);
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForReinstall")]
	class InterceptBlueprintPlaceBridgeFrame_Reinstall
	{
		//public static Blueprint_Install PlaceBlueprintForReinstall(Building buildingToReinstall, IntVec3 center, Map map, Rot4 rotation, Faction faction)
		public static void Prefix(Building buildingToReinstall, IntVec3 center, Map map, Rot4 rotation, Faction faction)
		{
			ThingDef def = buildingToReinstall.def;
			InterceptBlueprintPlaceBridgeFrame.Prefix(def, center, map, rotation, faction, buildingToReinstall.Stuff);
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
