using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	/* 
	 * 
	 * since at least 1.5, this method doesn't check Traversability.Impassable. Is it supposed to be CanPlaceBlueprintOver?
	 * Was canPlaceOverImpassablePlant added that made this obsolete?
	 * What did this even do though heh
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
	static class InteractionSpot
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo AndIsNotRockInfo = AccessTools.Method(typeof(InteractionSpot), nameof(InteractionSpot.AndIsNotRock));

			bool doneOnce = false;
			List<CodeInstruction> list = instructions.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				CodeInstruction inst = list[i];
				yield return inst;
				//if (thingList2[index].def.passability == Traversability.Impassable)
				//if (thingList2[index].def.passability == Traversability.Impassable && thingList2[index] is not rock)
				if (inst.LoadsConstant(2) && !doneOnce)//Traversability.Impassable
				{
					Log.Message("Did this run?");
					doneOnce = true;

					yield return new CodeInstruction(list[i - 5].opcode, list[i - 5].operand);//thingList2, but no labels
					yield return list[i - 4];//index
					yield return list[i - 3];//thingList[index]
					yield return new CodeInstruction(OpCodes.Call, AndIsNotRockInfo);
					i++;
					yield return new CodeInstruction(OpCodes.Brtrue, list[i].operand) { labels = list[i].labels };
				}
			}
		}

		public static bool AndIsNotRock(Traversability passibility, Traversability check, Thing blocker)
		{
			return passibility == check && !blocker.IsMineableRock();
		}
	}
	*/
}
