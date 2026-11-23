using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CUCompanion;

public class MedicalPartnerAssistant : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public const float MaxAssistDistance = 3.5f;

	public const float AutoRevertDistance = 8f;

	private WoundView woundView;

	private RectTransform btnRoot;

	private Image btnBg;

	private Outline btnOutline;

	private TextMeshProUGUI btnLabel;

	private Button button;

	private RectTransform statusBtnRoot;

	private Image statusBtnBg;

	private Outline statusBtnOutline;

	private TextMeshProUGUI statusBtnLabel;

	private Button statusButton;

	private GameObject packRoot;

	private Body lastShownBody;

	private RectTransform packScrollContent;

	private TextMeshProUGUI packTitle;

	private TextMeshProUGUI packSubtitle;

	private readonly List<CU_DoctorItemSlot> slotPool = new List<CU_DoctorItemSlot>();

	private CU_DoctorItemSlot selectedSlot;

	private float nextPackRefresh;

	private RectTransform dragGhostRoot;

	private Image dragGhostImg;

	private bool hovered;

	public static MedicalPartnerAssistant Instance { get; private set; }

	public static bool IsInspectingPartner { get; private set; }

	public static bool IsHovered
	{
		get
		{
			if (!((Object)(object)Instance != (Object)null) || !Instance.hovered)
			{
				if ((Object)(object)Instance != (Object)null && (Object)(object)Instance.dragGhostRoot != (Object)null)
				{
					return ((Component)Instance.dragGhostRoot).gameObject.activeSelf;
				}
				return false;
			}
			return true;
		}
	}

	private void Awake()
	{
		Instance = this;
		woundView = ((Component)this).GetComponent<WoundView>();
		lastShownBody = (((Object)(object)woundView != (Object)null) ? woundView.body : null);
		BuildButton();
		BuildDoctorPack();
	}

	private void OnEnable()
	{
		Instance = this;
		if ((Object)(object)btnRoot == (Object)null)
		{
			BuildButton();
		}
		if ((Object)(object)packRoot == (Object)null)
		{
			BuildDoctorPack();
		}
		UpdateLayoutPosition();
	}

	private void OnDisable()
	{
		IsInspectingPartner = false;
		if ((Object)(object)packRoot != (Object)null && packRoot.activeSelf)
		{
			packRoot.SetActive(false);
		}
		if ((Object)(object)dragGhostRoot != (Object)null && ((Component)dragGhostRoot).gameObject.activeSelf)
		{
			((Component)dragGhostRoot).gameObject.SetActive(false);
		}
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && (Object)(object)main.body != (Object)null && (Object)(object)woundView != (Object)null && woundView.body != main.body)
		{
			MedicalPanelDecoupler.SetInspectTarget(main.body);
		}
	}

	private void BuildButton()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Expected O, but got Unknown
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Expected O, but got Unknown
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Expected O, but got Unknown
		if ((Object)(object)btnRoot != (Object)null)
		{
			return;
		}
		int num = LayerMask.NameToLayer("UI");
		GameObject val = new GameObject("CU_MedicalPartnerSwitchBtn", new Type[1] { typeof(RectTransform) });
		if (num >= 0)
		{
			val.layer = num;
		}
		btnRoot = val.GetComponent<RectTransform>();
		((Transform)btnRoot).SetParent(((Component)this).transform, false);
		btnRoot.anchorMin = new Vector2(0.5f, 0.5f);
		btnRoot.anchorMax = new Vector2(0.5f, 0.5f);
		btnRoot.pivot = new Vector2(0.5f, 0.5f);
		btnRoot.sizeDelta = new Vector2(300f, 44f);
		btnRoot.anchoredPosition = new Vector2(0f, -415f);
		btnBg = val.AddComponent<Image>();
		((Graphic)btnBg).color = new Color(0.04f, 0.1f, 0.18f, 0.94f);
		((Graphic)btnBg).raycastTarget = true;
		btnOutline = val.AddComponent<Outline>();
		((Shadow)btnOutline).effectColor = new Color(0.25f, 0.7f, 1f, 0.85f);
		((Shadow)btnOutline).effectDistance = new Vector2(2f, -2f);
		GameObject val2 = new GameObject("Label", new Type[1] { typeof(RectTransform) });
		if (num >= 0)
		{
			val2.layer = num;
		}
		RectTransform component = val2.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)btnRoot, false);
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.sizeDelta = Vector2.zero;
		btnLabel = val2.AddComponent<TextMeshProUGUI>();
		TMP_FontAsset val3 = NativeHudController.GameFont;
		if ((Object)(object)val3 == (Object)null && (Object)(object)woundView != (Object)null)
		{
			object obj;
			if (!((Object)(object)woundView.speciesText != (Object)null))
			{
				TextMeshProUGUI nameText = woundView.nameText;
				obj = ((nameText != null) ? ((TMP_Text)nameText).font : null);
			}
			else
			{
				obj = ((TMP_Text)woundView.speciesText).font;
			}
			val3 = (TMP_FontAsset)obj;
		}
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)btnLabel).font = val3;
		}
		((TMP_Text)btnLabel).fontSize = 17f;
		Loc.Fit((TMP_Text)btnLabel);
		((TMP_Text)btnLabel).alignment = (TextAlignmentOptions)514;
		((Graphic)btnLabel).color = Color.white;
		((Graphic)btnLabel).raycastTarget = false;
		button = val.AddComponent<Button>();
		((Selectable)button).targetGraphic = (Graphic)(object)btnBg;
		((UnityEvent)button.onClick).AddListener(new UnityAction(OnClickSwitch));
		ModUIInputBlocker.Register(btnRoot);
		GameObject val4 = new GameObject("CU_DualStatusToggleBtn", new Type[1] { typeof(RectTransform) });
		if (num >= 0)
		{
			val4.layer = num;
		}
		statusBtnRoot = val4.GetComponent<RectTransform>();
		((Transform)statusBtnRoot).SetParent(((Component)this).transform, false);
		statusBtnRoot.anchorMin = new Vector2(0.5f, 0.5f);
		statusBtnRoot.anchorMax = new Vector2(0.5f, 0.5f);
		statusBtnRoot.pivot = new Vector2(0.5f, 0.5f);
		statusBtnRoot.sizeDelta = new Vector2(300f, 36f);
		statusBtnRoot.anchoredPosition = new Vector2(0f, -465f);
		statusBtnBg = val4.AddComponent<Image>();
		((Graphic)statusBtnBg).color = new Color(0.04f, 0.08f, 0.14f, 0.9f);
		((Graphic)statusBtnBg).raycastTarget = true;
		statusBtnOutline = val4.AddComponent<Outline>();
		((Shadow)statusBtnOutline).effectColor = new Color(0.2f, 0.5f, 0.75f, 0.7f);
		((Shadow)statusBtnOutline).effectDistance = new Vector2(1.5f, -1.5f);
		GameObject val5 = new GameObject("Label", new Type[1] { typeof(RectTransform) });
		if (num >= 0)
		{
			val5.layer = num;
		}
		RectTransform component2 = val5.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)statusBtnRoot, false);
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.sizeDelta = Vector2.zero;
		statusBtnLabel = val5.AddComponent<TextMeshProUGUI>();
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)statusBtnLabel).font = val3;
		}
		((TMP_Text)statusBtnLabel).fontSize = 15f;
		Loc.Fit((TMP_Text)statusBtnLabel);
		((TMP_Text)statusBtnLabel).alignment = (TextAlignmentOptions)514;
		((Graphic)statusBtnLabel).color = new Color(0.85f, 0.92f, 0.98f, 0.95f);
		((Graphic)statusBtnLabel).raycastTarget = false;
		((TMP_Text)statusBtnLabel).text = Loc.T("双人生理数据监视 [已关闭]");
		statusButton = val4.AddComponent<Button>();
		((Selectable)statusButton).targetGraphic = (Graphic)(object)statusBtnBg;
		((UnityEvent)statusButton.onClick).AddListener(new UnityAction(OnClickStatusToggle));
		ModUIInputBlocker.Register(statusBtnRoot);
		UpdateLayoutPosition();
	}

	private void OnClickStatusToggle()
	{
		DualMode.ShowDualStatus = !DualMode.ShowDualStatus;
		try
		{
			PlayerCamera.PlayUISound("click", 1f);
		}
		catch
		{
		}
	}

	private void BuildDoctorPack()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Expected O, but got Unknown
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Expected O, but got Unknown
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0495: Expected O, but got Unknown
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Expected O, but got Unknown
		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d5: Expected O, but got Unknown
		//IL_0604: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)packRoot != (Object)null)
		{
			return;
		}
		packRoot = new GameObject("CU_DoctorFieldPack", new Type[1] { typeof(RectTransform) });
		RectTransform component = packRoot.GetComponent<RectTransform>();
		((Transform)component).SetParent(((Component)this).transform, false);
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0f, 0.5f);
		component.sizeDelta = new Vector2(175f, 440f);
		component.anchoredPosition = new Vector2(365f, -10f);
		((Graphic)packRoot.AddComponent<Image>()).color = new Color(0.04f, 0.08f, 0.16f, 0.95f);
		Outline obj = packRoot.AddComponent<Outline>();
		((Shadow)obj).effectColor = new Color(0.25f, 0.7f, 1f, 0.85f);
		((Shadow)obj).effectDistance = new Vector2(1.5f, -1.5f);
		GameObject val = new GameObject("Header", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)component, false);
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.sizeDelta = new Vector2(0f, 44f);
		component2.anchoredPosition = Vector2.zero;
		((Graphic)val.AddComponent<Image>()).color = new Color(0.06f, 0.14f, 0.26f, 0.95f);
		GameObject val2 = new GameObject("Title", new Type[1] { typeof(RectTransform) });
		RectTransform component3 = val2.GetComponent<RectTransform>();
		((Transform)component3).SetParent((Transform)(object)component2, false);
		component3.anchorMin = new Vector2(0f, 0.45f);
		component3.anchorMax = new Vector2(1f, 1f);
		component3.sizeDelta = Vector2.zero;
		component3.anchoredPosition = Vector2.zero;
		packTitle = val2.AddComponent<TextMeshProUGUI>();
		TMP_FontAsset val3 = NativeHudController.GameFont;
		if ((Object)(object)val3 == (Object)null && (Object)(object)woundView != (Object)null)
		{
			object obj2;
			if (!((Object)(object)woundView.speciesText != (Object)null))
			{
				TextMeshProUGUI nameText = woundView.nameText;
				obj2 = ((nameText != null) ? ((TMP_Text)nameText).font : null);
			}
			else
			{
				obj2 = ((TMP_Text)woundView.speciesText).font;
			}
			val3 = (TMP_FontAsset)obj2;
		}
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)packTitle).font = val3;
		}
		((TMP_Text)packTitle).fontSize = 13f;
		((TMP_Text)packTitle).alignment = (TextAlignmentOptions)514;
		((Graphic)packTitle).color = new Color(0.35f, 0.85f, 1f, 1f);
		((TMP_Text)packTitle).text = Loc.T("【施治者行囊】");
		((Graphic)packTitle).raycastTarget = false;
		GameObject val4 = new GameObject("Subtitle", new Type[1] { typeof(RectTransform) });
		RectTransform component4 = val4.GetComponent<RectTransform>();
		((Transform)component4).SetParent((Transform)(object)component2, false);
		component4.anchorMin = new Vector2(0f, 0f);
		component4.anchorMax = new Vector2(1f, 0.45f);
		component4.sizeDelta = Vector2.zero;
		component4.anchoredPosition = Vector2.zero;
		packSubtitle = val4.AddComponent<TextMeshProUGUI>();
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)packSubtitle).font = val3;
		}
		((TMP_Text)packSubtitle).fontSize = 9.5f;
		((TMP_Text)packSubtitle).alignment = (TextAlignmentOptions)514;
		((Graphic)packSubtitle).color = new Color(0.7f, 0.82f, 0.95f, 0.8f);
		((TMP_Text)packSubtitle).text = Loc.T("双击或拖拽物资至部位施治");
		((Graphic)packSubtitle).raycastTarget = false;
		GameObject val5 = new GameObject("Viewport", new Type[1] { typeof(RectTransform) });
		RectTransform component5 = val5.GetComponent<RectTransform>();
		((Transform)component5).SetParent((Transform)(object)component, false);
		component5.anchorMin = Vector2.zero;
		component5.anchorMax = Vector2.one;
		component5.sizeDelta = new Vector2(-6f, -50f);
		component5.anchoredPosition = new Vector2(0f, -24f);
		val5.AddComponent<RectMask2D>();
		GameObject val6 = new GameObject("Content", new Type[1] { typeof(RectTransform) });
		packScrollContent = val6.GetComponent<RectTransform>();
		((Transform)packScrollContent).SetParent((Transform)(object)component5, false);
		packScrollContent.anchorMin = new Vector2(0f, 1f);
		packScrollContent.anchorMax = new Vector2(1f, 1f);
		packScrollContent.pivot = new Vector2(0.5f, 1f);
		packScrollContent.sizeDelta = new Vector2(0f, 0f);
		packScrollContent.anchoredPosition = Vector2.zero;
		VerticalLayoutGroup obj3 = val6.AddComponent<VerticalLayoutGroup>();
		((LayoutGroup)obj3).padding = new RectOffset(2, 2, 4, 4);
		((HorizontalOrVerticalLayoutGroup)obj3).spacing = 3f;
		((LayoutGroup)obj3).childAlignment = (TextAnchor)1;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlHeight = false;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandHeight = false;
		val6.AddComponent<ContentSizeFitter>().verticalFit = (ContentSizeFitter.FitMode)2;
		ScrollRect obj4 = packRoot.AddComponent<ScrollRect>();
		obj4.content = packScrollContent;
		obj4.viewport = component5;
		obj4.horizontal = false;
		obj4.vertical = true;
		obj4.scrollSensitivity = 25f;
		obj4.movementType = (ScrollRect.MovementType)2;
		GameObject val7 = new GameObject("CU_DoctorDragGhost", new Type[1] { typeof(RectTransform) });
		dragGhostRoot = val7.GetComponent<RectTransform>();
		((Transform)dragGhostRoot).SetParent(((Component)this).transform, false);
		dragGhostRoot.sizeDelta = new Vector2(44f, 44f);
		dragGhostImg = val7.AddComponent<Image>();
		dragGhostImg.preserveAspect = true;
		((Graphic)dragGhostImg).raycastTarget = false;
		val7.AddComponent<CanvasGroup>().blocksRaycasts = false;
		((Component)dragGhostRoot).gameObject.SetActive(false);
		ModUIInputBlocker.Register(component);
		packRoot.SetActive(false);
	}

	public void UpdateLayoutPosition()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)btnRoot == (Object)null)
		{
			return;
		}
		float num = -415f;
		InvButton[] componentsInChildren = ((Component)this).GetComponentsInChildren<InvButton>(true);
		float num2 = 9999f;
		bool flag = false;
		InvButton[] array = componentsInChildren;
		foreach (InvButton val in array)
		{
			if (!((Object)(object)val != (Object)null) || !val.woundView)
			{
				continue;
			}
			Transform transform = ((Component)val).transform;
			RectTransform val2 = (RectTransform)(object)((transform is RectTransform) ? transform : null);
			if ((Object)(object)val2 != (Object)null)
			{
				float num3 = val2.anchoredPosition.y - val2.sizeDelta.y * 0.5f;
				if (num3 < num2)
				{
					num2 = num3;
					flag = true;
				}
			}
		}
		if (flag && num2 < 0f && num2 < 9000f)
		{
			num = num2 - 30f;
		}
		btnRoot.anchoredPosition = new Vector2(0f, num);
		if ((Object)(object)statusBtnRoot != (Object)null)
		{
			statusBtnRoot.anchoredPosition = new Vector2(0f, num - 48f);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		hovered = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		hovered = false;
	}

	private void Update()
	{
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0561: Unknown result type (might be due to invalid IL or missing references)
		//IL_0546: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)btnRoot == (Object)null || (Object)(object)woundView == (Object)null)
		{
			return;
		}
		Body shown = woundView.body;
		if (shown != lastShownBody)
		{
			Body previous = (((Object)(object)lastShownBody != (Object)null) ? lastShownBody : shown);
			LimbIconCare.Reconcile(woundView, previous);
			lastShownBody = shown;
			LimbIconCare.Apply(woundView, shown);
		}
		else
		{
			LimbIconCare.Reconcile(woundView, shown);
		}
		DualMode instance = DualMode.Instance;
		if ((Object)(object)instance == (Object)null || !((Behaviour)instance).enabled)
		{
			if (((Component)btnRoot).gameObject.activeSelf)
			{
				((Component)btnRoot).gameObject.SetActive(false);
			}
			if ((Object)(object)packRoot != (Object)null && packRoot.activeSelf)
			{
				packRoot.SetActive(false);
			}
			IsInspectingPartner = false;
			return;
		}
		PlayerCamera main = PlayerCamera.main;
		Body val = (((Object)(object)main != (Object)null) ? main.body : null);
		if ((Object)(object)val == (Object)null)
		{
			if (((Component)btnRoot).gameObject.activeSelf)
			{
				((Component)btnRoot).gameObject.SetActive(false);
			}
			if ((Object)(object)packRoot != (Object)null && packRoot.activeSelf)
			{
				packRoot.SetActive(false);
			}
			IsInspectingPartner = false;
			return;
		}
		Body val2 = ((val == instance.Player) ? instance.Partner : instance.Player);
		if ((Object)(object)val2 == (Object)null || (Object)(object)((Component)val2).gameObject == (Object)null || !((Component)val2).gameObject.activeInHierarchy)
		{
			if (((Component)btnRoot).gameObject.activeSelf)
			{
				((Component)btnRoot).gameObject.SetActive(false);
			}
			if ((Object)(object)packRoot != (Object)null && packRoot.activeSelf)
			{
				packRoot.SetActive(false);
			}
			IsInspectingPartner = false;
			return;
		}
		if (!((Component)btnRoot).gameObject.activeSelf)
		{
			((Component)btnRoot).gameObject.SetActive(true);
		}
		float num = Vector3.Distance(((Component)val).transform.position, ((Component)val2).transform.position);
		bool flag = num <= 3.5f;
		bool flag3 = (IsInspectingPartner = woundView.body == val2);
		if ((Object)(object)packRoot != (Object)null)
		{
			if (packRoot.activeSelf != flag3)
			{
				packRoot.SetActive(flag3);
			}
			if (flag3 && Time.unscaledTime >= nextPackRefresh)
			{
				nextPackRefresh = Time.unscaledTime + 0.35f;
				RefreshDoctorPack(val, main?.selectedLimb);
			}
		}
		if (flag3 && num > 8f)
		{
			MedicalPanelDecoupler.SetInspectTarget(val);
			if (main != null)
			{
				main.DoAlert(Loc.T("距离过远，已退出同伴医疗协助"), false);
			}
			return;
		}
		if (flag3)
		{
			((TMP_Text)btnLabel).text = Loc.T("切回自身医疗 [") + ((Object)val).name + "]";
			Color val3 = default(Color);
			val3 = new Color(0.2f, 0.14f, 0.04f, 0.94f);
			Color effectColor = default(Color);
			effectColor = new Color(1f, 0.82f, 0.28f, 0.95f);
			((Graphic)btnBg).color = (Color)(hovered ? new Color(0.28f, 0.2f, 0.06f, 0.98f) : val3);
			((Shadow)btnOutline).effectColor = effectColor;
			((Graphic)btnLabel).color = new Color(1f, 0.92f, 0.5f, 1f);
			((Selectable)button).interactable = true;
		}
		else if (flag)
		{
			((TMP_Text)btnLabel).text = Loc.T("协助治疗同伴") + " [" + ((Object)val2).name + "] (" + num.ToString("F1") + "m)";
			Color val4 = default(Color);
			val4 = new Color(0.04f, 0.12f, 0.22f, 0.94f);
			Color effectColor2 = default(Color);
			effectColor2 = new Color(0.25f, 0.72f, 1f, 0.85f);
			((Graphic)btnBg).color = (Color)(hovered ? new Color(0.08f, 0.18f, 0.32f, 0.98f) : val4);
			((Shadow)btnOutline).effectColor = effectColor2;
			((Graphic)btnLabel).color = new Color(0.45f, 0.92f, 1f, 1f);
			((Selectable)button).interactable = true;
		}
		else
		{
			((TMP_Text)btnLabel).text = "[" + Loc.T("距离过远") + " (" + num.ToString("F1") + "m) " + Loc.T("无法协助") + "]";
			((Graphic)btnBg).color = new Color(0.08f, 0.08f, 0.1f, 0.8f);
			((Shadow)btnOutline).effectColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
			((Graphic)btnLabel).color = new Color(0.6f, 0.6f, 0.65f, 0.6f);
			((Selectable)button).interactable = false;
		}
		if ((Object)(object)statusBtnRoot != (Object)null)
		{
			bool flag4 = (Object)(object)instance != (Object)null && ((Behaviour)instance).enabled;
			if (((Component)statusBtnRoot).gameObject.activeSelf != flag4)
			{
				((Component)statusBtnRoot).gameObject.SetActive(flag4);
			}
			if ((Object)(object)statusBtnLabel != (Object)null)
			{
				((TMP_Text)statusBtnLabel).text = (DualMode.ShowDualStatus ? Loc.T("双人生理数据监视 [已开启]") : Loc.T("双人生理数据监视 [已关闭]"));
			}
			if ((Object)(object)statusBtnBg != (Object)null)
			{
				((Graphic)statusBtnBg).color = (DualMode.ShowDualStatus ? new Color(0.08f, 0.22f, 0.36f, 0.94f) : new Color(0.04f, 0.08f, 0.14f, 0.9f));
			}
			if ((Object)(object)statusBtnOutline != (Object)null)
			{
				((Shadow)statusBtnOutline).effectColor = (DualMode.ShowDualStatus ? new Color(0.4f, 0.85f, 1f, 0.95f) : new Color(0.2f, 0.45f, 0.65f, 0.6f));
			}
		}
	}

	private void OnDestroy()
	{
		LimbIconCare.Clear();
		ModUIInputBlocker.Unregister(btnRoot);
		ModUIInputBlocker.Unregister(statusBtnRoot);
		if ((Object)(object)packRoot != (Object)null)
		{
			ModUIInputBlocker.Unregister(packRoot.GetComponent<RectTransform>());
		}
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
	}

	public void OnLimbSelectedByClick(Limb limb)
	{
		if ((Object)(object)packSubtitle != (Object)null)
		{
			((TMP_Text)packSubtitle).text = (((Object)(object)limb != (Object)null) ? (Loc.T("选定部位: ") + ((Object)limb).name) : Loc.T("双击或拖拽物资至部位施治"));
		}
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && (Object)(object)main.body != (Object)null)
		{
			RefreshDoctorPack(main.body, limb);
		}
	}

	public void StartItemDrag(Item item, Vector2 screenPos)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)dragGhostRoot != (Object)null && (Object)(object)item != (Object)null)
		{
			SpriteRenderer component = ((Component)item).GetComponent<SpriteRenderer>();
			if ((Object)(object)component != (Object)null && (Object)(object)component.sprite != (Object)null)
			{
				dragGhostImg.sprite = component.sprite;
				((Graphic)dragGhostImg).color = new Color(1f, 1f, 1f, 0.85f);
				((Behaviour)dragGhostImg).enabled = true;
			}
			else
			{
				((Behaviour)dragGhostImg).enabled = false;
			}
			((Transform)dragGhostRoot).position = (Vector2)(screenPos);
			((Component)dragGhostRoot).gameObject.SetActive(true);
		}
	}

	public void UpdateItemDrag(Vector2 screenPos)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)dragGhostRoot != (Object)null && ((Component)dragGhostRoot).gameObject.activeSelf)
		{
			((Transform)dragGhostRoot).position = (Vector2)(screenPos);
		}
	}

	public void EndItemDrag()
	{
		if ((Object)(object)dragGhostRoot != (Object)null && ((Component)dragGhostRoot).gameObject.activeSelf)
		{
			((Component)dragGhostRoot).gameObject.SetActive(false);
		}
	}

	public void SelectPackSlot(CU_DoctorItemSlot slot)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		selectedSlot = slot;
		PlayerCamera main = PlayerCamera.main;
		Limb val = (((Object)(object)main != (Object)null) ? main.selectedLimb : null);
		for (int i = 0; i < slotPool.Count; i++)
		{
			CU_DoctorItemSlot cU_DoctorItemSlot = slotPool[i];
			if (!((Object)(object)cU_DoctorItemSlot == (Object)null) && ((Component)cU_DoctorItemSlot).gameObject.activeSelf)
			{
				if (cU_DoctorItemSlot == slot)
				{
					((Shadow)cU_DoctorItemSlot.slotOutline).effectColor = new Color(0.35f, 0.85f, 1f, 1f);
					continue;
				}
				bool flag = (Object)(object)val != (Object)null && (Object)(object)cU_DoctorItemSlot.item != (Object)null && cU_DoctorItemSlot.item.Stats != null && cU_DoctorItemSlot.item.Stats.ActuallyUsableOnLimb(cU_DoctorItemSlot.item);
				((Shadow)cU_DoctorItemSlot.slotOutline).effectColor = (flag ? new Color(0.2f, 1f, 0.55f, 0.95f) : new Color(0.2f, 0.35f, 0.5f, 0.5f));
			}
		}
	}

	public void ApplyItemDirect(Item it, Limb targetLimb)
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		if ((Object)(object)targetLimb == (Object)null)
		{
			main.DoAlert(Loc.T("请先在人体图上选择受创部位！"), true);
		}
		else
		{
			if ((Object)(object)it == (Object)null)
			{
				return;
			}
			main.selectedLimb = targetLimb;
			if (it.Stats != null && it.Stats.ActuallyUsableOnLimb(it))
			{
				main.ApplyWoundItem(it);
				WoundView view = WoundView.view;
				if ((Object)(object)view != (Object)null)
				{
					view.UpdateView();
				}
				main.DoAlert(Loc.T("已对 [") + ((Object)targetLimb).name + Loc.T("] 使用 [") + it.fullName + "]", false);
				RefreshDoctorPack(main.body, targetLimb);
			}
			else
			{
				main.DoAlert("[" + it.fullName + Loc.T("] 无法用于 [") + ((Object)targetLimb).name + Loc.T("] 的伤势！"), true);
			}
		}
	}

	public void ApplyItemToCurrentSelection(Item it)
	{
		PlayerCamera main = PlayerCamera.main;
		if (!((Object)(object)main == (Object)null))
		{
			if ((Object)(object)main.selectedLimb == (Object)null)
			{
				main.DoAlert(Loc.T("请先在人体图上单击选择受创部位（或直接将物品拖拽至该部位）！"), true);
			}
			else
			{
				ApplyItemDirect(it, main.selectedLimb);
			}
		}
	}

	private void RefreshDoctorPack(Body actor, Limb targetLimb)
	{
		if ((Object)(object)packRoot == (Object)null || (Object)(object)packScrollContent == (Object)null || (Object)(object)actor == (Object)null)
		{
			return;
		}
		if ((Object)(object)packSubtitle != (Object)null)
		{
			((TMP_Text)packSubtitle).text = (((Object)(object)targetLimb != (Object)null) ? (Loc.T("选定部位: ") + ((Object)targetLimb).name) : Loc.T("双击或拖拽物资至部位施治"));
		}
		List<Item> list = actor.GetAllItemsThorough();
		if (list == null)
		{
			list = new List<Item>();
		}
		List<Item> list2 = new List<Item>();
		foreach (Item item in list)
		{
			if (!((Object)(object)item == (Object)null))
			{
				list2.Add(item);
			}
		}
		list2.Sort(delegate(Item a, Item b)
		{
			bool flag = a.Stats != null && (a.Stats.usableOnLimb || a.Stats.usable || (Object)(object)((Component)a).GetComponent<WaterContainerItem>() != (Object)null);
			bool flag2 = b.Stats != null && (b.Stats.usableOnLimb || b.Stats.usable || (Object)(object)((Component)b).GetComponent<WaterContainerItem>() != (Object)null);
			if (flag && !flag2)
			{
				return -1;
			}
			return (!flag && flag2) ? 1 : string.Compare(a.fullName, b.fullName, StringComparison.OrdinalIgnoreCase);
		});
		int count = list2.Count;
		while (slotPool.Count < count)
		{
			slotPool.Add(CreateSlotInstance());
		}
		TMP_FontAsset val = NativeHudController.GameFont;
		if ((Object)(object)val == (Object)null && (Object)(object)woundView != (Object)null)
		{
			object obj;
			if (!((Object)(object)woundView.speciesText != (Object)null))
			{
				TextMeshProUGUI nameText = woundView.nameText;
				obj = ((nameText != null) ? ((TMP_Text)nameText).font : null);
			}
			else
			{
				obj = ((TMP_Text)woundView.speciesText).font;
			}
			val = (TMP_FontAsset)obj;
		}
		for (int i = 0; i < slotPool.Count; i++)
		{
			CU_DoctorItemSlot cU_DoctorItemSlot = slotPool[i];
			if (i < count)
			{
				((Component)cU_DoctorItemSlot).gameObject.SetActive(true);
				BindSlot(cU_DoctorItemSlot, list2[i], targetLimb, val);
			}
			else
			{
				((Component)cU_DoctorItemSlot).gameObject.SetActive(false);
			}
		}
	}

	private CU_DoctorItemSlot CreateSlotInstance()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Slot", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)packScrollContent, false);
		component.sizeDelta = new Vector2(165f, 42f);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = new Color(0.08f, 0.14f, 0.22f, 0.9f);
		Outline val3 = val.AddComponent<Outline>();
		((Shadow)val3).effectColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);
		((Shadow)val3).effectDistance = new Vector2(1f, -1f);
		GameObject val4 = new GameObject("Icon", new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val4.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)component, false);
		component2.anchorMin = new Vector2(0f, 0.5f);
		component2.anchorMax = new Vector2(0f, 0.5f);
		component2.pivot = new Vector2(0f, 0.5f);
		component2.sizeDelta = new Vector2(30f, 30f);
		component2.anchoredPosition = new Vector2(5f, 0f);
		Image val5 = val4.AddComponent<Image>();
		val5.preserveAspect = true;
		((Graphic)val5).raycastTarget = false;
		RectTransform component3 = new GameObject("TextInfo", new Type[1] { typeof(RectTransform) }).GetComponent<RectTransform>();
		((Transform)component3).SetParent((Transform)(object)component, false);
		component3.anchorMin = new Vector2(0f, 0f);
		component3.anchorMax = new Vector2(1f, 1f);
		component3.offsetMin = new Vector2(38f, 2f);
		component3.offsetMax = new Vector2(-4f, -2f);
		GameObject val6 = new GameObject("Name", new Type[1] { typeof(RectTransform) });
		RectTransform component4 = val6.GetComponent<RectTransform>();
		((Transform)component4).SetParent((Transform)(object)component3, false);
		component4.anchorMin = new Vector2(0f, 0.42f);
		component4.anchorMax = new Vector2(1f, 1f);
		component4.sizeDelta = Vector2.zero;
		component4.anchoredPosition = Vector2.zero;
		TextMeshProUGUI val7 = val6.AddComponent<TextMeshProUGUI>();
		((TMP_Text)val7).fontSize = 11.5f;
		((TMP_Text)val7).alignment = (TextAlignmentOptions)513;
		((Graphic)val7).color = Color.white;
		((Graphic)val7).raycastTarget = false;
		GameObject val8 = new GameObject("Cond", new Type[1] { typeof(RectTransform) });
		RectTransform component5 = val8.GetComponent<RectTransform>();
		((Transform)component5).SetParent((Transform)(object)component3, false);
		component5.anchorMin = new Vector2(0f, 0f);
		component5.anchorMax = new Vector2(1f, 0.45f);
		component5.sizeDelta = Vector2.zero;
		component5.anchoredPosition = Vector2.zero;
		TextMeshProUGUI val9 = val8.AddComponent<TextMeshProUGUI>();
		((TMP_Text)val9).fontSize = 9.5f;
		((TMP_Text)val9).alignment = (TextAlignmentOptions)513;
		((Graphic)val9).color = new Color(0.4f, 0.85f, 0.5f, 0.9f);
		((Graphic)val9).raycastTarget = false;
		CU_DoctorItemSlot cU_DoctorItemSlot = val.AddComponent<CU_DoctorItemSlot>();
		cU_DoctorItemSlot.assistant = this;
		cU_DoctorItemSlot.slotBg = val2;
		cU_DoctorItemSlot.slotOutline = val3;
		cU_DoctorItemSlot.iconImg = val5;
		cU_DoctorItemSlot.nameTxt = val7;
		cU_DoctorItemSlot.condTxt = val9;
		return cU_DoctorItemSlot;
	}

	private void BindSlot(CU_DoctorItemSlot slot, Item item, Limb targetLimb, TMP_FontAsset font)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		slot.item = item;
		if ((Object)(object)font != (Object)null)
		{
			if ((Object)(object)slot.nameTxt != (Object)null)
			{
				((TMP_Text)slot.nameTxt).font = font;
			}
			if ((Object)(object)slot.condTxt != (Object)null)
			{
				((TMP_Text)slot.condTxt).font = font;
			}
		}
		if ((Object)(object)slot.iconImg != (Object)null)
		{
			SpriteRenderer component = ((Component)item).GetComponent<SpriteRenderer>();
			if ((Object)(object)component != (Object)null && (Object)(object)component.sprite != (Object)null)
			{
				slot.iconImg.sprite = component.sprite;
				((Graphic)slot.iconImg).color = Color.white;
				((Behaviour)slot.iconImg).enabled = true;
			}
			else
			{
				((Behaviour)slot.iconImg).enabled = false;
			}
		}
		if ((Object)(object)slot.nameTxt != (Object)null)
		{
			((TMP_Text)slot.nameTxt).text = item.fullName;
		}
		bool flag = (Object)(object)targetLimb != (Object)null && item.Stats != null && item.Stats.ActuallyUsableOnLimb(item);
		bool flag2 = item.Stats != null && (item.Stats.usableOnLimb || item.Stats.usable || (Object)(object)((Component)item).GetComponent<WaterContainerItem>() != (Object)null);
		if (selectedSlot == slot)
		{
			((Shadow)slot.slotOutline).effectColor = new Color(0.35f, 0.85f, 1f, 1f);
			((Graphic)slot.slotBg).color = new Color(0.12f, 0.22f, 0.32f, 0.95f);
			if ((Object)(object)slot.nameTxt != (Object)null)
			{
				((Graphic)slot.nameTxt).color = Color.white;
			}
		}
		else if (flag)
		{
			((Shadow)slot.slotOutline).effectColor = new Color(0.2f, 1f, 0.55f, 0.95f);
			((Graphic)slot.slotBg).color = new Color(0.1f, 0.22f, 0.18f, 0.95f);
			if ((Object)(object)slot.nameTxt != (Object)null)
			{
				((Graphic)slot.nameTxt).color = new Color(0.7f, 1f, 0.8f, 1f);
			}
		}
		else if (flag2)
		{
			((Shadow)slot.slotOutline).effectColor = new Color(0.25f, 0.65f, 0.95f, 0.7f);
			((Graphic)slot.slotBg).color = new Color(0.08f, 0.14f, 0.22f, 0.9f);
			if ((Object)(object)slot.nameTxt != (Object)null)
			{
				((Graphic)slot.nameTxt).color = Color.white;
			}
		}
		else
		{
			((Shadow)slot.slotOutline).effectColor = new Color(0.2f, 0.25f, 0.3f, 0.4f);
			((Graphic)slot.slotBg).color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
			if ((Object)(object)slot.nameTxt != (Object)null)
			{
				((Graphic)slot.nameTxt).color = new Color(0.65f, 0.7f, 0.75f, 0.75f);
			}
		}
		if ((Object)(object)slot.condTxt != (Object)null)
		{
			int num = Mathf.Clamp(Mathf.RoundToInt(item.condition * 100f), 0, 100);
			((TMP_Text)slot.condTxt).text = Loc.T("耐久") + ": " + num + "%" + (flag ? Loc.T(" [可施治]") : "");
			((Graphic)slot.condTxt).color = ((num > 50) ? new Color(0.35f, 0.95f, 0.55f, 0.9f) : ((num > 20) ? new Color(1f, 0.85f, 0.35f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.9f)));
		}
	}

	private void OnClickSwitch()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		DualMode instance = DualMode.Instance;
		if ((Object)(object)instance == (Object)null || !((Behaviour)instance).enabled)
		{
			return;
		}
		PlayerCamera main = PlayerCamera.main;
		Body val = (((Object)(object)main != (Object)null) ? main.body : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Body val2 = ((val == instance.Player) ? instance.Partner : instance.Player);
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		if (woundView.body == val2)
		{
			MedicalPanelDecoupler.SetInspectTarget(val);
			if (main != null)
			{
				main.DoAlert(Loc.T("已切回自身医疗面板 [") + ((Object)val).name + "]", false);
			}
			return;
		}
		float num = Vector3.Distance(((Component)val).transform.position, ((Component)val2).transform.position);
		if (num > 3.5f)
		{
			if (main != null)
			{
				main.DoAlert(Loc.T("距离过远") + " (" + num.ToString("F1") + "m) " + Loc.T("无法协助！需在 ") + 3.5f.ToString("F1") + Loc.T(" 米内。"), true);
			}
			return;
		}
		MedicalPanelDecoupler.SetInspectTarget(val2);
		if (main != null)
		{
			main.DoAlert(Loc.T("正在对同伴 [") + ((Object)val2).name + Loc.T("] 进行医疗协助..."), false);
		}
	}
}
