using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using Replace_Stuff.PlaceBridges;

namespace Replace_Stuff
{
	public class Settings : ModSettings
	{
		public bool hideOverwallCoolers = false;
		public bool hideNormalCoolers = false;

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("TD.SettingsNoOverwallCoolers".Translate(), ref hideOverwallCoolers);
			options.CheckboxLabeled("TD.SettingsNoNormalCoolers".Translate(), ref hideNormalCoolers);
			options.GapLine();

			Text.Font = GameFont.Medium;
			options.Label("TD.PreferredBridge".Translate());
			Text.Font = GameFont.Small;

			float itemHeight = Text.LineHeight;
			Rect reorderRect = options.GetRect(BridgelikeTerrain.allBridgeTerrains.Count * itemHeight + 2);
			Widgets.DrawBox(reorderRect);

			Rect labelRect = reorderRect.ContractedBy(1).TopPartPixels(itemHeight);

			int reorderID = ReorderableWidget.NewGroup_NewTemp(BridgelikeTerrain.Reorder, ReorderableDirection.Vertical);

			foreach (TerrainDef terDef in BridgelikeTerrain.allBridgeTerrains)
			{
				Widgets.DefLabelWithIcon(labelRect, terDef, 0);
				ReorderableWidget.Reorderable(reorderID, labelRect);

				labelRect.y += itemHeight;
			}

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref hideOverwallCoolers, "hideOverwallCoolers", false);
			Scribe_Values.Look(ref hideNormalCoolers, "hideNormalCoolers", false);
		}
	}


	[HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
	public static class HideCoolerBuild
	{
		public static void Postfix(Designator_Build __instance, ref bool __result)
		{
			if (!__result) return;

			if (Mod.settings.hideOverwallCoolers &&
				(__instance.PlacingDef == OverWallDef.Cooler_Over ||
				__instance.PlacingDef == OverWallDef.Cooler_Over2W ||
				__instance.PlacingDef == OverWallDef.Vent_Over))
			{
				__result = false;
				return;
			}
			if (Mod.settings.hideNormalCoolers &&
				(__instance.PlacingDef == ThingDefOf.Cooler ||
				__instance.PlacingDef == OverWallDef.Vent))
			{
				__result = false;
				return;
			}
		}
	}
}