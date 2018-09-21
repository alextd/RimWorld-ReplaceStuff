using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;
using Harmony;

namespace Replace_Stuff.Replace
{
	[StaticConstructorOnStartup]
	static class ReserveSharing
	{
		//When you reserve a repalce frame, actually reserve the thing it's replacing
		static ReserveSharing()
		{
			HarmonyMethod prefix = new HarmonyMethod(typeof(ReserveSharing), nameof(ReserveSharing.Prefix));
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.Replace_Stuff.main");

			//HERE WE GO
			foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ReservationManager)))
			{
				if (method.GetParameters().Any(t => t.ParameterType == typeof(LocalTargetInfo)))
				{
					if (method.IsGenericMethod)
						harmony.Patch(method.MakeGenericMethod(new Type[] { typeof(JobDriver_TakeToBed) }), prefix, null);//JobDriver_TakeToBed seems to be the only one used;todo: foreach jobdriver subset
					else
						harmony.Patch(method, prefix, null);
				}
			}
		}

		//public bool CanReserve(Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		public static void Prefix(ref LocalTargetInfo target)//Luckily each parameter is named 'target' or this wouldn't work
		{
			if (target.Thing is ReplaceFrame replaceFrame)
				target = replaceFrame.oldThing;
		}
	}
}
