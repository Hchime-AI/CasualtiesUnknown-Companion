using HarmonyLib;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "HandlePeriodicChecks")]
internal static class BoostImmunityPatch
{
	private static void Postfix(Body __instance)
	{
		try
		{
			if (IntimateBoost.Applies(__instance))
			{
				if (__instance.immunity < 150f)
				{
					__instance.immunity = 150f;
				}
				if (__instance.curImmunityMult > 0.25f)
				{
					__instance.curImmunityMult = 0.25f;
				}
			}
		}
		catch
		{
		}
	}
}
