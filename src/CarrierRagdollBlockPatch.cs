using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(Body))]
internal static class CarrierRagdollBlockPatch
{
	[HarmonyPatch("Ragdoll")]
	[HarmonyPrefix]
	private static bool PreventCarriedRagdoll(Body __instance)
	{
		DualMode instance = DualMode.Instance;
		if ((Object)(object)instance != (Object)null && instance.Carrying && __instance == instance.CarriedBody)
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch("BrainDamageRagdoll")]
	[HarmonyPrefix]
	private static bool PreventCarriedBrainDamageRagdoll(Body __instance, ref IEnumerator __result)
	{
		DualMode instance = DualMode.Instance;
		if ((Object)(object)instance != (Object)null && instance.Carrying && __instance == instance.CarriedBody)
		{
			__result = EmptyRoutine();
			return false;
		}
		return true;
	}

	private static IEnumerator EmptyRoutine()
	{
		yield break;
	}
}
