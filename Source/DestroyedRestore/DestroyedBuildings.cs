using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.DestroyedRestore
{
	[HarmonyPatch(typeof(ThingUtility), nameof(ThingUtility.CheckAutoRebuildOnDestroyed))]
	static class SaveDestroyedBuildings
	{
		//public static void CheckAutoRebuildOnDestroyed(Thing thing, DestroyMode mode, Map map, BuildableDef buildingDef)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo PlaceBlueprintForBuildInfo = AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.Calls(PlaceBlueprintForBuildInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Thing thing
					yield return new CodeInstruction(OpCodes.Ldarg_2);//Map map
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DestroyedBuildings), nameof(DestroyedBuildings.SaveBuilding)));//SaveBuilding(thing)
				}
			}
		}
	}

	public class DestroyedBuildings : MapComponent
	{
		public Dictionary<IntVec3, Thing> destroyedBuildings;
		//Actually want this to be deep-ref since it's despawned!

		public DestroyedBuildings(Map map) : base(map)
		{
			destroyedBuildings = new Dictionary<IntVec3, Thing>();
		}

		public override void ExposeData()
		{
			if(Scribe.mode == LoadSaveMode.Saving)
			{
				//Sanity check
				foreach (IntVec3 pos in destroyedBuildings.Keys.ToList())
					if (!pos.GetThingList(map).Any(t => t.def.IsFrame || t.def.IsBlueprint))
					{
						Verse.Log.Warning($"ReplaceStuff - Forgetting unrevivable building {destroyedBuildings[pos]}:{pos} before saving - somehow it didn't get removed when it should have?");
						destroyedBuildings.Remove(pos);
					}
			}

			Scribe_Collections.Look(ref destroyedBuildings, "destroyedBuildings", LookMode.Value, LookMode.Deep);
		}


		public static void SaveBuilding(Thing thing, Map map)
		{
			if (!BuildingReviver.CanDo(thing)) return;

			thing.ForceSetStateToUnspawned();

			DestroyedBuildings comp = map.GetComponent<DestroyedBuildings>();
			Log.Message($"Saving {thing} to {map}:{thing.Position}");
			comp.destroyedBuildings[thing.Position] = thing;
		}

		public static void ReviveBuilding(Thing newBuilding, IntVec3 pos, Map map)
		{
			DestroyedBuildings comp = map.GetComponent<DestroyedBuildings>();
			if (comp.destroyedBuildings.TryGetValue(pos, out Thing building))
			{
				Log.Message($"got {building}");

				BuildingReviver.Transfer(building, newBuilding);

				comp.destroyedBuildings.Remove(pos);
			}
		}

		public static void RemoveAt(IntVec3 pos, Map map)
		{
			DestroyedBuildings comp = map.GetComponent<DestroyedBuildings>();
			if (comp.destroyedBuildings.TryGetValue(pos, out Thing building))
			{
				Log.Message($"Forgetting destroyed: {building}");
				//Probably should set building.mapIndexOrState to -2
				comp.destroyedBuildings.Remove(pos);
			}
		}
	}
}
