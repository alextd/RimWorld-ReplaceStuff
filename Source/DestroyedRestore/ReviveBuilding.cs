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
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//ThingMaker.MakeThing(thingDef, base.Stuff);
			MethodInfo MakeThingInfo = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing));
			MethodInfo CheckForRevivalInfo = AccessTools.Method(typeof(ReviveBuilding), nameof(CheckForRevival));

			foreach(CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Call && i.operand == MakeThingInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Frame
					yield return new CodeInstruction(OpCodes.Call, CheckForRevivalInfo);//CheckForRevival(thingDef, base.Stuff, Frame)
				}
				else 
					yield return i;
			}
		}

		//public static Thing MakeThing(ThingDef def, ThingDef stuff = null)
		public static Thing CheckForRevival(ThingDef def, ThingDef stuff, Frame frame)
		{
			Log.Message($"Finding for {frame} at {frame.Position}");
			if(DestroyedBuildings.destroyedBuildings.TryGetValue(frame.Position, out Thing building))
			{
				Log.Message($"got {building}");
				building.stackCount = 1;
				DestroyedBuildings.destroyedBuildings.Remove(frame.Position);

				return building;
			}
			return ThingMaker.MakeThing(def, stuff);
		}
	}
}
