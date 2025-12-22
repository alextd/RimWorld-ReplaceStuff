using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;


namespace Replace_Stuff.NewThing
{
	[StaticConstructorOnStartup]
	public static class FridgeCompat
	{
		public static Type fridgeType;
		public static FieldInfo DesiredTempInfo;
		static FridgeCompat()
		{
			try
			{
				fridgeType = AccessTools.TypeByName("Building_Refrigerator");
				if (fridgeType != null)
					DesiredTempInfo = AccessTools.Field(fridgeType, "DesiredTemp");
			}
			catch (System.Reflection.ReflectionTypeLoadException) //Aeh, this happens to people, should not happen, meh.
			{
				Verse.Log.Warning("Replace Stuff failed to check for RimFridges");
			}
		}
	}
	[StaticConstructorOnStartup]
	public static class NewThingReplacement
	{
		public class Replacement
		{
			Predicate<ThingDef> newCheck, oldCheck;
			Action<Thing, Thing> replaceAction, preReplaceAction;
			public Replacement(Predicate<ThingDef> n, Predicate<ThingDef> o = null, Action<Thing, Thing> preAction = null, Action < Thing, Thing> postAction = null)
			{
				newCheck = n;
				oldCheck = o ?? n;
				preReplaceAction = preAction;
				replaceAction = postAction;
			}

			public bool Matches(ThingDef n, ThingDef o)
			{
				return n != null && o != null && newCheck(n) && oldCheck(o);
			}

			//Pre-Despawn of old thing
			public void PreReplace(Thing n, Thing o)
			{
				if (preReplaceAction != null)
				{
					preReplaceAction(n, o);
				}
			}

			//Past-SpawnSetup of new thing, old thing saved even though despawned.
			public void Replace(Thing n, Thing o)
			{
				if (replaceAction != null)
				{
					replaceAction(n, o);
				}
			}
}

		public static List<Replacement> replacements;
		private static Dictionary<(ThingDef, ThingDef), bool> _replacementCache = new ();

		public static bool CanReplace(this ThingDef newDef, ThingDef oldDef)
		{
			if (!oldDef.building?.deconstructible ?? false)
				return false;

			newDef = GenConstruct.BuiltDefOf(newDef) as ThingDef;
			if (newDef == oldDef)
			{
				return false;
			}

			if (_replacementCache.TryGetValue((newDef, oldDef), out var result)) 
			{
				return result;
			} 

			// 1.6 added some tags for replacement. Add them here so Replace Stuff does then in-place
			try
			{
				if (newDef != null)
				{
					if (GenConstruct.HasMatchingReplacementTag(newDef, oldDef))
					{
						_replacementCache.Add((newDef, oldDef), true);
						return true;
					}
				}
			}
#pragma warning disable CS0168 // Variable is declared but never used
			catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
			{
				Debugger.Break();
			}

			foreach (var r in replacements)
			{
				if (!r.Matches(newDef, oldDef)) continue;
				
				_replacementCache.Add((newDef, oldDef), true);
				return true;
			}

			_replacementCache.Add((newDef, oldDef), false);
			return false;
		}

		public static void FinalizeNewThingReplace(this Thing newThing, Thing oldThing)
		{
			if (_replacementCache.TryGetValue((newThing.def, oldThing.def), out var result))
			{
				TransferBills(newThing, oldThing);

				TransferStorageSettings(newThing, oldThing);
			}

			replacements.ForEach(r =>
			{
				if (r.Matches(newThing.def, oldThing.def))
					r.Replace(newThing, oldThing);
			});
		}

		public static void PreFinalizeNewThingReplace(this Thing newThing, Thing oldThing)
		{
			replacements.ForEach(r =>
			{
				if (r.Matches(newThing.def, oldThing.def))
					r.PreReplace(newThing, oldThing);
			});
		}

		private static void TransferBills(Thing n, Thing o)
		{
			if (n is not Building_WorkTable newTable || o is not Building_WorkTable oldTable)
				return;

			foreach (Bill bill in oldTable.BillStack)
			{
				newTable.BillStack.AddBill(bill);
			}
		}

		private static void TransferStorageSettings(Thing n, Thing o)
		{
			if (n is not Building_Storage newStore || o is not Building_Storage oldStore)
				return;

			// There's some mods doing weird stuff with storage settings. Need to delay this for it to apply
			TickScheduler.NextTick(() => newStore.settings.CopyFrom(oldStore.settings));
		}


		static NewThingReplacement()
		{
			replacements = new List<Replacement>();

			//---------------------------------------------
			//---------------------------------------------
			//Here are valid replacements:

			// walls/fences/door
			replacements.Add(new Replacement(d => d.IsWall() || (d.building?.isPlaceOverableWall ?? false) || (d.building?.isFence ?? false) || typeof(Building_Door).IsAssignableFrom(d.thingClass)));

			// coolers
			replacements.Add(new Replacement(d => typeof(Building_Cooler).IsAssignableFrom(d.thingClass),
				postAction: (n, o) =>
				{
					Building_Cooler newCooler = n as Building_Cooler;
					Building_Cooler oldCooler = o as Building_Cooler;
					//newCooler.compPowerTrader.PowerOn = oldCooler.compPowerTrader.PowerOn;	//should be flickable
					newCooler.compTempControl.targetTemperature = oldCooler.compTempControl.targetTemperature;
				}
				));
			
			// beds
			bool isBed(ThingDef d) { return typeof(Building_Bed).IsAssignableFrom(d.thingClass); }
			replacements.Add(new Replacement(
				d => isBed(d) && d.GetStatValueAbstract(StatDefOf.WorkToBuild) > 0f,
				isBed,
				preAction: (n, o) =>
				{
					Building_Bed newBed = n as Building_Bed;
					Building_Bed oldBed = o as Building_Bed;
					newBed.ForPrisoners = oldBed.ForPrisoners;
					newBed.Medical = oldBed.Medical;
					oldBed.OwnersForReading.ListFullCopy().ForEach(p => p.ownership.ClaimBedIfNonMedical(newBed));
				}
				));

			// fences as a category (a mod)
			DesignationCategoryDef fencesDef = DefDatabase<DesignationCategoryDef>.GetNamed("Fences", false);
			if (fencesDef != null)
				replacements.Add(new Replacement(d => d.designationCategory == fencesDef));

			// Just tables.
			replacements.Add(new Replacement(d => d.IsTable));

			// Fridges from a mod
			replacements.Add(new Replacement(d => d.thingClass == FridgeCompat.fridgeType,
				postAction: (n, o) =>
				{
					FridgeCompat.DesiredTempInfo.SetValue(n, FridgeCompat.DesiredTempInfo.GetValue(o));
				}));

			// Allow all "plant growable items" to replace each other, and when they do attempt to set the growing plant type
			replacements.Add(new Replacement(
				building => typeof(IPlantToGrowSettable).IsAssignableFrom(building.thingClass),
				postAction: (newItem, oldItem) =>
				{
					((IPlantToGrowSettable)newItem).SetPlantDefToGrow(((IPlantToGrowSettable)oldItem).GetPlantDefToGrow());
				}));

			replacements.Add(new Replacement(
				building => typeof(Building_Battery).IsAssignableFrom(building.thingClass)));

			// We can use placeWorkers and comps to check what kind of power is being generated so that we don't have to worry
			// about each item individually
			replacements.Add(new Replacement(
				building => building.placeWorkers?.Any(placeWorker =>
					placeWorker == typeof(PlaceWorker_WatermillGenerator)) ?? false));
			replacements.Add(new Replacement(
				building => building.placeWorkers?.Any(placeWorker =>
					placeWorker == typeof(PlaceWorker_WindTurbine)) ?? false));
			replacements.Add(new Replacement(
				building => building.placeWorkers?.Any(placeWorker =>
					placeWorker == typeof(PlaceWorker_OnSteamGeyser)) ?? false));

			/* 1.6 added these as replaceTags (handled in CanReplace):
			replacements.Add(new Replacement(d => d.building?.isSittable ?? false));

			// Also requires PlaceWorker changes to match this
			replacements.Add(new Replacement(d =>
				(d.building?.isPowerConduit ?? false)
				|| typeof(Building_PowerSwitch).IsAssignableFrom(d.thingClass),
				o => o.building?.isPowerConduit ?? false));
			*/

			//---------------------------------------------
			//---------------------------------------------
		}

		public static bool IsNewThingReplacement(this Thing newThing, out Thing oldThing)
		{
			if (newThing.Spawned)
				return newThing.def.IsNewThingReplacement(newThing.Position, newThing.Rotation, newThing.Map, out oldThing);

			oldThing = null;
			return false;
		}

		public static bool IsNewThingReplacement(this ThingDef newDef, IntVec3 pos, Rot4 rotation, Map map, out Thing oldThing)
		{
			if (map == null)
			{
				oldThing = null;
				return false;
			}

			foreach (IntVec3 checkPos in GenAdj.OccupiedRect(pos, rotation, newDef.Size))
			{
				foreach (Thing oThing in checkPos.GetThingList(map))
				{
					if (newDef.CanReplace(oThing.def))
					{
						oldThing = oThing;
						return true;
					}
				}
			}

			oldThing = null;
			return false;
		}
		
		public static bool CanReplace(this Thing newThing, Thing oldThing)
		{
			return newThing.def.CanReplace(oldThing.def);
		}

		public static Thing BeingReplacedByNewThing(this Thing oldThing)
		{
			foreach (IntVec3 checkPos in GenAdj.OccupiedRect(oldThing.Position, oldThing.Rotation, oldThing.def.size))
				foreach (Thing newThing in checkPos.GetThingList(oldThing.Map))
					if (newThing.CanReplace(oldThing))
						return newThing;

			return null;
		}
	}
}
