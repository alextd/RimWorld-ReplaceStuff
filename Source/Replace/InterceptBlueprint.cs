using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild_NewTemp))]
	class InterceptBlueprint
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static bool Prefix(ref Blueprint_Build __result, BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer) return true;

			//Fix for door rotation
			if (sourceDef is ThingDef thingDef && typeof(Building_Door).IsAssignableFrom(thingDef.thingClass))
				rotation = Building_Door.DoorRotationAt(center, map);

			Func<Thing, bool> posCheck = t => t.Position == center && t.Rotation == rotation;
			
			Func<Thing, bool> newReplaceCheck = t => posCheck(t) &&
				t.def == sourceDef && t.Stuff != stuff;
			Func<Thing, bool> changeFrameStuffCheck = t => posCheck(t) &&
				t is Frame f && f.EntityToBuildStuff() != stuff && f.def.entityDefToBuild == sourceDef;
			Func<Thing, bool> changeReplaceStuffCheck = t => posCheck(t) &&
				t is ReplaceFrame rf && rf.EntityToBuildStuff() != stuff && rf.def.entityDefToBuild == sourceDef;

			List<Thing> thingsHere = center.GetThingList(map);
			if (thingsHere.FirstOrDefault(changeReplaceStuffCheck) is ReplaceFrame oldReplaceFrame)
			{
				if (oldReplaceFrame.oldStuff == stuff)
					oldReplaceFrame.Destroy(DestroyMode.Cancel);
				else
					oldReplaceFrame.ChangeStuff(stuff);
				//Okay so 1.3 uses the returned blueprint. Should we handle that, or pass it a dummy object?
				__result = new Blueprint_Build();
				//__result = null;
				return false;
			}
			else if (thingsHere.FirstOrDefault(changeFrameStuffCheck) is Thing oldFrame)
			{
				oldFrame.Destroy(DestroyMode.Cancel);
				return true;
			}
			else if (thingsHere.FirstOrDefault(newReplaceCheck) is Thing oldThing)
			{
				GenReplace.PlaceReplaceFrame(oldThing, stuff);
				//Okay so 1.3 uses the returned blueprint. Should we handle that, or pass it a dummy object?
				__result = new Blueprint_Build();
				//__result = null;
				return false;
			}
			return true;
		}
	}
}

