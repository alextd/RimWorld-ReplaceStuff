using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace Replace_Stuff.PlaceBridges
{
	[HarmonyPatch(typeof(Designator_Build), "DrawPlaceMouseAttachments")]
	static class DesignatorBuildCostCountsBridges
	{

		public static FieldInfo placingRotInfo = AccessTools.Field(typeof(Designator_Build), "placingRot");
		public static Rot4 PlacingRot(this Designator_Build designator) =>
			(Rot4)placingRotInfo.GetValue(designator);

		//protected override void DrawPlaceMouseAttachments(float curX, ref float curY)
		public static void Postfix(Designator_Build __instance, float curX, float curY)
		{
			ThingDef stuff = __instance.StuffDef;
			DesignationDragger dragger = Find.DesignatorManager.Dragger;
			int bridgeCount = 0;
			IEnumerable<IntVec3> cells = dragger.Dragging ? dragger.DragCells :
				GenAdj.OccupiedRect(UI.MouseCell(), __instance.PlacingRot(), __instance.PlacingDef.Size).Cells;
			foreach (IntVec3 dragPos in cells)
				if (PlaceBridges.NeedsBridge(__instance.PlacingDef, dragPos, __instance.Map, stuff))
					bridgeCount++;

			if (bridgeCount == 0) return;

			//could just say wood here, this is still assuming it costs only one thing.
			ThingDefCountClass bridgeCost = TerrainDefOf.Bridge.costList.First();	

			Widgets.ThingIcon(new Rect(curX, curY, 27f, 27f), bridgeCost.thingDef);

			int totalCost = bridgeCost.count * bridgeCount;

			string label = $"{totalCost} ({TerrainDefOf.Bridge.LabelCap})";
			//This doesn't account for normal building cost + under bridge cost, but what can you do
			if (__instance.Map.resourceCounter.GetCount(bridgeCost.thingDef) < totalCost)
			{
				GUI.color = Color.red;
				label = label + " (" + "NotEnoughStoredLower".Translate() + ")";
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(curX + 29f, curY, 999f, 29f), label);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}
}
