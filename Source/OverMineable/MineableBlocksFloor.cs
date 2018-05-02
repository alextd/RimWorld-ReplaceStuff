using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class MineableBlocksFloor
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (__result) return;

			BuildableDef thingDef = constructible is Blueprint ? constructible.def.entityDefToBuild
				: constructible is Frame ? constructible.def.entityDefToBuild
				: constructible.def;

			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if (thingDef is TerrainDef && t.def.mineable)
				__result = true;
		}
	}
}
