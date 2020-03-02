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
		//public static bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null, ThingDef stuffDef = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
		{
			LocalVariableInfo posInfo = method.GetMethodBody().LocalVariables.First(lv => lv.LocalType == typeof(IntVec3));
			MethodInfo ContainsInfo = AccessTools.Method(typeof(List<TerrainAffordanceDef>), nameof(List<TerrainAffordanceDef>.Contains));

			bool firstOnly = true;
			foreach(CodeInstruction i in instructions)
			{
				if(i.Calls(ContainsInfo) && firstOnly)
				{
					firstOnly = false;

					yield return new CodeInstruction(OpCodes.Ldloc, posInfo.LocalIndex);//IntVec3 pos
					yield return new CodeInstruction(OpCodes.Ldarg_2);//Map
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CanPlaceBlueprint), nameof(TerrainOrBridgesCanDo)));
				}
				else
					yield return i;
			}
		}

		public static bool TerrainOrBridgesCanDo(List<TerrainAffordanceDef> affordances, TerrainAffordanceDef neededDef, IntVec3 pos, Map map)
		{
			//Terrain already supports: vanilla, ok
			if (affordances.Contains(neededDef))
				return true;

			//If bridges won't help: no.
			if (!TerrainDefOf.Bridge.affordances.Contains(neededDef))
				return false;

			//Bridge blueprint there: ok
			if (pos.GetThingList(map).Any(t => t.def.entityDefToBuild == TerrainDefOf.Bridge))
				return true;

			//Player choosing to build and bridges possible: ok (elsewhere in code will place blueprints)
			if (DesignatorContext.designating &&
				affordances.Contains(TerrainDefOf.Bridge.terrainAffordanceNeeded))  //terrain can support bridges
				return true;

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
				EnsureBridge.PlaceBridgeIfNeeded(sourceDef, pos, map, rotation, faction, stuff);
		}
	}

	public class EnsureBridge
	{
		public static void PlaceBridgeIfNeeded(BuildableDef sourceDef, IntVec3 pos, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (pos.GetThingList(map).Any(t => t.def.entityDefToBuild == TerrainDefOf.Bridge))
				return;//Already building!
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
