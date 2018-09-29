using System.Reflection;
using Verse;
using UnityEngine;
using Harmony;
using RimWorld;

namespace Replace_Stuff
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			GetSettings<Settings>();
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif

			//Need to patch this while loading
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");
			harmony.Patch(AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"),
				null, new HarmonyMethod(typeof(ThingDefGenerator_ReplaceFrame), "Postfix"));
			harmony.Patch(AccessTools.Constructor(typeof(Designator_Dropdown)),
				null, new HarmonyMethod(typeof(Mod), nameof(Mod.Designator_DropdownPostfix)));

			//harmony.Patch(AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing"),
			//	null, new HarmonyMethod(typeof(ShowBluePrintOverFog), "Postfix"));
		}

		public static void Designator_DropdownPostfix(Designator_Dropdown __instance)
		{
			__instance.order = 20f;
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

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			GetSettings<Settings>().DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.ReplaceStuff".Translate();
		}
	}
}