using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.DestroyedRestore
{
	[HarmonyPatch(typeof(Blueprint), nameof(Blueprint.DeSpawn))]
	class BlueprintRemoval
	{
		//public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		public static void Prefix(Blueprint __instance, DestroyMode mode)
		{
			if (mode != DestroyMode.Vanish)
				DestroyedBuildings.RemoveAt(__instance.Position, __instance.Map);
		}
	}

	[HarmonyPatch(typeof(Frame), nameof(Blueprint.Destroy))]
	class FrameRemoval
	{
		//public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		public static void Prefix(Frame __instance, DestroyMode mode)
		{
			if (mode != DestroyMode.Vanish && mode != DestroyMode.FailConstruction && mode != DestroyMode.KillFinalize)
				DestroyedBuildings.RemoveAt(__instance.Position, __instance.Map);
		}
	}
}
