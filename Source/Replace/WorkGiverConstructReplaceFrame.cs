using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;

namespace Replace_Stuff
{
	[HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing")]
	class WorkGiverConstructReplaceFrame
	{
		//public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo MaterialsNeededInfo = AccessTools.Method(typeof(Frame), "MaterialsNeeded");

			MethodInfo ReplaceMaterialsNeededInfo = AccessTools.Method(typeof(WorkGiverConstructReplaceFrame), "MaterialsNeeded");

			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Callvirt && i.operand == MaterialsNeededInfo)
					i.operand = ReplaceMaterialsNeededInfo;
				yield return i;
			}
		}

		public static List<ThingCountClass> MaterialsNeeded(Frame frame)
		{
			//VIRTUAL virtual methods
			if (frame is ReplaceFrame replaceFrame)
				return replaceFrame.MaterialsNeeded();
			return frame.MaterialsNeeded();
		}
	}
}
