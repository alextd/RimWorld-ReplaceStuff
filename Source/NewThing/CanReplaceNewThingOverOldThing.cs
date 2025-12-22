using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanReplace))]
	class CanReplaceNewThingOverOldThing
	{
		// public static bool CanReplace(BuildableDef placing, BuildableDef existing, ThingDef placingStuff = null, ThingDef existingStuff = null)
		public static void Postfix(ref bool __result, BuildableDef placing, BuildableDef existing)
		{
			if (((placing as ThingDef)?.IsNonDeconstructibleAttackableBuilding ?? false) || 
			    ((existing as ThingDef)?.IsNonDeconstructibleAttackableBuilding ?? false))
			{
				__result = false;
				return;
			}

			if (__result) return;
			
			if (!DesignatorContext.designating) return;

			if (placing is ThingDef newD && existing is ThingDef oldD && newD.CanReplace(oldD))
				__result = true;
		}
	}

	[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing")]
	public static class FramesDrawOverBuildingsEvenTheDoors
	{
		//		private static ThingDef NewFrameDef_Thing(ThingDef def)
		public static void Postfix(ThingDef __result)
		{
			__result.altitudeLayer = AltitudeLayer.BuildingOnTop;
		}
	}
}
