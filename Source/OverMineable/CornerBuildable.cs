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
}
