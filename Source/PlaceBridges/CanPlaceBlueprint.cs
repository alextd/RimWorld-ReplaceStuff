using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using System.Diagnostics;
using System.Threading;
using Verse;

namespace Replace_Stuff.PlaceBridges
{
	public static class PlaceBridges
	{
		public static TerrainDef GetNeededBridge(BuildableDef def, IntVec3 pos, Map map, ThingDef stuff)
		{
			if (!pos.InBounds(map)) return null;
			TerrainAffordanceDef needed = def.GetTerrainAffordanceNeed(stuff);
			return BridgelikeTerrain.FindBridgeFor(map.terrainGrid.TerrainAt(pos), needed, map);
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "CanBuildOnTerrain")]
	class CanPlaceBlueprint
	{
		//public static bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null, ThingDef stuffDef = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
		{
			LocalVariableInfo posInfo = method.GetMethodBody().LocalVariables.First(lv => lv.LocalType == typeof(IntVec3));
			MethodInfo getAffordances = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetAffordances));
			MethodInfo listContains = AccessTools.Method(typeof(List<TerrainAffordanceDef>), nameof(List<TerrainAffordanceDef>.Contains));

			bool firstOnly = true;
			var instList = instructions.ToList();
			for(int i=0;i<instList.Count();i++)
			{
				var inst = instList[i];
				
				// Look for the pattern: c1.GetAffordances(map).Contains(terrainAffordanceNeed)
				// This will be: ldloc (c1), ldarg (map), call GetAffordances, ldloc (terrainAffordanceNeed), callvirt Contains
				if(inst.Calls(getAffordances) && firstOnly)
				{
					firstOnly = false;

					// We found the GetAffordances call
					// The stack at this point has: c1, map
					// After GetAffordances call, stack will have: List<TerrainAffordanceDef>
					yield return inst; // Keep the GetAffordances call
					
					// Next instruction should load terrainAffordanceNeed
					i++;
					yield return instList[i];
					
					// Next instruction should be Contains call - replace it
					i++;
					
					// Replace Contains call with our custom method
					// Stack now has: List<TerrainAffordanceDef>, TerrainAffordanceDef
					// We need to add: entDef, pos, map for our method
					yield return new CodeInstruction(OpCodes.Ldarg_0);//entDef
					yield return new CodeInstruction(OpCodes.Ldloc, posInfo.LocalIndex);//IntVec3 pos
					yield return new CodeInstruction(OpCodes.Ldarg_2);//Map
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CanPlaceBlueprint), nameof(TerrainOrBridgesCanDo)));
				}
				else
					yield return inst;
			}
		}

		public static bool TerrainOrBridgesCanDo(List<TerrainAffordanceDef> affordances, TerrainAffordanceDef neededDef, BuildableDef def, IntVec3 pos, Map map)
		{
			//Code Used to be:
			if (affordances.Contains(neededDef))
				return true;

			if (def is TerrainDef)
				return false;

			//Now it's gonna also check bridges:
			//Bridge blueprint there that will support this:
			//TODO isn't this redundant?
			if (pos.GetThingList(map).Any(t =>
				t.def.entityDefToBuild is TerrainDef bpTDef &&
				bpTDef.affordances.Contains(neededDef)))
				return true;

			//Player not choosing to build and bridges possible: ok (elsewhere in code will place blueprints)
			TerrainDef tDef = map.terrainGrid.TerrainAt(pos);
			if (DesignatorContext.designating && BridgelikeTerrain.FindBridgeFor(tDef, neededDef, map) != null)
				return true;

			return false;
		}
	}

	//This should technically go inside Designator's DesignateSingleCell, but this is easier.
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild))]
	class InterceptBlueprintPlaceBridgeFrame
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static void Prefix(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer || sourceDef.IsBridgelike()) return;

			foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(center, rotation, sourceDef.Size))
				EnsureBridge.PlaceBridgeIfNeeded(sourceDef, pos, map, rotation, faction, stuff);
		}
	}

	public class EnsureBridge
	{
		public static void PlaceBridgeIfNeeded(BuildableDef sourceDef, IntVec3 pos, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			TerrainDef bridgeDef = PlaceBridges.GetNeededBridge(sourceDef, pos, map, stuff);

			if (bridgeDef == null)
				return;

			if (!bridgeDef.IsResearchFinished)
				return;

			if (pos.GetThingList(map).Any(t => t.def.entityDefToBuild == bridgeDef))
				return;//Already building!

			Log.Message($"Replace Stuff placing {bridgeDef} for {sourceDef}({sourceDef.GetTerrainAffordanceNeed(stuff)}) on {map.terrainGrid.TerrainAt(pos)}");
			GenConstruct.PlaceBlueprintForBuild(bridgeDef, pos, map, rotation, faction, null);//Are there bridge precepts/styles?...
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
			if (oldEntDef is ThingDef tdef && (GenConstruct.BuiltDefOf(tdef) ?? oldEntDef).IsBridgelike())
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
