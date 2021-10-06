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
	//Cursor
	[HarmonyPatch(typeof(GhostUtility), "GhostGraphicFor")]
	public static class ShowGhostOverFog
	{
		public const int queueOverFog = 3176; //Just above fog at 3175 it seems

		//public static Graphic GhostGraphicFor(Graphic baseGraphic, ThingDef thingDef, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo CopyFromInfo = AccessTools.Method(typeof(GraphicData), nameof(GraphicData.CopyFrom));
			MethodInfo CopyFromRenderHighInfo = AccessTools.Method(typeof(ShowGhostOverFog), nameof(CopyFromRenderHigh));

			MethodInfo GraphicGetInfo = AccessTools.Method(typeof(GraphicDatabase), nameof(GraphicDatabase.Get),
				parameters: new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				generics: new Type[] { typeof(Graphic_Single)});
			MethodInfo GraphicGetRenderHighInfo = AccessTools.Method(typeof(ShowGhostOverFog), nameof(GetRenderHigh));

			return HarmonyLib.Transpilers.MethodReplacer(
				HarmonyLib.Transpilers.MethodReplacer(instructions, GraphicGetInfo, GraphicGetRenderHighInfo)
				, CopyFromInfo, CopyFromRenderHighInfo);
		}
		//		public void CopyFrom(GraphicData other)
		public static void CopyFromRenderHigh(GraphicData instance, GraphicData other)
		{
			instance.CopyFrom(other);
			instance.renderQueue = queueOverFog;
		}

		//public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color) where T : Graphic, new()
		public static Graphic GetRenderHigh(string path, Shader shader, Vector2 drawSize, Color color)
		{
			return GraphicDatabase.Get<Graphic_Single>(path, shader, drawSize, color, queueOverFog);
		}
	}

	//-------------------------------------------
	//Blueprint
	//-------------------------------------------

	//Can't do BaseBlueprintDef since NewBlueprintDef_Thing overwrites drawerType
	[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing")]
	public static class BlueprintOverFog
	{
		public static void Postfix(ThingDef __result)
		{
			__result.graphicData.renderQueue = ShowGhostOverFog.queueOverFog;
			__result.graphicData.linkFlags &= ~LinkFlags.Rock;//Prevent blueprint walls from showing links with rocks
		}
	}
}
