using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff
{
	class Settings : ModSettings
	{
		public bool hideOverwallCoolers = false;
		public bool hideNormalCoolers = false;

		public bool cornerBuildable = true;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Replace_Stuff.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("TD.SettingsNoOverwallCoolers".Translate(), ref hideOverwallCoolers);
			options.CheckboxLabeled("TD.SettingsNoNormalCoolers".Translate(), ref hideNormalCoolers);
			options.Gap();

			options.CheckboxLabeled("TD.SettingsCornerBuildable".Translate(), ref cornerBuildable, "TD.SettingsCornerBuildableDesc".Translate());
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref hideOverwallCoolers, "hideOverwallCoolers", false);
			Scribe_Values.Look(ref hideNormalCoolers, "hideNormalCoolers", false);

			Scribe_Values.Look(ref cornerBuildable, "cornerBuildable", true);
		}
	}


	[HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
	public static class HideCoolerBuild
	{
		public static bool Prefix(Designator_Build __instance, ref bool __result)
		{
			if (Settings.Get().hideOverwallCoolers &&
				(__instance.PlacingDef == OverWallDef.Cooler_Over ||
				__instance.PlacingDef == OverWallDef.Cooler_Over2W ||
				__instance.PlacingDef == OverWallDef.Vent_Over))
			{
				__result = false;
				return false;
			}
			if (Settings.Get().hideNormalCoolers &&
				(__instance.PlacingDef == ThingDefOf.Cooler ||
				__instance.PlacingDef == OverWallDef.Vent))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}