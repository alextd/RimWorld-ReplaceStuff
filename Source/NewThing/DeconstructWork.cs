using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(Frame), "WorkToBuild", MethodType.Getter)]
	public static class NewThingDeconstructWork
	{
		//public float WorkToBuild
		public static void Postfix(Frame __instance, ref float __result)
		{
			if (__instance.IsNewThingReplacement(out Thing oldThing))
				__result += ReplaceFrame.WorkToDeconstructDef(oldThing.def, oldThing.Stuff);
		}
	}
	[HarmonyPatch(typeof(Blueprint_Build), "WorkTotal", MethodType.Getter)]
	public static class NewThingDeconstructWork_Blueprint
	{
		//public float WorkToBuild
		public static void Postfix(Frame __instance, ref float __result)
		{
			if (__instance.IsNewThingReplacement(out Thing oldThing))
				__result += ReplaceFrame.WorkToDeconstructDef(oldThing.def, oldThing.Stuff);
		}
	}
}
