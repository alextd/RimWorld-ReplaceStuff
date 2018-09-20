using System.Reflection;
using Verse;
using UnityEngine;
using Harmony;

namespace Replace_Stuff
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			// GetSettings<Settings>();
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif

			//Need to patch this while loading
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");
			harmony.Patch(AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators"),
				null, new HarmonyMethod(typeof(CoolersOverWalls.DesignationCategoryDefRemovalService), "Postfix"));
		}

		[StaticConstructorOnStartup]
		public static class ModStartup
		{
			static ModStartup()
			{
				HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
			}
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