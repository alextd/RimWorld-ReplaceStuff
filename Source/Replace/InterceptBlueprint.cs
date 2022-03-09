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
		//public override void DesignateSingleCell(IntVec3 c)
		public static bool Prefix(Designator_Build __instance, IntVec3 c, BuildableDef ___entDef, Rot4 ___placingRot)
		{
			//Replace the entire Designator_Build.DesignateSingleCell to behave differently if there is a replacable thing
			// Technically this is bypassing godmode, tutorial, PlayerKnowledgeDatabase, PlaceWorkers, but none of that should matter.

			ThingDef thingDef = ___entDef as ThingDef;

			if (thingDef == null)//Terrain?
				return true;


			//Fix for door rotation so we find any rotation of doors
			if (typeof(Building_Door).IsAssignableFrom(thingDef.thingClass))
				___placingRot = Building_Door.DoorRotationAt(c, __instance.Map);

			List<Thing> replaceables = c.GetThingList(__instance.Map).FindAll(
				t =>
					t.Position == c &&
					t.Rotation == ___placingRot &&
					Designator_ReplaceStuff.CanReplaceStuffFor(__instance.StuffDef, t, thingDef)
			);

			if (replaceables.Count == 0)
				return true;

			Designator_ReplaceStuff.ChooseReplace(replaceables, __instance.StuffDef);

			return false;
		}
	}
}

