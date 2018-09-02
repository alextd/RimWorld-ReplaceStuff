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
	class RockCheck
	{
		public static bool IsMineableRock(Thing t) => IsMineableRock(t.def);
		public static bool IsMineableRock(ThingDef td)
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
			if(newDef.GetStatValueAbstract(StatDefOf.WorkToBuild) > 0f)
				__result |= RockCheck.IsMineableRock(oldDef);
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class MineableBlocksConstruction
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (__result) return;

			BuildableDef thingDef = constructible is Blueprint ? constructible.def.entityDefToBuild
				: constructible is Frame ? constructible.def.entityDefToBuild
				: constructible.def;
			
			if (RockCheck.IsMineableRock(t))	// any case that the thing can be built over plain rock?
				__result = true;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "HandleBlockingThingJob")]
	class HandleBlockingThingOverMineable
	{
		//public static Job HandleBlockingThingJob(Thing constructible, Pawn worker, bool forced = false)
		public static void Postfix(Thing constructible, Pawn worker, bool forced, ref Job __result)
		{
			Thing thing = GenConstruct.FirstBlockingThing(constructible, worker);
			if (RockCheck.IsMineableRock(thing))
			{
				__result = null;//Base would add deconstruct job for all buildings, no no no, rock walls are considered buildings, should not be deconstructed

				if (worker.story.WorkTypeIsDisabled(WorkTypeDefOf.Mining)) return;

				//if(worker.skills.GetSkill(SkillDefOf.Mining) < 2)	return;
				//Too much to think about to stop shitty miners from doing this.

				LocalTargetInfo target = thing;
				PathEndMode peMode = PathEndMode.Touch;
				Danger maxDanger = worker.NormalMaxDanger();
				if (worker.CanReserveAndReach(target, peMode, maxDanger, 1, -1, null, forced))
					__result = new Job(JobDefOf.Mine, thing)
					{
						ignoreDesignations = true
					};
			}
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

				foreach (Thing mineThing in map.thingGrid.ThingsAt(cell).Where(t => RockCheck.IsMineableRock(t)))
				{
					map.designationManager.AddDesignation(new Designation(mineThing, DesignationDefOf.Mine));
					if(mineThing.def.building?.mineableYieldWasteable ?? false)
						TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.BuildersTryMine);
				}
			}
		}
	}
}
