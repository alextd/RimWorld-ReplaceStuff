using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

//this code is straight-up from Erdelf, MIT license below
namespace StuffedReplacement
{
	[StaticConstructorOnStartup]
	public class StuffedReplacement
	{
		static StuffedReplacement()
		{
			HarmonyInstance harmony = HarmonyInstance.Create("rimworld.erdelf.stuffedReplacement");
			//harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintOver)), null, null, new HarmonyMethod(typeof(StuffedReplacement), nameof(CanPlaceBlueprintOverTranspiler)));
			harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt)), null, null, new HarmonyMethod(typeof(StuffedReplacement), nameof(CanPlaceBlueprintAtTranspiler)));
		}

		public static IEnumerable<CodeInstruction> CanPlaceBlueprintOverTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			MethodInfo isFrameInfo = AccessTools.Property(typeof(ThingDef), nameof(ThingDef.IsFrame)).GetGetMethod();

			List<CodeInstruction> instructionList = instructions.ToList();
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];
				yield return instruction;

				if (instruction.opcode == OpCodes.Brfalse && instructionList[i - 1].operand == isFrameInfo && instructionList[i - 2].opcode == OpCodes.Ldarg_1)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = instructionList[i + 1].labels };
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StuffedReplacement), nameof(StuffChecker)));
					Label label = il.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brfalse, label);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Ret);
					instructionList[i + 1].labels = new List<Label>() { label };
				}
			}
		}

		public static bool StuffChecker(BuildableDef newDef, ThingDef oldDef) => newDef == oldDef && newDef.MadeFromStuff;

		public static IEnumerable<CodeInstruction> CanPlaceBlueprintAtTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];
				yield return instruction;
				if (instruction.opcode == OpCodes.Bne_Un && (instructionList[i + 1].operand == "IdenticalThingExists" as object || instructionList[i + 4].operand as string == "IdenticalBlueprintExists"))
				{
					yield return new CodeInstruction(instructionList[i - 1]);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(ThingDef), nameof(ThingDef.MadeFromStuff)).GetGetMethod());
					//yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
					//Originally continuing with method, gonna just return true here: Identical thing exists means we certainly can build here
					Label label = il.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brfalse, label);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AcceptanceReport), "op_Implicit", new Type[] { typeof(bool) }));
					yield return new CodeInstruction(OpCodes.Ret);
					instructionList[i + 1].labels = new List<Label>() { label };
				}
			}
		}
	}
}


/*
 * MIT License

Copyright (c) 2018 erdelf

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/