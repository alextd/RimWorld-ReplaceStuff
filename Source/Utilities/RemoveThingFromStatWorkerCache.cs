using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Replace_Stuff.Utilities
{
	public static class RemoveThingFromStatWorkerCache
	{
		//class StatDef {
		//private StatWorker workerInt;
		public static AccessTools.FieldRef<StatDef, StatWorker> workerInt = AccessTools.FieldRefAccess<StatDef, StatWorker>("workerInt");

		//class StatWorker {
		//private Dictionary<Thing, StatCacheEntry> temporaryStatCache;
		//private Dictionary<Thing, float> immutableStatCache;
		public static AccessTools.FieldRef<StatWorker, Dictionary<Thing, StatCacheEntry>> temporaryStatCache = AccessTools.FieldRefAccess<StatWorker, Dictionary<Thing, StatCacheEntry>>("temporaryStatCache");
		public static AccessTools.FieldRef<StatWorker, Dictionary<Thing, float>> immutableStatCache = AccessTools.FieldRefAccess<StatWorker, Dictionary<Thing, float>>("immutableStatCache");

		public static void RemoveFromStatWorkerCaches(this Thing thing)
		{
			foreach (StatDef statDef in DefDatabase<StatDef>.AllDefsListForReading)
			{
				if (workerInt(statDef) is StatWorker worker)
				{
					if (temporaryStatCache(worker) is Dictionary<Thing, StatCacheEntry> tempCache)
						tempCache.Remove(thing);
					if (immutableStatCache(worker) is Dictionary<Thing, float> immuCache)
						immuCache.Remove(thing);
				}
			}
		}
	}
}
