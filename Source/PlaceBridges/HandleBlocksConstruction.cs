using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.PlaceBridges
{
	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class HandleBlocksConstruction
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static bool Prefix(ref bool __result, Thing constructible, Thing t)
		{
			if (constructible is Frame newFrame && newFrame.def.entityDefToBuild == TerrainDefOf.Bridge) return true;

			if(t is Frame frame && frame.def.entityDefToBuild == TerrainDefOf.Bridge)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
