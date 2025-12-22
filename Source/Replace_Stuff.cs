using System.Reflection;
using System.Linq;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using Replace_Stuff.Comps;

namespace Replace_Stuff
{
	public class Mod : Verse.Mod
	{
		public static Settings settings;
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			settings = GetSettings<Settings>();
			new Harmony("Uuugggg.rimworld.Replace_Stuff.main").PatchAll();
		}

		[StaticConstructorOnStartup]
		public static class ModStartup
		{
			static ModStartup()
			{
				//Hugslibs-added defs will be queued up before this Static Constructor
				//So queue replace frame generation after that
				LongEventHandler.QueueLongEvent(() => ThingDefGenerator_ReplaceFrame.AddReplaceFrames(), null, true, null);
				LongEventHandler.QueueLongEvent(CoolersOverWalls.DesignatorBuildDropdownStuffFix.SanityCheck, null, true, null);
				LongEventHandler.QueueLongEvent(Compatibility.AddRulesFromXML, null, true, null);
			}
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.ReplaceStuff".Translate();
		}
	}
}