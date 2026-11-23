using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace CUCompanion;

public class NativePanel : MonoBehaviour
{
	public static NativePanel Instance;

	private GameObject panel;

	private RectTransform veilRt;

	private RectTransform root;

	private TMP_FontAsset font;

	private Persona editing;

	private int editingIdx;

	private bool open;

	public static bool IsOpen => (Object)(object)Instance != (Object)null && Instance.open;

	private bool autoShown;

	private float sinceMenu;

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		if (open && Input.GetKeyDown((KeyCode)27))
		{
			Close();
		}
		if (Input.GetKeyDown((KeyCode)288))
		{
			if (open)
			{
				Close();
			}
			else
			{
				Show();
			}
		}
	}

	public static void Open()
	{
		if (!((Object)(object)Instance == (Object)null))
		{
			Instance.Show();
		}
	}

	public static void OpenAnywhere()
	{
		if (!((Object)(object)Instance == (Object)null))
		{
			Instance.Show();
		}
	}

	public void Show()
	{
		if (open)
		{
			return;
		}
		try
		{
			Build();
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log.LogWarning((object)("[ui] 面板构建失败：" + ex.Message));
			try
			{
				Close();
			}
			catch
			{
			}
		}
	}

	public void Close()
	{
		bool num = open || (Object)(object)panel != (Object)null;
		FlushFields();
		open = false;
		if ((Object)(object)panel != (Object)null)
		{
			Object.Destroy((Object)(object)panel);
		}
		if ((Object)(object)veilRt != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)veilRt).gameObject);
			veilRt = null;
		}
		panel = null;
		root = null;
		InputGuard.Set(on: false);
		if (num)
		{
			try
			{
				CompanionPlugin.SaveAll();
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log.LogWarning((object)("[ui] 关闭面板时保存失败：" + ex.Message));
			}
		}
	}

	private void Build()
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0859: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d7: Unknown result type (might be due to invalid IL or missing references)
		PreRunScript instance = PreRunScript.instance;
		Transform canvasTransform = NativeHudController.CanvasTransform;
		if (((Object)(object)instance == (Object)null || (Object)(object)instance.mainCanvas == (Object)null) && (Object)(object)canvasTransform == (Object)null)
		{
			CompanionPlugin.Log.LogWarning((object)"[ui] 没有可用画布，无法打开原生面板");
			return;
		}
		font = FindFont();
		if ((Object)(object)font == (Object)null)
		{
			CompanionPlugin.Log.LogWarning((object)"[ui] 没找到游戏字体，放弃原生面板");
			return;
		}
		editingIdx = (((Object)(object)instance != (Object)null) ? Mathf.Clamp(PreRunScript.characterSelected, 0, 2) : Mathf.Clamp(((Object)(object)CompanionHost.Instance != (Object)null && (Object)(object)CompanionHost.Instance.CurrentBody != (Object)null) ? CompanionHost.Instance.CurrentBody.charType : 0, 0, 2));
		editing = CompanionPlugin.Store.Get(editingIdx);
		panel = new GameObject("CUCompanionPanel", new Type[1] { typeof(RectTransform) });
		root = panel.GetComponent<RectTransform>();
		Transform val = canvasTransform;
		if ((Object)(object)instance != (Object)null && (Object)(object)instance.mainCanvas != (Object)null)
		{
			val = ((Component)instance.mainCanvas).transform;
		}
		if ((Object)(object)instance != (Object)null && (Object)(object)instance.charSelectName != (Object)null && (Object)(object)((Graphic)instance.charSelectName).canvas != (Object)null)
		{
			val = ((Component)((Graphic)instance.charSelectName).canvas).transform;
		}
		((Transform)root).SetParent(val, false);
		CompanionPlugin.Log.LogInfo((object)("[ui] 面板挂到画布：" + ((Object)val).name + " 排序=" + (((Object)(object)((Component)val).GetComponent<Canvas>() != (Object)null) ? ((Component)val).GetComponent<Canvas>().sortingOrder : 0)));
		root.anchorMin = new Vector2(0.5f, 0.5f);
		root.anchorMax = new Vector2(0.5f, 0.5f);
		root.pivot = new Vector2(0.5f, 0.5f);
		RectTransform val2 = (RectTransform)(object)((val is RectTransform) ? val : null);
		float num;
		Rect rect;
		if (!((Object)(object)val2 != (Object)null))
		{
			num = 2560f;
		}
		else
		{
			rect = val2.rect;
			num = ((Rect)rect).width;
		}
		float num2 = num;
		float num3;
		if (!((Object)(object)val2 != (Object)null))
		{
			num3 = 1440f;
		}
		else
		{
			rect = val2.rect;
			num3 = ((Rect)rect).height;
		}
		float num4 = num3;
		if (num2 < 100f)
		{
			num2 = 2560f;
		}
		if (num4 < 100f)
		{
			num4 = 1440f;
		}
		GameObject val3 = new GameObject("CUCompanionVeil", new Type[1] { typeof(RectTransform) });
		RectTransform component = val3.GetComponent<RectTransform>();
		((Transform)component).SetParent(val, false);
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image obj = val3.AddComponent<Image>();
		((Graphic)obj).color = new Color(0.01f, 0.012f, 0.016f, 0.86f);
		((Graphic)obj).raycastTarget = true;
		veilRt = component;
		float num5 = Mathf.Min(num2 - 120f, 2200f);
		float num6 = Mathf.Min(num4 - 100f, 1260f);
		root.sizeDelta = new Vector2(num5, num6);
		((Transform)root).localScale = Vector3.one;
		root.anchoredPosition = Vector2.zero;
		((Transform)root).SetAsLastSibling();
		Image obj2 = panel.AddComponent<Image>();
		((Graphic)obj2).color = new Color(0f, 0f, 0f, 0.94f);
		((Graphic)obj2).raycastTarget = true;
		Frame(num5, num6, 1.6f);
		Label(Loc.T("角色 ") + (editingIdx + 1) + Loc.T(" · 人设与互动"), 40f, 44f, new Vector2(28f, -22f), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.96f));
		Label(Loc.T("只有选中的角色会用这一套；切换角色时整套元数据跟着切换"), 24f, 26f, new Vector2(30f, -70f), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.5f));
		Bar(Loc.T("关闭"), new Vector2(-30f, -22f), 170f, 50f, Close);
		float num7 = 30f;
		float num8 = num5 * 0.52f;
		float y = -120f;
		y = Field(Loc.T("名字"), num7, y, num5 * 0.45f, 46f, 1, () => editing.name, delegate(string v)
		{
			editing.name = v;
		});
		y = Choice(Loc.T("性别"), num7, y, new string[3] { Loc.T("女"), Loc.T("男"), Loc.T("无性") }, () => editing.gender, delegate(int v)
		{
			editing.gender = v;
		});
		y = Field(Loc.T("人设 · 性格与说话方式"), num7, y, 660f, 150f, 3, () => editing.personality, delegate(string v)
		{
			editing.personality = v;
		});
		y = Field(Loc.T("外貌（只进 LLM 的认知）"), num7, y, 660f, 110f, 2, () => editing.appearance, delegate(string v)
		{
			editing.appearance = v;
		});
		float y2 = -120f;
		y2 = Field(Loc.T("称呼玩家为"), num8, y2, num5 * 0.45f, 46f, 1, () => editing.callPlayer, delegate(string v)
		{
			editing.callPlayer = v;
		});
		y2 = Field(Loc.T("与玩家的关系"), num8, y2, num5 * 0.45f, 150f, 3, () => editing.relationship, delegate(string v)
		{
			editing.relationship = v;
		});
		y2 = Field(Loc.T("额外设定（只进模型认知，不做机制）"), num8, y2, num5 * 0.45f, 120f, 3, () => editing.traits, delegate(string v)
		{
			editing.traits = v;
		});
		y2 = Field(Loc.T("可用互动（、分隔）"), num8, y2, num5 * 0.45f, 46f, 1, () => (editing.interactions != null) ? string.Join(Loc.P("、"), editing.interactions) : "", delegate(string v)
		{
			editing.interactions = SplitList(v);
		});
		y2 = Toggle(Loc.T("身体颜色"), num8, y2, () => editing.bodyTint, delegate(bool v)
		{
			editing.bodyTint = v;
		});
		if (editing.bodyTint)
		{
			y2 = Slider(Loc.T("色相"), num8, y2, num5 * 0.45f, 0f, 1f, () => editing.bodyHue, delegate(float v)
			{
				editing.bodyHue = v;
			});
			y2 = Slider(Loc.T("明度"), num8, y2, num5 * 0.45f, 0.3f, 1.2f, () => editing.bodyValue, delegate(float v)
			{
				editing.bodyValue = v;
			});
		}
		y2 = Toggle(Loc.T("眼睛颜色"), num8, y2, () => editing.eyeTint, delegate(bool v)
		{
			editing.eyeTint = v;
		});
		if (editing.eyeTint)
		{
			y2 = Slider(Loc.T("色相"), num8, y2, num5 * 0.45f, 0f, 1f, () => editing.eyeHue, delegate(float v)
			{
				editing.eyeHue = v;
			});
			y2 = Slider(Loc.T("明度"), num8, y2, num5 * 0.45f, 0.3f, 1.2f, () => editing.eyeValue, delegate(float v)
			{
				editing.eyeValue = v;
			});
		}
		float num9 = 0f - (num6 - 120f);
		Label(Loc.T("可用互动（预览）"), 24f, 26f, new Vector2(num7, num9 + 20f), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.5f));
		string[] obj3 = ((editing.interactions != null && editing.interactions.Length != 0) ? editing.interactions : CompanionHost.Presets);
		float num10 = num7;
		int num11 = 0;
		string[] array = obj3;
		foreach (string text in array)
		{
			string captured = text;
			Bar(captured, new Vector2(num10, num9), 200f, 48f, delegate
			{
				if ((Object)(object)LegacyCompanionUi.Instance != (Object)null)
				{
					LegacyCompanionUi.Instance.SetInputText(captured);
				}
			});
			num10 += 210f;
			num11++;
			if (num11 >= 3)
			{
				break;
			}
		}
		Bar(Loc.T("保存"), new Vector2(num8, num9), 200f, 56f, Save);
		Bar(Loc.T("恢复默认"), new Vector2(num8 + 216f, num9), 240f, 56f, ResetToDefault);
		open = true;
		CompanionPlugin.Log.LogInfo((object)("[ui] 原生人设面板已打开（字体 " + ((Object)font).name + "，角色槽 " + (editingIdx + 1) + "）"));
	}

	private static string[] SplitList(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return new string[0];
		}
		string[] array = raw.Split(new string[3] { "、", ",", "，" }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
		}
		return array;
	}

	private void Save()
	{
		FlushFields();
		CompanionPlugin.SaveAll();
		CompanionPlugin.Log.LogInfo((object)("[ui] 人设已保存：槽 " + editingIdx + " / " + ((editing != null) ? editing.name : "?")));
		Close();
	}

	public void FlushFields()
	{
		if ((Object)(object)panel == (Object)null)
		{
			return;
		}
		NativeField[] componentsInChildren = panel.GetComponentsInChildren<NativeField>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null)
			{
				componentsInChildren[i].Apply();
			}
		}
	}

	private void ResetToDefault()
	{
		Persona persona = PersonaStore.CreateDefault().slots[editingIdx];
		if (string.IsNullOrEmpty(persona.name))
		{
			persona.name = editing.name;
		}
		CompanionPlugin.Store.slots[editingIdx] = persona;
		Close();
		Show();
	}

	private TMP_FontAsset FindFont()
	{
		PreRunScript instance = PreRunScript.instance;
		if ((Object)(object)instance != (Object)null && (Object)(object)instance.charSelectName != (Object)null && (Object)(object)((TMP_Text)instance.charSelectName).font != (Object)null)
		{
			return ((TMP_Text)instance.charSelectName).font;
		}
		TextMeshProUGUI[] array = Object.FindObjectsOfType<TextMeshProUGUI>();
		foreach (TextMeshProUGUI val in array)
		{
			if ((Object)(object)val != (Object)null && (Object)(object)((TMP_Text)val).font != (Object)null && ((Component)val).gameObject.activeInHierarchy)
			{
				return ((TMP_Text)val).font;
			}
		}
		return NativeHudController.GameFont;
	}

	private GameObject Node(string name, Transform parent, float x, float y, float w, float h)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = new Vector2(x, y);
		return val;
	}

	private Image Box(Transform parent, float x, float y, float w, float h, Color c, string name)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = new Vector2(x, y);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = c;
		((Graphic)obj).raycastTarget = false;
		return obj;
	}

	private void Frame(float w, float h, float t)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		Color c = default(Color);
		c = new Color(1f, 1f, 1f, 0.92f);
		Box((Transform)(object)root, 0f, 0f, w, t, c, "frameTop");
		Box((Transform)(object)root, 0f, 0f - h, w, t, c, "frameBottom");
		Box((Transform)(object)root, 0f, 0f, t, h, c, "frameLeft");
		Box((Transform)(object)root, w - t, 0f, t, h, c, "frameRight");
	}

	private TextMeshProUGUI Label(string text, float size, float h, Vector2 pos, TextAlignmentOptions align, Color color)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("label", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)root, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(1300f, h);
		component.anchoredPosition = pos;
		TextMeshProUGUI obj = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = font;
		((TMP_Text)obj).fontSize = size;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = color;
		((TMP_Text)obj).alignment = align;
		((TMP_Text)obj).text = text;
		((Graphic)obj).raycastTarget = false;
		((TMP_Text)obj).enableWordWrapping = true;
		return obj;
	}

	private void Bar(string text, Vector2 pos, float w, float h, Action onClick)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("bar_" + text, new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)root, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		if (pos.x < 0f)
		{
			component.anchorMin = new Vector2(1f, 1f);
			component.anchorMax = new Vector2(1f, 1f);
			component.pivot = new Vector2(1f, 1f);
		}
		component.sizeDelta = new Vector2(w, h);
		component.anchoredPosition = pos;
		((Graphic)val.AddComponent<Image>()).color = new Color(0f, 0f, 0f, 0.3f);
		Color c = default(Color);
		c = new Color(1f, 1f, 1f, 0.9f);
		Box((Transform)(object)component, 0f, 0f, w, 1.4f, c, "t");
		Box((Transform)(object)component, 0f, 0f - h, w, 1.4f, c, "b");
		Box((Transform)(object)component, 0f, 0f, 1.4f, h, c, "l");
		Box((Transform)(object)component, w - 1.4f, 0f, 1.4f, h, c, "r");
		GameObject val2 = new GameObject("text", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val2.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)component, false);
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		TextMeshProUGUI obj = val2.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = font;
		((TMP_Text)obj).fontSize = 26f;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = Color.white;
		((TMP_Text)obj).alignment = (TextAlignmentOptions)514;
		((TMP_Text)obj).enableWordWrapping = false;
		((TMP_Text)obj).overflowMode = (TextOverflowModes)0;
		((TMP_Text)obj).text = text;
		((Graphic)obj).raycastTarget = false;
		val.AddComponent<NativeHit>().Init(onClick);
	}

	private float Field(string title, float x, float y, float w, float h, int lines, Func<string> get, Action<string> set)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		Label(title, 24f, 26f, new Vector2(x, y), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.6f));
		GameObject val = Node("field_" + title, (Transform)(object)root, x, y - 28f, w, h);
		((Graphic)val.AddComponent<Image>()).color = new Color(0f, 0f, 0f, 0.3f);
		Color c = default(Color);
		c = new Color(1f, 1f, 1f, 0.7f);
		Box(val.transform, 0f, 0f, w, 1.2f, c, "t");
		Box(val.transform, 0f, 0f - h, w, 1.2f, c, "b");
		Box(val.transform, 0f, 0f, 1.2f, h, c, "l");
		Box(val.transform, w - 1.2f, 0f, 1.2f, h, c, "r");
		val.AddComponent<NativeField>().Init(font, get, set, lines > 1);
		return y - 28f - h - 24f;
	}

	private float Choice(string title, float x, float y, string[] options, Func<int> get, Action<int> set)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		Label(title, 24f, 26f, new Vector2(x, y), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.6f));
		float num = y - 28f;
		float num2 = x;
		for (int i = 0; i < options.Length; i++)
		{
			int idx = i;
			bool flag = get() == idx;
			GameObject val = new GameObject("choice", new Type[1] { typeof(RectTransform) });
			RectTransform component = val.GetComponent<RectTransform>();
			((Transform)component).SetParent((Transform)(object)root, false);
			component.anchorMin = new Vector2(0f, 1f);
			component.anchorMax = new Vector2(0f, 1f);
			component.pivot = new Vector2(0f, 1f);
			component.sizeDelta = new Vector2(110f, 46f);
			component.anchoredPosition = new Vector2(num2, num);
			((Graphic)val.AddComponent<Image>()).color = (flag ? new Color(1f, 1f, 1f, 0.16f) : new Color(0f, 0f, 0f, 0.3f));
			Color c = (Color)(flag ? Color.white : new Color(1f, 1f, 1f, 0.45f));
			Box((Transform)(object)component, 0f, 0f, 110f, 1.2f, c, "t");
			Box((Transform)(object)component, 0f, -46f, 110f, 1.2f, c, "b");
			Box((Transform)(object)component, 0f, 0f, 1.2f, 46f, c, "l");
			Box((Transform)(object)component, 109f, 0f, 1.2f, 46f, c, "r");
			GameObject val2 = new GameObject("text", new Type[1] { typeof(RectTransform) });
			RectTransform component2 = val2.GetComponent<RectTransform>();
			((Transform)component2).SetParent((Transform)(object)component, false);
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.offsetMin = Vector2.zero;
			component2.offsetMax = Vector2.zero;
			TextMeshProUGUI obj = val2.AddComponent<TextMeshProUGUI>();
			((TMP_Text)obj).font = font;
			((TMP_Text)obj).fontSize = 26f;
			Loc.Fit((TMP_Text)obj);
			((Graphic)obj).color = (Color)(flag ? Color.white : new Color(1f, 1f, 1f, 0.6f));
			((TMP_Text)obj).alignment = (TextAlignmentOptions)514;
			((TMP_Text)obj).text = options[i];
			((Graphic)obj).raycastTarget = false;
			val.AddComponent<NativeHit>().Init(delegate
			{
				set(idx);
				Close();
				Show();
			});
			num2 += 120f;
		}
		return num - 46f - 22f;
	}

	private float Toggle(string title, float x, float y, Func<bool> get, Action<bool> set)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		bool on = get();
		GameObject val = new GameObject("toggle", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)root, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(660f, 44f);
		component.anchoredPosition = new Vector2(x, y);
		((Graphic)val.AddComponent<Image>()).color = new Color(0f, 0f, 0f, 0.28f);
		Box((Transform)(object)component, 0f, 0f, 660f, 1.2f, new Color(1f, 1f, 1f, 0.55f), "t");
		Box((Transform)(object)component, 0f, -44f, 660f, 1.2f, new Color(1f, 1f, 1f, 0.55f), "b");
		GameObject val2 = new GameObject("text", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val2.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)component, false);
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = new Vector2(12f, 0f);
		component2.offsetMax = Vector2.zero;
		TextMeshProUGUI obj = val2.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = font;
		((TMP_Text)obj).fontSize = 26f;
		((Graphic)obj).color = (Color)(on ? Color.white : new Color(1f, 1f, 1f, 0.55f));
		((TMP_Text)obj).alignment = (TextAlignmentOptions)513;
		((TMP_Text)obj).text = (on ? "[x] " : "[  ] ") + title;
		((Graphic)obj).raycastTarget = false;
		val.AddComponent<NativeHit>().Init(delegate
		{
			set(!on);
			Close();
			Show();
		});
		return y - 44f - 14f;
	}

	private float Slider(string title, float x, float y, float w, float min, float max, Func<float> get, Action<float> set)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		Label(title + " " + get().ToString("F2"), 22f, 24f, new Vector2(x, y), (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.55f));
		GameObject val = Node("slider_" + title, (Transform)(object)root, x, y - 28f, w, 24f);
		((Graphic)val.AddComponent<Image>()).color = new Color(1f, 1f, 1f, 0.2f);
		float num = Mathf.InverseLerp(min, max, get());
		((Graphic)Node("knob", val.transform, w * num - 6f, 0f, 12f, 24f).AddComponent<Image>()).color = Color.white;
		val.AddComponent<NativeDrag>().Init(w, min, max, get, set);
		return y - 28f - 24f - 18f;
	}
}
