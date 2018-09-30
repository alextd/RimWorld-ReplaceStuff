using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
	static class InteractionSpot
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo AndIsNotRockInfo = AccessTools.Method(typeof(InteractionSpot), nameof(InteractionSpot.AndIsNotRock));
			Log.Message($"AndIsNotRockInfo is {AndIsNotRockInfo}");

			bool doneOnce = false;
			List<CodeInstruction> list = instructions.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				CodeInstruction inst = list[i];
				yield return inst;
				//if (thingList2[index].def.passability == Traversability.Impassable)
				//if (thingList2[index].def.passability == Traversability.Impassable && thingList2[index] is not rock)
				if (inst.opcode == OpCodes.Ldc_I4_2 && !doneOnce)//Traversability.Impassable
				{
					doneOnce = true;

					yield return new CodeInstruction(list[i - 5].opcode, list[i - 5].operand);//thingList2, but no labels
					yield return list[i - 4];//index
					yield return list[i - 3];//thingList[index]
					yield return new CodeInstruction(OpCodes.Call, AndIsNotRockInfo);
					list[++i].opcode = OpCodes.Brfalse;
					yield return list[i];
				}
			}
		}

		public static bool AndIsNotRock(Traversability passibility, Traversability check, Thing blocker)
		{
			return passibility == check && !blocker.IsMineableRock();
		}
	}
}
