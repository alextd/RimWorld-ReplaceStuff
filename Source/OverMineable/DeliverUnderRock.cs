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
	//Still deliver resources to construction-blocked frames
	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "IsNewValidNearbyNeeder")]
	public static class DeliverUnderRock
	{
		//private bool IsNewValidNearbyNeeder(Thing t, HashSet<Thing> nearbyNeeders, IConstructible constructible, Pawn pawn)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//Replace
			MethodInfo CanConstructInfo = AccessTools.Method(typeof(GenConstruct), "CanConstruct");

			//With
			MethodInfo CanDeliverInfo = AccessTools.Method(typeof(DeliverUnderRock), "CanDeliver");

			return Harmony.Transpilers.MethodReplacer(instructions, CanConstructInfo, CanDeliverInfo);
		}

		//public static bool CanConstruct(Thing t, Pawn p, bool checkConstructionSkill = true, bool forced = false)
		//but with less restrictions (remove GenConstruct.FirstBlockingThing)
		public static bool CanDeliver(Thing t, Pawn p, bool checkConstructionSkill, bool forced)
		{
			if (!p.CanReserveAndReach(t, PathEndMode.Touch, p.NormalMaxDanger()))
			{
				return false;
			}
			if (t.IsBurning())
			{
				return false;
			}
			return true;
		}
	}

	//Haul job needs to deliver to frames even if construction blocked
	[StaticConstructorOnStartup]
	public static class HaulToBlueprintUnderRock
	{
		static HaulToBlueprintUnderRock()
		{
			HarmonyMethod transpiler = new HarmonyMethod(typeof(DeliverUnderRock), nameof(DeliverUnderRock.Transpiler));
			Harmony harmony = new Harmony("Uuugggg.rimworld.Replace_Stuff.main");

			MethodInfo CanConstructInfo = AccessTools.Method(typeof(GenConstruct), "CanConstruct");
			Predicate<MethodInfo> check = delegate (MethodInfo method)
			{
				DynamicMethod dm = DynamicTools.CreateDynamicMethod(method, "-unused");

				return (Harmony.ILCopying.MethodBodyReader.GetInstructions(dm.GetILGenerator(), method).
					Any(ilcode => ilcode.operand.Equals(CanConstructInfo)));
			};

			harmony.PatchGeneratedMethod(typeof(Toils_Haul), check, transpiler: transpiler);
		}
	}

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
				if (inst.opcode == OpCodes.Call && inst.operand.Equals(FirstBlockingThingInfo))
				{
					//Frame can be made
					yield return new CodeInstruction(OpCodes.Pop);
					list[++i].opcode = OpCodes.Br;
					yield return list[i];
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
			return Harmony.Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(EdificeUtility), "IsEdifice"),
				AccessTools.Method(typeof(FramesAreEdificesInSomeCases), "IsEdificeOrFrame"));
		}

		public static bool IsEdificeOrFrame(BuildableDef def)
		{
			return def.IsEdifice() || (def is ThingDef thingDef && thingDef.IsFrame);
		}
	}
}
