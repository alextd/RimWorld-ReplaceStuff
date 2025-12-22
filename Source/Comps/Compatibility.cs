using HarmonyLib;
using Replace_Stuff.NewThing;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Replace_Stuff.Comps
{
#if DEBUG
	[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
	public static class Patch_ThingDef_SpecialDisplayStats
	{
		// Postfix composes the original result with ours
		public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, StatRequest req, ThingDef __instance)
		{
			// Preserve originals
			foreach (var e in __result) yield return e;

			var report = 
$"""
ReplaceTags: {(__instance.replaceTags == null ? "None" : string.Join(", ", __instance.replaceTags) )}
""";
			// Category, label, value text, tooltip, priority
			yield return new StatDrawEntry(
				StatCategoryDefOf.Basics,
				"defName",
				__instance.defName,
				report,
				displayPriorityWithinCategory: 999);
			
		}
	}
#endif

	public static class Compatibility
	{
		private static Dictionary<string, IReplacementComp> compCache = new();

		public static void AddRulesFromXML()
		{
			var categories = new Dictionary<string, ReplaceList>();
			var comps = new List<ReplaceList>();

			foreach (var def in DefDatabase<InterchangeableItems>.AllDefs)
			{
				var nonCategory = 0;
				foreach (var list in def.replaceLists)
				{

					if (list.category.Any())
					{
						if (!categories.ContainsKey(list.category)) categories.Add(list.category, new ReplaceList());

						list.items.ForEach(x =>
						{
							x.replaceTags ??= new List<string>();

							if(!x.replaceTags.Contains(list.category))
								x.replaceTags.Add(list.category);
						});

						categories[list.category].items.AddRange(list.items);
					}

					if (list.comps.Any()) 
						comps.Add(list);

					if (!list.category.Any() && !list.comps.Any())
						nonCategory++;
				}

				if (nonCategory > 0) 
					Verse.Log.Warning($"Loaded Compatibility patch {def.defName} includes {nonCategory} patch{(nonCategory == 1 ? "" : "es")} that have no category or comp.\nThese patches should be updated to use an existing category");
			}

			foreach (var itemList in comps)
			{

				foreach (var compName in itemList.comps)
				{
					if (compCache.ContainsKey(compName)) continue;

					var type = Type.GetType(compName);
					if (type is null) continue;

					var comp = (IReplacementComp)Activator.CreateInstance(type);
					if (comp is null) continue;

					compCache.Add(compName, comp);
				}
			}

			foreach (var itemList in comps)
			{
				AddInterchangeableItems(itemList);
			}
		}

		static void AddInterchangeableItems(ReplaceList items)
		{
			List<string> comps = new();

			if (items.comps.Any())
			{
				comps.AddRange(items.comps
					.Where(compName => compCache.ContainsKey(compName))
				);
			}

			AddInterchangeableList(
				items.items,
				preAction: (newThing, oldThing) => { comps.ForEach(comp => compCache[comp].PreAction(newThing, oldThing)); },
				postAction: (newThing, oldThing) => { comps.ForEach(comp => compCache[comp].PostAction(newThing, oldThing)); }
			);
		}
		static void AddInterchangeableList(List<ThingDef> items, Action<Thing, Thing> preAction = null,
			Action<Thing, Thing> postAction = null)
		{
			if (items.Count < 2) return;

			NewThingReplacement.replacements.Add(
				new NewThingReplacement.Replacement(
					ListContainsThingDef(new HashSet<ThingDef>(items)),
					preAction: preAction,
					postAction: postAction
				)
			);
		}

		static Predicate<ThingDef> ListContainsThingDef(HashSet<ThingDef> list) => list.Contains;

	}
}
