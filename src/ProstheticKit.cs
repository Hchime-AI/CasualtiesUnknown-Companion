using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

/// <summary>
/// 开局给 P1Alloy 装义肢。
/// 数值全部照抄游戏原版三档义肢（Item.cs 里的 peglimb / steelprosthesis / peo），
/// 走 Limb 自己的 ApplyProstheticValues（私有），这样不会顺带解锁成就、也不会播放安装音效。
/// </summary>
internal static class ProstheticKit
{
	internal const int TierNone = 0;

	internal const int TierPeg = 1;

	internal const int TierSteel = 2;

	internal const int TierPeo = 3;

	/// <summary>四个可选部位的查找前缀，对应 GameObject 名 UpperArmA1 / UpperArmB1 / UpperLegA1 …</summary>
	internal static readonly string[] SlotKeys = new string[4] { "UpperArmA", "UpperArmB", "UpperLegA", "UpperLegB" };

	/// <summary>名字查不到时的兜底下标（游戏原版 amputee 开局用的就是这四个）。</summary>
	internal static readonly int[] SlotFallback = new int[4] { 3, 6, 9, 12 };

	private static MethodInfo applyValues;

	internal static string TierLabel(int tier)
	{
		switch (tier)
		{
		case 1:
			return Loc.T("木桩义肢");
		case 2:
			return Loc.T("钢制义肢");
		case 3:
			return Loc.T("强化义肢");
		default:
			return Loc.T("不装");
		}
	}

	internal static void ApplyAll(Body body, ModConfig cfg)
	{
		if ((Object)(object)body == (Object)null || cfg == null)
		{
			return;
		}
		int[] array = new int[4] { cfg.p1ProstheticArmA, cfg.p1ProstheticArmB, cfg.p1ProstheticLegA, cfg.p1ProstheticLegB };
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] > TierNone)
			{
				Apply(body, i, array[i]);
			}
		}
	}

	internal static void Apply(Body body, int slot, int tier)
	{
		if ((Object)(object)body == (Object)null || body.limbs == null || slot < 0 || slot >= SlotKeys.Length)
		{
			return;
		}
		Limb limb = FindLimb(body, slot);
		if ((Object)(object)limb == (Object)null)
		{
			return;
		}
		Limb.ProstheticInfo prostheticInfo = Info(tier);
		if (!prostheticInfo.hasProsthetic)
		{
			return;
		}
		try
		{
			if (!Paint(limb, prostheticInfo))
			{
				limb.AttachProsthetic(prostheticInfo, isLoad: true);
			}
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogInfo((object)("[P1Alloy] 义肢已安装：" + ((Object)limb).name + " ← " + TierLabel(tier)));
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = CompanionPlugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("[P1Alloy] 义肢安装失败（" + SlotKeys[slot] + "）：" + ex.Message));
			}
		}
	}

	private static Limb FindLimb(Body body, int slot)
	{
		string value = SlotKeys[slot];
		Limb[] array = body.limbs;
		for (int i = 0; i < array.Length; i++)
		{
			Limb limb = array[i];
			if (!((Object)(object)limb == (Object)null) && ((Object)limb).name != null && ((Object)limb).name.StartsWith(value, StringComparison.Ordinal))
			{
				return limb;
			}
		}
		int num = SlotFallback[slot];
		if (num >= 0 && num < array.Length)
		{
			return array[num];
		}
		return null;
	}

	/// <summary>
	/// 直接写 prostheticInfo + 换 overlay 贴图，覆盖整条肢体（上臂 → 前臂 → 手）。
	/// 返回 false 表示拿不到私有方法，调用方回退到 AttachProsthetic。
	/// </summary>
	private static bool Paint(Limb top, Limb.ProstheticInfo info)
	{
		if (applyValues == null)
		{
			applyValues = typeof(Limb).GetMethod("ApplyProstheticValues", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		if (applyValues == null)
		{
			return false;
		}
		bool result = true;
		foreach (Limb lowerLimb in TourniquetScript.GetLowerLimbs(top))
		{
			if ((Object)(object)lowerLimb == (Object)null)
			{
				continue;
			}
			try
			{
				applyValues.Invoke(lowerLimb, new object[1] { info });
			}
			catch
			{
				result = false;
			}
		}
		return result;
	}

	internal static Limb.ProstheticInfo Info(int tier)
	{
		Limb.ProstheticInfo result = default(Limb.ProstheticInfo);
		switch (tier)
		{
		case TierPeg:
			result.hasProsthetic = true;
			result.itemId = "peglimb";
			result.forceChange = -0.55f;
			result.armorChange = -0.1f;
			result.destroyItem = "woodscraps";
			result.appearancePath = "Sprites/peglimboverlay";
			result.maxHoles = 10;
			result.r = 0.412f;
			result.g = 0.231f;
			result.b = 0.174f;
			break;
		case TierSteel:
			result.hasProsthetic = true;
			result.itemId = "steelprosthesis";
			result.forceChange = 0f;
			result.armorChange = 0.2f;
			result.destroyItem = "scrapmetal";
			result.appearancePath = "Sprites/steelprosthesisoverlay";
			result.maxHoles = 15;
			result.r = 0.5f;
			result.g = 0.5f;
			result.b = 0.5f;
			break;
		case TierPeo:
			result.hasProsthetic = true;
			result.itemId = "peo";
			result.forceChange = 0.25f;
			result.armorChange = 0.6f;
			result.destroyItem = "titaniumsheet";
			result.appearancePath = "Sprites/peooverlay";
			result.maxHoles = 20;
			result.r = 0.361f;
			result.g = 0.729f;
			result.b = 0.729f;
			break;
		}
		return result;
	}
}
