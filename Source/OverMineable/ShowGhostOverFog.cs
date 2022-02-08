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

		//public void CopyFrom(GraphicData other)
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

	//Graphic_Appearances doesn't pass renderQueue along.
	//Fences blueprints uses Graphic_Appearances and nothing else does.
	//Fix that bug so fences blueprints can render over fog

	[HarmonyPatch(typeof(Graphic_Appearances), nameof(Graphic_Appearances.Init))]
	public static class GraphicAppearancesPassData
	{
		//public override void Init(GraphicRequest req)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color) where T : Graphic, new()
			//public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, GraphicData data, string maskPath = null) where T : Graphic, new()

			MethodInfo GetInfo = AccessTools.Method(typeof(GraphicDatabase), nameof(GraphicDatabase.Get),
					parameters: new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
					generics: new Type[] { typeof(Graphic_Single) });
			MethodInfo GetWithDataInfo = AccessTools.Method(typeof(GraphicDatabase), nameof(GraphicDatabase.Get),
					parameters: new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
					generics: new Type[] { typeof(Graphic_Single) });

			//Color.white
			MethodInfo ColorWhiteInfo = AccessTools.Property(typeof(Color), nameof(Color.white)).GetGetMethod();

			//Graphic.data
			FieldInfo dataInfo = AccessTools.Field(typeof(Graphic), nameof(Graphic.data));

			foreach (var inst in instructions)
			{
				if(inst.Calls(GetInfo))
				{
					//From:	GraphicDatabase.Get<Graphic_Single>(text + "/" + texture2D.name, req.shader, drawSize, color);
					//To:		GraphicDatabase.Get<Graphic_Single>(text + "/" + texture2D.name, req.shader, drawSize, color, Color.white, this.data, null);
					yield return new CodeInstruction(OpCodes.Call, ColorWhiteInfo);//Color.white (property getter)
					yield return new CodeInstruction(OpCodes.Ldarg_0);//this(Graphic_Appearances)
					yield return new CodeInstruction(OpCodes.Ldfld, dataInfo);//this.data(GraphicData)
					yield return new CodeInstruction(OpCodes.Ldnull);//null (string)
					inst.operand = GetWithDataInfo;//Override method...
					yield return inst;//Get(path, shader, drawSize, color, Color.white, this.data, null);
				}
				else
					yield return inst;
			}
		}
	}

}
