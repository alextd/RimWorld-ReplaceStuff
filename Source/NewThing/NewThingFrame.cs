using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;


namespace Replace_Stuff.NewThing
{
	[DefOf]
	public static class NewThingDefOf
	{
		public static ThingDef ElectricStove;
		public static ThingDef FueledStove;
		public static ThingDef HandTailoringBench;
		public static ThingDef ElectricTailoringBench;
	}
	[StaticConstructorOnStartup]
	static class NewThingReplacement
	{
		public class Replacement
		{
			Predicate<ThingDef> newCheck, oldCheck;
			Action<Thing, Thing> replaceAction, preReplaceAction;
			public Replacement(Predicate<ThingDef> n, Predicate<ThingDef> o, Action<Thing, Thing> p = null, Action < Thing, Thing> r = null)
			{
				newCheck = n;
				oldCheck = o;
				preReplaceAction = p;
				replaceAction = r;
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

		public static bool CanReplace(this ThingDef newDef, ThingDef oldDef)
		{
			return newDef != oldDef && replacements.Any(r => r.Matches(newDef, oldDef));
		}

		public static void FinalizeNewThingReplace(this Thing newThing, Thing oldThing)
		{
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

		static NewThingReplacement()
		{
			replacements = new List<Replacement>();

			//---------------------------------------------
			//---------------------------------------------
			//Here are valid replacements:
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Door), n => n.IsWall() || n.thingClass == typeof(Building_Door)));
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Cooler), n => n.thingClass == typeof(Building_Cooler),
				null,
				(n, o) =>
				{
					Building_Cooler newCooler = n as Building_Cooler;
					Building_Cooler oldCooler = o as Building_Cooler;
					//newCooler.compPowerTrader.PowerOn = oldCooler.compPowerTrader.PowerOn;	//should be flickable
					newCooler.compTempControl.targetTemperature = oldCooler.compTempControl.targetTemperature;
				}
				));
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Bed), n => n.thingClass == typeof(Building_Bed),
				(n, o) =>
				{
					Building_Bed newBed = n as Building_Bed;
					Building_Bed oldBed = o as Building_Bed;
					newBed.ForPrisoners = oldBed.ForPrisoners;
					newBed.Medical = oldBed.Medical;
					oldBed.owners.ForEach(p => p.ownership.ClaimBedIfNonMedical(newBed));
				}
				));
			replacements.Add(new Replacement(d => d.IsWall(), n => n.IsWall()));

			Action<Thing, Thing> transferBills = (n, o) =>
				{
					Building_WorkTable newTable = n as Building_WorkTable;
					Building_WorkTable oldTable = o as Building_WorkTable;

					foreach (Bill bill in oldTable.BillStack)
					{
						newTable.BillStack.AddBill(bill);
					}
				};
			replacements.Add(new Replacement(d => d == NewThingDefOf.ElectricStove, n => n == NewThingDefOf.FueledStove, transferBills));
			replacements.Add(new Replacement(d => d == NewThingDefOf.ElectricTailoringBench, n => n == NewThingDefOf.HandTailoringBench, transferBills));
			//---------------------------------------------
			//---------------------------------------------
		}

		public static bool WasReplacedByNewThing(this Thing oldThing, out Thing replacement) => WasReplacedByNewThing(oldThing, oldThing.Map, out replacement);
		public static bool WasReplacedByNewThing(this Thing oldThing, Map map, out Thing replacement)
		{
			foreach (IntVec3 checkPos in GenAdj.OccupiedRect(oldThing.Position, oldThing.Rotation, oldThing.def.Size))
				foreach (Thing newThing in checkPos.GetThingList(map))
				if (newThing.def.CanReplace(oldThing.def))
				{
					replacement = newThing;
					return true;
				}

			replacement = null;
			return false;
		}

		public static bool IsNewThingReplacement(this Thing newThing, out Thing oldThing)
		{
			if (newThing.Spawned && GenConstruct.BuiltDefOf(newThing.def) is ThingDef newDef)
				return newDef.IsNewThingReplacement(newThing.Position, newThing.Rotation, newThing.Map, out oldThing);
			oldThing = null;
			return false;
		}

		public static bool IsNewThingReplacement(this ThingDef newDef, IntVec3 pos, Rot4 rotation, Map map, out Thing oldThing)
		{
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
		
		public static bool CanReplaceOldThing(this Thing newThing, Thing oldThing)
		{
			ThingDef newDef = GenConstruct.BuiltDefOf(newThing.def) as ThingDef;
			return newDef?.CanReplace(oldThing.def) ?? false;
		}
	}
}
