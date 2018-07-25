using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using UnityEngine;
using Harmony;


namespace Replace_Stuff
{
	class ReplaceFrame : Frame, IConstructible
	{
		public Thing oldThing;
		public ThingDef oldStuff;
		
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref oldThing, "oldThing");
			Scribe_Defs.Look(ref oldStuff, "oldStuff");
		}
		
		private const int MaxDeconstructWork = 3000;
		public float WorkToDeconstruct
		{
			get
			{
				float deWork = def.entityDefToBuild.GetStatValueAbstract(StatDefOf.WorkToBuild, oldStuff);
				return Mathf.Min(deWork, MaxDeconstructWork);
			}
		}
		public float WorkToReplace
		{
			get
			{
				return def.entityDefToBuild.GetStatValueAbstract(StatDefOf.WorkToBuild, Stuff);
			}
		}

        public float GetWorkToMake()
        {
            return WorkToDeconstruct + WorkToReplace;
        }

        public new float WorkLeft
		{
			get
			{
				return this.GetWorkToMake() - this.workDone;
			}
		}

		public new float PercentComplete
		{
			get
			{
				return this.workDone / this.GetWorkToMake();
			}
		}

		public override string Label
		{
			get
			{
				string text = this.def.entityDefToBuild.label + "TD.ReplacingTag".Translate();
				if (base.Stuff != null)
				{
					return base.Stuff.label + " " + text;
				}
				return text;
			}
		}

		public int TotalStuffNeeded()
		{
			return TotalStuffNeeded(def.entityDefToBuild, Stuff);
		}
		public static int TotalStuffNeeded(BuildableDef toBuild, ThingDef stuff)
		{
			int count = Mathf.RoundToInt((float)toBuild.costStuffCount / stuff.VolumePerUnit);
			if (count < 1) count = 1;
			return count;
		}
		public int CountStuffHas()
		{
			return resourceContainer.TotalStackCountOfDef(Stuff);
		}
		public int CountStuffNeeded()
		{
			return TotalStuffNeeded() - CountStuffHas();
		}

		private List<ThingDefCountClass> cachedMaterialsNeeded = new List<ThingDefCountClass>();
		public new List<ThingDefCountClass> MaterialsNeeded()
		{
			this.cachedMaterialsNeeded.Clear();
			
			int need = CountStuffNeeded();

			if (need > 0)
				this.cachedMaterialsNeeded.Add(new ThingDefCountClass(Stuff, need));

			return this.cachedMaterialsNeeded;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
		}

		public new void CompleteConstruction(Pawn worker)
		{
			if (oldThing != null && oldThing.Spawned)
			{
				FinalizeReplace(oldThing, Stuff, worker);

				this.resourceContainer.ClearAndDestroyContents(DestroyMode.Vanish);
				this.Destroy(DestroyMode.Vanish);

				worker.records.Increment(RecordDefOf.ThingsConstructed);
				worker.records.Increment(RecordDefOf.ThingsDeconstructed);
			}
			else
			{
				this.resourceContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
				this.Destroy(DestroyMode.Cancel);
			}
		}

		public new void FailConstruction(Pawn worker)
		{
			Log.Message("Failed replace frame! work was " + workDone + ", Decon is " + WorkToDeconstruct + ", total is " + GetWorkToMake());

			workDone = Mathf.Min(workDone, WorkToDeconstruct);
			if (workDone < WorkToDeconstruct) return;	//Deconstruction doesn't fail

			int total = TotalStuffNeeded();
			int lostResources = total - GenLeaving.GetBuildingResourcesLeaveCalculator(oldThing, DestroyMode.FailConstruction)(total);
			Log.Message("resources total " + total + ", lost " + lostResources + " stuff");

			while (lostResources > 0)
			{
				Thing replacementStuff = resourceContainer.First();
				if (replacementStuff.stackCount > lostResources)
				{
					replacementStuff.stackCount -= lostResources;
					break;
				}
				else
				{
					lostResources -= replacementStuff.stackCount;
					replacementStuff.Destroy();
					resourceContainer.Remove(replacementStuff);
				}
			}
			
			MoteMaker.ThrowText(this.DrawPos, Map, "TextMote_ConstructionFail".Translate());
			if (base.Faction == Faction.OfPlayer && this.WorkToReplace > 1400f)
			{
				Messages.Message("MessageConstructionFailed".Translate(new object[]
				{
					this.Label,
					worker.LabelShort
				}), new TargetInfo(base.Position, Map), MessageTypeDefOf.NegativeEvent);
			}
		}

		public static void DeconstructDropStuff(Thing oldThing)
		{
			if (Current.ProgramState != ProgramState.Playing)	return;

			ThingDef oldDef = oldThing.def;
			ThingDef stuffDef = oldThing.Stuff;
			
			if (GenLeaving.CanBuildingLeaveResources(oldThing, DestroyMode.Deconstruct))
			{
				int count = TotalStuffNeeded(oldDef, stuffDef);
				int leaveCount = GenLeaving.GetBuildingResourcesLeaveCalculator(oldThing, DestroyMode.Deconstruct)(count);
				if (leaveCount > 0)
				{
					Thing leftThing = ThingMaker.MakeThing(stuffDef);
					leftThing.stackCount = leaveCount;
					GenDrop.TryDropSpawn(leftThing, oldThing.Position, oldThing.Map, ThingPlaceMode.Near, out Thing dummyThing);
				}
			}
		}

		public static void FinalizeReplace(Thing thing, ThingDef stuff, Pawn worker = null)
		{
			DeconstructDropStuff(thing);
			
			thing.SetStuffDirect(stuff);
			thing.HitPoints = thing.MaxHitPoints;	//Deconstruction/construction implicitly repairs
			thing.Notify_ColorChanged();
			thing.Map.mapDrawer.SectionAt(thing.Position).RegenerateLayers(MapMeshFlag.Things);

			if (worker != null && thing.TryGetComp<CompQuality>() is CompQuality compQuality)
			{
				QualityCategory qualityCreatedByPawn = QualityUtility.GenerateQualityCreatedByPawn(worker, SkillDefOf.Construction);
				compQuality.SetQuality(qualityCreatedByPawn, ArtGenerationContext.Colony);
				QualityUtility.SendCraftNotification(thing, worker);
			}
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("ContainedResources".Translate() + ":");
			stringBuilder.AppendLine(string.Concat(new object[]
			{
				Stuff.LabelCap,
				": ",
				CountStuffHas(),
				" / ",
				TotalStuffNeeded()
			}));
			stringBuilder.Append("WorkLeft".Translate() + ": " + this.WorkLeft.ToStringWorkAmount());
			return stringBuilder.ToString();
		}
	}


	//VIRTUAL virtual methods
	[HarmonyPatch(typeof(Frame), "MaterialsNeeded")]
	public static class Virtualize_MaterialsNeeded
	{
		//public List<ThingDefCountClass> MaterialsNeeded()
		public static bool Prefix(Frame __instance, ref List<ThingDefCountClass> __result)
		{
			if (__instance is ReplaceFrame replaceFrame)
			{
				__result = replaceFrame.MaterialsNeeded();
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(Frame), "CompleteConstruction")]
	public static class Virtualize_CompleteConstruction
	{
		//public void CompleteConstruction(Pawn worker)
		public static bool Prefix(Frame __instance, Pawn worker)
		{
			if (__instance is ReplaceFrame replaceFrame)
			{
				replaceFrame.CompleteConstruction(worker);
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(Frame), "FailConstruction")]
	public static class Virtualize_FailConstruction
	{
		//public void FailConstruction(Pawn worker)
		public static bool Prefix(Frame __instance, Pawn worker)
		{
			if (__instance is ReplaceFrame replaceFrame)
			{
				replaceFrame.FailConstruction(worker);
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(Frame))]
	[HarmonyPatch("WorkToBuild", PropertyMethod.Getter)]
	public static class Virtualize_WorkToBuild
	{
		//public float WorkToMake
		public static bool Prefix(Frame __instance, ref float __result)
		{
			if (__instance is ReplaceFrame replaceFrame)
			{
				__result = replaceFrame.GetWorkToMake();
				return false;
			}
			return true;
		}
	}
	
}
