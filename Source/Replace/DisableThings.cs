using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using Verse.AI;
using RimWorld;

namespace Replace_Stuff.Replace
{
	static class DisableThing
	{
		public static bool IsReplacing(Thing thing)
		{
			return thing != null && thing.Spawned &&
				thing.Position.GetThingList(thing.Map)
				.Any(t => t is ReplaceFrame f && f.oldThing == thing && f.workDone > 0);
		}
	}

	[HarmonyPatch(typeof(Building_TurretGun), "TryStartShootSomething")]
	class DisableTurret
	{
		//protected void TryStartShootSomething(bool canBeginBurstImmediately)
		public static bool Prefix(Building_TurretGun __instance)
		{
			return !DisableThing.IsReplacing(__instance);//__instance.ResetCurrentTarget();
		}
	}

	[HarmonyPatch(typeof(Building_WorkTable), "UsableForBillsAfterFueling")]
	class DisableWorkbench
	{
		//public virtual bool UsableNow
		public static void Postfix(ref bool __result, Building_WorkTable __instance)
		{
			if (DisableThing.IsReplacing(__instance))
			{
				__result = false;
				JobFailReason.Is("stuff being replaced");
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
	public static class DisableBed
	{
		//protected override Job TryGiveJob(Pawn pawn)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo FindBedForInfo = AccessTools.Method(typeof(RestUtility), "FindBedFor", new Type[] { typeof(Pawn)});

			MethodInfo NullifyReplacingBedInfo = AccessTools.Method(typeof(DisableBed), nameof(DisableBed.NullifyBed));

			foreach(CodeInstruction i in instructions)
			{
				yield return i;

				if(i.opcode == OpCodes.Call && i.operand == FindBedForInfo)
				{
					//Ideally filter out the bed in IsValidBedFor,
					//but then FindBedFor would skip your owned bed, find another bed and claim it
					//so this is simplest, just sleep on the ground for tonight if your bed is being worked on
					yield return new CodeInstruction(OpCodes.Call, NullifyReplacingBedInfo);
				}
			}
		}

		public static Building_Bed NullifyBed(Building_Bed bed)
		{
			return DisableThing.IsReplacing(bed) ? null : bed;
		}
	}

}
