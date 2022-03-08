using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.DesignateSingleCell))]
	class InterceptDesignator_Build
	{
		//Replace the entire Designator_Build.DesignateSingleCell to behave differently in these cases:
		//- Changing the stuff of a replace frame
		//- Changing the stuff of an upgrade (just kill it and let it handle normal)
		//- Make a replaceframe
		// (TODO: Just use DesignatorReplaceStuff? That'd probably be easier.)
		// (Wouldn't handle upgrades though. Sigh)
		// Technically this is bypassing godmode, tutorial, PlayerKnowledgeDatabase, PlaceWorkers, but none of that should matter.
		// But I made to to keep ThrowMetaPuffs.

		//public override void DesignateSingleCell(IntVec3 c)
		public static bool Prefix(Designator_Build __instance, IntVec3 c, BuildableDef ___entDef, Rot4 ___placingRot)
		{
			Map map = __instance.Map;
			ThingDef stuff = __instance.StuffDef;

			//Fix for door rotation
			if (___entDef is ThingDef thingDef && typeof(Building_Door).IsAssignableFrom(thingDef.thingClass))
				___placingRot = Building_Door.DoorRotationAt(c, map);

			List<Thing> oldThings = c.GetThingList(map).FindAll(t => t.Position == c && t.Rotation == ___placingRot);

			Log.Message($"Can Designator_Build Replace something from ({oldThings.ToStringSafeEnumerable()})?");

			//First check for Replace Frames, and change the stuff.
			foreach (Thing oldThing in oldThings)
			{
				if (oldThing is ReplaceFrame oldReplaceFrame &&
					oldReplaceFrame.def.entityDefToBuild == ___entDef &&
					oldReplaceFrame.EntityToBuildStuff() != stuff)
				{
					Log.Message($"It's a ReplaceFrame {oldReplaceFrame} from {oldReplaceFrame.oldStuff} to {oldReplaceFrame.EntityToBuildStuff()}, now {stuff}");
					if (oldReplaceFrame.oldStuff == stuff)
						oldReplaceFrame.Destroy(DestroyMode.Cancel);
					else
						oldReplaceFrame.ChangeStuff(stuff);

					FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, ___placingRot, ___entDef.Size), map);
					return false;
				}
			}

			//First check for Frames, and kill it to re-place it
			foreach (Thing oldThing in oldThings)
			{
				if (oldThing is Frame oldFrame &&
					oldFrame.def.entityDefToBuild == ___entDef &&
					oldFrame.EntityToBuildStuff() != stuff)
				{
					Log.Message($"It's a Frame {oldFrame} (just canceling it)");
					oldFrame.Destroy(DestroyMode.Cancel);
					return true;
				}
			}

			//Then if it's the same thing, create replace frame (should already have verified in CanDesignate)
			foreach (Thing oldThing in oldThings)
			{
				if (
					oldThing.def == ___entDef &&
					oldThing.Stuff != stuff)
				{
					Log.Message($"It's a Thing {oldThing}");
					GenReplace.PlaceReplaceFrame(oldThing, stuff);

					FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, ___placingRot, ___entDef.Size), map);
					return false;
				}
			}
				
			return true;
		}
	}
}

