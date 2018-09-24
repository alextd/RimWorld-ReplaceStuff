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
			Predicate<BuildableDef> newCheck, oldCheck;
			public Replacement(Predicate<BuildableDef> n, Predicate<BuildableDef> o)
			{
				newCheck = n;
				oldCheck = o;
			}

			public bool Matches(BuildableDef n, BuildableDef o)
			{
				return n != null && o != null && newCheck(n) && oldCheck(o);
			}
		}

		public static List<Replacement> replacements;

		public static bool CanBeReplaced(BuildableDef newDef, BuildableDef oldDef)
		{
			return replacements.Any(r => r.Matches(newDef, oldDef));
		}

		static NewThingFrame()
		{
			replacements = new List<Replacement>();
			replacements.Add(new Replacement(d => d == ThingDefOf.Door, n => n.IsWall()));
		}

		public static bool WasReplacedByNewThing(this Thing oldThing, out Thing replacement) => WasReplacedByNewThing(oldThing, oldThing.Map, out replacement);
		public static bool WasReplacedByNewThing(this Thing oldThing, Map map, out Thing replacement)
		{
			foreach (Thing newThing in oldThing.Position.GetThingList(map))
			{
				if (CanBeReplaced(newThing.def, oldThing.def))
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
				return IsNewThingReplacement(newThing is Frame ? newThing.def.entityDefToBuild : newThing.def, newThing.Position, newThing.Map, out oldThing);
			oldThing = null;
			return false;
		}

		public static bool IsNewThingReplacement(this BuildableDef newDef, IntVec3 pos, Map map, out Thing oldThing)
		{
			foreach (Thing oThing in pos.GetThingList(map))
			{
				if (CanBeReplaced(newDef, oThing.def))
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
			return CanBeReplaced(newThing.def.entityDefToBuild, oldThing.def);
		}
	}
}
