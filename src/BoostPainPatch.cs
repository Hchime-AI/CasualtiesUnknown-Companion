using HarmonyLib;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "HandleBody")]
internal static class BoostPainPatch
{
	private static void Postfix(Body __instance)
	{
		try
		{
			if (IntimateBoost.Applies(__instance))
			{
				if (__instance.averagePain > 8f)
				{
					__instance.averagePain = 8f;
				}
				if (__instance.painShock > 0f)
				{
					__instance.painShock = 0f;
				}
			}
		}
		catch
		{
		}
	}
}
