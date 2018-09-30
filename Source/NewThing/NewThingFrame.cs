using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;


namespace Replace_Stuff.NewThing
{
	[StaticConstructorOnStartup]
	static class NewThingFrame
	{
		public class Replacement
		{
			Predicate<ThingDef> newCheck, oldCheck;
			Action<Thing, Thing> replaceAction;
			public Replacement(Predicate<ThingDef> n, Predicate<ThingDef> o)
			{
				newCheck = n;
				oldCheck = o;
			}
			public Replacement(Predicate<ThingDef> n, Predicate<ThingDef> o, Action<Thing, Thing> r) :this(n,o)
			{
				replaceAction = r;
			}

			public bool Matches(ThingDef n, ThingDef o)
			{
				return n != null && o != null && newCheck(n) && oldCheck(o);
			}

			public void Replace(Thing n, Thing  o)
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
			return replacements.Any(r => r.Matches(newDef, oldDef));
		}

		public static void FinalizeNewThingReplace(this Thing newThing, Thing oldThing)
		{
			replacements.ForEach(r =>
			{
				if (r.Matches(newThing.def, oldThing.def))
					r.Replace(newThing, oldThing);
			});
		}

		static NewThingFrame()
		{
			replacements = new List<Replacement>();

			//---------------------------------------------
			//---------------------------------------------
			//Here are valid replacements:
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Door), n => n.IsWall() || n.thingClass == typeof(Building_Door)));
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

		//Sort of assume this is a frame...
		public static bool CanReplaceOldThing(this Thing newThing, Thing oldThing)
		{
			ThingDef newDef = newThing is Frame ? newThing.def.entityDefToBuild as ThingDef : newThing.def;
			return newDef?.CanReplace(oldThing.def) ?? false;
		}
	}
}
