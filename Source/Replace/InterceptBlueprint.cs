using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForBuild")]
	class InterceptBlueprint
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static bool Prefix(ref Blueprint_Build __result, BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer) return true;

			Func<Thing, bool> validator = t =>
			t.def == sourceDef &&
			t.Position == center &&
			t.Rotation == rotation &&
			map.designationManager.DesignationOn(t)?.def == DesignationDefOf.Deconstruct;

			if (center.GetThingList(map).FirstOrDefault() is Thing oldThing)
			{
				GenReplace.PlaceReplaceFrame(oldThing, stuff);
				__result = null;
				return false;
			}
			return true;
		}
	}
}

