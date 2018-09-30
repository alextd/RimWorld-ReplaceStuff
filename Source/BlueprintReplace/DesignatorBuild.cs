using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace Replace_Stuff.BlueprintReplace
{
	//public override AcceptanceReport CanDesignateCell(IntVec3 c)
	[HarmonyPatch(typeof(Designator_Build), "CanDesignateCell")]
	static class NoDesignateSameStuff
	{
		public static void Postfix(ref AcceptanceReport __result, IntVec3 c, Designator_Build __instance)
		{
			if (!__result.Accepted) return;

			BuildableDef entDef = __instance.PlacingDef;
			Rot4 placingRot = (Rot4)AccessTools.Field(typeof(Designator_Build), "placingRot").GetValue(__instance);
			Map map = __instance.Map;
			ThingDef stuffDef = (ThingDef)AccessTools.Field(typeof(Designator_Build), "stuffDef").GetValue(__instance);

			//It would be nice to pass stuff into CanPlaceBlueprintAt, but here we are
			if(c.GetThingList(map).All(thing => 
				GenConstruct.BuiltDefOf( thing.def ) == entDef &&
				thing.Position == c && thing.Rotation == placingRot &&
				(thing.Stuff == stuffDef || thing is Blueprint b && b.UIStuff() == stuffDef)))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	static class WipeBlueprints
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (__result || newEntDef != oldEntDef) return;

			if (newEntDef is ThingDef newD && newD.IsBlueprint &&
					oldEntDef is ThingDef oldD && oldD.IsBlueprint)
				__result = true;
		}
	}
}
