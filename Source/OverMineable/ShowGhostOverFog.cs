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
	/* 1.0 has cooler blueprints/ghost shader which this would revert to meta
	 * TODO: find a way to display over fog but with edgedetect shader
	 * 
	[HarmonyPatch(typeof(GhostUtility), "GhostGraphicFor")]
	public static class ShowGhostOverFog
	{
		//public static Graphic GhostGraphicFor(Graphic baseGraphic, ThingDef thingDef, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo EdgeDetectInfo = AccessTools.Field(typeof(ShaderTypeDefOf), "EdgeDetect");
			FieldInfo MetaOverlayInfo = AccessTools.Field(typeof(ShaderTypeDefOf), "MetaOverlay");

			foreach (CodeInstruction i in instructions)
			{
				//ldsfld       class Verse.ShaderTypeDef RimWorld.ShaderTypeDefOf::EdgeDetect
				if (i.opcode == OpCodes.Ldsfld && i.operand == EdgeDetectInfo)
					i.operand = MetaOverlayInfo;
				yield return i;
			}
		}
	}


	[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing")]
	public static class ShowBluePrintOverFog
	{
		//private static ThingDef NewBlueprintDef_Thing(ThingDef def, bool isInstallBlueprint, ThingDef normalBlueprint = null)
		public static void Postfix(ref ThingDef __result)
		{
			//This seems like it would work butno:
			//__result.altitudeLayer = AltitudeLayer.MetaOverlays;
			//__result.drawerType = DrawerType.MapMeshAndRealTime;

			__result.graphicData.shaderType = ShaderTypeDefOf.MetaOverlay;
		}
	}
	*/
}
