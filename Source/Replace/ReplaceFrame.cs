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
	public static class StuffChanger
	{
		public static void ChangeStuff(this Thing thing, ThingDef stuff)
		{
			thing.SetStuffDirect(stuff);
			thing.HitPoints = thing.MaxHitPoints; //Deconstruction/construction implicitly repairs
			if(thing is Frame frame)
			{
				GenLeaving.DoLeavingsFor(thing, thing.Map, DestroyMode.Cancel);
				frame.workDone = 0;
			}

			thing.Notify_ColorChanged();

			thing.Map.reservationManager.ReleaseAllForTarget(thing);
		}
	}

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
		
		private const float MaxDeconstructWork = 3000f;
		public static float WorkToDeconstruct(ThingDef def, ThingDef oldStuff = null)
		{
			float deWork = (def.entityDefToBuild as ThingDef ?? def)
				.GetStatValueAbstract(StatDefOf.WorkToBuild, oldStuff);
			return Mathf.Min(deWork, MaxDeconstructWork);
		}

		public float WorkToReplace
		{
			get
			{
				return def.entityDefToBuild.GetStatValueAbstract(StatDefOf.WorkToBuild, Stuff);
			}
		}
		public new float WorkToBuild
		{
			get
			{
				return WorkToDeconstruct(def, oldStuff) + WorkToReplace;
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
			Log.Message("Failed replace frame! work was " + workDone + ", Decon is " + WorkToDeconstruct(def, oldStuff) + ", total is " + WorkToBuild);

			workDone = Mathf.Min(workDone, WorkToDeconstruct(def, oldStuff));
			if (workDone < WorkToDeconstruct(def, oldStuff)) return;	//Deconstruction doesn't fail

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

			thing.ChangeStuff(stuff);

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
		//public float WorkToBuild
		public static bool Prefix(Frame __instance, ref float __result)
		{
			if (__instance is ReplaceFrame replaceFrame)
			{
				__result = replaceFrame.WorkToBuild;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Thing), "Notify_ColorChanged")]
	public static class Virtualize_Notify_ColorChanged
	{
		//public virtual void Notify_ColorChanged()
		public static void Postfix(Thing __instance)
		{
			if (__instance is Frame frame)
			{
				//base.Notify but that's too much
				AccessTools.Field(typeof(Frame), "cachedCornerMat").SetValue(frame, null);
				AccessTools.Field(typeof(Frame), "cachedTileMat").SetValue(frame, null);
			}
		}
	}
}
