using HarmonyLib;
using UnityEngine;

namespace CUCompanion;

[HarmonyPatch(typeof(Body), "PlaceBody")]
internal static class DualPlaceBodyPatch
{
	private static void Postfix(Body __instance)
	{
		try
		{
			DualMode instance = DualMode.Instance;
			if ((Object)(object)instance == (Object)null || !instance.Active || (Object)(object)__instance == (Object)null)
			{
				return;
			}
			Body other = null;
			if (__instance == instance.Partner && (Object)(object)instance.Player != (Object)null && instance.Player.alive)
			{
				other = instance.Player;
			}
			else if (__instance == instance.Player && (Object)(object)instance.Partner != (Object)null && instance.Partner.alive)
			{
				other = instance.Partner;
			}
			if ((Object)(object)other == (Object)null)
			{
				return;
			}
			Vector3 anchorPos = ((Component)__instance).transform.position;
			float facing = (__instance.isRight ? 1f : -1f);
			Vector3 targetPos = new Vector3(anchorPos.x - 0.45f * facing, anchorPos.y + 0.05f, anchorPos.z);
			if ((Object)(object)other.rb != (Object)null)
			{
				other.rb.velocity = Vector2.zero;
				other.rb.angularVelocity = 0f;
				other.rb.position = new Vector2(targetPos.x, targetPos.y);
			}
			other.moveDir = Vector2.zero;
			((Component)other).transform.position = targetPos;

			// 同步重置所有物理肢体位置与刚体速度，杜绝跨越数百米造成的关节拉扯 NaN 崩溃
			if (other.limbs != null)
			{
				for (int i = 0; i < other.limbs.Length; i++)
				{
					Limb l = other.limbs[i];
					if ((Object)(object)l != (Object)null)
					{
						if ((Object)(object)l.rb != (Object)null)
						{
							l.rb.velocity = Vector2.zero;
							l.rb.angularVelocity = 0f;
							l.rb.position = new Vector2(targetPos.x, targetPos.y);
						}
						((Component)l).transform.position = targetPos;
					}
				}
			}
		}
		catch
		{
		}
	}
}
