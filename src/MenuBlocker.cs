using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CUCompanion;

internal static class MenuBlocker
{
	private static readonly HashSet<string> keys = new HashSet<string>();

	private static GameObject root;

	private static Image plate;

	private static Texture2D backdrop;

	private static int lastCount = -1;

	private static bool held;

	internal static bool AnyOpen => keys.Count > 0;

	internal static void Require(string key, bool on)
	{
		if (on)
		{
			keys.Add(key);
		}
		else
		{
			keys.Remove(key);
		}
	}

	internal static void Sync()
	{
		bool flag = keys.Count > 0;
		if (flag != held)
		{
			held = flag;
			InputGuard.MenuHold(flag);
		}
		Ensure();
		if ((Object)(object)root == (Object)null)
		{
			return;
		}
		if (root.activeSelf != flag)
		{
			root.SetActive(flag);
		}
		if (lastCount != keys.Count)
		{
			lastCount = keys.Count;
			root.transform.SetAsLastSibling();
		}
	}

	internal static void Paint()
	{
		if ((Object)(object)backdrop == (Object)null)
		{
			backdrop = new Texture2D(1, 1);
			backdrop.SetPixel(0, 0, new Color(0.015f, 0.019f, 0.023f, 1f));
			backdrop.Apply();
			backdrop.hideFlags = HideFlags.HideAndDontSave;
		}
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), (Texture)(object)backdrop);
	}

	private static void Ensure()
	{
		if (!((Object)(object)root == (Object)null))
		{
			return;
		}
		root = new GameObject("CU_MenuBlocker", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		Canvas canvas = root.GetComponent<Canvas>();
		canvas.renderMode = (RenderMode)0;
		canvas.overrideSorting = true;
		canvas.sortingOrder = 32000;
		CanvasScaler canvasScaler = root.GetComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = (CanvasScaler.ScaleMode)1;
		canvasScaler.referenceResolution = new Vector2(2560f, 1440f);
		canvasScaler.matchWidthOrHeight = 0.5f;
		GameObject val = new GameObject("Plate", typeof(RectTransform), typeof(Image));
		RectTransform component = val.GetComponent<RectTransform>();
		component.SetParent(root.transform, false);
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		plate = val.GetComponent<Image>();
		plate.color = new Color(0f, 0f, 0f, 0f);
		plate.raycastTarget = true;
		root.SetActive(false);
		Object.DontDestroyOnLoad(root);
	}
}
