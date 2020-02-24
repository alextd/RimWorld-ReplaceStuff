/* Weird Harmony problem patching PlaceWorker.AllowsPlacing. TODO.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.CoolersOverWalls
{
	[HarmonyPatch(typeof(PlaceWorker_Cooler), "AllowsPlacing")]
	class AllowBuildPlugged
	{
		//public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null)
		public static bool Prefix(ref AcceptanceReport __result)
		{
			__result = true;
			return false;
		}
	}
	[HarmonyPatch(typeof(PlaceWorker_Vent), "AllowsPlacing")]
	class AllowBuildPlugged_Vent
	{
		//public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null)
		public static bool Prefix(ref AcceptanceReport __result)
		{
			__result = true;
			return false;
		}
	}
}
*/