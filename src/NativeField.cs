using System;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace CUCompanion;

public class NativeField : MonoBehaviour
{
	private TMP_FontAsset font;

	private Func<string> get;

	private Action<string> set;

	private bool multiline;

	private bool focused;

	private TextMeshProUGUI text;

	private GameObject caret;

	private float nextCaretLog;

	private const string CaretMarker = "<color=#00000000>|</color>";

	private string buffer = "";

	public float GrowHeight = 40f;

	public bool autoGrow;

	public bool IsFocused => focused;

	public void Init(TMP_FontAsset f, Func<string> g, Action<string> s, bool multi)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		font = f;
		get = g;
		set = s;
		multiline = multi;
		buffer = g() ?? "";
		GameObject val = new GameObject("text", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent(((Component)this).transform, false);
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = new Vector2(12f, 8f);
		component.offsetMax = new Vector2(-12f, -8f);
		text = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)text).font = font;
		((TMP_Text)text).fontSize = 26f;
		Loc.Fit((TMP_Text)text);
		((Graphic)text).color = Color.white;
		((TMP_Text)text).alignment = (TextAlignmentOptions)257;
		((TMP_Text)text).enableWordWrapping = true;
		((Graphic)text).raycastTarget = false;
		((TMP_Text)text).text = buffer + "<color=#00000000>|</color>";
		GameObject val2 = new GameObject("caret", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val2.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)((TMP_Text)text).rectTransform, false);
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.zero;
		component2.pivot = new Vector2(0f, 1f);
		component2.sizeDelta = new Vector2(2f, 26f);
		component2.anchoredPosition = Vector2.zero;
		Image obj = val2.AddComponent<Image>();
		((Graphic)obj).color = Color.white;
		((Graphic)obj).raycastTarget = false;
		caret = val2;
		caret.SetActive(false);
	}

	private void Update()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!Input.GetMouseButtonDown(0))
		{
			return;
		}
		Transform transform = ((Component)this).transform;
		RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		Vector2 val2 = default(Vector2);
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(val, (Vector2)(Input.mousePosition), (Camera)null, out val2))
		{
			return;
		}
		Rect rect = val.rect;
		bool flag = ((Rect)rect).Contains(val2);
		if (flag && !focused)
		{
			focused = true;
			InputGuard.Set(on: true);
			if ((Object)(object)caret != (Object)null)
			{
				caret.SetActive(true);
			}
		}
		else if (!flag && focused)
		{
			focused = false;
			InputGuard.Set(on: false);
			Apply();
			if ((Object)(object)caret != (Object)null)
			{
				caret.SetActive(false);
			}
		}
	}

	private void LateUpdate()
	{
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		if (!focused)
		{
			return;
		}
		try
		{
			Input.imeCompositionMode = (IMECompositionMode)1;
		}
		catch
		{
		}
		bool flag = false;
		string text = buffer ?? "";
		string inputString = Input.inputString;
		if (!string.IsNullOrEmpty(inputString))
		{
			string text2 = inputString;
			for (int i = 0; i < text2.Length; i++)
			{
				char c = text2[i];
				if (c == '\b')
				{
					if (text.Length > 0)
					{
						text = text.Substring(0, text.Length - 1);
					}
				}
				else if (c == '\n' || c == '\r')
				{
					if (multiline)
					{
						text += "\n";
					}
				}
				else if (c >= ' ')
				{
					text += c;
				}
			}
			flag = true;
		}
		string text3 = "";
		try
		{
			text3 = Input.compositionString;
		}
		catch
		{
		}
		if (text3 == null)
		{
			text3 = "";
		}
		if ((Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305)) && Input.GetKeyDown((KeyCode)118))
		{
			string clip = "";
			try
			{
				clip = GUIUtility.systemCopyBuffer ?? "";
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(clip))
			{
				clip = clip.Replace("\r\n", multiline ? "\n" : " ").Replace('\r', multiline ? '\n' : ' ');
				if (!multiline)
				{
					clip = clip.Replace('\n', ' ');
				}
				text += clip;
				flag = true;
			}
		}
		if (flag)
		{
			buffer = text;
		}
		if ((Object)(object)this.text != (Object)null)
		{
			((TMP_Text)this.text).text = DisplaySafe(buffer) + DisplaySafe(text3) + "<color=#00000000>|</color>";
		}
		if ((Object)(object)this.text != (Object)null)
		{
			float preferredHeight = ((TMP_Text)this.text).preferredHeight;
			GrowHeight = Mathf.Clamp(preferredHeight + 14f, 40f, 150f);
			if (autoGrow)
			{
				Transform transform = ((Component)this).transform;
				RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
				if ((Object)(object)val != (Object)null && Mathf.Abs(val.sizeDelta.y - GrowHeight) > 0.5f)
				{
					val.sizeDelta = new Vector2(val.sizeDelta.x, GrowHeight);
				}
			}
			if ((Object)(object)caret != (Object)null)
			{
				((TMP_Text)this.text).ForceMeshUpdate(false, false);
				float num = 26f;
				bool flag2 = false;
				Vector3 val2 = Vector3.zero;
				TMP_TextInfo textInfo = ((TMP_Text)this.text).textInfo;
				if (textInfo != null && textInfo.characterCount > 0)
				{
					int num2 = -1;
					for (int num3 = textInfo.characterCount - 1; num3 >= 0; num3--)
					{
						if (textInfo.characterInfo[num3].character == '|')
						{
							num2 = num3;
							break;
						}
					}
					if (num2 >= 0)
					{
						TMP_CharacterInfo val3 = textInfo.characterInfo[num2];
						int num4 = Mathf.Clamp(val3.lineNumber, 0, Mathf.Max(0, textInfo.lineCount - 1));
						TMP_LineInfo val4 = textInfo.lineInfo[num4];
						num = Mathf.Max(6f, val4.ascender - val4.descender);
						val2 = val3.topRight;
						flag2 = true;
					}
				}
				RectTransform component = caret.GetComponent<RectTransform>();
				if ((Object)(object)component != (Object)null)
				{
					component.sizeDelta = new Vector2(2f, num);
					if (flag2)
					{
						((Transform)component).position = ((TMP_Text)this.text).transform.TransformPoint(val2);
					}
				}
			}
		}
		if ((Object)(object)caret != (Object)null)
		{
			caret.SetActive(Time.unscaledTime % 1f < 0.55f);
		}
		if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug && focused && (Object)(object)caret != (Object)null && Time.unscaledTime > nextCaretLog)
		{
			nextCaretLog = Time.unscaledTime + 2f;
			RectTransform rectTransform = ((TMP_Text)this.text).rectTransform;
			RectTransform component2 = caret.GetComponent<RectTransform>();
			ManualLogSource log = CompanionPlugin.Log;
			object[] obj3 = new object[5]
			{
				((Transform)rectTransform).position,
				null,
				null,
				null,
				null
			};
			Rect rect = rectTransform.rect;
			obj3[1] = ((Rect)rect).size;
			obj3[2] = ((Transform)component2).position;
			obj3[3] = component2.sizeDelta;
			obj3[4] = (buffer ?? "").Length;
			log.LogInfo((object)string.Format("[caret] textPos={0} textSize={1} caretPos={2} caretSize={3} len={4}", obj3));
		}
	}

	public void Apply()
	{
		if (set != null)
		{
			set(buffer ?? "");
		}
	}

	private static string DisplaySafe(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s.Replace('<', '＜').Replace('>', '＞');
	}

	public void SetText(string v)
	{
		buffer = v ?? "";
		if ((Object)(object)text != (Object)null)
		{
			((TMP_Text)text).text = DisplaySafe(buffer) + "<color=#00000000>|</color>";
		}
		Apply();
	}

	public void Focus()
	{
		focused = true;
		InputGuard.Set(on: true);
		if ((Object)(object)caret != (Object)null)
		{
			caret.SetActive(true);
		}
	}

	public void Blur()
	{
		if (focused)
		{
			focused = false;
			InputGuard.Set(on: false);
			Apply();
			if ((Object)(object)caret != (Object)null)
			{
				caret.SetActive(false);
			}
		}
	}

	private void OnDestroy()
	{
		if (focused)
		{
			focused = false;
			InputGuard.Set(on: false);
		}
		Apply();
	}
}
