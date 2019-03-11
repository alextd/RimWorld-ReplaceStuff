using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace Replace_Stuff.DestroyedRestore
{
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
			handlers[typeof(Building_Heater)] = delegate (Thing fromThing, Thing toThing)
			{
				if (fromThing is Building_Heater from && toThing is Building_Heater to)
					to.compTempControl.targetTemperature = from.compTempControl.targetTemperature;
			};
			handlers[typeof(Building_PlantGrower)] = delegate (Thing fromThing, Thing toThing)
			{
				if (fromThing is Building_PlantGrower from && toThing is Building_PlantGrower to)
					to.SetPlantDefToGrow(from.GetPlantDefToGrow());
			};
			handlers[typeof(Building_Storage)] = delegate (Thing fromThing, Thing toThing)
			{
				if (fromThing is Building_Storage from && toThing is Building_Storage to)
					to.settings.CopyFrom(from.GetStoreSettings());
			};
			//If building_bed didn't forget their owner it'd be easier;
			//otherwise the entire system could save an object instead of the Building before it despawns. 

			//CompProperties_Refuelable if targetFuelLevelConfigurable
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
