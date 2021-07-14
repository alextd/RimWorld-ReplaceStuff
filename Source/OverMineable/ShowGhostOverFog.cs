using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace Replace_Stuff.OverMineable
{
	[HarmonyPatch(typeof(GhostDrawer), "DrawGhostThing")]
	[HarmonyPatch]
	public static class GhostOverFogChecker
	{
		public static bool ghostIsOverFog;
		//public static void DrawGhostThing(IntVec3 center, Rot4 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude)
		public static void Prefix(IntVec3 center, Rot4 rot, ThingDef thingDef)
		{
			ghostIsOverFog = center.IsUnderFog(rot, thingDef);
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
				if (i.Calls(HashCombineInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, HashFogInfo);
				}
				//use meta shader if fog:
				if (i.LoadsField(EdgeDetectInfo))
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

	//-------------------------------------------
	//Blueprint
	//-------------------------------------------

	[HarmonyPatch(typeof(Thing), "DefaultGraphic", MethodType.Getter)]
	public static class ShowBluePrintOverFogDynamic
	{
		//public Graphic DefaultGraphic
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo graphicDataInfo = AccessTools.Field(typeof(ThingDef), "graphicData");

			MethodInfo FogGraphicMakerInfo = AccessTools.Method(typeof(ShowBluePrintOverFogDynamic), nameof(ShowBluePrintOverFogDynamic.FogGraphicMaker));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				//hash with fog bool for graphics cache:
				if (i.LoadsField(graphicDataInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Thing
					yield return new CodeInstruction(OpCodes.Call, FogGraphicMakerInfo);
				}
			}
		}

		public static GraphicData FogGraphicMaker(GraphicData normalGraphicData, Thing thing)
		{
			return thing.def.IsBlueprint && thing.IsUnderFog() ? FogBlueprintGraphicFor(normalGraphicData) : normalGraphicData;
		}

		private static Dictionary<int, GraphicData> fogGraphics = new Dictionary<int, GraphicData>();
		public static GraphicData FogBlueprintGraphicFor(GraphicData baseGraphicData)
		{
			int hashKey = Gen.HashCombine<GraphicData>(0, baseGraphicData);
			GraphicData graphicData;
			if (!fogGraphics.TryGetValue(hashKey, out graphicData))
			{
				graphicData = new GraphicData();
				graphicData.CopyFrom(baseGraphicData);
				graphicData.shaderType = ShaderTypeDefOf.MetaOverlay;
				fogGraphics.Add(hashKey, graphicData);
			}
			return graphicData;
		}
	}

}
