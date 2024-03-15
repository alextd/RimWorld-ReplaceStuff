using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
namespace Replace_Stuff.PlaceBridges
{
	//a list of terrain that adds new affordances, like what a bridge does
	[StaticConstructorOnStartup]
	public static class BridgelikeTerrain
	{
		//If you have TerrainDef and need Affordance, you can build bridge from from TerrainDefs to get that affordance
		//TODO: group terrains by affordances. eg different water types all have the same set of bridges that would work but are all handled separately
		private static Dictionary<(TerrainDef, TerrainAffordanceDef), HashSet<TerrainDef>> bridgesForTerrain;
		public static List<TerrainDef> allBridgeTerrains;

		private static bool IsFloorBase(this TerrainDef def)
		{
			//Nothing special with these, is not a bridgelike terrain
			return def.terrainAffordanceNeeded == TerrainAffordanceDefOf.Heavy &&
				def.affordances.Count == 3
				&& def.affordances.Contains(TerrainAffordanceDefOf.Light)
				&& def.affordances.Contains(TerrainAffordanceDefOf.Medium)
				&& def.affordances.Contains(TerrainAffordanceDefOf.Heavy);
			//Theoretically this should check if medium gives light/medium and light gives light but those don't exist
			//Also VFE has duplicate Light/Medium/Heavy/Light/Medium/Heavy so this doesn't catch that but isn't a problem.
		}

		static BridgelikeTerrain()
		{
			//Ignore providing diggable because VFE's dirt can turn any terrain into diggable
			HashSet<TerrainAffordanceDef> ignoreAff = new HashSet<TerrainAffordanceDef>
			{
				DefDatabase<TerrainAffordanceDef>.GetNamed("Diggable"),
				DefDatabase<TerrainAffordanceDef>.GetNamed("GrowSoil")
			};


			//If you have Affordance 1 and need Affordance 2, you can build one of these TerrainDef
			var affordanceBridges = new Dictionary<(TerrainAffordanceDef, TerrainAffordanceDef), List<TerrainDef>>();

			//Find bridge terrains:
			foreach (TerrainDef terdef in DefDatabase<TerrainDef>.AllDefs)
			{
				//Filter out some terrains
				if (terdef.IsFloorBase()) continue;//nothing special, so easy pass on these

				TerrainAffordanceDef bridgeAff = terdef.terrainAffordanceNeeded;
				if (bridgeAff == null) continue;//nothing needed implies it's not buildable

				if (ignoreAff.Contains(bridgeAff)) continue;//don't care to bridge from these types aka growsoil -> heavy, marsh->tilled soil 

				if (!terdef.Removable) continue;//If you can't remove it, it's a permanent change, let's not do that automatically

				foreach (TerrainAffordanceDef provideAff in terdef.affordances)
				{
					if (provideAff == bridgeAff) continue;  //Already can do, not an upgrade
					if (ignoreAff.Contains(provideAff)) continue; //Don't want to bridge to this affordance (aka diggable)

					//If some terrain exists with 'bridgeAff' and we want to place something that needs 'provideAff',
					//This terrain can act as a bridge, because it needs 'bridgeAff' and can provide 'provideAff'

					if (affordanceBridges.TryGetValue((bridgeAff, provideAff), out List<TerrainDef> bridgeTerrains))
						bridgeTerrains.Add(terdef);
					else
						affordanceBridges[(bridgeAff, provideAff)] = new List<TerrainDef> { terdef };
				}
			}

			bridgesForTerrain = new Dictionary<(TerrainDef, TerrainAffordanceDef), HashSet<TerrainDef>>();
			allBridgeTerrains = new List<TerrainDef>();

			//Check for Affordances are actually neeeded by any buildind
			HashSet<TerrainAffordanceDef> actuallyRequiredAffordances = new HashSet<TerrainAffordanceDef>();
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsBuildingArtificial))
			{
				var aff = thingDef.terrainAffordanceNeeded;
				if (aff != null && !ignoreAff.Contains(aff))
					actuallyRequiredAffordances.Add(aff);
			}

			//Log.Message($"All affordances: {DefDatabase<TerrainAffordanceDef>.AllDefs.ToStringSafeEnumerable()}");
			Log.Message($"Affordances worth bridging: {actuallyRequiredAffordances.ToStringSafeEnumerable()}");

			foreach (TerrainAffordanceDef needDef in actuallyRequiredAffordances)
			{
				foreach (TerrainDef terDef in DefDatabase<TerrainDef>.AllDefs)
				{
					//If we have terdef and we need affdef
					if (terDef.Removable ||	//Can't build terrain over removable terrain
						terDef.affordances.Contains(needDef)) continue;//Can already do it

					HashSet<TerrainDef> possibleBridges = null;//Bridge terrains to get needDef on top of terDef
					foreach (TerrainAffordanceDef affDef in terDef.affordances)
					{
						if (affordanceBridges.TryGetValue((affDef, needDef), out List<TerrainDef> bridgeTerrains))
						{
							if (possibleBridges == null && !bridgesForTerrain.TryGetValue((terDef, needDef), out possibleBridges))
							{
								possibleBridges = new HashSet<TerrainDef>();
								bridgesForTerrain[(terDef, needDef)] = possibleBridges;
							}
							//Log.Message($"Adding {terDef} => {bridgeTerrains.ToStringSafeEnumerable()} for {affDef} => {needDef}");
							possibleBridges.AddRange(bridgeTerrains);
						}
					}
					if (possibleBridges != null)
					{
						allBridgeTerrains.AddRange(possibleBridges);
					}
					//else
						//Log.Message($"There is no bridge for {terDef} => {needDef}");
				}
			}
			allBridgeTerrains.RemoveDuplicates();
			Log.Message($"Bridges: {allBridgeTerrains.ToStringSafeEnumerable()}");
		}

		public static bool IsBridgelike(this BuildableDef tdef) => allBridgeTerrains.Contains(tdef);

		public static TerrainDef FindBridgeFor(TerrainDef tDef, TerrainAffordanceDef needed, Map map)
		{
			TerrainDef bestBridge = null;
			TerrainDef backupBridge = null;
			if (bridgesForTerrain.TryGetValue((tDef, needed), out var bridges))
			{
				foreach (TerrainDef bridge in allBridgeTerrains)
					if (bridges.Contains(bridge))
					{
						if (backupBridge == null) backupBridge = bridge;  //First possible option

						ThingDefCount cost = bridge.CostList?.FirstOrDefault();
						if (cost.ThingDef == null) //Free bridge? Okay. Or some mod's error. Not my fault.
							return bridge;

						int resourceCount = map.resourceCounter.GetCount(cost.ThingDef);

						if (resourceCount > cost.Count * 10)
							return bridge;//Plently. Use this.

						if (resourceCount > 0)
							bestBridge = bridge;//Not enough but at least this will work.
					}
			}
			return bestBridge ?? backupBridge;
		}


		public static void Reorder(int terIndex, int newIndex)
		{
			if (terIndex == newIndex)	return;

			allBridgeTerrains.Insert(newIndex, allBridgeTerrains[terIndex]);
			allBridgeTerrains.RemoveAt((terIndex < newIndex) ? terIndex : (terIndex + 1));
		}
	}
}
