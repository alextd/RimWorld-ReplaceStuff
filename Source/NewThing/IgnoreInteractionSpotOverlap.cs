/* Weird Harmony problem patching PlaceWorker.AllowsPlacing. TODO.
 * 
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Replace_Stuff.NewThing
{
	[HarmonyPatch(typeof(PlaceWorker_PreventInteractionSpotOverlap), nameof(PlaceWorker_PreventInteractionSpotOverlap.AllowsPlacing))]
	public static class IgnoreInteractionSpotOverlap
	{
		//public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thingToPlace = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instList = instructions.ToList();

			for (int i=0;i<instList.Count;i++)
			{
				CodeInstruction inst = instList[i];

				yield return inst;

				//Find (thing != thingToIgnore) and break to same place when newthing can replace old thing
				//IL_007e: ldloc.s thing
				//IL_0080: ldarg.s thingToIgnore
				//IL_0082: beq IL_0122
				if (inst.opcode == OpCodes.Beq
					//Harmony breaks here with Ldarg_S cast to int exception && instList[i - 1].IsLdarg(5)) // thingToIgnore
					&& instList[i - 1].opcode == OpCodes.Ldarg_S && instList[i - 1].OperandIs(5) // thingToIgnore
					&& instList[i - 2].IsLdloc()) // thing (in list at pos)
				{
					yield return instList[i - 2];//thing (adj to interaction spot)
					yield return new CodeInstruction(OpCodes.Ldarg_1);// checkingDef
					yield return new CodeInstruction(OpCodes.Ldarg_2);// loc
					yield return new CodeInstruction(OpCodes.Ldarg_3);// rot
					yield return new CodeInstruction(OpCodes.Ldarg, 4);// map
					MethodInfo SkipInfo = AccessTools.Method(typeof(IgnoreInteractionSpotOverlap), nameof(SkipForNewThing));
					yield return new CodeInstruction(OpCodes.Call, SkipInfo);
					yield return new CodeInstruction(OpCodes.Brtrue, inst.operand);//Same as if(thing != thingToIgnore)
				}
			}
		}

		public static bool SkipForNewThing(Thing oldThing, BuildableDef checkingDef, IntVec3 pos, Rot4 rot, Map map)
		{
			//If the oldThing overlaps the new thing,
			//and the new thing CanReplaceOldThing, then skip it
			return checkingDef is ThingDef newDef &&
				newDef.IsNewThingReplacement(pos, rot, map, out Thing foundThing) &&
				foundThing == oldThing;
		}
	}
}

*/