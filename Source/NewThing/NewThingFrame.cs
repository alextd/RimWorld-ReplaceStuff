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
			public Replacement(Predicate<ThingDef> n, Predicate<ThingDef> o)
			{
				newCheck = n;
				oldCheck = o;
			}

			public bool Matches(ThingDef n, ThingDef o)
			{
				return n != null && o != null && newCheck(n) && oldCheck(o);
			}
		}

		public static List<Replacement> replacements;

		public static bool CanReplace(this ThingDef newDef, ThingDef oldDef)
		{
			return replacements.Any(r => r.Matches(newDef, oldDef));
		}

		static NewThingFrame()
		{
			replacements = new List<Replacement>();

			//Here are valid replacements:
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Door), n => n.IsWall() || n.thingClass == typeof(Building_Door)));
			replacements.Add(new Replacement(d => d.thingClass == typeof(Building_Bed), n => n.thingClass == typeof(Building_Bed)));
			//----------
		}

		public static bool WasReplacedByNewThing(this Thing oldThing, out Thing replacement) => WasReplacedByNewThing(oldThing, oldThing.Map, out replacement);
		public static bool WasReplacedByNewThing(this Thing oldThing, Map map, out Thing replacement)
		{
			foreach (Thing newThing in oldThing.Position.GetThingList(map))
			{
				if (newThing.def.CanReplace(oldThing.def))
				{
					replacement = newThing;
					return true;
				}
			}

			replacement = null;
			return false;
		}

		public static bool IsNewThingFrame(this Thing newThing, out Thing oldThing)
		{
			if (newThing.Spawned)
				return IsNewThingReplacement(newThing is Frame ? newThing.def.entityDefToBuild as ThingDef : newThing.def, newThing.Position, newThing.Map, out oldThing);
			oldThing = null;
			return false;
		}

		public static bool IsNewThingReplacement(this ThingDef newDef, IntVec3 pos, Map map, out Thing oldThing)
		{
			foreach (Thing oThing in pos.GetThingList(map))
			{
				if (newDef.CanReplace(oThing.def))
				{
					oldThing = oThing;
					return true;
				}
			}

			oldThing = null;
			return false;
		}

		//Sort of assume this is a frame...
		public static bool CanReplaceOldThing(this Thing newThing, Thing oldThing)
		{
			return (newThing.def.entityDefToBuild as ThingDef).CanReplace(oldThing.def);
		}
	}
}
