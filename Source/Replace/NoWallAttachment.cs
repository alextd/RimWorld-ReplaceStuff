using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Replace_Stuff
{
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.GetWallAttachedTo), [typeof(IntVec3), typeof(Rot4), typeof(Map)])]
	public static class NoWallAttachment
	{
		//public static Thing GetWallAttachedTo(IntVec3 pos, Rot4 rot, Map map)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// In the loop: foreach (Thing thing in c.GetThingList(map))
			// insert:
			// if(thing is ReplaceFrame) continue;

			// (This is probably redundant because the wall under the replace frame will probably always be checked first and returned.)


			// The first br should branch to the entry point for the loop, keep that label to continue to
			Label continueLabel = (Label)instructions.First(ci => ci.opcode == OpCodes.Br_S).operand;

			FieldInfo defInfo = AccessTools.Field(typeof(Thing), nameof(Thing.def));

			List<CodeInstruction> insts = instructions.ToList();
			for (int i = 0; i < insts.Count; i++)
			{
				CodeInstruction inst = insts[i];

				yield return inst;

				// Before we get the thing.def, use the thing:
				if (i + 1 > insts.Count && insts[i + 1].LoadsField(defInfo))
				{
					Log.Message($"TPILE!");
					// stack has: Thing thing from the list
					yield return new CodeInstruction(OpCodes.Isinst, typeof(ReplaceFrame));// thing == typeof(ReplaceFrame)
					yield return new CodeInstruction(OpCodes.Brtrue_S, continueLabel);// if(thing == typeof(ReplaceFrame)) continue;


					// Call ldlocal for Thing again to replace what was there (with, maybe, no labels...)
					yield return new CodeInstruction(inst.opcode, inst.operand);
				}
			}
		}
	}


	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.GetAttachedBuildings))]
	public static class NoAttachedBuildings
	{
		private static List<Thing> emptyList = [];

		//public static List<Thing> GetAttachedBuildings(Thing thing)
		public static bool Prefix(Thing thing, ref List<Thing> __result)
		{
			if (thing is ReplaceFrame)
			{
				__result = emptyList;
				return false;
			}
			return true;
		}
	}
}

