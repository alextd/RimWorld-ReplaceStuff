using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.PlaceBridges
{
	public static class CancelAboveBridges
	{
		public static void CancelAbove(BuildableDef defToBuild, DestroyMode mode, Map map, IntVec3 pos)
		{
			if (defToBuild == TerrainDefOf.Bridge
				&& mode != DestroyMode.Vanish)
			{
				List<Thing> toKill = new List<Thing>();
				foreach(Thing thing in map.thingGrid.ThingsListAtFast(pos))
				{
					if (thing is Blueprint bp && bp.def.entityDefToBuild != TerrainDefOf.Bridge)
						toKill.Add(thing);
					if (thing is Frame fr && fr.def.entityDefToBuild != TerrainDefOf.Bridge)
						toKill.Add(thing);
				}
				toKill.Do(t => t.Destroy(DestroyMode.Refund));
			}
		}
	}

	[HarmonyPatch(typeof(Blueprint), nameof(Blueprint.DeSpawn))]
	public static class CancelBlueprint
	{ 
		//public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		public static void Prefix(Blueprint __instance, DestroyMode mode)
		{
			CancelAboveBridges.CancelAbove(__instance.def.entityDefToBuild, mode, __instance.Map, __instance.Position);
		}
	}

	[HarmonyPatch(typeof(Frame), nameof(Frame.Destroy))]
	public static class CancelFrame
	{
		//publicpublic override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		public static void Prefix(Frame __instance, DestroyMode mode)
		{
			CancelAboveBridges.CancelAbove(__instance.def.entityDefToBuild, mode, __instance.Map, __instance.Position);
		}
	}
	
}