using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.PlaceBridges
{
	[HarmonyPatch(typeof(Blueprint), nameof(Blueprint.DeSpawn))]
	class CancelBridgeCancelsBlueprintAbove
	{
		//public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		public static void Prefix(Blueprint __instance, DestroyMode mode)
		{
			if (__instance.def.entityDefToBuild == TerrainDefOf.Bridge 
				&& mode != DestroyMode.Vanish)
			{
				__instance.Map.thingGrid.ThingsListAtFast(__instance.Position)
					.Where(t => t is Blueprint && t != __instance).ToList()
					.Do(t => t.Destroy());
			}
		}
	}
}