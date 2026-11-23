using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;

namespace CUCompanion;

public static class ModUIInputBlocker
{
	[HarmonyPatch(typeof(PlayerCamera), "HandleAttacks")]
	internal static class AttackSuppressionPatch
	{
		[HarmonyPrefix]
		private static bool Prefix()
		{
			if (IsPointerOverAnyModUI())
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(UIUtil), "IsPointerOverUIElement", new Type[] { })]
	internal static class UIUtilNoArgPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(ref bool __result)
		{
			if (IsPointerOverAnyModUI())
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(UIUtil), "IsPointerOverUIElement", new Type[] { typeof(List<RaycastResult>) })]
	internal static class UIUtilWithListPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(ref bool __result)
		{
			if (IsPointerOverAnyModUI())
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	private static readonly List<RectTransform> RegisteredUIRects = new List<RectTransform>();

	public static void Register(RectTransform rt)
	{
		if (!((Object)(object)rt == (Object)null) && !RegisteredUIRects.Contains(rt))
		{
			RegisteredUIRects.Add(rt);
		}
	}

	public static void Unregister(RectTransform rt)
	{
		if (!((Object)(object)rt == (Object)null))
		{
			RegisteredUIRects.Remove(rt);
		}
	}

	public static bool IsPointerOverAnyModUI()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)EventSystem.current != (Object)null && EventSystem.current.IsPointerOverGameObject())
			{
				return true;
			}
		}
		catch
		{
		}
		if (MedicalPartnerAssistant.IsInspectingPartner && MedicalPartnerAssistant.IsHovered)
		{
			return true;
		}
		Vector2 val = (Vector2)(Input.mousePosition);
		for (int num = RegisteredUIRects.Count - 1; num >= 0; num--)
		{
			RectTransform val2 = RegisteredUIRects[num];
			if ((Object)(object)val2 == (Object)null)
			{
				RegisteredUIRects.RemoveAt(num);
			}
			else if (((Component)val2).gameObject.activeInHierarchy)
			{
				try
				{
					if (RectTransformUtility.RectangleContainsScreenPoint(val2, val, (Camera)null))
					{
						return true;
					}
				}
				catch
				{
				}
			}
		}
		return false;
	}
}
