using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(InvButton), "get_body")]
internal static class InvButtonTargetBodyPatch
{
	private static void Postfix(InvButton __instance, ref Body __result)
	{
		if ((Object)(object)__instance != (Object)null && __instance.woundView)
		{
			WoundView view = WoundView.view;
			if ((Object)(object)view != (Object)null && ((Component)view).gameObject.activeInHierarchy && (Object)(object)view.body != (Object)null)
			{
				__result = view.body;
			}
		}
	}
}
