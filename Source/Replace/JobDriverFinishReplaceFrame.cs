using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
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
			MethodInfo CompleteConstructionInfo = AccessTools.Method(typeof(Frame), "CompleteConstruction");
			MethodInfo WorkToMakeInfo = AccessTools.Property(typeof(Frame), "WorkToMake").GetGetMethod();

			MethodInfo IsReplaceFrameInfo = AccessTools.Method(typeof(JobDriverFinishReplaceFrame), nameof(IsReplaceFrame));
			MethodInfo ReplaceCompleteConstructionInfo = AccessTools.Method(typeof(JobDriverFinishReplaceFrame), nameof(CompleteConstruction));
			MethodInfo ReplaceWorkToMakeInfo = AccessTools.Method(typeof(JobDriverFinishReplaceFrame), nameof(ReplaceWorkToMake));

			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Callvirt && i.operand == CompleteConstructionInfo)
					i.operand = ReplaceCompleteConstructionInfo;
				if (i.opcode == OpCodes.Callvirt && i.operand == WorkToMakeInfo)
					i.operand = ReplaceWorkToMakeInfo;
				yield return i;
				if (i.opcode == OpCodes.Bne_Un)
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1);//frame
					yield return new CodeInstruction(OpCodes.Call, IsReplaceFrameInfo);
					yield return new CodeInstruction(OpCodes.Brtrue, i.operand);
				}
			}
		}

		public static bool IsReplaceFrame(Frame f) => f is ReplaceFrame;

		public static void CompleteConstruction(Frame frame, Pawn worker)
		{
			//VIRTUAL virtual methods
			if (frame is ReplaceFrame replaceFrame)
				replaceFrame.CompleteConstruction(worker);
			else
				frame.CompleteConstruction(worker);
		}

		public static float ReplaceWorkToMake(Frame frame)
		{
			//VIRTUAL virtual methods
			if (frame is ReplaceFrame replaceFrame)
				return replaceFrame.WorkToMake;
			else
				return frame.WorkToMake;
		}
	}
}
