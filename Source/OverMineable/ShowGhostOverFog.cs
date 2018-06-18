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
	public static class ShowGhostOverFog
	{
		//private static Graphic GhostGraphicFor(Graphic baseGraphic, ThingDef thingDef, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo TransparentInfo = AccessTools.Field(typeof(ShaderDatabase), "Transparent");
			FieldInfo MetaOverlayInfo = AccessTools.Field(typeof(ShaderDatabase), "MetaOverlay");

			foreach (CodeInstruction i in instructions)
			{
				//ldsfld       class [UnityEngine]UnityEngine.Shader Verse.ShaderDatabase::Transparent
				if (i.opcode == OpCodes.Ldsfld && i.operand == TransparentInfo)
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
			//__result.altitudeLayer = AltitudeLayer.MetaOverlays;
			//__result.drawerType = DrawerType.RealtimeOnly;
			//__result.graphicData.shaderType = ShaderType.MetaOverlay;
		}
	}
}
