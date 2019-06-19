using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace Replace_Stuff.BlueprintReplace
{
	[HarmonyPatch(typeof(Designator_Install), "CanDesignateCell")]
	//public override AcceptanceReport CanDesignateCell(IntVec3 c)
	static class DesignatorInstall
	{
		public static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
		{
			bool next = false;
			foreach(CodeInstruction i in instructions)
			{
				if(next)
				{
					next = false;
					yield return new CodeInstruction(OpCodes.Pop);
				}
				else
					yield return i;

				//Code checks !this.MiniToInstallOrBuildingToReinstall is MinifiedThing to set IdenticalThingExists, so let's ignore taht
				if (i.opcode == OpCodes.Isinst)
					next = true;
			}
		}

		public static void Prefix()
		{
			DesignatorContext.designating = true;
		}
		public static void Postfix()
		{
			DesignatorContext.designating = false;
		}
	}
}
