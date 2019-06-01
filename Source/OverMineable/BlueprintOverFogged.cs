using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.OverMineable
{
	public static class FogUtil
	{
		public static bool IsUnderFog(this Thing thing)
		{
			return IsUnderFog(thing.Position, thing.Rotation, thing.def);
		}

		public static bool IsUnderFog(this IntVec3 center, Rot4 rot, ThingDef thingDef)
		{
			CellRect.CellRectIterator iterator = GenAdj.OccupiedRect(center, rot, thingDef.Size).GetIterator();
			while (!iterator.Done())
			{
				if (Find.CurrentMap.fogGrid.IsFogged(iterator.Current))
				{
					return true;
				}
				iterator.MoveNext();
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
	public static class BlueprintOverFogged
	{
		//public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo FoggedInfo = AccessTools.Method(typeof(GridsUtility), "Fogged", new Type[] { typeof(IntVec3), typeof(Map)});

			MethodInfo BlueprintAcceptedInfo = AccessTools.Method(typeof(BlueprintOverFogged), "BlueprintExistsAcceptance");

			bool foundFogged = false;
			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (foundFogged)  //skip the brfalse after Fogged
				{
					yield return new CodeInstruction(OpCodes.Ldarg_3);//map
					yield return new CodeInstruction(OpCodes.Ldarg_1);//center
					yield return new CodeInstruction(OpCodes.Ldarg_0);//entDef
					yield return new CodeInstruction(OpCodes.Call, BlueprintAcceptedInfo);
					yield return new CodeInstruction(OpCodes.Ret);
					foundFogged = false;
				}
				if (i.opcode == OpCodes.Call && i.operand == FoggedInfo)
					foundFogged = true;
			}
		}

		//if found fogged:
		public static AcceptanceReport BlueprintExistsAcceptance(Map map, IntVec3 center, ThingDef entDef)
		{
			if (!OverMineable.PlaySettings_BlueprintOverRockToggle.blueprintOverRock)
				return new AcceptanceReport("CannotPlaceInUndiscovered".Translate());
			if(center.GetThingList(map).Any(t => t is Blueprint && t.def.entityDefToBuild == entDef))
				return new AcceptanceReport("IdenticalBlueprintExists".Translate());
			if (entDef.GetStatValueAbstract(StatDefOf.WorkToBuild) == 0f)
				return new AcceptanceReport("CannotPlaceInUndiscovered".Translate());
			return true;
		}
	}

	//TL;DR: actually ignore the thingToIgnore argument
	[HarmonyPatch(typeof(PlaceWorker_Conduit), "AllowsPlacing")]
	public static class FixConduitPlaceWorker
	{
		//AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo entityDefInfo = AccessTools.Field(typeof(ThingDef), "entityDefToBuild");
			
			//need to find loop continue label
			object continueLabel = null;

			//just easier to find the 3-line code to get thingList[i]
			bool foundLdLoc = false;
			List<CodeInstruction> currentThing = new List<CodeInstruction>();

			List<CodeInstruction> iList = instructions.ToList();
			for(int k=0; k < iList.Count(); k++)
			{
				CodeInstruction i = iList[k];
				if (!foundLdLoc && i.opcode == OpCodes.Ldloc_0)
				{
					foundLdLoc = true;
					currentThing.AddRange(iList.GetRange(k, 3));
				}
				if (i.opcode == OpCodes.Ldfld && i.operand == entityDefInfo)
				{
					continueLabel = iList[k+1].operand;
					break;
				}
			}

			foundLdLoc = false;
			foreach(CodeInstruction i in instructions)
			{
				if(!foundLdLoc && i.opcode == OpCodes.Ldloc_0)
				{
					foundLdLoc = true;

					yield return new CodeInstruction(OpCodes.Nop) { labels = i.labels };//start of loop label
					i.labels = new List<Label>();
					foreach (CodeInstruction ci in currentThing)
						yield return new CodeInstruction(ci.opcode, ci.operand);//thingList[i]
					yield return new CodeInstruction(OpCodes.Ldarg_S, 5);//thingToIgnore
					yield return new CodeInstruction(OpCodes.Beq, continueLabel);//continue
				}
				yield return i;
			}
		}
	}


	[HarmonyPatch(typeof(FogGrid), "UnfogWorker")]
	public static class UnFogFix
	{
		//private void UnfogWorker(IntVec3 c)
		public static void Postfix(FogGrid __instance, IntVec3 c, Map ___map)
		{
			Map map = ___map;
			if (c.GetThingList(map).FirstOrDefault(t => t.def.IsBlueprint) is Thing blueprint && !blueprint.IsUnderFog())
			{
				if (!GenConstruct.CanPlaceBlueprintAt(blueprint.def.entityDefToBuild, blueprint.Position, blueprint.Rotation, map, false, blueprint).Accepted)
					blueprint.Destroy();
				else
					blueprint.Notify_ColorChanged();//does the job, haha.
			}
		}
	}
}
