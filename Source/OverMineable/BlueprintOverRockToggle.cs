using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
	[StaticConstructorOnStartup]
	public static class PlaySettings_BlueprintOverRockToggle
	{
		public static bool enabled = true;
		private static Texture2D icon = ContentFinder<Texture2D>.Get("BlueprintOverRockToggle", true);

		[HarmonyPostfix]
		public static void AddButton(WidgetRow row, bool worldView)
		{
			if (worldView) return;

			row.ToggleableIcon(ref enabled, icon, "Place blueprints over mountain rock");
		}
	}
}
