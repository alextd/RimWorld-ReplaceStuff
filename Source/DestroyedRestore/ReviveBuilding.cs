using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.DestroyedRestore
{
	[HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
	static class ReviveBuilding
	{
		//public void CompleteConstruction(Pawn worker)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase mb, ILGenerator ilg)
		{
			//ThingMaker.MakeThing(thingDef, base.Stuff);
			MethodInfo SpawnInfo = AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), 
				new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool)});
			MethodInfo CheckForRevivalInfo = AccessTools.Method(typeof(ReviveBuilding), nameof(CheckForRevival));

			int localMapIndex = mb.GetMethodBody().LocalVariables.FirstOrDefault(lv => lv.LocalType == typeof(Map)).LocalIndex;

			foreach(CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Call && i.operand == SpawnInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, CheckForRevivalInfo);//CheckForRevival(thingDef, base.Stuff, Frame, map)
				}
				else 
					yield return i;
			}
		}

		//public static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false)
		public static Thing CheckForRevival(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false)
		{
			Thing thing = GenSpawn.Spawn(newThing, loc, map, rot, wipeMode, respawningAfterLoad);
			DestroyedBuildings.ReviveBuilding(thing, loc, map);	//After spawn so SpawnSetup is called, comps created.
			return thing;
		}
	}
}
