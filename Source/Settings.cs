using System;
using System.Collections.Generic;
using System.Linq;
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

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				Scribe_Collections.Look(ref BridgelikeTerrain.allBridgeTerrains, "bridgePrefNames");
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				List<string> defNames = null;
				Scribe_Collections.Look(ref defNames, "bridgePrefNames");

				//Gotta wait for DefOfs to load to use DefDatabase
				LongEventHandler.ExecuteWhenFinished(() =>
				{
					List<TerrainDef> loadedBridgeOrder = defNames.Select(n => DefDatabase<TerrainDef>.GetNamed(n, false)).ToList();
					loadedBridgeOrder.RemoveAll(d => d == null);//Any removed mods, forget about em.

					//To merge with maybe new modded bridges:
					//Take all from known loadedBridgeOrder and push to front:
					//Any new modded terrains will be in back - which is normal for modded terrain anyway.
					//TODO: order default list by cost or something. Meh.
					loadedBridgeOrder.Reverse();
					foreach (TerrainDef terDef in loadedBridgeOrder)
					{
						BridgelikeTerrain.allBridgeTerrains.Remove(terDef);
						BridgelikeTerrain.allBridgeTerrains.Insert(0, terDef);
					}
				});
			}
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