using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(MoodleManager), "AddAllMoodles")]
internal static class IntimateMoodleHook
{
	private static void Prefix(MoodleManager __instance)
	{
		IntimateController instance = IntimateController.Instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)__instance == (Object)null || (!instance.InMast && !instance.InAfterglow && !instance.AtPeak && !IntimateController.Debt))
		{
			return;
		}
		Body val = (((Object)(object)CompanionHost.Instance == (Object)null) ? null : CompanionHost.Instance.CurrentBody);
		if ((Object)(object)val == (Object)null || !val.alive)
		{
			return;
		}
		try
		{
			if (IntimateController.Debt)
			{
				__instance.AddMoodle(8, "exertion", LocalizationManager.T("debtName"), LocalizationManager.T("debtDesc").Replace("{n}", IntimateController.DebtCalorieNeed.ToString()), false, false);
			}
			if (instance.InMast)
			{
				__instance.AddMoodle(8, "energized", LocalizationManager.T("mastName"), LocalizationManager.T("mastDesc"), false, false);
			}
			if (!IntimateController.Debt && (instance.AtPeak || instance.InAfterglow))
			{
				__instance.AddMoodle(8, "happy", LocalizationManager.T("afterName"), LocalizationManager.T("afterDesc"), false, false);
			}
		}
		catch
		{
		}
	}
}
