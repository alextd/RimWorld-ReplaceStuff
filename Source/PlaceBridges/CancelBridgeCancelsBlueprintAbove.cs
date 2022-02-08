using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.PlaceBridges
{
	public static class CancelAboveBridges
	{
		public static void CancelAbove(BuildableDef defToBuild, DestroyMode mode, Map map, IntVec3 pos)
		{
			if (defToBuild == TerrainDefOf.Bridge
				&& mode != DestroyMode.Vanish && mode != DestroyMode.FailConstruction)
			{
				List<Thing> toKill = new List<Thing>();
				foreach(Thing thing in map.thingGrid.ThingsListAtFast(pos))
				{
					//this sorta assumes the thing is not actually built, vanilla would handle that.
					if (thing is Blueprint bp && bp.def.entityDefToBuild != TerrainDefOf.Bridge)
						toKill.Add(thing);
					if (thing is Frame fr && fr.def.entityDefToBuild != TerrainDefOf.Bridge)
						toKill.Add(thing);
				}
				//Kill unless it's already killed or it's IsSelected.
				//IsSelected probably not the best since possible non-cancel destruction would keep the thing above, but what are the chances of that?
				toKill.Do(t => { if (!t.Destroyed && !Find.Selector.IsSelected(t)) t.Destroy(DestroyMode.Refund); });
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

	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.RemoveTopLayer))]
	public static class DestroyedTerrain
	{
		//public void RemoveTopLayer(IntVec3 c, bool doLeavings = true)
		public static void Prefix(TerrainGrid __instance, IntVec3 c, Map ___map)
		{
			CancelAboveBridges.CancelAbove(__instance.TerrainAt(c), DestroyMode.KillFinalize, ___map, c);
		}
	}
}