using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	/*
	 * //Redundant due to Deliver under rock allowing all frames
	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	class ReplaceFrameNoWipe
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (!__result) return;
			
			if(newEntDef is ThingDef newDef && newDef.thingClass == typeof(ReplaceFrame) && newDef.entityDefToBuild == oldEntDef)
				__result = false;
		}
	}
	*/

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class ReplaceFrameNoBlock
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (!__result) return;

			if (constructible is ReplaceFrame frame) __result = false;
		}
	}
}
