using System.Reflection;
using System.Linq;
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

			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");

			//Turn off DefOf warning since harmony patches trigger it.
			MethodInfo DefOfHelperInfo = AccessTools.Method(typeof(DefOfHelper), "EnsureInitializedInCtor");
			if (!harmony.GetPatchedMethods().Contains(DefOfHelperInfo))
				harmony.Patch(DefOfHelperInfo, new HarmonyMethod(typeof(Mod), "EnsureInitializedInCtorPrefix"), null);
			
			harmony.PatchAll();
		}

		public static bool EnsureInitializedInCtorPrefix()
		{
			//No need to display this warning.
			return false;
		}

		[StaticConstructorOnStartup]
		public static class ModStartup
		{
			static ModStartup()
			{
				//Hugslibs-added defs will be queued up before this Static Constructor
				//So queue replace frame generation after that
				LongEventHandler.QueueLongEvent(() => ThingDefGenerator_ReplaceFrame.AddReplaceFrames(), null, true, null);
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