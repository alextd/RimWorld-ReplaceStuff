using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Replace_Stuff
{
	static class GenReplace
	{
		public static ReplaceFrame PlaceReplaceFrame(Thing oldThing, ThingDef stuff)
		{
			ThingDef replaceFrameDef = ThingDefGenerator_ReplaceFrame.ReplaceFrameDefFor(oldThing.def);

			if (replaceFrameDef == null) return null;

			if (oldThing.Position.GetFirstThing(oldThing.Map, replaceFrameDef) != null) return null;

			ReplaceFrame replaceFrame = (ReplaceFrame)ThingMaker.MakeThing(replaceFrameDef, stuff);

			//QualityBuilder
			if(DefDatabase<DesignationDef>.GetNamed("SkilledBuilder", false) is DesignationDef d &&
				AccessTools.TypeByName("CompQualityBuilder") is Type qbType &&
				replaceFrame.def.HasComp(qbType))
				oldThing.Map.designationManager.AddDesignation(new Designation(replaceFrame, d));

			replaceFrame.SetFactionDirect(Faction.OfPlayer);
			replaceFrame.oldThing = oldThing;
			replaceFrame.oldStuff = oldThing.Stuff;
			GenSpawn.Spawn(replaceFrame, oldThing.Position, oldThing.Map, oldThing.Rotation);
			return replaceFrame;
		}
	}

	//[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
	[StaticConstructorOnStartup]
	public static class ThingDefGenerator_ReplaceFrame
	{
		/*
		public static void Postfix()
		{
			// would be nice but mods don't add defs early enough.
			// I mean they assume blueprints/frames don't need to be implied from them
			// But replace frames sure can!
			AddReplaceFrames(false);
		}
		*/
		public static void AddReplaceFrames(bool addShortHash = true)
		{
			IEnumerable<ThingDef> enumerable = ThingDefGenerator_ReplaceFrame.ImpliedReplaceFrameDefs();
			var hashMethod = AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash");
			foreach (ThingDef current in enumerable)
			{
				if(addShortHash)	//Wouldn't need this if other mods added defs earlier. Oh well.
					hashMethod.Invoke(null, new object[] { current, typeof(ThingDef) }); ;
				current.PostLoad();
				DefDatabase<ThingDef>.Add(current);
			}
		}

		public static Dictionary<ThingDef, ThingDef> replaceFrameDefs;
		public static ThingDef ReplaceFrameDefFor(ThingDef def)
		{
			if (replaceFrameDefs.TryGetValue(def, out ThingDef replaceFrame))
				return replaceFrame;
			Verse.Log.Warning($"Couldn't find replace frame for {def} : probably a mod's building that isn't added to the database soon enough");

			return null;
		}

		public static bool HasReplaceFrame(this ThingDef def)
		{
			return replaceFrameDefs.ContainsKey(def);
		}

		public static IEnumerable<ThingDef> ImpliedReplaceFrameDefs()
		{
			replaceFrameDefs = new Dictionary<ThingDef, ThingDef>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.ToList<ThingDef>())
			{
				if (def.designationCategory != null && def.IsBuildingArtificial && !def.IsFrame && def.MadeFromStuff)
				{
					ThingDef replaceFrameDef = NewReplaceFrameDef_Thing(def);
					replaceFrameDefs[def] = replaceFrameDef;
					yield return replaceFrameDef;
				}
			}
		}

		public static Type qbType;//QualityBuilder
		static ThingDefGenerator_ReplaceFrame()
		{
			//Maybe defining this above will load before QB so let's just do it here to be safe
			qbType = AccessTools.TypeByName("CompProperties_QualityBuilderr");
		}
		public static ThingDef NewReplaceFrameDef_Thing(ThingDef def)
		{
			ThingDef thingDef = ThingDefGenerator_ReplaceFrame.BaseFrameDef();
			thingDef.defName = def.defName + "_ReplaceStuff";
			thingDef.label = def.label + "TD.ReplacingTag".Translate();//Not entirely sure if this is needed since ReplaceFrame.Label doesn't use it, but, this is vanilla Frame code.
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

			//Support QualityBuilder
			if (def.HasComp(typeof(CompQuality)) && def.building != null)
				if (qbType != null)	//rr [sic]
					thingDef.comps.Add((CompProperties)Activator.CreateInstance(qbType));

			thingDef.entityDefToBuild = def;
			//def.replaceFrameDef = thingDef;	//Dictionary instead

			thingDef.modContentPack = LoadedModManager.GetMod<Mod>().Content;
			return thingDef;
		}

		static ThingDef BaseFrameDef()
		{
			return new ThingDef
			{
				isFrameInt = true,
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
