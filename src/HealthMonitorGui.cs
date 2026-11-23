using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CUCompanion;

public class HealthMonitorGui : MonoBehaviour
{
	public class DragPart : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
	{
		private RectTransform target;

		private RectTransform area;

		private Vector2 grab;

		public void Init(RectTransform t)
		{
			target = t;
			Canvas componentInParent = ((Component)t).GetComponentInParent<Canvas>();
			area = (((Object)(object)componentInParent != (Object)null) ? (((Component)componentInParent).transform as RectTransform) : null);
		}

		private bool ToTopLeft(Vector2 screen, out Vector2 local)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			local = Vector2.zero;
			if ((Object)(object)area == (Object)null)
			{
				return false;
			}
			Vector2 val = default(Vector2);
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, (Camera)null, out val))
			{
				return false;
			}
			float x = val.x;
			Rect rect = area.rect;
			float num = x - ((Rect)rect).width * 0.5f;
			float y = val.y;
			rect = area.rect;
			local = new Vector2(num, y + ((Rect)rect).height * 0.5f);
			return true;
		}

		public void OnPointerDown(PointerEventData e)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			if (ToTopLeft(e.position, out var local))
			{
				grab = target.anchoredPosition - local;
			}
			((Transform)target).SetAsLastSibling();
		}

		public void OnDrag(PointerEventData e)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			if (ToTopLeft(e.position, out var local))
			{
				Vector2 val = local + grab;
				float x = val.x;
				Rect rect = area.rect;
				val.x = Mathf.Clamp(x, 0f, ((Rect)rect).width - 160f);
				float y = val.y;
				rect = area.rect;
				val.y = Mathf.Clamp(y, 0f - (((Rect)rect).height - 120f), 0f);
				target.anchoredPosition = val;
			}
		}
	}

	public static HealthMonitorGui Instance;

	private static GameObject menuGo;

	private static bool menuOpen;

	private static readonly Dictionary<string, GameObject> windows = new Dictionary<string, GameObject>();

	private const float W = 620f;

	private const float H = 620f;

	private const float RowH = 92f;

	private const float X = 1000f;

	private const float Y = -60f;

	private TMP_FontAsset font;

	private RectTransform win;

	private static readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();

	private void Awake()
	{
		Instance = this;
	}

	public static void Toggle()
	{
		if (!((Object)(object)Instance == (Object)null))
		{
			if (menuOpen)
			{
				Instance.CloseMenu();
			}
			else
			{
				Instance.ShowMenu();
			}
		}
	}

	private void ShowMenu()
	{
		BuildMenu();
		menuOpen = true;
		if (CompanionPlugin.Cfg != null)
		{
			CompanionPlugin.Cfg.monitorOpen = true;
		}
	}

	private void CloseMenu()
	{
		menuOpen = false;
		if ((Object)(object)menuGo != (Object)null)
		{
			Object.Destroy((Object)(object)menuGo);
		}
		menuGo = null;
		if (CompanionPlugin.Cfg != null)
		{
			CompanionPlugin.Cfg.monitorOpen = false;
		}
	}

	private void Update()
	{
		if (menuOpen && InputGuard.ConsumeEscape())
		{
			CloseMenu();
		}
		if ((Object)(object)CompanionHost.Instance == (Object)null || !CompanionHost.Instance.HasBody)
		{
			if (menuOpen)
			{
				CloseMenu();
			}
			if (windows.Count <= 0)
			{
				return;
			}
			List<GameObject> list = new List<GameObject>(windows.Values);
			windows.Clear();
			for (int i = 0; i < list.Count; i++)
			{
				if ((Object)(object)list[i] != (Object)null)
				{
					Object.Destroy((Object)(object)list[i]);
				}
			}
		}
		else
		{
			if (!((Object)(object)CompanionHost.Instance != (Object)null) || !CompanionHost.Instance.HasBody || CompanionPlugin.Cfg == null)
			{
				return;
			}
			for (int j = 0; j < CompanionPlugin.Cfg.monitorParts.Count; j++)
			{
				MonitorPart monitorPart = CompanionPlugin.Cfg.monitorParts[j];
				if (monitorPart != null && monitorPart.enabled && !windows.ContainsKey(Key(monitorPart)))
				{
					OpenPartWindow(monitorPart);
				}
			}
		}
	}

	private void BuildMenu()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menuGo != (Object)null)
		{
			Object.Destroy((Object)(object)menuGo);
		}
		Transform canvasTransform = NativeHudController.CanvasTransform;
		if ((Object)(object)canvasTransform == (Object)null)
		{
			return;
		}
		font = NativeHudController.GameFont;
		float num = ((CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.monitorParts != null) ? ((float)Mathf.Clamp(CompanionPlugin.Cfg.monitorParts.Count, 1, 12)) : 2f);
		float num2 = Mathf.Clamp(170f + num * 92f + 74f, 420f, 920f);
		menuGo = new GameObject("CUCompanionMonitor", new Type[1] { typeof(RectTransform) });
		win = menuGo.GetComponent<RectTransform>();
		((Transform)win).SetParent(canvasTransform, false);
		win.anchorMin = new Vector2(0f, 1f);
		win.anchorMax = new Vector2(0f, 1f);
		win.pivot = new Vector2(0f, 1f);
		win.sizeDelta = new Vector2(620f, num2);
		win.anchoredPosition = new Vector2(1000f, -60f);
		((Transform)win).SetAsLastSibling();
		Image obj = menuGo.AddComponent<Image>();
		((Graphic)obj).color = new Color(0.015f, 0.017f, 0.022f, 0.95f);
		((Graphic)obj).raycastTarget = true;
		Frame(620f, num2);
		HeaderBar(Loc.T("状态监测"));
		Label(Loc.T("启用后会在右边单独开一个可拖动的小窗；关掉小窗等于停用，要回这里重新启用。"), 19f, new Vector2(16f, -64f), 588f, 46f, (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.5f));
		List<MonitorPart> monitorParts = CompanionPlugin.Cfg.monitorParts;
		float num3 = -112f;
		int num4 = 0;
		for (int i = 0; i < monitorParts.Count; i++)
		{
			if (num4 >= 12)
			{
				break;
			}
			if (monitorParts[i] != null)
			{
				BuildRow(monitorParts[i], num3);
				num3 -= 92f;
				num4++;
			}
		}
		Bar(Loc.T("添加部位"), 16f, 0f - (num2 - 34f), 160f, 34f, delegate
		{
			CompanionPlugin.Cfg.monitorParts.Add(new MonitorPart
			{
				id = Guid.NewGuid().ToString("N").Substring(0, 8),
				name = Loc.T("新部位"),
				enabled = false,
				tokens = 300
			});
			CloseMenu();
			ShowMenu();
		});
	}

	private void BuildRow(MonitorPart p, float y)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		Box(12f, y, 596f, 82f, new Color(1f, 1f, 1f, 0.03f));
		Bar(p.enabled ? Loc.T("已启用") : Loc.T("未启用"), 20f, y - 8f, 110f, 32f, delegate
		{
			p.enabled = !p.enabled;
			if (p.enabled)
			{
				OpenPart(p);
			}
			else
			{
				ClosePart(p);
			}
			CloseMenu();
			ShowMenu();
		});
		GameObject val = new GameObject("nameField", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)win, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(160f, 34f);
		component.anchoredPosition = new Vector2(140f, y - 8f);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = new Color(1f, 1f, 1f, 0.06f);
		((Graphic)obj).raycastTarget = true;
		val.AddComponent<NativeField>().Init(font, () => p.name, delegate(string v)
		{
			p.name = v;
			RefreshPart(p);
		}, multi: false);
		Label("token", 17f, new Vector2(312f, y - 14f), 56f, 24f, (TextAlignmentOptions)513, new Color(1f, 1f, 1f, 0.55f));
		Bar(p.tokens.ToString(), 368f, y - 8f, 80f, 34f, delegate
		{
			int[] monitorTokenSteps = Limits.MonitorTokenSteps;
			int num = Array.IndexOf(monitorTokenSteps, p.tokens);
			p.tokens = monitorTokenSteps[(num + 1) % monitorTokenSteps.Length];
			CloseMenu();
			ShowMenu();
		});
		Bar(Loc.T("删除"), 458f, y - 8f, 70f, 34f, delegate
		{
			ClosePart(p);
			CompanionPlugin.Cfg.monitorParts.Remove(p);
			CloseMenu();
			ShowMenu();
		});
		string text = ((!p.enabled) ? Loc.T("未启用（点左侧开关打开小窗）") : (string.IsNullOrEmpty(p.desc) ? Loc.T("窗口已打开，等待第一次生成…") : (Loc.T("窗口已打开：") + Head(p.desc, 46))));
		Label(text, 18f, new Vector2(20f, y - 50f), 576f, 34f, (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.62f));
	}

	public static void OpenPart(MonitorPart p)
	{
		if (!((Object)(object)Instance == (Object)null) && p != null)
		{
			Instance.OpenPartWindow(p);
		}
	}

	public static void ClosePart(MonitorPart p)
	{
		if (p != null && windows.TryGetValue(Key(p), out var value))
		{
			if ((Object)(object)value != (Object)null)
			{
				Object.Destroy((Object)(object)value);
			}
			windows.Remove(Key(p));
		}
	}

	private void OpenPartWindow(MonitorPart p)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!positions.TryGetValue(Key(p), out var value))
		{
			value = new Vector2(1640f, -60f - 340f * (float)windows.Count);
		}
		OpenPartWindowAt(p, value);
	}

	private void OpenPartWindowAt(MonitorPart p, Vector2 at)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		Transform canvasTransform = NativeHudController.CanvasTransform;
		if ((Object)(object)canvasTransform == (Object)null)
		{
			return;
		}
		if (windows.TryGetValue(Key(p), out var value) && (Object)(object)value != (Object)null)
		{
			Object.Destroy((Object)(object)value);
		}
		GameObject val = new GameObject("part_" + p.name, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(canvasTransform, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(440f, 300f);
		component.anchoredPosition = at;
		((Transform)component).SetAsLastSibling();
		positions[Key(p)] = at;
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = new Color(0.015f, 0.017f, 0.022f, 0.93f);
		((Graphic)obj).raycastTarget = true;
		string text = (string.IsNullOrEmpty(p.desc) ? Loc.T("（还没有生成内容，点下面「手动生成」）") : p.desc);
		TextMeshProUGUI val2 = AddLabel(val.transform, text, 18f, new Vector2(14f, -54f), 412f, 190f, (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.82f), "descTxt");
		((TMP_Text)val2).enableWordWrapping = true;
		((TMP_Text)val2).ForceMeshUpdate(false, false);
		float num = Mathf.Max(36f, ((TMP_Text)val2).preferredHeight);
		float num2 = Mathf.Clamp(54f + num + 76f, 240f, 640f);
		component.sizeDelta = new Vector2(440f, num2);
		((TMP_Text)val2).rectTransform.sizeDelta = new Vector2(412f, num2 - 54f - 76f);
		AddBox(val.transform, 0f, 0f, 440f, 1.6f);
		AddBox(val.transform, 0f, 0f - num2, 440f, 1.6f);
		AddBox(val.transform, 0f, 0f, 1.6f, num2);
		AddBox(val.transform, 438.4f, 0f, 1.6f, num2);
		AddBox(val.transform, 0f, -44f, 440f, 1.6f);
		GameObject val3 = new GameObject("head", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val3.GetComponent<RectTransform>();
		((Transform)component2).SetParent(val.transform, false);
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.offsetMin = new Vector2(0f, -44f);
		component2.offsetMax = Vector2.zero;
		Image obj2 = val3.AddComponent<Image>();
		((Graphic)obj2).color = new Color(1f, 1f, 1f, 0f);
		((Graphic)obj2).raycastTarget = true;
		val3.AddComponent<DragPart>().Init(component);
		AddLabel(val.transform, p.name, 24f, new Vector2(14f, -8f), 280f, 32f, (TextAlignmentOptions)513, Color.white);
		BarIn(val.transform, "×", 386f, -6f, 44f, 32f, delegate
		{
			p.enabled = false;
			ClosePart(p);
			if (menuOpen)
			{
				CloseMenu();
				ShowMenu();
			}
		}, Key(p));
		GameObject val4 = new GameObject("foot", new Type[1] { typeof(RectTransform) });
		RectTransform component3 = val4.GetComponent<RectTransform>();
		((Transform)component3).SetParent(val.transform, false);
		component3.anchorMin = new Vector2(0f, 0f);
		component3.anchorMax = new Vector2(0f, 0f);
		component3.pivot = new Vector2(0f, 0f);
		component3.sizeDelta = new Vector2(440f, 46f);
		component3.anchoredPosition = new Vector2(0f, 8f);
		BarIn(val4.transform, Loc.T("手动生成"), 14f, -5f, 140f, 30f, delegate
		{
			CompanionHost instance = CompanionHost.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.RequestMonitor(p);
			}
		}, Key(p));
		AddLabel(val4.transform, Loc.T("每 ") + 3 + Loc.T(" 分钟自动刷新"), 15f, new Vector2(166f, -8f), 260f, 22f, (TextAlignmentOptions)513, new Color(1f, 1f, 1f, 0.45f));
		windows[Key(p)] = val;
	}

	public static void RefreshPart(MonitorPart p)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (p != null && windows.TryGetValue(Key(p), out var value) && !((Object)(object)value == (Object)null))
		{
			Vector2 anchoredPosition = value.GetComponent<RectTransform>().anchoredPosition;
			positions[Key(p)] = anchoredPosition;
			if (!((Object)(object)Instance == (Object)null))
			{
				Instance.OpenPartWindowAt(p, anchoredPosition);
			}
		}
	}

	private static string Key(MonitorPart p)
	{
		if (p == null)
		{
			return "";
		}
		if (!string.IsNullOrEmpty(p.id))
		{
			return p.id;
		}
		p.id = Guid.NewGuid().ToString("N").Substring(0, 8);
		return p.id;
	}

	private static string Head(string s, int n)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		s = s.Replace("\n", " ");
		if (s.Length > n)
		{
			return s.Substring(0, n) + "…";
		}
		return s;
	}

	private void Frame(float w, float h)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		Color c = default(Color);
		c = new Color(1f, 1f, 1f, 0.9f);
		Box(0f, 0f, w, 1.6f, c);
		Box(0f, 0f - h, w, 1.6f, c);
		Box(0f, 0f, 1.6f, h, c);
		Box(w - 1.6f, 0f, 1.6f, h, c);
		Box(0f, -54f, w, 1.6f, c);
	}

	private void HeaderBar(string title)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Label(title, 30f, new Vector2(16f, -10f), 300f, 36f, (TextAlignmentOptions)513, Color.white);
		Bar("—", 512f, -9f, 44f, 38f, delegate
		{
			CloseMenu();
		});
		Bar("×", 562f, -9f, 44f, 38f, delegate
		{
			CloseMenu();
		});
	}

	private void Box(float x, float y, float w, float h, Color c)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("box", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)win, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = new Vector2(x, y);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = c;
		((Graphic)obj).raycastTarget = false;
	}

	private static void AddBox(Transform parent, float x, float y, float w, float h)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("box", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = new Vector2(x, y);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = new Color(1f, 1f, 1f, 0.9f);
		((Graphic)obj).raycastTarget = false;
	}

	private TextMeshProUGUI Label(string text, float size, Vector2 pos, float w, float h, TextAlignmentOptions align, Color color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return AddLabel((Transform)(object)win, text, size, pos, w, h, align, color);
	}

	private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Vector2 pos, float w, float h, TextAlignmentOptions align, Color color, string name = "label")
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = pos;
		TextMeshProUGUI obj = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = NativeHudController.GameFont;
		((TMP_Text)obj).fontSize = size;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = color;
		((TMP_Text)obj).alignment = align;
		((TMP_Text)obj).text = text;
		((Graphic)obj).raycastTarget = false;
		((TMP_Text)obj).enableWordWrapping = true;
		return obj;
	}

	private void Bar(string text, float x, float y, float w, float h, Action onClick)
	{
		BarIn((Transform)(object)win, text, x, y, w, h, onClick, text);
	}

	private static void BarIn(Transform parent, string text, float x, float y, float w, float h, Action onClick, string name)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("bar_" + name, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = new Vector2(x, y);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = new Color(1f, 1f, 1f, 0.06f);
		((Graphic)obj).raycastTarget = true;
		AddBox(parent, x, y, w, 1.4f);
		AddBox(parent, x, y - h + 1.4f, w, 1.4f);
		AddBox(parent, x, y, 1.4f, h);
		AddBox(parent, x + w - 1.4f, y, 1.4f, h);
		GameObject val2 = new GameObject("txt", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val2.GetComponent<RectTransform>();
		((Transform)component2).SetParent(val.transform, false);
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		TextMeshProUGUI obj2 = val2.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj2).font = NativeHudController.GameFont;
		((TMP_Text)obj2).fontSize = 20f;
		((Graphic)obj2).color = Color.white;
		((TMP_Text)obj2).alignment = (TextAlignmentOptions)514;
		((TMP_Text)obj2).text = text;
		((Graphic)obj2).raycastTarget = false;
		val.AddComponent<NativeHit>().Init(delegate
		{
			try
			{
				PlayerCamera.PlayUISound("click", 1f);
			}
			catch
			{
			}
			onClick();
		});
	}
}
