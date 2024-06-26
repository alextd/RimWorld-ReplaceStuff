﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Replace_Stuff.NewThing;
using Verse;

namespace Replace_Stuff
{
	//public override AcceptanceReport CanDesignateCell(IntVec3 c)
	[HarmonyPatch(typeof(Designator_Build), "CanDesignateCell")]
	static class DesignatorContext
	{
		public static ThingDef stuffDef;
		public static bool designating;

		public static void Prefix(Designator_Build __instance)
		{
			stuffDef = __instance.StuffDef;
			designating = true;
		}
		public static void Postfix()
		{
			stuffDef = null;
			designating = false;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt_NewTemp))]
	public static class NormalBuildReplace
	{
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
		public static void Postfix(ref AcceptanceReport __result, BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode, Thing thingToIgnore)
		{
			if (__result.Reason != "IdenticalThingExists".Translate() &&
				__result.Reason != "IdenticalBlueprintExists".Translate()) return;

			if (!(entDef is ThingDef)) return;

			if (!entDef.MadeFromStuff) return;

			ThingDef newStuff = DesignatorContext.stuffDef;

			//It would not be so easy to transpile this part
			//it doesn't simply change __result to true when a replace frame can be placed,
			//it also checks if the replace frame is already there and overrides that with false
			bool makeItTrue = false;
			foreach (Thing thing in center.GetThingList(map))
			{
				if (thing == thingToIgnore) continue;

				if (thing.Position == center &&
					(thing.Rotation == rot || PlacingRotationDoesntMatter(entDef)) &&
					GenConstruct.BuiltDefOf(thing.def) == entDef)
				{
					//Same thing or Building the same thing
					ThingDef oldStuff = thing is Blueprint bp ? bp.EntityToBuildStuff() : thing.Stuff;
					if (thing is ReplaceFrame rf && oldStuff == newStuff)
					{
						//Identical things already exists
						return;
					}

					if (newStuff != oldStuff)
					{
						if (GenConstruct.CanBuildOnTerrain(entDef, center, map, rot, thing, newStuff))
							makeItTrue = true;
						else
						{
							__result = "TerrainCannotSupport_TerrainAffordanceFromStuff".Translate(entDef, entDef.GetTerrainAffordanceNeed(newStuff), newStuff).CapitalizeFirst();
						}
					}
				}
				else if ((thing is Blueprint || thing is Frame) && thing.def.entityDefToBuild is ThingDef)
				{
					//Upgrade blueprint/frame exists
					__result = "TD.ReplacementAlreadyInProgess".Translate(); 
					return;
				}
			}

			if (makeItTrue) //Only if We found a replaceable, identical thing, and no other blueprints/frames for other defs
				__result = true;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo RotationEquals = AccessTools.Method(typeof(Rot4), "op_Equality");
			MethodInfo OrRotDoesntMatter = AccessTools.Method(typeof(NormalBuildReplace), nameof(OrRotDoesntMatter));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.Calls(RotationEquals))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//BuildableDef entDef
					yield return new CodeInstruction(OpCodes.Call, OrRotDoesntMatter);
				}
			}
		}

		public static bool OrRotDoesntMatter(bool result, BuildableDef entDef)
		{
			return result || PlacingRotationDoesntMatter(entDef);
		}

		public static bool PlacingRotationDoesntMatter(BuildableDef entDef)
		{
			return entDef is ThingDef def &&
				(!def.rotatable ||
				typeof(Building_Door).IsAssignableFrom(def.thingClass));
		}
	}
}