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
		public static AccessTools.FieldRef<Designator_Build, Rot4> placingRot =
			AccessTools.FieldRefAccess<Designator_Build, Rot4>("placingRot");

		//protected override void DrawPlaceMouseAttachments(float curX, ref float curY)
		public static void Postfix(Designator_Build __instance, float curX, ref float curY)
		{
			List<TerrainDef> neededBridges = new List<TerrainDef>();

			ThingDef stuff = __instance.StuffDef;
			DesignationDragger dragger = Find.DesignatorManager.Dragger;
			IEnumerable<IntVec3> cells = dragger.Dragging ? dragger.DragCells :
				GenAdj.OccupiedRect(UI.MouseCell(), placingRot(__instance), __instance.PlacingDef.Size).Cells;

			foreach (IntVec3 dragPos in cells)
				if (PlaceBridges.GetNeededBridge(__instance.PlacingDef, dragPos, __instance.Map, stuff) is TerrainDef tdef)
					neededBridges.Add(tdef);

			if (neededBridges.Count == 0) return;

			Dictionary<ThingDef, int> bridgeTotalCost = new Dictionary<ThingDef, int>();
			float work = 0;
			foreach(TerrainDef bridgeDef in neededBridges)
			{
				work += bridgeDef.GetStatValueAbstract(StatDefOf.WorkToBuild);
				if(bridgeDef.costList != null)
					foreach (ThingDefCountClass bridgeCost in bridgeDef.costList)
					{
						bridgeTotalCost.TryGetValue(bridgeCost.thingDef, out int costCount);
						bridgeTotalCost[bridgeCost.thingDef] = costCount + bridgeCost.count;
					}
			}

			if(bridgeTotalCost.Count == 0)
			{
				string label = $"{StatDefOf.WorkToBuild.LabelCap}: {work.ToStringWorkAmount()} ({TerrainDefOf.Bridge.LabelCap})"; //Not bridgeCostDef.LabelCap

				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(curX + 29f, curY, 999f, 29f), label); //private const float DragPriceDrawNumberX
				curY += 29f;
				Text.Anchor = TextAnchor.UpperLeft;
			}

			foreach (var (bridgeCostDef, bridgeCostCount) in bridgeTotalCost.Select(x => (x.Key, x.Value)))
			{
				Widgets.ThingIcon(new Rect(curX, curY, 27f, 27f), bridgeCostDef);

				string label = $"{bridgeCostCount} ({TerrainDefOf.Bridge.LabelCap})"; //Not bridgeCostDef.LabelCap
				//This doesn't account for normal building cost + under bridge cost, but what can you do
				if (__instance.Map.resourceCounter.GetCount(bridgeCostDef) < bridgeCostCount)
				{
					GUI.color = Color.red;
					label = label + " (" + "NotEnoughStoredLower".Translate() + ")";
				}
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(curX + 29f, curY, 999f, 29f), label); //private const float DragPriceDrawNumberX
				curY += 29f;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			GUI.color = Color.white;
		}
	}
}
