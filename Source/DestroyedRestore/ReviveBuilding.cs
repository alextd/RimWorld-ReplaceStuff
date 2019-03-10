using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace Replace_Stuff.DestroyedRestore
{
	[HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
	static class ReviveBuilding
	{
		//public void CompleteConstruction(Pawn worker)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase mb, ILGenerator ilg)
		{
			//ThingMaker.MakeThing(thingDef, base.Stuff);
			MethodInfo MakeThingInfo = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing));
			MethodInfo CheckForRevivalInfo = AccessTools.Method(typeof(ReviveBuilding), nameof(CheckForRevival));

			int localMapIndex = mb.GetMethodBody().LocalVariables.FirstOrDefault(lv => lv.LocalType == typeof(Map)).LocalIndex;

			foreach(CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Call && i.operand == MakeThingInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Frame
					yield return new CodeInstruction(OpCodes.Ldloc_S, localMapIndex);//Map map;
					yield return new CodeInstruction(OpCodes.Call, CheckForRevivalInfo);//CheckForRevival(thingDef, base.Stuff, Frame, map)
				}
				else 
					yield return i;
			}
		}

		//public static Thing MakeThing(ThingDef def, ThingDef stuff = null)
		public static Thing CheckForRevival(ThingDef def, ThingDef stuff, Frame frame, Map map)
		{
			return DestroyedBuildings.FindBuilding(frame.Position, map) ?? ThingMaker.MakeThing(def, stuff);
		}
	}
}
