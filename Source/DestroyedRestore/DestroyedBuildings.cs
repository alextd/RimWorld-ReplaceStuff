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
	static class SaveDestroyedBuildings
	{
		//public static void CheckAutoRebuildOnDestroyed(Thing thing, DestroyMode mode, Map map, BuildableDef buildingDef)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo PlaceBlueprintForBuildInfo = AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.opcode == OpCodes.Call && i.operand == PlaceBlueprintForBuildInfo)
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
			Scribe_Collections.Look(ref destroyedBuildings, "destroyedBuildings", LookMode.Value, LookMode.Deep);
		}
		

		public static void SaveBuilding(Thing thing, Map map)
		{
			if (!BuildingReviver.CanDo(thing)) return;

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

	[StaticConstructorOnStartup]
	public static class BuildingReviver
	{
		public static Dictionary<Type, Action<Thing, Thing>> handlers;
		static BuildingReviver()
		{
			handlers = new Dictionary<Type, Action<Thing, Thing>>();

			//Here are the types 
			handlers[typeof(Building_WorkTable)] = delegate (Thing fromThing, Thing toThing)
			{
				if (fromThing is Building_WorkTable from && toThing is Building_WorkTable to)
					foreach (Bill bill in from.BillStack)
						to.BillStack.AddBill(bill);
			};
			handlers[typeof(Building_Cooler)] = delegate (Thing fromThing, Thing toThing)
			{
				if (fromThing is Building_Cooler from && toThing is Building_Cooler to)
					to.compTempControl.targetTemperature = from.compTempControl.targetTemperature;
			};
		}

		public static bool CanDo(Thing thing)
		{
			return handlers.ContainsKey(thing.GetType());
		}

		public static void Transfer(Thing from, Thing to)
		{
			if (handlers.TryGetValue(from.GetType(), out Action<Thing, Thing> handler))
			{
				handler(from, to);
			}
			//else log warning no this shouldn't happen
		}
	}
}
