using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CUCompanion
{
	/// <summary>
	/// 医疗面板借给别的躯体用时，把它自己的状态补齐：
	/// 物种标题与头像、小睡/锻炼按钮的可用性、感染显形标记、肢体贴图图标的归属。
	/// 这些原本都绑在 WoundView.Start() 时那具躯体上，只改 WoundView.body 是不会变的。
	/// </summary>
	internal static class WoundViewBridge
	{
		/// <summary>按 charType 还原物种标题与头部贴图（Start 里那段逻辑的等价物）。</summary>
		public static void ApplySpecies(WoundView view, Body target)
		{
			if ((Object)(object)view == (Object)null || (Object)(object)target == (Object)null)
			{
				return;
			}
			int num = Mathf.Clamp(target.charType, 0, 2);
			try
			{
				if ((Object)(object)view.speciesText != (Object)null)
				{
					string text = ((num == 1) ? "milky" : ((num == 2) ? "dune" : "experiment"));
					((TMP_Text)view.speciesText).text = Locale.GetOther(text).ToUpper();
				}
			}
			catch
			{
			}
			try
			{
				Sprite[] array = view.headCharSprites;
				Image[] array2 = view.limbImages;
				if (array != null && array2 != null && array2.Length != 0 && (Object)(object)array2[0] != (Object)null)
				{
					int num2 = num * 2;
					if (num2 + 1 < array.Length)
					{
						((Image)array2[0]).sprite = array[num2];
						((Image)array2[0]).SetNativeSize();
						Transform child = ((Component)array2[0]).transform.GetChild(0);
						if ((Object)(object)child != (Object)null)
						{
							Image component = ((Component)child).GetComponent<Image>();
							if ((Object)(object)component != (Object)null)
							{
								component.sprite = array[num2 + 1];
								component.SetNativeSize();
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		/// <summary>看别人时不该出现"小睡 / 锻炼"这类只对自己有意义的按钮。</summary>
		public static void ApplyHeader(WoundView view, Body target)
		{
			if ((Object)(object)view == (Object)null)
			{
				return;
			}
			PlayerCamera main = PlayerCamera.main;
			bool flag = (Object)(object)main == (Object)null || (Object)(object)main.body == (Object)null || target != main.body;
			try
			{
				if ((Object)(object)view.napbutton != (Object)null)
				{
					((Component)view.napbutton).gameObject.SetActive(!flag);
				}
			}
			catch
			{
			}
			try
			{
				Transform val = ((Component)view).transform.Find("WorkoutButton");
				if ((Object)(object)val != (Object)null)
				{
					((Component)val).gameObject.SetActive(!flag);
					Button component = ((Component)val).GetComponent<Button>();
					if ((Object)(object)component != (Object)null)
					{
						((Selectable)component).interactable = !flag;
					}
				}
			}
			catch
			{
			}
		}

		public static void Refresh(WoundView view)
		{
			if ((Object)(object)view == (Object)null)
			{
				return;
			}
			Body body = view.body;
			try
			{
				LimbIconCare.Reconcile(view, body);
				LimbIconCare.Apply(view, body);
			}
			catch
			{
			}
			ApplySpecies(view, body);
			ApplyHeader(view, body);
		}
	}

	/// <summary>
	/// PlayerCamera.showInfection 只有一份、按肢体下标记录"这处感染已经显形"。
	/// 看同伴时它被同伴的感染值改写，切回来自己身上就会凭空多出感染图标。
	/// 渲染面板期间借用目标躯体自己的那一份，渲染完放回去。
	/// </summary>
	[HarmonyPatch(typeof(WoundView), "UpdateView")]
	internal static class WoundView_UpdateView_InfectionSwap
	{
		private static void Prefix(WoundView __instance, ref bool[] __state)
		{
			__state = null;
			try
			{
				PlayerCamera main = PlayerCamera.main;
				if ((Object)(object)main == (Object)null || (Object)(object)__instance == (Object)null)
				{
					return;
				}
				Body body = __instance.body;
				if ((Object)(object)body == (Object)null || body.limbs == null)
				{
					return;
				}
				// 无论看谁（自己或同伴），showInfection 长度都必须 >= body.limbs.Length，
				// 否则原生 UpdateView 按 limbs 遍历访问 showInfection[i] 必抛 IndexOutOfRangeException！
				if (main.showInfection == null || main.showInfection.Length < body.limbs.Length)
				{
					bool[] expanded = new bool[body.limbs.Length];
					if (main.showInfection != null)
					{
						Array.Copy(main.showInfection, expanded, main.showInfection.Length);
					}
					for (int i = (main.showInfection?.Length ?? 0); i < expanded.Length; i++)
					{
						expanded[i] = body.limbs[i].infected;
					}
					main.showInfection = expanded;
				}
				if (body == main.body)
				{
					return;
				}
				__state = main.showInfection;
				bool[] array = MedicalPanelDecoupler.InfectionFor(body, main.showInfection.Length);
				main.showInfection = array;
			}
			catch
			{
				__state = null;
			}
		}

		private static void Postfix(WoundView __instance, bool[] __state)
		{
			try
			{
				PlayerCamera main = PlayerCamera.main;
				if (__state != null && (Object)(object)main != (Object)null)
				{
					if ((Object)(object)__instance != (Object)null)
					{
						MedicalPanelDecoupler.RememberInfection(__instance.body, main.showInfection);
					}
					main.showInfection = __state;
				}
				WoundViewBridge.Refresh(__instance);
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// AddImageToLimb 用 Array.IndexOf(body.limbs, limb) 定位面板槽位。
	/// 面板正借给别的躯体时，这个下标会算错，于是图标贴到错误部位；调用期间把 body 临时指向肢体所属的那具。
	/// </summary>
	[HarmonyPatch(typeof(WoundView), "AddImageToLimb")]
	internal static class WoundView_AddImageToLimb_BodyPin
	{
		private static void Prefix(WoundView __instance, Limb limb, ref Body __state)
		{
			__state = null;
			try
			{
				if ((Object)(object)__instance == (Object)null || (Object)(object)limb == (Object)null)
				{
					return;
				}
				Body body = limb.body;
				if ((Object)(object)body != (Object)null && body != __instance.body)
				{
					__state = __instance.body;
					__instance.body = body;
				}
			}
			catch
			{
				__state = null;
			}
		}

		private static void Postfix(WoundView __instance, Body __state)
		{
			try
			{
				if (__state != null && (Object)(object)__instance != (Object)null)
				{
					__instance.body = __state;
				}
			}
			catch
			{
			}
		}
	}
}
