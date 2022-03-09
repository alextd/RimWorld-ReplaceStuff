using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	public static class NewThingBlocksConstruction
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (!__result || constructible is Blueprint_Install) return;
			
			if (constructible.CanReplace(t))
				__result = false;
		}
	}
}
