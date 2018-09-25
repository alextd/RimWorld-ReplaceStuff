using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[]
	{	typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool)})]
	class TransferSettings
	{
		//public static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false)
		public static void Prefix(Thing newThing, IntVec3 loc, Map map)
		{
			if (newThing.def.IsNewThingReplacement(loc, map, out Thing oldThing))
				newThing.FinalizeNewThingReplace(oldThing);
		}
	}
}
