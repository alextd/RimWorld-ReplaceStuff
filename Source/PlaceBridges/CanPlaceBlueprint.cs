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
			FieldInfo affordancesInfo = AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.affordances));

			bool firstOnly = true;
			var instList = instructions.ToList();
			for(int i=0;i<instList.Count();i++)
			{
				var inst = instList[i];
				if(inst.LoadsField(affordancesInfo) && firstOnly)
				{
					firstOnly = false;

					//
					// IL_0053: ldarg.2      // map
					// IL_0054: ldfld        class Verse.TerrainGrid Verse.Map::terrainGrid
					// IL_0059: ldloc.3      // c1
					// IL_005a: callvirt     instance class Verse.TerrainDef Verse.TerrainGrid::TerrainAt(valuetype Verse.IntVec3)
					// IL_005f: ldfld        class [mscorlib]System.Collections.Generic.List`1<class Verse.TerrainAffordanceDef> Verse.TerrainDef::affordances
					// IL_0064: ldloc.0      // terrainAffordanceNeed
					// IL_0065: callvirt     instance bool class [mscorlib]System.Collections.Generic.List`1<class Verse.TerrainAffordanceDef>::Contains(!0/*class Verse.TerrainAffordanceDef*/)
					// IL_006a: brtrue.s     IL_0071

					//Skip ldfld affordances, load terrainAffordanceNeed
					i++;
					yield return instList[i];

					//skip call to Contains
					i++;

					//replace with TerrainOrBridgesCanDo (below)
					yield return new CodeInstruction(OpCodes.Ldarg_0);//entDef
					yield return new CodeInstruction(OpCodes.Ldloc, posInfo.LocalIndex);//IntVec3 pos
					yield return new CodeInstruction(OpCodes.Ldarg_2);//Map
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CanPlaceBlueprint), nameof(TerrainOrBridgesCanDo)));

					//and it'll continue with the brtrue
				}
				else
					yield return inst;
			}
		}

		public static bool TerrainOrBridgesCanDo(TerrainDef tDef, TerrainAffordanceDef neededDef, BuildableDef def, IntVec3 pos, Map map)
		{
			//Code Used to be:
			if (tDef.affordances.Contains(neededDef))
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
