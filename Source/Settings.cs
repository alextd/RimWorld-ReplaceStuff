using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Replace_Stuff
{
	class Settings : ModSettings
	{
		public bool hideOverwallCoolers = false;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Replace_Stuff.Mod>().GetSettings<Settings>();
		}

		public void SetHideCoolerDefs()
		{
			if (Current.Game == null) return;

			Current.Game.Rules.SetAllowBuilding(OverWallDef.Cooler_Over, !hideOverwallCoolers);
			Current.Game.Rules.SetAllowBuilding(OverWallDef.Cooler_Over2W, !hideOverwallCoolers);
			Current.Game.Rules.SetAllowBuilding(OverWallDef.Vent_Over, !hideOverwallCoolers);
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			bool hideOverwallCoolersP = hideOverwallCoolers;
			options.CheckboxLabeled("Hide those super-nifty over-wall coolers from build menu", ref hideOverwallCoolers);
			if (hideOverwallCoolers != hideOverwallCoolersP)
				SetHideCoolerDefs();
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref hideOverwallCoolers, "hideOverwallCoolers", false);
		}
	}
}