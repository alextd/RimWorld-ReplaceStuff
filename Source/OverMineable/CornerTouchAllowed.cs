using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace Replace_Stuff.OverMineable
{
	//Include blueprints and frames in IsCornerTouchAllowed
	//(Frames were included, but ReplaceStuff removes their 'edifice' status so they need to be re-included)
	[HarmonyPatch(typeof(TouchPathEndModeUtility), nameof(TouchPathEndModeUtility.IsCornerTouchAllowed))]
	public static class IsCornerTouchAllowed
	{
		//public static bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, PathingContext pc)
		public static void Postfix(ref bool __result, int cornerX, int cornerZ, PathingContext pc)
		{
			if (!__result)
			{
				foreach (Thing thing in pc.map.thingGrid.ThingsListAtFast(new IntVec3(cornerX, 0, cornerZ)))
					if (thing is Blueprint || thing is Frame && TouchPathEndModeUtility.MakesOccupiedCellsAlwaysReachableDiagonally(thing.def))
					{
						__result = true;
						return;
					}
			}
		}
	}

	//remove rock from rejection of CanInteractThroughCorners
	[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.CanInteractThroughCorners), MethodType.Getter)]
	public static class CanInteractThroughCorners
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo buildingInfo = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.building));

			List<CodeInstruction> instList = instructions.ToList();
			for(int i=0;i<instList.Count();i++)
			{
				CodeInstruction inst = instList[i];

				if(inst.LoadsField(buildingInfo))
				{
					//IL_0015: ldarg.0      // this
					//IL_0016: ldfld        class RimWorld.BuildingProperties Verse.ThingDef::building

					//replace the this.building code with the end return true:

					instList[i - 1].opcode = OpCodes.Ldc_I4_1;//preserve label here
					instList[i] = new CodeInstruction(OpCodes.Ret);

					//chop off rest of the code that checks rock and smooth ezpz, we can corner touch rocks now!
					return instList.Take(i + 1);
				}
			}
			Verse.Log.Warning("Replace Stuff failed to patch CanInteractThroughCorners");
			return instructions;
		}
	}
}
