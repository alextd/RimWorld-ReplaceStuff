using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Harmony;
using RimWorld;

namespace Replace_Stuff.Replace
{
	[StaticConstructorOnStartup]
	public static class JobDriverFinishReplaceFrame
	{
		static JobDriverFinishReplaceFrame()
		{
			HarmonyInstance harmony = Mod.Harmony();
			{
				Type nestedType = AccessTools.Inner(typeof(JobDriver_ConstructFinishFrame), "<MakeNewToils>c__Iterator0");
				nestedType = AccessTools.Inner(nestedType, "<MakeNewToils>c__AnonStorey1");
				harmony.Patch(AccessTools.Method(nestedType, "<>m__1"),
					null, null, new HarmonyMethod(typeof(JobDriverFinishReplaceFrame), "Transpiler"));
			}
		}

		//MakeNewToils, Toil, tickAction:
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//        IL_00a2: call         instance void Verse.AI.JobDriver::ReadyForNextToil()
			MethodInfo ReadyForNextToilInfo = AccessTools.Method(typeof(JobDriver), "ReadyForNextToil");

			MethodInfo ReplaceReadyForNextToilInfo = AccessTools.Method(typeof(JobDriver), "ReplaceReadyForNextToil");
			
			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Call && i.operand == ReadyForNextToilInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1);//frame
					yield return new CodeInstruction(OpCodes.Call, ReplaceReadyForNextToilInfo);
				}
				else
					yield return i;
			}
		}

		public static void ReplaceReadyForNextToil(JobDriver driver, Frame frame)
		{
			if (frame is ReplaceFrame)
				return;
			driver.ReadyForNextToil();
		}
	}
}
