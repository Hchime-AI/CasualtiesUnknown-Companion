using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CUCompanion;

public class NativeMenuButton : MonoBehaviour
{
	public static bool Created;

	private GameObject created;

	private float nextTry;

	private const string CloneName = "CUCompanionMenuButton";

	private void Update()
	{
		if (!(Time.unscaledTime < nextTry))
		{
			nextTry = Time.unscaledTime + 0.75f;
			TryCreate();
		}
	}

	private void TryCreate()
	{
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Expected O, but got Unknown
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)CompanionHost.Instance != (Object)null && (Object)(object)CompanionHost.Instance.CurrentBody != (Object)null)
			{
				if ((Object)(object)created != (Object)null || Created)
				{
					RemoveClones();
				}
			}
			else
			{
				if ((Object)(object)created != (Object)null)
				{
					return;
				}
				RectTransform val = FindStartCell();
				if ((Object)(object)val == (Object)null)
				{
					return;
				}
				RemoveClones();
				Image component = ((Component)val).GetComponent<Image>();
				TextMeshProUGUI componentInChildren = ((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
				if ((Object)(object)component == (Object)null || (Object)(object)componentInChildren == (Object)null)
				{
					return;
				}
				Transform val2 = null;
				if ((Object)(object)PreRunScript.instance != (Object)null && (Object)(object)PreRunScript.instance.mainCanvas != (Object)null)
				{
					val2 = ((Component)PreRunScript.instance.mainCanvas).transform;
				}
				if ((Object)(object)val2 == (Object)null)
				{
					val2 = (((Object)(object)((Transform)val).parent != (Object)null) ? ((Transform)val).parent.parent : null);
				}
				if (!((Object)(object)val2 == (Object)null))
				{
					GameObject val3 = new GameObject("CUCompanionMenuButton", new Type[4]
					{
						typeof(RectTransform),
						typeof(CanvasRenderer),
						typeof(Image),
						typeof(Button)
					});
					RectTransform component2 = val3.GetComponent<RectTransform>();
					((Transform)component2).SetParent(val2, false);
					((Transform)component2).SetAsLastSibling();
					Rect rect = val.rect;
					float num = 220f;
					float height = ((Rect)rect).height;
					component2.anchorMin = new Vector2(0.5f, 0.5f);
					component2.anchorMax = new Vector2(0.5f, 0.5f);
					component2.pivot = new Vector2(0.5f, 0.5f);
					component2.sizeDelta = new Vector2(num, height);
					((Transform)component2).localScale = Vector3.one;
					((Transform)component2).localRotation = Quaternion.identity;
					Vector3[] array = (Vector3[])(object)new Vector3[4];
					val.GetWorldCorners(array);
					float num2 = Mathf.Max(array[0].x, array[3].x);
					float num3 = (array[0].y + array[1].y) * 0.5f;
					((Transform)component2).position = new Vector3(num2 + num * ((Transform)component2).lossyScale.x * 0.5f + 6f, num3, ((Transform)val).position.z);
					Image component3 = val3.GetComponent<Image>();
					component3.sprite = component.sprite;
					component3.type = component.type;
					((Graphic)component3).color = ((Graphic)component).color;
					((Graphic)component3).raycastTarget = true;
					try
					{
						component3.pixelsPerUnitMultiplier = component.pixelsPerUnitMultiplier;
					}
					catch
					{
					}
					GameObject val4 = new GameObject("Label", new Type[2]
					{
						typeof(RectTransform),
						typeof(CanvasRenderer)
					});
					RectTransform component4 = val4.GetComponent<RectTransform>();
					((Transform)component4).SetParent((Transform)(object)component2, false);
					component4.anchorMin = Vector2.zero;
					component4.anchorMax = Vector2.one;
					component4.offsetMin = Vector2.zero;
					component4.offsetMax = Vector2.zero;
					TextMeshProUGUI val5 = val4.AddComponent<TextMeshProUGUI>();
					((TMP_Text)val5).font = ((TMP_Text)componentInChildren).font;
					try
					{
						((TMP_Text)val5).fontSharedMaterial = ((TMP_Text)componentInChildren).fontSharedMaterial;
					}
					catch
					{
					}
					((TMP_Text)val5).fontSize = ((TMP_Text)componentInChildren).fontSize;
					((Graphic)val5).color = Color.white;
					((TMP_Text)val5).alignment = (TextAlignmentOptions)514;
					((TMP_Text)val5).enableWordWrapping = false;
					((Graphic)val5).raycastTarget = false;
					((TMP_Text)val5).text = Loc.T("陪伴设置");
					Button component5 = val3.GetComponent<Button>();
					((Selectable)component5).targetGraphic = (Graphic)(object)component3;
					((Selectable)component5).transition = (Selectable.Transition)0;
					component5.onClick = new Button.ButtonClickedEvent();
					((UnityEvent)component5.onClick).AddListener(new UnityAction(OpenPanel));
					val3.AddComponent<NativeBarButton>().Bind(component3, val, val5);
					val3.SetActive(true);
					created = val3;
					Created = true;
					Vector3[] array2 = (Vector3[])(object)new Vector3[4];
					component2.GetWorldCorners(array2);
					CompanionPlugin.Log.LogInfo((object)("[btn] 屏幕 " + Screen.width + "x" + Screen.height + " 左下=" + array2[0].x + "," + array2[0].y + " 右上=" + array2[2].x + "," + array2[2].y));
					CompanionPlugin.Log.LogInfo((object)("[btn] 已生成独立『陪伴设置』按钮（父节点 " + ((Object)val2).name + "，尺寸 " + Mathf.RoundToInt(num) + "x" + Mathf.RoundToInt(height) + "）"));
				}
			}
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log.LogWarning((object)("[btn] 生成失败：" + ex));
		}
	}

	private static void OpenPanel()
	{
		CompanionPlugin.Log.LogInfo((object)("[btn] 收到点击，Blocked=" + Blocked()));
		if (!Blocked())
		{
			try
			{
				PlayerCamera.PlayUISound("click", 1f);
			}
			catch
			{
			}
			CompanionPlugin.Log.LogInfo((object)"[btn] 点击『陪伴设置』");
			NativePanel.Open();
		}
	}

	private void RemoveClones()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		created = null;
		Created = false;
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject val in array)
		{
			if (!((Object)(object)val == (Object)null) && !(((Object)val).name != "CUCompanionMenuButton"))
			{
				Scene scene = val.scene;
				if (((Scene)scene).IsValid())
				{
					Object.Destroy((Object)(object)val);
				}
			}
		}
	}

	private static bool Blocked()
	{
		try
		{
			if (ScrollableText.textExists)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if ((Object)(object)SettingsMenu.instance != (Object)null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static RectTransform FindStartCell()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		string text = Loc.T("开始冒险");
		try
		{
			string other = Locale.GetOther("startrun");
			if (!string.IsNullOrEmpty(other))
			{
				text = other.Trim();
			}
		}
		catch
		{
		}
		RectTransform val = null;
		Transform[] array = Resources.FindObjectsOfTypeAll<Transform>();
		foreach (Transform val2 in array)
		{
			if ((Object)(object)val2 == (Object)null || ((Object)val2).name != "Start")
			{
				continue;
			}
			Scene scene = ((Component)val2).gameObject.scene;
			if (((Scene)scene).IsValid() && !((Object)(object)val2.parent == (Object)null) && !((Object)(object)((Component)val2).GetComponent<Image>() == (Object)null) && ((Object)val2.parent).name.ToLowerInvariant().Contains("bottom"))
			{
				TextMeshProUGUI componentInChildren = ((Component)val2).GetComponentInChildren<TextMeshProUGUI>(true);
				string text2 = (((Object)(object)componentInChildren == (Object)null || ((TMP_Text)componentInChildren).text == null) ? "" : ((TMP_Text)componentInChildren).text.Trim());
				if (text2 == text)
				{
					return (RectTransform)(object)((val2 is RectTransform) ? val2 : null);
				}
				if ((Object)(object)val == (Object)null && text2.Length > 0)
				{
					val = (RectTransform)(object)((val2 is RectTransform) ? val2 : null);
				}
			}
		}
		return val;
	}

	public static string CleanCharName(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return "";
		}
		return Regex.Replace(raw, "\\[[^\\]]*\\]", "").Trim().Trim('"', '“', '”', '\'', ' ');
	}
}
