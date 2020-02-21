using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.Other
{
	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	static class PawnBlockConstruction
	{
		static bool Prefix(ref bool __result, Thing t)
		{
			if (t is Pawn)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
