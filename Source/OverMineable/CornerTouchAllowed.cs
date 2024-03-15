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


	//Make sure mineable drop is in miner's region so it's not blocked off 
	[HarmonyPatch(typeof(Mineable), "TrySpawnYield", [typeof(Map), typeof(bool), typeof(Pawn)])]
	public static class DropOnPawn
	{
		//private void TrySpawnYield(Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//There's two overloads for TryPlaceThing, one just literally calls the other and tosses out the (out Thing)..
			//I don't want to write out all the generic params here...
			//so just find the one with less parameters.
			MethodInfo TryPlaceThingInfo = typeof(GenPlace).GetMethods(AccessTools.all)
				.Where(mi => mi.Name == "TryPlaceThing")
				.MinBy(mi => mi.GetParameters().Length);

			//Same thing put takes a pawn to validate room.
			MethodInfo TryPlaceThingInSameRoomInfo = AccessTools.Method(typeof(DropOnPawn), nameof(TryPlaceThingInSameRoom));

			foreach(var inst in instructions)
			{
				if(inst.Calls(TryPlaceThingInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_S, 3);//Pawn pawn
					yield return new CodeInstruction(OpCodes.Call, TryPlaceThingInSameRoomInfo);
				}
				else yield return inst;
			}
		}

		//public static bool TryPlaceThing(Thing thing, IntVec3 center, Map map, ThingPlaceMode mode, Action<Thing, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null, Rot4 rot = default(Rot4))
		public static bool TryPlaceThingInSameRoom(Thing thing, IntVec3 center, Map map, ThingPlaceMode mode, Action<Thing, int> placedAction, Predicate<IntVec3> nearPlaceValidator, Rot4 rot, Pawn miner)
		{
			//Given that nearPlaceValidator will be null as it's a call from TrySpawnYield:
			
			//For godmode mining there is no pawn
			if (miner != null)
				//(Good luck setting this up in ILCode so I'll do it here)
				nearPlaceValidator = pos => pos.GetRoom(miner.Map) == miner.GetRoom();

			return GenPlace.TryPlaceThing(thing, center, map, mode, placedAction, nearPlaceValidator, rot);
		}
	}
}
