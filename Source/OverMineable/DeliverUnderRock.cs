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
			//AccessTools.Inner
			HarmonyMethod transpiler = new HarmonyMethod(typeof(DeliverUnderRock), nameof(DeliverUnderRock.Transpiler));
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");
			MethodInfo CanConstructInfo = AccessTools.Method(typeof(GenConstruct), "CanConstruct");


			//Find the compiler-created method in Toils_Haul that calls CanConstruct
			List<Type> nestedTypes = new List<Type>(typeof(Toils_Haul).GetNestedTypes(BindingFlags.NonPublic));
			while (!nestedTypes.NullOrEmpty())
			{
				Type type = nestedTypes.Pop();
				nestedTypes.AddRange(type.GetNestedTypes(BindingFlags.NonPublic));
				
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
				{
					if (method.DeclaringType != type) continue;
					
					DynamicMethod dm = DynamicTools.CreateDynamicMethod(method, "-unused");

					if(Harmony.ILCopying.MethodBodyReader.GetInstructions(dm.GetILGenerator(), method).
						Any(ilcode => ilcode.operand == CanConstructInfo))
					{
						Log.Message($"patchin {method} for CanConstruct in Toils_Haul");
						harmony.Patch(method, null, null, transpiler);
					}
				}
			}
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
				if (inst.opcode == OpCodes.Call && inst.operand == FirstBlockingThingInfo)
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
}
