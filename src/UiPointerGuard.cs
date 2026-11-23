using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CUCompanion
{
	/// <summary>
	/// 让游戏的「鼠标是不是压在 UI 上」判定认得我们的 IMGUI 窗口。
	/// IMGUI 不吃 uGUI 事件，所以窗口开着时点按钮会顺带打到游戏本体；
	/// 这里在 OnGUI 里登记窗口矩形，再把判定补进 UIUtil.IsPointerOverUIElement。
	/// 只在指针真的压在窗口上时才算，窗口外的点击照常进游戏。
	/// </summary>
	internal static class UiPointerGuard
	{
		private static float lastHitAt = -999f;
		private static Rect lastRect;
		private static int marksThisFrame;

		/// <summary>鼠标位置换算成 IMGUI 逻辑坐标（窗口按 UiScale 缩放，所以要除回去）。</summary>
		public static Vector2 GuiMouse()
		{
			float v = UiScale.Value;
			return new Vector2(Input.mousePosition.x / v, ((float)Screen.height - Input.mousePosition.y) / v);
		}

		/// <summary>在 OnGUI 里画窗口前后调一次，把窗口矩形登记进来。</summary>
		public static void Mark(Rect rect)
		{
			marksThisFrame++;
			if (!rect.Contains(GuiMouse()))
			{
				return;
			}
			lastHitAt = Time.unscaledTime;
			lastRect = rect;
		}

		/// <summary>指针是否正压在某个刚登记的窗口上（0.2 秒内有效，窗口关掉后自动失效）。</summary>
		public static bool Hovering
		{
			get { return Time.unscaledTime - lastHitAt < 0.2f; }
		}

		public static Rect HoveredRect
		{
			get { return lastRect; }
		}

		public static int MarksThisFrame
		{
			get { return marksThisFrame; }
		}
	}

	[HarmonyPatch(typeof(UIUtil), "IsPointerOverUIElement", new Type[] { typeof(List<RaycastResult>) })]
	internal static class UiUtil_IsPointerOverUiElement_Patch
	{
		private static void Postfix(ref bool __result)
		{
			if (__result)
			{
				return;
			}
			try
			{
				if (UiPointerGuard.Hovering)
				{
					__result = true;
				}
			}
			catch
			{
			}
		}
	}
}
