using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(WoundViewLimb), "OnPointerEnter")]
internal static class WoundViewLimbPointerPatch
{
	private static bool Prefix(WoundViewLimb __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.woundview == (Object)null)
		{
			return true;
		}
		if (MedicalPartnerAssistant.IsInspectingPartner)
		{
			int limb = __instance.limb;
			if (__instance.woundview.limbImageFlash != null && limb >= 0 && limb < __instance.woundview.limbImageFlash.Length)
			{
				__instance.woundview.limbImageFlash[limb] = 0.35f;
			}
			return false;
		}
		Body body = __instance.woundview.body;
		if ((Object)(object)body == (Object)null || body.limbs == null)
		{
			return true;
		}
		int limb2 = __instance.limb;
		if (limb2 < 0 || limb2 >= body.limbs.Length)
		{
			return true;
		}
		Limb val = body.limbs[limb2];
		if ((Object)(object)val == (Object)null || val.dismembered)
		{
			return true;
		}
		__instance.woundview.limbLookingAt = limb2;
		if ((Object)(object)PlayerCamera.main != (Object)null)
		{
			PlayerCamera.main.selectedLimb = val;
		}
		if (__instance.woundview.limbImageFlash != null && limb2 < __instance.woundview.limbImageFlash.Length)
		{
			__instance.woundview.limbImageFlash[limb2] = 1f;
		}
		return false;
	}
}
