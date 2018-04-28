using System.Reflection;
using Verse;
using UnityEngine;
using Harmony;

namespace Replace_Stuff
{
	public class Mod : Verse.Mod
	{
		HarmonyInstance harmony;
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			// GetSettings<Settings>();
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif
			harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static HarmonyInstance Harmony()
		{
			return LoadedModManager.GetMod<Replace_Stuff.Mod>().harmony;
		}

//		public override void DoSettingsWindowContents(Rect inRect)
//		{
//			base.DoSettingsWindowContents(inRect);
//			GetSettings<Settings>().DoWindowContents(inRect);
//		}
//
//		public override string SettingsCategory()
//		{
//			return "TD.ReplaceStuff".Translate();
//		}
	}
}