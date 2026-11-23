using HarmonyLib;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "get_bleedClottingSpeed")]
internal static class BoostClottingPatch
{
	private static void Postfix(Body __instance, ref float __result)
	{
		try
		{
			if (IntimateBoost.Applies(__instance) && __result < 1f)
			{
				__result = 1f;
			}
		}
		catch
		{
		}
	}
}
