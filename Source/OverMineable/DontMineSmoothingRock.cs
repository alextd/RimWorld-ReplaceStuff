using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Verse.AI;
using Harmony;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(GenConstruct), "HandleBlockingThingJob")]
	static class DontMineSmoothingRock
	{
		//public static Job HandleBlockingThingJob(Thing constructible, Pawn worker, bool forced = false)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			FieldInfo mineableInfo = AccessTools.Field(typeof(ThingDef), "mineable");

			MethodInfo ToBeSmoothedInfo = AccessTools.Method(typeof(DontMineSmoothingRock), nameof(DontMineSmoothingRock.ToBeSmoothed));
			MethodInfo SmoothItJobInfo = AccessTools.Method(typeof(DontMineSmoothingRock), nameof(DontMineSmoothingRock.SmoothItJob));

			List<CodeInstruction> list = instructions.ToList();
			yield return list[0];
			for (int i = 1; i < list.Count; i++)
			{
				yield return list[i];
				if (list[i-1].opcode == OpCodes.Ldfld && list[i-1].operand == mineableInfo)
				{
					Label otherwise = iLGenerator.DefineLabel();
					list[i + 1].labels.Add(otherwise);

					CodeInstruction workerInst = new CodeInstruction(OpCodes.Ldarg_1);//worker
					CodeInstruction thingInst = new CodeInstruction(list[i - 3].opcode);//thing; Ld_loc_0

					yield return workerInst;
					yield return thingInst;
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Thing constructible
					yield return new CodeInstruction(OpCodes.Call, ToBeSmoothedInfo);// ToBeSmoothed(worker, thing, constructible)
					yield return new CodeInstruction(OpCodes.Brfalse, otherwise);//if(ToBeSmoothed){...}

					yield return workerInst;
					yield return thingInst;
					yield return new CodeInstruction(OpCodes.Ldarg_2);//forced
					yield return new CodeInstruction(OpCodes.Call, SmoothItJobInfo);
					yield return new CodeInstruction(OpCodes.Ret);//return SmoothItJob(worker,thing,forced)
				}
			}
		}

		public static bool ToBeSmoothed(Pawn worker, Thing thing, Thing constructible)
		{
			return !GenSpawn.SpawningWipes(GenConstruct.BuiltDefOf( constructible.def), thing.def.building?.smoothedThing) &&
				thing.Map.edificeGrid[thing.Position] == thing &&
				worker.Map.designationManager.DesignationAt(thing.Position, DesignationDefOf.SmoothWall) != null;
		}

		public static Job SmoothItJob(Pawn worker, Thing thing, bool forced)
		{
			if (worker.story != null && worker.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
			{
				JobFailReason.Is("incapable of smoothing");
				return null;
			}
			if (worker.CanReserveAndReach(thing, PathEndMode.Touch, worker.NormalMaxDanger(), 1, -1, null, forced) && 
				worker.CanReserve(thing.Position, 1, -1, null, forced))
			{
				return new Job(JobDefOf.SmoothWall, thing)
				{
					ignoreDesignations = true
				};
			}
			return null;
		}
	}

}
