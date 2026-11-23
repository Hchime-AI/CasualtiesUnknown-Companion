using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;

namespace CUCompanion;

public static class InputGuard
{
	[HarmonyPatch(typeof(Input), "GetKey", new Type[] { typeof(KeyCode) })]
	private static class PGetKey
	{
		private static bool Prefix(KeyCode key, ref bool __result)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			if (!Capturing || Allowed(key))
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetKeyDown", new Type[] { typeof(KeyCode) })]
	private static class PGetKeyDown
	{
		private static bool Prefix(KeyCode key, ref bool __result)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Invalid comparison between Unknown and I4
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			if (!Capturing)
			{
				return true;
			}
			if ((int)key == 27)
			{
				string inputString = Input.inputString;
				if (!string.IsNullOrEmpty(inputString) && inputString.IndexOf('\u001b') >= 0)
				{
					escapePressed = true;
				}
				__result = false;
				return false;
			}
			if (Allowed(key))
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetKeyUp", new Type[] { typeof(KeyCode) })]
	private static class PGetKeyUp
	{
		private static bool Prefix(KeyCode key, ref bool __result)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			if (!Capturing || Allowed(key))
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetAxis")]
	private static class PGetAxis
	{
		private static bool Prefix(ref float __result)
		{
			if (!Capturing)
			{
				return true;
			}
			__result = 0f;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetAxisRaw")]
	private static class PGetAxisRaw
	{
		private static bool Prefix(ref float __result)
		{
			if (!Capturing)
			{
				return true;
			}
			__result = 0f;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetMouseButtonDown")]
	private static class PMouseDown
	{
		private static bool Prefix(ref bool __result)
		{
			if (!Capturing || OverOurUi())
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetMouseButtonUp")]
	private static class PMouseUp
	{
		private static bool Prefix(ref bool __result)
		{
			if (!Capturing || OverOurUi())
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Input), "GetMouseButton")]
	private static class PMouseHeld
	{
		private static bool Prefix(ref bool __result)
		{
			if (!Capturing || OverOurUi())
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	public static bool Capturing;

	public static bool HardLock;

	private static readonly KeyCode[] Passthrough;

	private static bool escapePressed;

	public static bool ConsumeEscape()
	{
		if (!escapePressed)
		{
			return false;
		}
		escapePressed = false;
		return true;
	}

	private static bool Allowed(KeyCode k)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between I4 and Unknown
		if (HardLock)
		{
			return false;
		}
		for (int i = 0; i < Passthrough.Length; i++)
		{
			if ((int)Passthrough[i] == (int)k)
			{
				return true;
			}
		}
		return false;
	}

	private static bool fieldHold;

	private static int menuHolds;

	public static void Set(bool on)
	{
		fieldHold = on;
		Apply();
	}

	public static void MenuHold(bool on)
	{
		menuHolds = Mathf.Max(0, menuHolds + (on ? 1 : -1));
		Apply();
	}

	private static void Apply()
	{
		Capturing = fieldHold || menuHolds > 0;
		try
		{
			Input.imeCompositionMode = (IMECompositionMode)(Capturing ? 1 : 0);
		}
		catch
		{
		}
	}

	private static bool IsOurCanvas(Canvas canvas)
	{
		if ((Object)(object)canvas == (Object)null)
		{
			return false;
		}
		Transform transform = ((Component)canvas).transform;
		while ((Object)(object)transform != (Object)null)
		{
			string name = transform.name;
			if (name == "CUCompanionHUD" || name.StartsWith("CUCompanion"))
			{
				return true;
			}
			transform = transform.parent;
		}
		return false;
	}

	private static bool IsOurUiObject(GameObject go)
	{
		if ((Object)(object)go == (Object)null)
		{
			return false;
		}
		Transform transform = go.transform;
		while ((Object)(object)transform != (Object)null)
		{
			string name = transform.name;
			if (name == "CUCompanionHUD" ||
				name == "CUCompanionPanel" ||
				name == "CUCompanionVeil" ||
				name == "CUCompanionMonitor" ||
				name.StartsWith("CU_") ||
				name.StartsWith("CUCompanion") ||
				name.StartsWith("part_"))
			{
				return true;
			}
			if (transform.GetComponent<NativeHit>() != null ||
				transform.GetComponent<NativeField>() != null ||
				transform.GetComponent<NativeDrag>() != null ||
				transform.GetComponent<NativePanel>() != null ||
				transform.GetComponent<HealthMonitorGui>() != null ||
				transform.GetComponent<CU_DoctorItemSlot>() != null ||
				transform.GetComponent<CU_LimbClickHandler>() != null)
			{
				return true;
			}
			transform = transform.parent;
		}
		return false;
	}

	public static bool OverOurUi()
	{
		if (NativePanel.IsOpen)
		{
			return true;
		}
		if (UiPointerGuard.Hovering)
		{
			return true;
		}
		try
		{
			EventSystem current = EventSystem.current;
			if ((Object)(object)current == (Object)null)
			{
				return false;
			}
			PointerEventData val = new PointerEventData(current)
			{
				position = (Vector2)(Input.mousePosition)
			};
			List<RaycastResult> list = new List<RaycastResult>();
			current.RaycastAll(val, list);
			for (int i = 0; i < list.Count; i++)
			{
				GameObject hitGo = list[i].gameObject;
				if ((Object)(object)hitGo != (Object)null && IsOurUiObject(hitGo))
				{
					return true;
				}
				Component module = (Component)(object)list[i].module;
				if (!((Object)(object)module == (Object)null))
				{
					Canvas componentInParent = module.GetComponentInParent<Canvas>();
					if ((Object)(object)componentInParent != (Object)null && IsOurCanvas(componentInParent))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	static InputGuard()
	{
		Passthrough = new KeyCode[11]
		{
			KeyCode.Return,
			KeyCode.KeypadEnter,
			KeyCode.Backspace,
			KeyCode.T,
			KeyCode.F4,
			KeyCode.F6,
			KeyCode.F8,
			KeyCode.F9,
			KeyCode.F10,
			KeyCode.F11,
			KeyCode.F12
		};
	}
}
