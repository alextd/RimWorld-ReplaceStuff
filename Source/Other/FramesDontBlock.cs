using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.Other
{
	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class FramesDontBlock
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo FillageInfo = AccessTools.Property(typeof(ThingDef), "Fillage").GetGetMethod();

			MethodInfo AndNotFrameInfo = AccessTools.Method(typeof(FramesDontBlock), nameof(FramesDontBlock.AndNotFrame));

			//The Fillage of frames shouldn't block construction;
			//A power conduit being built over a wall frame is fine, it doesn't clearBuildingArea,
			//but the same wall built over a power conduit frame would deconstruct that frame since it has FillCategory.Partial
			//So set that result to false if the blocking thing is a frame
			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count(); i++)
			{
				if (instList[i].opcode == OpCodes.Blt && //if(Fillage >= Partial)
					instList[i - 1].opcode == OpCodes.Ldc_I4_1 && //FillCategory.Partial
					instList[i - 2].opcode == OpCodes.Callvirt && instList[i - 2].operand == FillageInfo) //Fillage
				{
					yield return new CodeInstruction(OpCodes.Ldarg_1);//t
					yield return new CodeInstruction(OpCodes.Call, AndNotFrameInfo);//AndNotFrame(Fillage,  Partial, t)
					yield return new CodeInstruction(OpCodes.Brfalse, instList[i].operand);//No longer Blt, Brfalse so return from AndNotFrame matches BlocksConstruction
				}

				else
					yield return instList[i];
			}
		}

		public static bool AndNotFrame(FillCategory fillage, FillCategory greaterThanThis, Thing t)
		{
			return t is Frame ? false : fillage >= greaterThanThis;
		}
	}
}
