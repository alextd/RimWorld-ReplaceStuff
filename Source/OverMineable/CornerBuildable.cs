using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(TouchPathEndModeUtility), "IsCornerTouchAllowed")]
	class CornerBuildable
	{
		public static bool Prefix(ref bool __result, int cornerX, int cornerZ, Map map)
		{
			//public static bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, Map map)
			if (map.thingGrid.ThingsAt(new IntVec3(cornerX, 0, cornerZ))
				.Any(t => TouchPathEndModeUtility.MakesOccupiedCellsAlwaysReachableDiagonally(t.def)))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(TouchPathEndModeUtility), "MakesOccupiedCellsAlwaysReachableDiagonally")]
	public static class ConrnerMineableOkay
	{
		//public static bool MakesOccupiedCellsAlwaysReachableDiagonally(ThingDef def)
		public static bool Prefix(ref bool __result, ThingDef def)
		{
			ThingDef thingDef = (def.IsFrame || def.IsBlueprint) ? (def.entityDefToBuild as ThingDef) : def;
			__result = thingDef != null && thingDef.category == ThingCategory.Building && thingDef.holdsRoof;
			return false;
		}
	}

	
	[HarmonyPatch(typeof(GenPath), "ShouldNotEnterCell")]
	public static class ShouldNotEnterCellPatch
	{
		//private static bool ShouldNotEnterCell(Pawn pawn, Map map, IntVec3 dest)
		public static void Postfix(ref bool __result, Pawn pawn, Map map, IntVec3 dest)
		{
			if (__result || !dest.InBounds(map)) return;

			//Return if any direction open
			foreach (IntVec3 adj in GenAdj.CardinalDirections)
			{
				IntVec3 pos = dest + adj;
				if (pos.InBounds(map) && (!map.edificeGrid[pos]?.BlocksPawn(pawn) ?? true)) return;
			}
			//continuing, all directions are blocked

			foreach (IntVec3 adj in GenAdj.DiagonalDirections)
			{
				IntVec3 pos = dest + adj;
				if (pos.InBounds(map) && (!map.edificeGrid[pos]?.BlocksPawn(pawn) ?? true))
				{
					//One corner is open, so don't try to enter this cell, just do the thing diagonally
					__result = true;
					return;
				}
			}
		}
	}
	[HarmonyPatch(typeof(HaulAIUtility), "TryFindSpotToPlaceHaulableCloseTo")]
	//private static bool TryFindSpotToPlaceHaulableCloseTo(Thing haulable, Pawn worker, IntVec3 center, out IntVec3 spot)
	public static class TryFindSpotToPlaceHaulableCloseToPatch
	{
		public static bool recursive = true;
		public static void Postfix(ref bool __result, Thing haulable, Pawn worker, IntVec3 center, ref IntVec3 spot)
		{
			if (__result || !recursive) return;
			recursive = false;

			foreach (IntVec3 adj in GenAdj.DiagonalDirections)
			{
				IntVec3 dCenter = center + adj;
				if (TryFindSpotToPlaceHaulableCloseTo(haulable, worker, dCenter, out spot))
				{
					__result = true;
					break;
				}
			}

			recursive = true;
		}

		//Private, you say?
		public static bool TryFindSpotToPlaceHaulableCloseTo(Thing haulable, Pawn worker, IntVec3 center, out IntVec3 spot)
		{
			object[] args = new object[] { haulable, worker, center, null};
			bool result = (bool)AccessTools.Method(typeof(HaulAIUtility), "TryFindSpotToPlaceHaulableCloseTo").Invoke(null, args);
			spot = (IntVec3)args[3];
			return result;
		}
	}
}
