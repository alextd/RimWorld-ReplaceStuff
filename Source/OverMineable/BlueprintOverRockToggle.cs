using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
	[StaticConstructorOnStartup]
	public static class PlaySettings_BlueprintOverRockToggle
	{
		public static bool blueprintOverRock = true;
		private static Texture2D icon = ContentFinder<Texture2D>.Get("BlueprintOverRockToggle", true);

		[HarmonyPostfix]
		public static void AddButton(WidgetRow row, bool worldView)
		{
			if (worldView) return;

			row.ToggleableIcon(ref blueprintOverRock, icon, "TD.ToggleBlueprintOverRock".Translate());
		}
	}


	[HarmonyPatch(typeof(PlaySettings), "ExposeData")]
	public static class PlaySettings_ExposeData
	{
		public static void Prefix()
		{
			Scribe_Values.Look(ref PlaySettings_BlueprintOverRockToggle.blueprintOverRock, "blueprintOverRock", true);
		}
	}
}
