namespace Replace_Stuff
{
	[Harmony.HarmonyPatch(typeof(RimWorld.DefOfHelper), "EnsureInitializedInCtor")]
	public static class EnsureInitializedInCtor
	{
		public static bool Prefix()
		{
			//No need to display this warning.
			return false;
		}
	}
}
