using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;
using UnityEngine;

namespace Replace_Stuff
{

	[HarmonyPatch(typeof(GhostDrawer), "DrawGhostThing")]
	public static class GhostOverFogChecker
	{
		public static bool ghostIsOverFog;

		//public static void DrawGhostThing(IntVec3 center, Rot4 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude)
		public static void Prefix(IntVec3 center, Rot4 rot, ThingDef thingDef)
		{
			ghostIsOverFog = false;
			CellRect.CellRectIterator iterator = GenAdj.OccupiedRect(center, rot, thingDef.Size).GetIterator();
			while (!iterator.Done())
			{
				if (Find.CurrentMap.fogGrid.IsFogged(iterator.Current))
				{
					ghostIsOverFog = true;
				}
				iterator.MoveNext();
			}
		}
	}
	
	[HarmonyPatch(typeof(GhostUtility), "GhostGraphicFor")]
	public static class ShowGhostOverFog
	{
		//public static Graphic GhostGraphicFor(Graphic baseGraphic, ThingDef thingDef, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo HashCombineInfo = AccessTools.Method(typeof(Gen), nameof(Gen.HashCombineStruct), null, new Type[] { typeof(UnityEngine.Color) });
			MethodInfo HashFogInfo = AccessTools.Method(typeof(ShowGhostOverFog), nameof(ShowGhostOverFog.HashFog));

			FieldInfo EdgeDetectInfo = AccessTools.Field(typeof(ShaderTypeDefOf), "EdgeDetect");
			MethodInfo MakeMetaIfOverFogInfo = AccessTools.Method(typeof(ShowGhostOverFog), nameof(ShowGhostOverFog.MakeMetaIfOverFog));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				//hash with fog bool for graphics cache:
				if (i.opcode == OpCodes.Call && i.operand == HashCombineInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, HashFogInfo);
				}
				//use meta shader if fog:
				if (i.opcode == OpCodes.Ldsfld && i.operand == EdgeDetectInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, MakeMetaIfOverFogInfo);
				}
			}
		}

		public static int HashFog(int hash)
		{
			return Gen.HashCombine(hash, GhostOverFogChecker.ghostIsOverFog);
		}

		public static ShaderTypeDef MakeMetaIfOverFog(ShaderTypeDef def)
		{
			return GhostOverFogChecker.ghostIsOverFog ? ShaderTypeDefOf.MetaOverlay : def;
		}
	}
	
	
		/*
	//[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing")]
	public static class ShowBluePrintOverFog
	{
		//private static ThingDef NewBlueprintDef_Thing(ThingDef def, bool isInstallBlueprint, ThingDef normalBlueprint = null)
		public static void Postfix(ref ThingDef __result)
		{
			//This seems like it would work butno:
			//__result.altitudeLayer = AltitudeLayer.MetaOverlays;
			//__result.drawerType = DrawerType.MapMeshAndRealTime;
			
			__result.graphicData.shaderType = ShaderTypeDefOf.MetaOverlay;
			__result.drawerType = DrawerType.MapMeshAndRealTime;
		}
	}

	[HarmonyPatch(typeof(Blueprint), "Draw")]
	public static class ShowBluePrintOverFogDynamic
	{
		//public override void Draw()
		public static void Prefix(Blueprint __instance, ShaderTypeDef __state)
		{
			Thing thing = __instance;
			
			CellRect.CellRectIterator iterator = thing.OccupiedRect().GetIterator();
			while (!iterator.Done())
			{
				if(thing.Map.fogGrid.IsFogged(iterator.Current))
				{
					__state = thing.Graphic.Shader
				}
				iterator.MoveNext();
			}
		}
	}*/

}
