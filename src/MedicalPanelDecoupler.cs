using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public static class MedicalPanelDecoupler
{
	private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	public static void SetInspectTarget(Body target)
	{
		try
		{
			WoundView view = WoundView.view;
			if ((Object)(object)view == (Object)null || (Object)(object)target == (Object)null)
			{
				return;
			}
			FieldInfo field = typeof(WoundView).GetField("body", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				return;
			}
			Body previous = view.body;
			if (previous != target)
			{
				// 先把这具躯体在面板上留下的贴图图标（止血带/夹板/注射创口…）收起，
				// 再把目标躯体自己的图标贴回来，最后恢复它的感染显形标记。
				// 这里任何一步失败都不该中断换面板，否则医疗面板会卡在上一具躯体上。
				try
				{
					LimbIconCare.Reconcile(view, previous);
					LimbIconCare.Apply(view, target);
				}
				catch (Exception ex4)
				{
					WarnOnce("icons", ex4);
				}
				try
				{
					SwapInfectionState(previous, target);
				}
				catch (Exception ex5)
				{
					WarnOnce("infection", ex5);
				}
			}
			field.SetValue(view, target);
			WoundViewBridge.ApplySpecies(view, target);
			WoundViewBridge.ApplyHeader(view, target);
			if (target.limbs != null && target.limbs.Length != 0)
			{
				if (view.limbLookingAt < 0 || view.limbLookingAt >= target.limbs.Length)
				{
					view.limbLookingAt = 0;
				}
				if ((Object)(object)PlayerCamera.main != (Object)null)
				{
					PlayerCamera.main.selectedLimb = target.limbs[view.limbLookingAt];
				}
			}
			// Bug fix: 即使 previous == target（跳过了 SwapInfectionState），
			// showInfection 的长度也可能与当前 body.limbs 不匹配，
			// 导致 UpdateView 内部按 limbs 下标访问 showInfection 时越界。
			// 在调用 UpdateView 前强制对齐长度。
			try
			{
				PlayerCamera cam = PlayerCamera.main;
				if ((Object)(object)cam != (Object)null && cam.showInfection != null
					&& target.limbs != null && cam.showInfection.Length != target.limbs.Length)
				{
					bool[] aligned = new bool[target.limbs.Length];
					int copyLen = Mathf.Min(cam.showInfection.Length, aligned.Length);
					for (int m = 0; m < copyLen; m++)
					{
						aligned[m] = cam.showInfection[m];
					}
					// 超出旧数组范围的部分，用 limbs 的实际感染值填充
					for (int m = copyLen; m < aligned.Length; m++)
					{
						aligned[m] = target.limbs[m].infected;
					}
					cam.showInfection = aligned;
				}
			}
			catch (Exception ex2)
			{
				WarnOnce("showInfection-align", ex2);
			}
			InvokeQuietly(view, "UpdateView");
			InvokeQuietly(view, "UpdateSkillUI");
			if ((Object)(object)((Component)view).GetComponent<MedicalPartnerAssistant>() == (Object)null)
			{
				((Component)view).gameObject.AddComponent<MedicalPartnerAssistant>();
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[MedicalDecoupler] Failed to sync target: " + ex.Message));
			}
		}
	}

	private static readonly Dictionary<Body, bool[]> infectionByBody = new Dictionary<Body, bool[]>();

	/// <summary>
	/// PlayerCamera.showInfection 只有一份、按肢体下标记录“这处感染已经显形”。
	/// 借面板看同伴时这份标记会被同伴的感染改写，切回自己后同一下标就会凭空多出感染图标
	/// （只有图标、实际没有感染）。这里按躯体各存一份，切到谁就恢复谁的。
	/// </summary>
	private static void SwapInfectionState(Body from, Body to)
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null || main.showInfection == null)
		{
			return;
		}
		bool[] array = main.showInfection;
		if (to != null && to.limbs != null && array.Length != to.limbs.Length)
		{
			// showInfection 的长度必须跟当前显示躯体一致，否则 WoundView.UpdateView 会下标越界。
			bool[] array9 = new bool[to.limbs.Length];
			for (int n = 0; n < array9.Length; n++)
			{
				array9[n] = to.limbs[n].infected;
			}
			main.showInfection = array9;
			array = array9;
		}
		if (infectionByBody.Count > 8)
		{
			infectionByBody.Clear();
		}
		if ((Object)(object)from != (Object)null && from.limbs != null)
		{
			bool[] array2 = new bool[array.Length];
			int num = Mathf.Min(array2.Length, from.limbs.Length);
			for (int i = 0; i < num; i++)
			{
				array2[i] = array[i];
			}
			infectionByBody[from] = array2;
		}
		bool[] value;
		if ((Object)(object)to != (Object)null && infectionByBody.TryGetValue(to, out value) && value.Length == array.Length)
		{
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = value[j];
			}
			return;
		}
		if ((Object)(object)to != (Object)null && to.limbs != null)
		{
			int num2 = Mathf.Min(array.Length, to.limbs.Length);
			for (int k = 0; k < num2; k++)
			{
				array[k] = to.limbs[k].infected;
			}
		}
	}

	/// <summary>取 body 自己那份 showInfection（长度不符或没存过时按 limbs[k].infected 重算）。</summary>
	public static bool[] InfectionFor(Body body, int length)
	{
		bool[] value;
		if ((Object)(object)body != (Object)null && infectionByBody.TryGetValue(body, out value) && value != null && value.Length >= length)
		{
			return value;
		}
		int num3 = length;
		if ((Object)(object)body != (Object)null && body.limbs != null && body.limbs.Length > num3)
		{
			num3 = body.limbs.Length;
		}
		bool[] array = new bool[num3];
		if ((Object)(object)body != (Object)null && body.limbs != null)
		{
			int num = Mathf.Min(num3, body.limbs.Length);
			for (int i = 0; i < num; i++)
			{
				array[i] = body.limbs[i].infected;
			}
		}
		if ((Object)(object)body != (Object)null)
		{
			RememberInfection(body, array);
		}
		return array;
	}

	/// <summary>把 body 当前的显形标记存下来。</summary>
	public static void RememberInfection(Body body, bool[] state)
	{
		if ((Object)(object)body == (Object)null || state == null)
		{
			return;
		}
		if (infectionByBody.Count > 8)
		{
			infectionByBody.Clear();
		}
		bool[] array = new bool[state.Length];
		Array.Copy(state, array, state.Length);
		infectionByBody[body] = array;
	}

	private static readonly HashSet<string> warned = new HashSet<string>();

	private static void WarnOnce(string tagName, Exception ex)
	{
		Exception ex2 = ex.InnerException ?? ex;
		string text = tagName + ": " + ex2.GetType().Name + ": " + ex2.Message;
		if (warned.Add(text))
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[MedicalDecoupler] " + text));
			}
		}
	}

	private static void InvokeQuietly(WoundView view, string methodName)
	{
		try
		{
			MethodInfo method = typeof(WoundView).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				method.Invoke(view, null);
			}
		}
		catch (Exception ex)
		{
			WarnOnce(methodName, ex);
		}
	}

	public static void SyncToCurrentControl()
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && (Object)(object)main.body != (Object)null)
		{
			SetInspectTarget(main.body);
		}
	}
}
