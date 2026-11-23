using HarmonyLib;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "get_inCardiacArrest")]
internal static class BoostNoArrestPatch
{
	private static void Postfix(Body __instance, ref bool __result)
	{
		try
		{
			if (__result && IntimateBoost.Applies(__instance))
			{
				__result = false;
			}
		}
		catch
		{
		}
	}
}
