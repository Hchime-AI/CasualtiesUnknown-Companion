using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(WoundView), "Awake")]
internal static class WoundViewAwakePatch
{
	private static void Postfix(WoundView __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		if ((Object)(object)((Component)__instance).GetComponent<MedicalPartnerAssistant>() == (Object)null)
		{
			((Component)__instance).gameObject.AddComponent<MedicalPartnerAssistant>();
		}
		WoundViewLimb[] componentsInChildren = ((Component)__instance).GetComponentsInChildren<WoundViewLimb>(true);
		foreach (WoundViewLimb val in componentsInChildren)
		{
			if ((Object)(object)val != (Object)null && (Object)(object)((Component)val).GetComponent<CU_LimbClickHandler>() == (Object)null)
			{
				((Component)val).gameObject.AddComponent<CU_LimbClickHandler>().limbView = val;
			}
		}
	}
}
