using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

/// <summary>
/// 「机器核心」是一条状态，不是一堆散落在各处的补丁。
///
/// 层级只有一个方向：设置页开关 → 机器核心状态 → 状态名下的全部效果。
/// 状态名下的效果现在只有三样，且只有这里能写：
///   1. 生理锁定（意识/情绪/精力/睡眠/睡眠债/休克/疼痛休克/心颤/脑状态）
///   2. 倒地自动起身
///   3. moodle 图标「机器核心 / Machine Core」
///
/// 其它任何地方都不许再判断 cfg.dualMachineCore，一律问 MachineCore.On。
/// </summary>
internal static class MachineCore
{
	/// <summary>机器核心的三种存在形态。</summary>
	internal enum Phase
	{
		/// <summary>设置里关掉，或不在双人模式。状态不存在。</summary>
		Off,
		/// <summary>设置里开着，但 P1Alloy 不在场（未生成 / 已死）。状态待机，不产生效果。</summary>
		Standby,
		/// <summary>P1Alloy 在场且存活。状态生效，名下的全部效果都在跑。</summary>
		On
	}

	private const float StatInterval = 0.25f;

	private static readonly string[] IconCandidates = new string[6]
	{
		"energized",
		"stimulated",
		"stimulants",
		"keratinbooster",
		"loosescrews",
		"prosthetic"
	};

	private static Phase phase;

	private static float nextStat;

	private static string cachedIcon;

	private static float lastLogAt;

	/// <summary>当前形态。</summary>
	internal static Phase Current => phase;

	/// <summary>状态是否真的在生效（图标亮着、数值被锁着）。</summary>
	internal static bool On => phase == Phase.On;

	private static Body P1
	{
		get
		{
			DualMode instance = DualMode.Instance;
			return (Object)(object)instance == (Object)null ? null : instance.Player;
		}
	}

	/// <summary>退场/换局时清状态。</summary>
	internal static void Reset()
	{
		phase = Phase.Off;
		nextStat = 0f;
		cachedIcon = null;
	}

	/// <summary>每帧一次的唯一入口：决定状态、再让状态自己施加效果。</summary>
	internal static void Tick()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		Body body = P1;
		Phase next;
		if (cfg == null || !cfg.dualMode || !cfg.dualMachineCore)
		{
			next = Phase.Off;
		}
		else if ((Object)(object)body == (Object)null || !body.alive)
		{
			next = Phase.Standby;
		}
		else
		{
			next = Phase.On;
		}
		if (next != phase)
		{
			phase = next;
			Log("机器核心：" + Describe(next));
		}
		if (phase != Phase.On)
		{
			return;
		}
		try
		{
			LockBrain(body);
			if (Time.unscaledTime < nextStat)
			{
				return;
			}
			nextStat = Time.unscaledTime + StatInterval;
			LockStats(body);
		}
		catch (Exception ex)
		{
			Log("机器核心施加效果失败：" + ex.GetType().Name + " " + ex.Message);
		}
	}

	/// <summary>脑状态：每帧钉在 100（游戏 alive => brainHealth > 0，所以钉住就不会脑死亡）。</summary>
	private static void LockBrain(Body body)
	{
		if (body.brainHealth < 100f)
		{
			body.brainHealth = 100f;
		}
	}

	/// <summary>其余生理锁定：0.25 秒一次，够用且几乎不占性能。</summary>
	private static void LockStats(Body body)
	{
		body.consciousness = 100f;
		body.sleeping = false;
		if (body.energy < 96f)
		{
			body.energy = 96f;
		}
		if (body.badSleepAmount > 0f)
		{
			body.badSleepAmount = 0f;
		}
		if (body.happiness < 100f)
		{
			body.happiness = 100f;
		}
		if (body.opiateHappiness < 100f)
		{
			body.opiateHappiness = 100f;
		}
		if (body.antidepressantHappiness < 100f)
		{
			body.antidepressantHappiness = 100f;
		}
		if (body.shock > 0f)
		{
			body.shock = 0f;
		}
		if (body.painShock > 0f)
		{
			body.painShock = 0f;
		}
		if (body.fibrillationForced)
		{
			body.fibrillationForced = false;
		}
		if (body.fibrillationProgress > 0f)
		{
			body.fibrillationProgress = 0f;
		}
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && main.body == body && !body.standing && body.conscious)
		{
			body.Stand(true);
		}
	}

	/// <summary>图标是否该出现在当前看到的这具身体上。</summary>
	internal static bool IconOn(Body viewed)
	{
		if (!On)
		{
			return false;
		}
		Body p = P1;
		return (Object)(object)viewed != (Object)null && (Object)(object)p != (Object)null && viewed == p;
	}

	private static string Describe(Phase p)
	{
		return p switch
		{
			Phase.On => "生效（P1Alloy 在场，状态与图标都在跑）",
			Phase.Standby => "待机（开关开着，但场上的 P1Alloy 不在或已死）",
			_ => "关闭",
		};
	}

	private static void Log(string msg)
	{
		if (!(Time.unscaledTime - lastLogAt < 0.2f))
		{
			lastLogAt = Time.unscaledTime;
			Debug.Log((object)("[core] " + msg));
		}
	}

	private static string PickIcon(MoodleManager mm)
	{
		if (!string.IsNullOrEmpty(cachedIcon) && HasIcon(mm, cachedIcon))
		{
			return cachedIcon;
		}
		for (int i = 0; i < IconCandidates.Length; i++)
		{
			if (HasIcon(mm, IconCandidates[i]))
			{
				cachedIcon = IconCandidates[i];
				return cachedIcon;
			}
		}
		try
		{
			if (typeof(MoodleManager).GetField("icons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mm) is IDictionary dictionary)
			{
				foreach (object key in dictionary.Keys)
				{
					if (key is string value && !string.IsNullOrEmpty(value))
					{
						cachedIcon = value;
						return cachedIcon;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool HasIcon(MoodleManager mm, string key)
	{
		try
		{
			return typeof(MoodleManager).GetField("icons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mm) is IDictionary dictionary && dictionary.Contains(key);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>机器核心状态的图标：状态生效时挂在 P1Alloy 的 moodle 栏上。</summary>
	[HarmonyPatch(typeof(MoodleManager), "AddAllMoodles")]
	internal static class IconPatch
	{
		private static void Prefix(MoodleManager __instance)
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main == (Object)null)
			{
				return;
			}
			if (!IconOn(main.body))
			{
				return;
			}
			string text = PickIcon(__instance);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				__instance.AddMoodle(8, text, LocalizationManager.T("coreName"), LocalizationManager.T("coreDesc"), false, false);
			}
			catch
			{
			}
		}
	}
}
