using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace Replace_Stuff.OverMineable
{
	static class RockCheck
	{
		public static bool IsMineableRock(this Thing t) => IsMineableRock(t.def);
		public static bool IsMineableRock(this ThingDef td)
		{
			return td.mineable && !td.IsSmoothed;
		}
	}
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver")]
	class CanPlaceBlueprintOverMineable
	{
		//public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
		public static void Postfix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
		{
			if (!OverMineable.PlaySettings_BlueprintOverRockToggle.blueprintOverRock)
				return;

			if(newDef.GetStatValueAbstract(StatDefOf.WorkToBuild) > 0f)
				__result |= oldDef.IsMineableRock();
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class MineableBlocksConstruction
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (__result) return;
			
			if (t.IsMineableRock())	// any case that the thing can be built over plain rock?
				__result = true;
		}
	}

	[DefOf]
	public static class ConceptDefOf
	{
		public static ConceptDef BuildersTryMine;
	}

	//This should technically go inside Designator_Build.DesignateSingleCell, but this is easier.
	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForBuild")]
	class InterceptBlueprintOverMinable
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static void Prefix(ref Blueprint_Build __result, BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer) return;

			foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(center, rotation, sourceDef.Size))
			{
				if (map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) != null)
					continue;

				if (sourceDef is ThingDef thingDef)
					foreach (Thing mineThing in map.thingGrid.ThingsAt(cell).Where(t => t.IsMineableRock()))
					{
						if (!DontMineSmoothingRock.ToBeSmoothed(mineThing, thingDef))
						{
							map.designationManager.AddDesignation(new Designation(mineThing, DesignationDefOf.Mine));

							if (mineThing.def.building?.mineableYieldWasteable ?? false)
								TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.BuildersTryMine);
						}
					}
			}
		}
	}
}
