using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(ThingSelectionUtility), "SelectableByMapClick")]
	class FoggedSelectable
	{
		//public static bool SelectableByMapClick(Thing t)
		public static bool Prefix(ref bool __result, Thing t)
		{
			if (t.def.IsBlueprint) // && t.def.selectable && t.Spawned //redundant checks
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
