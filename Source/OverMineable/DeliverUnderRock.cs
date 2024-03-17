using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using TD.Utilities;

namespace Replace_Stuff.OverMineable
{

	// In CanConstruct, skip FirstBlockingThing if it's just a haul job
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanConstruct), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef)])]
	public static class DeliverUnderRock
	{
		//public static bool CanConstruct(Thing t, Pawn p, bool checkSkills = true, bool forced = false, JobDef jobForReservation = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//Replace
			MethodInfo FirstBlockingThingInfo = AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.FirstBlockingThing));

			//With
			MethodInfo FirstBlockingThingNotHaulInfo = AccessTools.Method(typeof(DeliverUnderRock), nameof(FirstBlockingThingNotHaul));

			foreach (var inst in instructions)
			{
				if(inst.Calls(FirstBlockingThingInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_S, 4);//JobDef jobForReservation
					yield return new CodeInstruction(OpCodes.Call, FirstBlockingThingNotHaulInfo);//JobDef jobForReservation
				}
				else
				//JobDef jobForReservation
					yield return inst;
			}
		}

		//public static Thing FirstBlockingThing(Thing constructible, Pawn pawnToIgnore)
		public static Thing FirstBlockingThingNotHaul(Thing constructible, Pawn pawnToIgnore, JobDef jobForReservation)
		{
			if (jobForReservation == JobDefOf.HaulToContainer)
				return null;

			return GenConstruct.FirstBlockingThing(constructible, pawnToIgnore);
		}
	}
	

	//Haul job needs to deliver to frames even if construction blocked
	/*
	 // 1.5: JumpToCarryToNextContainerIfPossiblenow calls TryGetNextDestinationFromQueue
	// but TryGetNextDestinationFromQueue no longer calls CanConstruct
	// Presumably that is now checked when the queue was created.
	[StaticConstructorOnStartup]
	public static class HaulToBlueprintUnderRock
	{
		static HaulToBlueprintUnderRock()
		{
			HarmonyMethod transpiler = new HarmonyMethod(typeof(DeliverUnderRock), nameof(DeliverUnderRock.Transpiler));
			Harmony harmony = new Harmony("Uuugggg.rimworld.Replace_Stuff.main");

			Predicate<MethodInfo> check = m => m.Name.Contains("JumpToCarryToNextContainerIfPossible");

			harmony.PatchGeneratedMethod(typeof(Toils_Haul), check, transpiler: transpiler);
		}
	}*/

	//Blueprint can become a frame even if final thing would be blocked
	[HarmonyPatch(typeof(Blueprint), "TryReplaceWithSolidThing")]
	public static class BlueprintToFrameUnderRock
	{
		//public virtual bool TryReplaceWithSolidThing(Pawn workerPawn, out Thing createdThing, out bool jobEnded)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//Replace
			MethodInfo FirstBlockingThingInfo = AccessTools.Method(typeof(GenConstruct), "FirstBlockingThing");

			List<CodeInstruction> list = instructions.ToList();
			for(int i=0;i< list.Count; i++)
			{
				CodeInstruction inst = list[i];
				yield return inst;
				if (inst.Calls(FirstBlockingThingInfo))
				{
					//Frame can be made
					yield return new CodeInstruction(OpCodes.Pop);
					i++;
					yield return new CodeInstruction(OpCodes.Br, list[i].operand) { labels = list[i].labels };
				}
			}
		}
	}
	
	//Frames can overlap anything. That shouldn't create a problem, right?
	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	class NoWipeFrame
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (!__result) return;

			if (newEntDef is ThingDef newDef && newDef.IsFrame)
				__result = false;
		}
	}

	//It did create a problem! Putting two edifices in same spot is a problem
	//So frames aren't edifices... that shouldn't create a problem, right?
	[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing")]
	public static class FramesArentEdifices
	{
		//private static ThingDef NewFrameDef_Thing(ThingDef def)
		public static void Postfix(ThingDef __result)
		{
			__result.building.isEdifice = false;
		}
	}

	//It did create a problem! Frames counting as edifices meant they blocked blueprints
	//So frames are edifices for blueprint consideration... that shouldn't create a problem, right?
	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver")]
	public static class FramesAreEdificesInSomeCases
	{
		//public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(EdificeUtility), "IsEdifice"),
				AccessTools.Method(typeof(FramesAreEdificesInSomeCases), "IsEdificeOrFrame"));
		}

		public static bool IsEdificeOrFrame(BuildableDef def)
		{
			return def.IsEdifice() || (def is ThingDef thingDef && thingDef.IsFrame);
		}
	}

	[HarmonyPatch(typeof(GenConstruct))]//, "CanPlaceBlueprintOver.IsEdificeOverNonEdifice")]
	public static class FramesAreEdificesInSomeCasesAndAlsoInTheCompilerGeneratedMethod
	{
		public static MethodInfo TargetMethod() =>
			// "IsEdificeOverNonEdifice" Isn't compiled away? Okay I'll use that
			AccessTools.FirstMethod(typeof(GenConstruct), method => method.Name.Contains("IsEdificeOverNonEdifice"));

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => 
			FramesAreEdificesInSomeCases.Transpiler(instructions);
	}
}
