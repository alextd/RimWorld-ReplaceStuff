using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver")]
	class CanPlaceBlueprintOverOldThing
	{
		//public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
		public static void Postfix(ref bool __result, BuildableDef newDef, ThingDef oldDef)
		{
			if (__result) return;

			if (!DesignatorContext.designating) return;

			if (newDef is ThingDef newD && newD.CanReplace(oldDef))
				__result = true;
		}
	}
}
