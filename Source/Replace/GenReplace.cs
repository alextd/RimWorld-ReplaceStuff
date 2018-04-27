using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;

namespace Replace_Stuff
{
	static class GenReplace
	{
		public static ReplaceFrame PlaceReplaceFrame(Thing oldThing,ThingDef stuff)
		{
			ThingDef replaceFrameDef = ThingDefGenerator_ReplaceFrame.ReplaceFrameDefFor(oldThing.def);

			ReplaceFrame replaceFrame = (ReplaceFrame)ThingMaker.MakeThing(replaceFrameDef, stuff);
			replaceFrame.SetFactionDirect(Faction.OfPlayer);
			replaceFrame.oldThing = oldThing;
			replaceFrame.oldStuff = oldThing.Stuff;
			GenSpawn.Spawn(replaceFrame, oldThing.Position, oldThing.Map, oldThing.Rotation);
			return replaceFrame;
		}
	}

	[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
	public static class ThingDefGenerator_ReplaceFrame
	{
		public static void Prefix()
		{
			IEnumerable<ThingDef> enumerable = ThingDefGenerator_ReplaceFrame.ImpliedReplaceFrameDefs();
			foreach (ThingDef current in enumerable)
			{
				current.PostLoad();
				DefDatabase<ThingDef>.Add(current);
			}
		}

		public static Dictionary<ThingDef, ThingDef> replaceFrameDefs;
		public static ThingDef ReplaceFrameDefFor(ThingDef thing)
		{
			return replaceFrameDefs[thing]; // or bad things
		}

		public static IEnumerable<ThingDef> ImpliedReplaceFrameDefs()
		{
			replaceFrameDefs = new Dictionary<ThingDef, ThingDef>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.ToList<ThingDef>())
			{
				if (def.designationCategory != null && def.IsBuildingArtificial && !def.IsFrame && def.MadeFromStuff)
				{
					ThingDef replaceFrameDef = NewFrameDef_Thing(def);
					replaceFrameDefs[def] = replaceFrameDef;
					yield return replaceFrameDef;
				}
			}
		}


		public static ThingDef NewFrameDef_Thing(ThingDef def)
		{
			ThingDef thingDef = ThingDefGenerator_ReplaceFrame.BaseFrameDef();
			thingDef.defName = def.defName + "_ReplaceStuff";
			thingDef.label = def.label + "FrameLabelExtra".Translate();
			thingDef.size = def.size;
			thingDef.SetStatBaseValue(StatDefOf.MaxHitPoints, (float)def.BaseMaxHitPoints * 0.25f);
			thingDef.SetStatBaseValue(StatDefOf.Beauty, -8f);
			thingDef.fillPercent = 0.2f;
			thingDef.pathCost = 10;
			thingDef.description = def.description;
			thingDef.passability = def.passability;
			thingDef.selectable = def.selectable;
			thingDef.constructEffect = def.constructEffect;
			thingDef.building.isEdifice = false;
			thingDef.constructionSkillPrerequisite = def.constructionSkillPrerequisite;
			thingDef.clearBuildingArea = false;
			thingDef.drawPlaceWorkersWhileSelected = def.drawPlaceWorkersWhileSelected;
			thingDef.stuffCategories = def.stuffCategories;

			thingDef.entityDefToBuild = def;
			//def.replaceFrameDef = thingDef;	//Dictionary instead
			return thingDef;
		}

		static ThingDef BaseFrameDef()
		{
			return new ThingDef
			{
				isFrame = true,
				category = ThingCategory.Building,
				label = "Unspecified stuff replacement frame",
				thingClass = typeof(ReplaceFrame),
				altitudeLayer = AltitudeLayer.BuildingOnTop,
				useHitPoints = true,
				selectable = true,
				building = new BuildingProperties(),
				comps =
				{
					new CompProperties_Forbiddable()
				},
				scatterableOnMapGen = false,
				leaveResourcesWhenKilled = true
			};
		}
	}
}
