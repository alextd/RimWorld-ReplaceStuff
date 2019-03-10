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
	[HarmonyPatch(typeof(ThingUtility), nameof(ThingUtility.CheckAutoRebuildOnDestroyed))]
	static class DestroyedBuildings
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo PlaceBlueprintForBuildInfo = AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if(i.opcode == OpCodes.Call && i.operand == PlaceBlueprintForBuildInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Thing thing
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DestroyedBuildings), nameof(SaveBuilding)));//SaveBuilding(thing)
				}
			}
		}

		//todo: more maps
		public static Dictionary<IntVec3, Thing> destroyedBuildings = new Dictionary<IntVec3, Thing>();
		public static void SaveBuilding(Thing thing)
		{
			Log.Message($"Saving {thing} to {thing.Position}");
			destroyedBuildings[thing.Position] = thing;
			thing.ForceSetStateToUnspawned();
		}

		public static void RemoveAt(IntVec3 pos)
		{
			if (destroyedBuildings.TryGetValue(pos, out Thing building))
			{
				Log.Message($"Removed destroyed: {building}");
				//Probably should set building.mapIndexOrState to -2
				destroyedBuildings.Remove(pos);
			}
		}
	}
}
