using HarmonyLib;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "get_bleedingSpeedMultiplier")]
internal static class BoostBleedMultPatch
{
	private static void Postfix(Body __instance, ref float __result)
	{
		try
		{
			if (IntimateBoost.Applies(__instance) && __result > 0.15f)
			{
				__result = 0.15f;
			}
		}
		catch
		{
		}
	}
}
