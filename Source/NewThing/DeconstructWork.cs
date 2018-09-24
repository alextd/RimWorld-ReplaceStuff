using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(Frame))]
	[HarmonyPatch("WorkToBuild", PropertyMethod.Getter)]
	public static class NewThingDeconstructWork
	{
		//public float WorkToBuild
		public static void Postfix(Frame __instance, ref float __result)
		{
			if (__instance.IsNewThingFrame(out Thing oldThing))
				__result += ReplaceFrame.WorkToDeconstruct(oldThing.def, oldThing.Stuff);
		}
	}
}
