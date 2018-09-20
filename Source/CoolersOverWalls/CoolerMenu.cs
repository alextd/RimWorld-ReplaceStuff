using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;
using UnityEngine;

namespace Replace_Stuff.CoolersOverWalls
{
	[HarmonyPatch(typeof(Designator_Build), "ProcessInput")]
	[StaticConstructorOnStartup]
	public static class CoolerWidth
	{
		public static List<ThingDef> options;
		static CoolerWidth()
		{
			options = new List<ThingDef>() { OverWallDef.Cooler_Over, OverWallDef.Cooler_Over2W };
		}

		//public override void ProcessInput(Event ev)
		public static bool Prefix(Designator_Build __instance, Event ev)
		{
			//if (!__instance.CheckCanInteract())	return; //If this wasn't protected I'd do this check ; tutorial only anyway.

			if (__instance.PlacingDef is ThingDef def && options.Contains(def))
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();

				foreach (ThingDef o in options)
					list.Add(new FloatMenuOption(o.label, delegate
				{
					__instance.ProcessInput(ev);
					Find.DesignatorManager.Select(__instance);
					AccessTools.Field(typeof(Designator_Build), "entDef").SetValue(__instance, o);
				}));

				FloatMenu floatMenu = new FloatMenu(list);
				floatMenu.vanishIfMouseDistant = true;
				Find.WindowStack.Add(floatMenu);
				Find.DesignatorManager.Select(__instance);
				
				return false;
			}
			else
				return true;
		}
	}

	//Harmony PatchAll called too late, called in Mod ctor
	//[HarmonyPatch(typeof(DesignationCategoryDef), "ResolveDesignators")]
	public static class DesignationCategoryDefRemovalService
	{
		//the wide cooler def needs to have DesignationCategory so the game auto-generates blueprint and frame,
		//but don't want it in the actual list, menu options above
		public static void Postfix(DesignationCategoryDef __instance)
		{
			__instance.AllResolvedDesignators.RemoveAll(d => d is Designator_Build db && db.PlacingDef == OverWallDef.Cooler_Over2W);
		}
	}
}
