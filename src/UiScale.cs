using System;
using UnityEngine;
using UnityEngine.UI;

namespace CUCompanion;

/// <summary>
/// 全局界面比例。一个数值同时管两套 UI：
///   - IMGUI 窗口（设置页、人设页、历史框）：用 GUI.matrix 整体缩放，文字和控件一起放大；
///   - uGUI HUD（历史面板、输入条、互动栏、状态表）：改 CanvasScaler 的参考分辨率。
/// 比例来自 settings.json 的 uiScale，默认 1.00。
/// </summary>
internal static class UiScale
{
	internal const float Min = 0.7f;
	internal const float Max = 2f;

	private const float BaseWidth = 2560f;

	private const float BaseHeight = 1440f;

	private static float applied = -1f;

	/// <summary>当前比例。</summary>
	internal static float Value
	{
		get
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			if (cfg == null)
			{
				return 1f;
			}
			return Mathf.Clamp(cfg.uiScale, Min, Max);
		}
	}

	/// <summary>缩放后的逻辑宽度（IMGUI 里当成"屏幕宽"来排版）。</summary>
	internal static float SW => (float)Screen.width / Value;

	/// <summary>缩放后的逻辑高度。</summary>
	internal static float SH => (float)Screen.height / Value;

	/// <summary>开始 IMGUI 缩放（必须与 End 成对）。</summary>
	internal static void Begin()
	{
		float value = Value;
		if (Mathf.Abs(value - 1f) < 0.001f)
		{
			GUI.matrix = Matrix4x4.identity;
			return;
		}
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(value, value, 1f));
	}

	/// <summary>结束 IMGUI 缩放。</summary>
	internal static void End()
	{
		GUI.matrix = Matrix4x4.identity;
	}

	/// <summary>把比例应用到 uGUI HUD：参考分辨率变小 = 界面变大。</summary>
	internal static void ApplyHud(CanvasScaler scaler)
	{
		if ((UnityEngine.Object)(object)scaler == (UnityEngine.Object)null)
		{
			return;
		}
		float value = Value;
		if (Mathf.Abs(value - applied) < 0.001f)
		{
			return;
		}
		applied = value;
		try
		{
			scaler.referenceResolution = new Vector2(BaseWidth / value, BaseHeight / value);
		}
		catch
		{
		}
	}

	/// <summary>GUI 坐标 → 真实屏幕坐标（用于 UiPointerGuard 这类要和 Input.mousePosition 对齐的地方）。</summary>
	internal static Vector2 ToScreen(Vector2 guiPoint)
	{
		float value = Value;
		return new Vector2(guiPoint.x * value, guiPoint.y * value);
	}
}
