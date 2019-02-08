using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace Replace_Stuff.PlaceBridges
{
	[HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.DrawMouseAttachments))]
	static class DesignatorBuildCostCountsBridges
	{
		//public override void DrawMouseAttachments()
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
		{
			LocalVariableInfo posInfo = method.GetMethodBody().LocalVariables.First(lv => lv.LocalType == typeof(Vector2));
			LocalVariableInfo curYInfo = method.GetMethodBody().LocalVariables.First(lv => lv.LocalType == typeof(float));

			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count - 1; i++)
				yield return instList[i];

			//after the for loop but before the ret call
			yield return new CodeInstruction(OpCodes.Ldarg_0);//Designator_Build
			yield return new CodeInstruction(OpCodes.Ldloc_S, posInfo.LocalIndex);//pos
			yield return new CodeInstruction(OpCodes.Ldloc_S, curYInfo.LocalIndex);//y
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DesignatorBuildCostCountsBridges), nameof(DrawBridgeCost))); //DrawBridgeCost(Designator_Build, pos, curY)

			 yield return instList[instList.Count - 1];
		}

		public static FieldInfo placingRotInfo = AccessTools.Field(typeof(Designator_Build), "placingRot");
		public static Rot4 PlacingRot(this Designator_Build designator) =>
			(Rot4)placingRotInfo.GetValue(designator);
		public static void DrawBridgeCost(Designator_Build designator, Vector2 drawPos, float curY)
		{
			DesignationDragger dragger = Find.DesignatorManager.Dragger;
			int bridgeCount = 0;
			IEnumerable<IntVec3> cells = dragger.Dragging ? dragger.DragCells :
				GenAdj.OccupiedRect(UI.MouseCell(), designator.PlacingRot(), designator.PlacingDef.Size).Cells;
			foreach (IntVec3 dragPos in cells)
				if (PlaceBridges.NeedsBridge(designator.PlacingDef, dragPos, designator.Map))
					bridgeCount++;

			if (bridgeCount == 0) return;

			//could just say wood here, this is still assuming it costs only one thing.
			ThingDefCountClass bridgeCost = TerrainDefOf.Bridge.costList.First();	

			Widgets.ThingIcon(new Rect(drawPos.x, drawPos.y + curY, 27f, 27f), bridgeCost.thingDef);

			int totalCost = bridgeCost.count * bridgeCount;

			string label = $"{totalCost} ({TerrainDefOf.Bridge.LabelCap})";
			//This doesn't account for normal building cost + under bridge cost, but what can you do
			if (designator.Map.resourceCounter.GetCount(bridgeCost.thingDef) < totalCost)
			{
				GUI.color = Color.red;
				label = label + " (" + "NotEnoughStoredLower".Translate() + ")";
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(drawPos.x + 29f, drawPos.y + curY, 999f, 29f), label);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}
}
