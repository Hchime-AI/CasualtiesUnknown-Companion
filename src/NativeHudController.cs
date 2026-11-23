using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CUCompanion;

public class NativeHudController : MonoBehaviour
{
	private class DragHandle : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
	{
		private RectTransform target;

		private RectTransform area;

		private Canvas canvas;

		private Vector2 grab;

		public void Init(RectTransform t, RectTransform a, Canvas c)
		{
			target = t;
			area = a;
			canvas = c;
		}

		private bool ToTopLeft(Vector2 screen, out Vector2 tl)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			tl = Vector2.zero;
			if ((Object)(object)area == (Object)null)
			{
				return false;
			}
			Camera val = (((Object)(object)canvas != (Object)null && (int)canvas.renderMode != 0) ? canvas.worldCamera : null);
			Vector2 val2 = default(Vector2);
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, val, out val2))
			{
				return false;
			}
			float x = val2.x;
			Rect rect = area.rect;
			float num = x + ((Rect)rect).width * 0.5f;
			float y = val2.y;
			rect = area.rect;
			tl = new Vector2(num, y - ((Rect)rect).height * 0.5f);
			return true;
		}

		public void OnPointerDown(PointerEventData e)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)target == (Object)null) && ToTopLeft(e.position, out var tl))
			{
				grab = target.anchoredPosition - tl;
			}
		}

		public void OnDrag(PointerEventData e)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)target == (Object)null) && ToTopLeft(e.position, out var tl))
			{
				Vector2 val = tl + grab;
				Rect rect = area.rect;
				float num = Mathf.Max(0f, ((Rect)rect).width - target.sizeDelta.x * 0.35f);
				rect = area.rect;
				float num2 = 0f - Mathf.Max(0f, ((Rect)rect).height - 120f);
				val.x = Mathf.Clamp(val.x, 0f, num);
				val.y = Mathf.Clamp(val.y, num2, 0f);
				target.anchoredPosition = val;
			}
		}
	}

	public static NativeHudController Instance;

	private Canvas canvas;

	private CanvasScaler hudScaler;

	private RectTransform canvasRt;

	private GameObject historyGo;

	private GameObject tabGo;

	private GameObject inputGo;

	private GameObject apiGo;

	private TextMeshProUGUI tabLabel;

	private RectTransform histRt;

	private Canvas histCanvas;

	private CanvasGroup histCanvasGroup;

	private TextMeshProUGUI histText;

	private TextMeshProUGUI reactText;

	private TextMeshProUGUI apiResult;

	private TextMeshProUGUI busyText;

	private TextMeshProUGUI histTail;

	private string prefixCache = "";

	private Line prefixFirstLine;

	private int prefixDone = -1;

	private bool emptyShown;

	private GameObject typingGo;

	private AudioSource typingSource;

	private AudioClip typingClip;

	private string typingClipName;

	private static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

	private RectTransform histView;

	private RectTransform histContent;

	private float histOffset;

	private float histMax;

	private bool histFollow = true;

	private const float HistW = 864f;

	private const float HistH = 470f;

	private NativeField urlField;

	private NativeField keyField;

	private NativeField modelField;

	private NativeField tokenField;

	private TextMeshProUGUI streamBarText;

	private TextMeshProUGUI thinkBarText;

	private float nextBarRefresh;

	private GameObject thinkBar;

	private GameObject streamBar;

	private NativeField inputField;

	private Vector2 histPos = new Vector2(40f, -200f);

	private Image histBg;

	private readonly List<Image> histFrame = new List<Image>();

	private float appliedTop = float.NaN;

	private float appliedRight = float.NaN;

	private float appliedAlpha = float.NaN;

	private float appliedTabBottom = float.NaN;

	private float nextRefresh;

	private bool histWanted = true;

	private Vector2 lastGood;

	private float lastBeepAt;

	private static TMP_FontAsset cachedFont;

	private string inputText = "";

	private float lastBarH = 64f;

	public bool HistoryOpen
	{
		get
		{
			if ((Object)(object)historyGo != (Object)null)
			{
				return historyGo.activeSelf;
			}
			return false;
		}
	}

	public RectTransform HistoryRect => histRt;

	public static Transform CanvasTransform
	{
		get
		{
			if (!((Object)(object)Instance != (Object)null) || !((Object)(object)Instance.canvas != (Object)null))
			{
				return null;
			}
			return ((Component)Instance.canvas).transform;
		}
	}

	public static TMP_FontAsset GameFont
	{
		get
		{
			if ((Object)(object)cachedFont != (Object)null)
			{
				return cachedFont;
			}
			TMP_FontAsset val = null;
			TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (TMP_FontAsset val2 in array)
			{
				if (!((Object)(object)val2 == (Object)null))
				{
					if ((Object)(object)val == (Object)null)
					{
						val = val2;
					}
					if (((Object)val2).name != null && ((Object)val2).name.IndexOf("Retro", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						cachedFont = val2;
						return val2;
					}
					if ((Object)(object)val == (Object)null || HasCjk(val2))
					{
						val = val2;
					}
				}
			}
			cachedFont = val;
			return val;
		}
	}

	private static float TypingCps
	{
		get
		{
			float num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.typingCharsPerSec : 0f);
			if (num <= 0f)
			{
				num = 22f;
			}
			return Mathf.Clamp(num, 2f, 200f);
		}
	}

	private static float TypingGap
	{
		get
		{
			float num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.typingLineGap : (-1f));
			if (num < 0f)
			{
				num = 0.45f;
			}
			return Mathf.Clamp(num, 0f, 5f);
		}
	}

	private static float TypingGrace
	{
		get
		{
			float num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.typingSoundGrace : (-1f));
			if (num < 0f)
			{
				num = 0.05f;
			}
			return Mathf.Clamp(num, 0f, 2f);
		}
	}

	private static FieldInfo loadingField;

	private static float nextLoadingCheck;

	private static int loadingOrder = -1;

	/// <summary>
	/// 游戏自己的加载遮罩（World/Canvas/LoadingImage）正在显示时，返回一个比它更高的
	/// 排序值，让对话历史画在加载动画之上；不在加载中返回 -1。
	/// </summary>
	private static int LoadingOverlayOrder()
	{
		if (Time.unscaledTime < nextLoadingCheck)
		{
			return loadingOrder;
		}
		nextLoadingCheck = Time.unscaledTime + 0.2f;
		loadingOrder = -1;
		try
		{
			WorldGeneration worldGeneration = WorldGeneration.world;
			if ((Object)(object)worldGeneration == (Object)null)
			{
				return loadingOrder;
			}
			if (loadingField == null)
			{
				loadingField = typeof(WorldGeneration).GetField("loadingObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			GameObject gameObject = ((loadingField == null) ? null : (loadingField.GetValue(worldGeneration) as GameObject));
			if ((Object)(object)gameObject == (Object)null || !gameObject.activeInHierarchy)
			{
				return loadingOrder;
			}
			Canvas canvas = gameObject.GetComponentInParent<Canvas>();
			int num = (((Object)(object)canvas != (Object)null) ? canvas.sortingOrder : 0);
			loadingOrder = Mathf.Max(2500, num + 1);
		}
		catch
		{
			loadingOrder = -1;
		}
		return loadingOrder;
	}

	/// <summary>
	/// 角色无意识黑屏保护：仅当角色真正陷入昏迷/濒死黑屏时，
	/// 动态让历史对话框仅比黑屏遮罩高 1 级，确保对话记录可见；
	/// 正常游玩时返回 -1 绝对不影响任何其他游戏内 UI。
	/// </summary>
	private static int UnconsciousOverlayOrder()
	{
		try
		{
			PlayerCamera pc = PlayerCamera.main;
			if ((Object)(object)pc == (Object)null || (Object)(object)pc.body == (Object)null)
			{
				return -1;
			}

			// 严格判定：必须是角色真正失去意识 (无意识/濒死)，且黑屏透明度真正遮盖屏幕时才生效
			bool isUnconscious = !pc.body.conscious || pc.body.consciousness < 20f || pc.didDeathScreen;
			if (!isUnconscious || pc.blackAmount < 0.6f)
			{
				return -1;
			}

			// 动态探测黑屏遮罩所在的实际 Canvas 排序层级
			int fadeOrder = 0;
			if ((Object)(object)pc.consciousnessFade != null)
			{
				Canvas fadeCanvas = pc.consciousnessFade.GetComponentInParent<Canvas>();
				if ((Object)(object)fadeCanvas != null)
				{
					fadeOrder = fadeCanvas.sortingOrder;
				}
			}
			else if ((Object)(object)pc.mainCanvas != null)
			{
				fadeOrder = pc.mainCanvas.sortingOrder;
			}

			// 仅比黑屏高 1 级（fadeOrder + 1，通常为 0 + 1 = 1）
			return fadeOrder + 1;
		}
		catch
		{
			return -1;
		}
	}

	public static bool IsHighPriorityMenuOpen()
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null)
		{
			if ((Object)(object)main.woundView != (Object)null && main.woundView.activeSelf)
			{
				return true;
			}
			if (main.radialOpen)
			{
				return true;
			}
			if ((Object)(object)main.craftingPanel != (Object)null && main.craftingPanel.activeSelf)
			{
				return true;
			}
			if ((Object)(object)main.traderInventory != (Object)null && ((Component)main.traderInventory).gameObject.activeSelf)
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasCjk(TMP_FontAsset f)
	{
		try
		{
			for (int i = 0; i < f.characterTable.Count; i++)
			{
				if (((TMP_TextElement)f.characterTable[i]).unicode == 20320)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void Awake()
	{
		Instance = this;
		Build();
	}

	private void OnDestroy()
	{
		if ((Object)(object)histRt != (Object)null)
		{
			ModUIInputBlocker.Unregister(histRt);
		}
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
	}

	private void Build()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("CUCompanionHUD", new Type[4]
		{
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster)
		});
		val.transform.SetParent(((Component)this).transform, false);
		canvas = val.GetComponent<Canvas>();
		canvas.renderMode = (RenderMode)0;
		canvas.sortingOrder = 3000;
		CanvasScaler component = val.GetComponent<CanvasScaler>();
		component.uiScaleMode = (CanvasScaler.ScaleMode)1;
		component.referenceResolution = new Vector2(2560f, 1440f);
		component.screenMatchMode = (CanvasScaler.ScreenMatchMode)0;
		component.matchWidthOrHeight = 0.5f;
		hudScaler = component;
		canvasRt = val.GetComponent<RectTransform>();
		if ((Object)(object)EventSystem.current == (Object)null)
		{
			GameObject eventSystemGo = new GameObject("CUCompanionEventSystem", new Type[2]
			{
				typeof(EventSystem),
				typeof(StandaloneInputModule)
			});
			eventSystemGo.transform.SetParent(((Component)this).transform, false);
		}
		BuildHistory();
		BuildTab();
		BuildInput();
		BuildApi();
		SetHistory(open: true);
		RefreshHistory();
	}

	private void BuildHistory()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_054a: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		//IL_063e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		//IL_0672: Unknown result type (might be due to invalid IL or missing references)
		historyGo = Node("History", (Transform)(object)canvasRt, 0f, 0f, 900f, 560f);
		histRt = historyGo.GetComponent<RectTransform>();
		histRt.anchorMin = new Vector2(0f, 1f);
		histRt.anchorMax = new Vector2(0f, 1f);
		histRt.pivot = new Vector2(0f, 1f);
		histRt.anchoredPosition = histPos;
		histCanvas = historyGo.AddComponent<Canvas>();
		histCanvas.overrideSorting = true;
		histCanvas.sortingOrder = 2500;
		// 嵌套 Canvas 必须自带 GraphicRaycaster，否则根画布射线扫不到这里的按钮，
		// 表现就是「互动 ▼」「标签页」全都点不动。
		historyGo.AddComponent<GraphicRaycaster>();
		histCanvasGroup = historyGo.AddComponent<CanvasGroup>();
		histCanvasGroup.blocksRaycasts = true;
		ModUIInputBlocker.Register(histRt);
		Panel(historyGo, 900f, 560f, 0.93f);
		histBg = historyGo.GetComponent<Image>();
		histFrame.Clear();
		for (int i = 0; i < historyGo.transform.childCount; i++)
		{
			Transform child = historyGo.transform.GetChild(i);
			if (((Object)child).name == "t" || ((Object)child).name == "b" || ((Object)child).name == "l" || ((Object)child).name == "r")
			{
				Image component = ((Component)child).GetComponent<Image>();
				if ((Object)(object)component != (Object)null)
				{
					histFrame.Add(component);
				}
			}
		}
		GameObject obj = Node("head", historyGo.transform, 0f, 0f, 900f, 56f);
		Image obj2 = obj.AddComponent<Image>();
		((Graphic)obj2).color = new Color(1f, 1f, 1f, 0.004f);
		((Graphic)obj2).raycastTarget = true;
		obj.AddComponent<DragHandle>().Init(histRt, canvasRt, canvas);
		Bar(historyGo, "API", 12f, -10f, 96f, 40f, delegate
		{
			ToggleApi();
		});
		Bar(historyGo, Lang.T("hudInput"), 116f, -10f, 96f, 40f, delegate
		{
			ToggleInput();
		});
		Bar(historyGo, Lang.T("hudPersona"), 220f, -10f, 96f, 40f, delegate
		{
			NativePanel.OpenAnywhere();
		});
		Bar(historyGo, Lang.T("hudMonitor"), 324f, -10f, 96f, 40f, delegate
		{
			HealthMonitorGui.Toggle();
		});
		reactText = Label(historyGo, "", 26f, new Vector2(436f, -16f), 300f, 32f, (TextAlignmentOptions)513, new Color(1f, 1f, 1f, 0.55f));
		Bar(historyGo, "—", 780f, -10f, 46f, 40f, delegate
		{
			SetHistory(open: false);
		});
		Bar(historyGo, "×", 838f, -10f, 46f, 40f, delegate
		{
			SetHistory(open: false);
		});
		GameObject val = Node("HistView", historyGo.transform, 18f, -66f, 864f, 470f);
		histView = val.GetComponent<RectTransform>();
		val.AddComponent<RectMask2D>();
		GameObject val2 = Node("HistContent", (Transform)(object)histView, 0f, 0f, 864f, 470f);
		histContent = val2.GetComponent<RectTransform>();
		histContent.anchorMin = new Vector2(0f, 1f);
		histContent.anchorMax = new Vector2(0f, 1f);
		histContent.pivot = new Vector2(0f, 1f);
		histContent.anchoredPosition = Vector2.zero;
		histText = Label(val2, "", 24f, new Vector2(0f, 0f), 864f, 470f, (TextAlignmentOptions)257, Color.white);
		((TMP_Text)histText).enableWordWrapping = true;
		((TMP_Text)histText).rectTransform.anchorMin = new Vector2(0f, 1f);
		((TMP_Text)histText).rectTransform.anchorMax = new Vector2(1f, 1f);
		((TMP_Text)histText).rectTransform.pivot = new Vector2(0f, 1f);
		if ((Object)(object)IntimateController.Instance != (Object)null)
		{
			IntimateController.Instance.Attach(histRt);
		}
		((TMP_Text)histText).rectTransform.anchoredPosition = Vector2.zero;
		((TMP_Text)histText).rectTransform.sizeDelta = new Vector2(-28f, 0f);
		histTail = Label(val2, "", 24f, new Vector2(0f, 0f), 864f, 470f, (TextAlignmentOptions)257, Color.white);
		((TMP_Text)histTail).enableWordWrapping = true;
		((TMP_Text)histTail).rectTransform.anchorMin = new Vector2(0f, 1f);
		((TMP_Text)histTail).rectTransform.anchorMax = new Vector2(1f, 1f);
		((TMP_Text)histTail).rectTransform.pivot = new Vector2(0f, 1f);
		((TMP_Text)histTail).rectTransform.anchoredPosition = Vector2.zero;
		((TMP_Text)histTail).rectTransform.sizeDelta = new Vector2(-28f, 0f);
		((Component)histTail).gameObject.SetActive(false);
	}

	private void BuildTab()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		tabGo = Node("Tab", (Transform)(object)canvasRt, 0f, 0f, 260f, 52f);
		RectTransform component = tabGo.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(0f, 0f);
		component.pivot = new Vector2(0f, 0f);
		float num = ((CompanionPlugin.Cfg != null) ? Mathf.Clamp(CompanionPlugin.Cfg.hudTabBottom, 0f, 2000f) : 380f);
		component.anchoredPosition = new Vector2(24f, num);
		appliedTabBottom = num;
		Panel(tabGo, 260f, 52f, 0.9f);
		tabLabel = Label(tabGo, Lang.T("hudTab") + " ▲", 26f, new Vector2(16f, -12f), 220f, 32f, (TextAlignmentOptions)513, Color.white);
		tabGo.AddComponent<NativeHit>().Init(delegate
		{
			SetHistory(!histWanted);
		});
	}

	private void BuildInput()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		inputGo = Node("Input", (Transform)(object)canvasRt, 0f, 0f, 640f, 64f);
		RectTransform component = inputGo.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(0f, 0f);
		component.pivot = new Vector2(0.5f, 0f);
		Panel(inputGo, 640f, 64f, 0.92f);
		GameObject val = Node("field", inputGo.transform, 18f, -12f, 496f, 40f);
		((Graphic)val.AddComponent<Image>()).color = new Color(0f, 0f, 0f, 0.35f);
		Box(val.transform, 0f, 0f, 496f, 1.2f, new Color(1f, 1f, 1f, 0.6f), "t");
		Box(val.transform, 0f, -40f, 496f, 1.2f, new Color(1f, 1f, 1f, 0.6f), "b");
		inputField = val.AddComponent<NativeField>();
		inputField.Init(GameFont, () => inputText, delegate(string v)
		{
			inputText = v;
		}, multi: false);
		inputField.autoGrow = true;
		Bar(inputGo, Loc.T("说出口"), 526f, -12f, 96f, 40f, delegate
		{
			SubmitInput();
		});
		inputGo.SetActive(false);
	}

	private void SyncInputSize()
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)inputField == (Object)null || (Object)(object)inputGo == (Object)null || !inputGo.activeSelf)
		{
			return;
		}
		float num = Mathf.Clamp(inputField.GrowHeight, 40f, 150f);
		if (!(Mathf.Abs(num - lastBarH) < 0.5f))
		{
			lastBarH = num;
			float num2 = num + 24f;
			RectTransform component = inputGo.GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				component.sizeDelta = new Vector2(640f, num2);
			}
			Transform transform = ((Component)inputField).transform;
			RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
			if ((Object)(object)val != (Object)null)
			{
				val.sizeDelta = new Vector2(496f, num);
			}
			SetChildRect(inputGo.transform, "b", 0f, 0f - num2, 640f, 1.6f);
			SetChildRect(inputGo.transform, "l", 0f, 0f, 1.6f, num2);
			SetChildRect(inputGo.transform, "r", 638.4f, 0f, 1.6f, num2);
			SetChildRect(((Component)inputField).transform, "b", 0f, 0f - num, 496f, 1.2f);
		}
	}

	private static void SetChildRect(Transform parent, string childName, float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)parent == (Object)null)
		{
			return;
		}
		Transform val = parent.Find(childName);
		if (!((Object)(object)val == (Object)null))
		{
			RectTransform val2 = (RectTransform)(object)((val is RectTransform) ? val : null);
			if (!((Object)(object)val2 == (Object)null))
			{
				val2.sizeDelta = new Vector2(w, h);
				val2.anchoredPosition = new Vector2(x, y);
			}
		}
	}

	private void SubmitInput()
	{
		if ((Object)(object)inputField != (Object)null)
		{
			inputField.Apply();
		}
		if (!string.IsNullOrWhiteSpace(inputText))
		{
			CompanionHost instance = CompanionHost.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.SubmitPlayerInput(inputText);
			}
			inputText = "";
			if ((Object)(object)inputField != (Object)null)
			{
				inputField.SetText("");
			}
		}
	}

	private void BuildApi()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		apiGo = Node("Api", (Transform)(object)canvasRt, 0f, 0f, 900f, 640f);
		RectTransform component = apiGo.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = Vector2.zero;
		Panel(apiGo, 900f, 640f, 0.96f);
		Label(apiGo, Loc.T("API 设置"), 32f, new Vector2(24f, -18f), 500f, 40f, (TextAlignmentOptions)513, Color.white);
		Bar(apiGo, Loc.T("关闭"), 760f, -14f, 112f, 44f, delegate
		{
			ToggleApi();
		});
		Bar(apiGo, "OpenAI", 24f, -64f, 140f, 42f, delegate
		{
			SetProvider("openai");
		});
		Bar(apiGo, "Anthropic", 172f, -64f, 160f, 42f, delegate
		{
			SetProvider("anthropic");
		});
		urlField = Field(apiGo, Loc.T("接口地址 baseUrl"), 24f, -118f, 852f, 46f, () => CompanionPlugin.Cfg.api.baseUrl, delegate(string v)
		{
			CompanionPlugin.Cfg.api.baseUrl = v;
		});
		keyField = Field(apiGo, "API Key", 24f, -196f, 852f, 46f, () => CompanionPlugin.Cfg.api.apiKey, delegate(string v)
		{
			CompanionPlugin.Cfg.api.apiKey = v;
		});
		modelField = Field(apiGo, Loc.T("模型 model"), 24f, -274f, 852f, 46f, () => CompanionPlugin.Cfg.api.model, delegate(string v)
		{
			CompanionPlugin.Cfg.api.model = v;
		});
		tokenField = Field(apiGo, Loc.T("单次发言 token 上限（默认 ") + 300 + Loc.P("）"), 24f, -350f, 852f, 46f, () => CompanionPlugin.Cfg.api.maxTokens.ToString(), delegate(string v)
		{
			if (int.TryParse(v, out var result))
			{
				CompanionPlugin.Cfg.api.maxTokens = Mathf.Clamp(result, 60, 4000);
			}
		});
		Bar(apiGo, Loc.T("测试连接"), 24f, -410f, 180f, 46f, delegate
		{
			TestApi();
		});
		Bar(apiGo, Loc.T("保存"), 216f, -410f, 120f, 46f, delegate
		{
			SaveApi();
		});
		thinkBar = Bar(apiGo, Loc.T("思考中提示"), 348f, -410f, 260f, 46f, delegate
		{
			CompanionPlugin.Cfg.api.showThinking = !CompanionPlugin.Cfg.api.showThinking;
			CompanionPlugin.SaveAll();
		});
		streamBar = Bar(apiGo, Loc.T("直播模式"), 616f, -410f, 240f, 46f, delegate
		{
			CompanionPlugin.Cfg.streamMode = !CompanionPlugin.Cfg.streamMode;
			CompanionPlugin.SaveAll();
			CompanionPlugin.Log.LogInfo((object)("[ui] 直播模式：" + (CompanionPlugin.Cfg.streamMode ? "开" : "关")));
		});
		apiResult = Label(apiGo, "", 24f, new Vector2(24f, -470f), 852f, 150f, (TextAlignmentOptions)257, new Color(1f, 1f, 1f, 0.75f));
		((TMP_Text)apiResult).enableWordWrapping = true;
		apiGo.SetActive(false);
	}

	private void SetProvider(string p)
	{
		CompanionPlugin.Cfg.api.provider = p;
		if (string.IsNullOrEmpty(CompanionPlugin.Cfg.api.baseUrl) || CompanionPlugin.Cfg.api.baseUrl.Contains("openai.com") || CompanionPlugin.Cfg.api.baseUrl.Contains("anthropic.com"))
		{
			CompanionPlugin.Cfg.api.baseUrl = ((p == "anthropic") ? "https://api.anthropic.com/v1/messages" : "https://api.openai.com/v1/chat/completions");
			if ((Object)(object)urlField != (Object)null)
			{
				urlField.SetText(CompanionPlugin.Cfg.api.baseUrl);
			}
		}
	}

	private void SaveApi()
	{
		if ((Object)(object)urlField != (Object)null)
		{
			urlField.Apply();
		}
		if ((Object)(object)keyField != (Object)null)
		{
			keyField.Apply();
		}
		if ((Object)(object)modelField != (Object)null)
		{
			modelField.Apply();
		}
		if ((Object)(object)tokenField != (Object)null)
		{
			tokenField.Apply();
		}
		CompanionPlugin.SaveAll();
		if ((Object)(object)apiResult != (Object)null)
		{
			((TMP_Text)apiResult).text = Loc.T("已保存到 settings.json");
		}
	}

	private async void TestApi()
	{
		if ((Object)(object)urlField != (Object)null)
		{
			urlField.Apply();
		}
		if ((Object)(object)keyField != (Object)null)
		{
			keyField.Apply();
		}
		if ((Object)(object)modelField != (Object)null)
		{
			modelField.Apply();
		}
		if ((Object)(object)tokenField != (Object)null)
		{
			tokenField.Apply();
		}
		if ((Object)(object)apiResult != (Object)null)
		{
			((TMP_Text)apiResult).text = Loc.T("测试中…");
		}
		try
		{
			List<ChatMsg> list = new List<ChatMsg>();
			list.Add(new ChatMsg("user", Loc.T("只回两个字：在。")));
			string text = await Llm.ChatAsync(CompanionPlugin.Cfg.api, list, CancellationToken.None);
			if ((Object)(object)apiResult != (Object)null)
			{
				((TMP_Text)apiResult).text = (string.IsNullOrEmpty(text) ? Loc.T("没有返回内容（检查 key / 地址 / 模型）") : (Loc.T("返回：") + text));
			}
		}
		catch (Exception ex)
		{
			if ((Object)(object)apiResult != (Object)null)
			{
				((TMP_Text)apiResult).text = Loc.T("失败：") + ex.Message;
			}
		}
	}

	private void Update()
	{
		UiScale.ApplyHud(hudScaler);
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		MenuBlocker.Sync();
		bool flag = IsHighPriorityMenuOpen();
		if ((Object)(object)histCanvas != (Object)null)
		{
			// 默认层级就是最低（-5），完全处于所有游戏原生UI层级之下，绝不遮挡任何游戏UI
			int num = -5;

			// 1. 无意识黑屏保护：仅当黑屏真正遮蔽视野时，才将历史框刚好拉到黑屏层级之上（fadeOrder + 1）
			int numUncon = UnconsciousOverlayOrder();
			if (numUncon >= 0 && numUncon > num)
			{
				num = numUncon;
			}

			// 2. 进图 / 换区域加载动画：将历史框拉到加载遮罩之上
			int numLoad = LoadingOverlayOrder();
			if (numLoad >= 0 && numLoad > num)
			{
				num = numLoad;
			}

			if (histCanvas.sortingOrder != num)
			{
				histCanvas.sortingOrder = num;
			}
		}
		if ((Object)(object)histCanvasGroup != (Object)null)
		{
			// 以前这里整块关掉射线（blocksRaycasts = !flag）：医疗面板 / 轮盘 / 工作台
			// 只要有一个被判成「打开」，整个 HUD 就连「互动 ▼」都点不动。
			// 现在 HUD 始终可点，菜单打开时只让底板 Image 透传鼠标，
			// 游戏自己的面板照样能操作，mod 的按钮也不会跟着一起失灵。
			if (!histCanvasGroup.blocksRaycasts)
			{
				histCanvasGroup.blocksRaycasts = true;
			}
			if (!histCanvasGroup.interactable)
			{
				histCanvasGroup.interactable = true;
			}
		}
		if ((Object)(object)histBg != (Object)null && histBg.raycastTarget == flag)
		{
			histBg.raycastTarget = !flag;
		}
		if ((Object)(object)IntimateController.Instance != (Object)null && (Object)(object)histRt != (Object)null)
		{
			IntimateController.Instance.Attach(histRt);
			IntimateController.Instance.SetVisible(HistoryOpen);
		}
		SyncInputSize();
		if (Time.unscaledTime >= nextBarRefresh)
		{
			nextBarRefresh = Time.unscaledTime + 0.25f;
			if ((Object)(object)streamBar != (Object)null)
			{
				if ((Object)(object)streamBarText == (Object)null)
				{
					streamBarText = FindChildText(streamBar, "text");
				}
				if ((Object)(object)streamBarText != (Object)null)
				{
					((TMP_Text)streamBarText).text = Loc.T("直播模式：") + (CompanionPlugin.Cfg.streamMode ? Loc.T("开") : Loc.T("关"));
				}
			}
			if ((Object)(object)thinkBar != (Object)null)
			{
				if ((Object)(object)thinkBarText == (Object)null)
				{
					thinkBarText = FindChildText(thinkBar, "text");
				}
				if ((Object)(object)thinkBarText != (Object)null)
				{
					((TMP_Text)thinkBarText).text = Loc.T("思考中提示：") + (CompanionPlugin.Cfg.api.showThinking ? Loc.T("开") : Loc.T("关"));
				}
			}
		}
		if (!InputGuard.HardLock && InputGuard.Capturing && ((Object)(object)inputField == (Object)null || !inputField.IsFocused))
		{
			InputGuard.Set(on: false);
		}
		if ((Object)(object)inputGo != (Object)null && inputGo.activeSelf && InputGuard.ConsumeEscape())
		{
			SetInputOpen(open: false);
		}
		if (Input.GetKeyDown((KeyCode)287) && CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug)
		{
			PreRunScript instance = PreRunScript.instance;
			if ((Object)(object)instance != (Object)null && (Object)(object)instance.loadButton != (Object)null)
			{
				CompanionPlugin.Log.LogInfo((object)"[ui] F6 直接开始一局（调试）");
				instance.StartRun();
			}
		}
		if ((Object)(object)historyGo != (Object)null && historyGo.activeSelf && (Object)(object)histView != (Object)null && (Object)(object)histContent != (Object)null && histMax > 0f)
		{
			Vector2 val = (Vector2)(Input.mousePosition);
			Vector2 val2 = default(Vector2);
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(histView, val, (Camera)null, out val2))
			{
				Rect rect = histView.rect;
				if (((Rect)rect).Contains(val2))
				{
					float y = Input.mouseScrollDelta.y;
					if (Mathf.Abs(y) > 0.01f)
					{
						histOffset = Mathf.Clamp(histOffset - y * 70f, 0f, histMax);
						histFollow = histOffset >= histMax - 1f;
						histContent.anchoredPosition = new Vector2(0f, histOffset);
					}
				}
			}
		}
		if (Input.GetKeyDown((KeyCode)289))
		{
			SetHistory(!HistoryOpen);
		}
		if (Input.GetKeyDown((KeyCode)293) && CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug)
		{
			SetInputOpen(open: true);
			if ((Object)(object)inputField != (Object)null)
			{
				inputField.SetText(Loc.T("测试光标位置 abcdefghij 12345"));
			}
		}
		if (Input.GetKeyDown((KeyCode)291))
		{
			ToggleApi();
		}
		if (Input.GetKeyDown((KeyCode)116) && (Object)(object)CompanionHost.Instance != (Object)null && CompanionHost.Instance.HasBody)
		{
			ToggleInput();
		}
		if ((Object)(object)inputGo != (Object)null && inputGo.activeSelf && (Input.GetKeyDown((KeyCode)13) || Input.GetKeyDown((KeyCode)271)))
		{
			SubmitInput();
		}
		CompanionHost instance2 = CompanionHost.Instance;
		bool flag3 = (Object)(object)instance2 != (Object)null && instance2.HasBody;
		if ((Object)(object)historyGo != (Object)null && historyGo.activeSelf != (histWanted && flag3))
		{
			historyGo.SetActive(histWanted && flag3);
		}
		if ((Object)(object)tabGo != (Object)null && tabGo.activeSelf != flag3)
		{
			tabGo.SetActive(flag3);
		}
		if ((Object)(object)inputGo != (Object)null && inputGo.activeSelf && !flag3)
		{
			inputGo.SetActive(false);
		}
		if ((Object)(object)apiGo != (Object)null && apiGo.activeSelf && !flag3)
		{
			apiGo.SetActive(false);
		}
		ApplyHudLayout();
		FollowFeet();
		TickTypingSound();
		if (Time.unscaledTime >= nextRefresh)
		{
			nextRefresh = Time.unscaledTime + 0.08f;
			RefreshHistory();
		}
	}

	private void ApplyHudLayout()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null || (Object)(object)canvasRt == (Object)null)
		{
			return;
		}
		float num = Mathf.Clamp(cfg.hudTabBottom, 0f, 2000f);
		if ((Object)(object)tabGo != (Object)null && !Mathf.Approximately(appliedTabBottom, num))
		{
			appliedTabBottom = num;
			tabGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(24f, num);
		}
		if ((Object)(object)histRt == (Object)null)
		{
			return;
		}
		float num2 = Mathf.Clamp(cfg.hudHistoryTop, 0f, 2000f);
		float num3 = cfg.hudHistoryRight;
		Rect rect = canvasRt.rect;
		float num4 = ((Rect)rect).width;
		if (num4 <= 1f)
		{
			num4 = Screen.width;
		}
		if (num3 <= 0f)
		{
			num3 = Mathf.Max(0f, num4 - 940f);
		}
		num3 = Mathf.Clamp(num3, 0f, 4000f);
		if (!Mathf.Approximately(appliedTop, num2) || !Mathf.Approximately(appliedRight, num3))
		{
			appliedTop = num2;
			appliedRight = num3;
			histPos = new Vector2(Mathf.Max(0f, num4 - 900f - num3), 0f - num2);
			histRt.anchoredPosition = histPos;
		}
		float num5 = Mathf.Clamp01(cfg.hudHistoryAlpha);
		if (Mathf.Approximately(appliedAlpha, num5))
		{
			return;
		}
		appliedAlpha = num5;
		if ((Object)(object)histBg != (Object)null)
		{
			((Graphic)histBg).color = new Color(0.015f, 0.017f, 0.022f, num5);
		}
		float num6 = Mathf.Clamp01(num5 * 0.92f);
		Color color = default(Color);
		color = new Color(1f, 1f, 1f, num6);
		for (int i = 0; i < histFrame.Count; i++)
		{
			if ((Object)(object)histFrame[i] != (Object)null)
			{
				((Graphic)histFrame[i]).color = color;
			}
		}
	}

	private void FollowFeet()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)inputGo == (Object)null || (Object)(object)instance == (Object)null || !instance.HasBody || (Object)(object)Camera.main == (Object)null || (Object)(object)canvasRt == (Object)null)
		{
			return;
		}
		Body currentBody = instance.CurrentBody;
		Transform val = null;
		if ((Object)(object)currentBody != (Object)null)
		{
			val = ((Component)currentBody).transform;
		}
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Vector3 val2 = Camera.main.WorldToScreenPoint(val.position + new Vector3(0f, -1.2f, 0f));
		Vector2 val3 = default(Vector2);
		if (!(val2.z < 0f) && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, (Vector2)(val2), (Camera)null, out val3))
		{
			RectTransform component = inputGo.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0.5f, 1f);
			float num = 320f;
			Rect rect = canvasRt.rect;
			float num2 = ((Rect)rect).height * 0.5f;
			rect = canvasRt.rect;
			float num3 = ((Rect)rect).width * 0.5f;
			float num4 = Mathf.Clamp(val3.x, 0f - num3 + num + 20f, num3 - num - 20f);
			float num5 = Mathf.Clamp(val3.y - 10f, 0f - num2 + 90f, num2 - 130f);
			Vector2 val4 = default(Vector2);
			val4 = new Vector2(num4, num5);
			Vector2 val5 = component.anchoredPosition - val4;
			if (((Vector2)val5).sqrMagnitude > 0.25f)
			{
				component.anchoredPosition = val4;
			}
			if (inputGo.activeSelf)
			{
				lastGood = val4;
			}
		}
	}

	private static TextMeshProUGUI FindChildText(GameObject go, string child)
	{
		if ((Object)(object)go == (Object)null)
		{
			return null;
		}
		try
		{
			Transform val = go.transform.Find(child);
			return ((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<TextMeshProUGUI>() : null;
		}
		catch
		{
			return null;
		}
	}

	private void RefreshHistory()
	{
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_058a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0470: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)histText == (Object)null || ((Object)(object)historyGo != (Object)null && !historyGo.activeSelf))
		{
			return;
		}
		if ((Object)(object)instance == (Object)null)
		{
			((TMP_Text)histText).text = Loc.T("（还没有陪伴对象）");
			if ((Object)(object)histTail != (Object)null)
			{
				((Component)histTail).gameObject.SetActive(false);
			}
			return;
		}
		List<Line> history = instance.history;
		if (history.Count == 0)
		{
			if (!emptyShown)
			{
				emptyShown = true;
				prefixDone = -1;
				prefixFirstLine = null;
				prefixCache = "";
				((TMP_Text)histText).text = Loc.T("<color=#7A8394>（还没有对话。按 T 或点【输入】说点什么。）</color>");
				((Component)histText).gameObject.SetActive(true);
			}
			if ((Object)(object)histTail != (Object)null && ((Component)histTail).gameObject.activeSelf)
			{
				((TMP_Text)histTail).text = "";
				((Component)histTail).gameObject.SetActive(false);
			}
			if ((Object)(object)histContent != (Object)null)
			{
				histContent.sizeDelta = new Vector2(864f, 470f);
				histMax = 0f;
				histOffset = 0f;
				histContent.anchoredPosition = Vector2.zero;
			}
			UpdateReactLine(instance);
			return;
		}
		emptyShown = false;
		float unscaledTime = Time.unscaledTime;
		float num = -1f;
		int num2 = 0;
		int num3 = -1;
		int length = 0;
		string text = null;
		for (int i = 0; i < history.Count; i++)
		{
			Line line = history[i];
			if (line == null)
			{
				continue;
			}
			string text2 = line.text ?? "";
			if (text2.Length == 0)
			{
				if (num3 < 0)
				{
					num2 = i + 1;
				}
				continue;
			}
			int num4;
			if (line.kind == 1 || line.instant)
			{
				num4 = text2.Length;
			}
			else
			{
				float num5 = Mathf.Max(line.time, num);
				num4 = Mathf.Clamp((int)((unscaledTime - num5) * TypingCps), 0, text2.Length);
				num = num5 + (float)text2.Length / TypingCps + TypingGap;
			}
			if (num4 >= text2.Length)
			{
				if (num3 < 0)
				{
					num2 = i + 1;
				}
				continue;
			}
			if (num4 > 0)
			{
				num3 = i;
				length = num4;
				text = text2;
			}
			break;
		}
		Line line2 = ((history.Count > 0) ? history[0] : null);
		if (prefixDone != num2 || prefixFirstLine != line2)
		{
			prefixFirstLine = line2;
			prefixDone = num2;
			prefixCache = BuildPrefix(history, num2);
			((TMP_Text)histText).text = prefixCache;
			((Component)histText).gameObject.SetActive(num2 > 0);
		}
		float num6 = 0f;
		if (num2 > 0)
		{
			num6 = ((TMP_Text)histText).preferredHeight;
			if (Mathf.Abs(((TMP_Text)histText).rectTransform.sizeDelta.y - num6) > 0.5f)
			{
				((TMP_Text)histText).rectTransform.sizeDelta = new Vector2(-28f, num6);
			}
		}
		float num7 = 0f;
		Vector2 val2;
		if ((Object)(object)histTail != (Object)null)
		{
			bool flag = num3 >= 0;
			if (((Component)histTail).gameObject.activeSelf != flag)
			{
				((Component)histTail).gameObject.SetActive(flag);
			}
			if (flag)
			{
				Line line3 = history[num3];
				string text3 = ((line3.kind != 1) ? ("<color=#8FA6C8>" + SafeText(line3.who) + Loc.Colon + "</color>" + SafeText(text.Substring(0, length))) : ("<color=#6FC3FF>" + SafeText(line3.who) + Loc.Colon + "</color><color=#CFE4F5>" + SafeText(text) + "</color>"));
				if (((TMP_Text)histTail).text != text3)
				{
					((TMP_Text)histTail).text = text3;
				}
				Vector2 val = default(Vector2);
				val = new Vector2(0f, 0f - num6);
				val2 = ((TMP_Text)histTail).rectTransform.anchoredPosition - val;
				if (((Vector2)val2).sqrMagnitude > 0.25f)
				{
					((TMP_Text)histTail).rectTransform.anchoredPosition = val;
				}
				num7 = ((TMP_Text)histTail).preferredHeight;
				if (Mathf.Abs(((TMP_Text)histTail).rectTransform.sizeDelta.y - num7) > 0.5f)
				{
					((TMP_Text)histTail).rectTransform.sizeDelta = new Vector2(-28f, num7);
				}
			}
		}
		if ((Object)(object)histContent != (Object)null && (num2 > 0 || num3 >= 0 || history.Count == 0))
		{
			float num8 = num6 + num7;
			if (num8 <= 0f)
			{
				num8 = 470f;
			}
			if (Mathf.Abs(histContent.sizeDelta.y - num8) > 0.5f)
			{
				histContent.sizeDelta = new Vector2(864f, num8);
			}
			histMax = Mathf.Max(0f, num8 - 470f + 8f);
			if (histFollow)
			{
				histOffset = histMax;
			}
			histOffset = Mathf.Clamp(histOffset, 0f, histMax);
			Vector2 val3 = default(Vector2);
			val3 = new Vector2(0f, histOffset);
			val2 = histContent.anchoredPosition - val3;
			if (((Vector2)val2).sqrMagnitude > 0.25f)
			{
				histContent.anchoredPosition = val3;
			}
		}
		UpdateReactLine(instance);
	}

	private void UpdateReactLine(CompanionHost h)
	{
		if (!((Object)(object)reactText == (Object)null) && !((Object)(object)h == (Object)null))
		{
			int num = h.BudgetUsed();
			int num2 = Mathf.Max(1, CompanionPlugin.Cfg.reactionsPerWindow);
			string text = Loc.T("反应 ") + num + "/" + num2 + ((h.busy && CompanionPlugin.Cfg.api.showThinking) ? Loc.T("  ·  思考中…") : "");
			if (((TMP_Text)reactText).text != text)
			{
				((TMP_Text)reactText).text = text;
			}
		}
	}

	private static string BuildPrefix(List<Line> lines, int n)
	{
		if (n <= 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(n * 24);
		for (int i = 0; i < n && i < lines.Count; i++)
		{
			Line line = lines[i];
			if (line != null)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('\n');
				}
				if (line.kind == 1)
				{
					stringBuilder.Append("<color=").Append("#6FC3FF").Append(">")
						.Append(SafeText(line.who))
						.Append(Loc.Colon + "</color>")
						.Append("<color=")
						.Append("#CFE4F5")
						.Append(">")
						.Append(SafeText(line.text ?? ""))
						.Append("</color>");
				}
				else
				{
					stringBuilder.Append("<color=#8FA6C8>").Append(SafeText(line.who)).Append(Loc.Colon + "</color>")
						.Append(SafeText(line.text ?? ""));
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static string SafeText(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s.Replace('<', '＜').Replace('>', '＞');
	}

	private static bool IsCharOutsideParens(string s, int index)
	{
		if (string.IsNullOrEmpty(s) || index < 0 || index >= s.Length)
		{
			return false;
		}
		int depth = 0;
		for (int i = 0; i <= index; i++)
		{
			char c = s[i];
			if (c == '（' || c == '(')
			{
				depth++;
			}
			else if (c == '）' || c == ')')
			{
				if (depth > 0)
				{
					depth--;
				}
			}
			else if (i == index)
			{
				return depth == 0 && !char.IsWhiteSpace(c);
			}
		}
		return false;
	}

	private void TickTypingSound()
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance == (Object)null || !instance.HasBody || (Object)(object)historyGo == (Object)null || !historyGo.activeSelf)
		{
			return;
		}
		List<Line> history = instance.history;
		float unscaledTime = Time.unscaledTime;
		float cps = TypingCps;
		float gap = TypingGap;
		float grace = TypingGrace;
		float nextStart = -1f;
		bool flag = false;
		for (int i = 0; i < history.Count; i++)
		{
			Line line = history[i];
			if (line == null || string.IsNullOrEmpty(line.text))
			{
				continue;
			}
			if (line.kind == 1 || line.instant)
			{
				continue;
			}
			float startTime = Mathf.Max(line.time, nextStart);
			float endTime = startTime + (float)line.text.Length / cps;
			nextStart = endTime + gap;
			if (unscaledTime < startTime || unscaledTime >= endTime + grace)
			{
				continue;
			}
			int curIdx = Mathf.Clamp((int)((unscaledTime - startTime) * cps), 0, line.text.Length - 1);
			if (IsCharOutsideParens(line.text, curIdx))
			{
				flag = true;
				break;
			}
			if (unscaledTime >= endTime && grace > 0f)
			{
				int graceBack = Mathf.Max(1, Mathf.CeilToInt(grace * cps));
				for (int j = curIdx; j >= Math.Max(0, curIdx - graceBack); j--)
				{
					if (IsCharOutsideParens(line.text, j))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		if (!flag || Time.unscaledTime - lastBeepAt < 0.085f)
		{
			return;
		}
		lastBeepAt = Time.unscaledTime;
		Body currentBody = instance.CurrentBody;
		if ((Object)(object)currentBody == (Object)null)
		{
			return;
		}
		int num = 0;
		try
		{
			num = currentBody.charType;
		}
		catch
		{
		}
		string name = num switch
		{
			2 => "speechdune", 
			1 => "milspeech", 
			_ => "speech", 
		};
		float num2 = 0.22f;
		try
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			if (cfg != null)
			{
				num2 = Mathf.Clamp(cfg.typingVolume, 0f, 1f);
			}
		}
		catch
		{
		}
		if (!(num2 <= 0.001f))
		{
			PlayTypingSound(name, ((Component)currentBody).transform.position, num2);
		}
	}

	private void PlayTypingSound(string name, Vector3 pos, float vol)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)typingGo == (Object)null)
			{
				typingGo = new GameObject("CU_TypingAudio");
				Object.DontDestroyOnLoad((Object)(object)typingGo);
				typingSource = typingGo.AddComponent<AudioSource>();
				typingSource.playOnAwake = false;
				typingSource.dopplerLevel = 0f;
				typingSource.bypassReverbZones = true;
				typingSource.spatialBlend = 1f;
				try
				{
					if ((Object)(object)WorldGeneration.world != (Object)null)
					{
						typingSource.outputAudioMixerGroup = WorldGeneration.world.soundMixerGroup;
					}
				}
				catch
				{
				}
			}
			if ((Object)(object)typingClip == (Object)null || typingClipName != name)
			{
				typingClipName = name;
				if (!clipCache.TryGetValue(name, out var value) || (Object)(object)value == (Object)null)
				{
					value = Resources.Load<AudioClip>("Sounds/" + name);
					clipCache[name] = value;
				}
				typingClip = value;
			}
			if (!((Object)(object)typingClip == (Object)null))
			{
				typingGo.transform.position = pos;
				typingSource.volume = vol;
				typingSource.pitch = 1f + Random.Range(-0.15f, 0.15f);
				typingSource.PlayOneShot(typingClip);
			}
		}
		catch
		{
		}
	}

	private void SetHistory(bool open)
	{
		histWanted = open;
		bool flag = ((Object)(object)CompanionHost.Instance != (Object)null && CompanionHost.Instance.HasBody) || (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug);
		if ((Object)(object)historyGo != (Object)null)
		{
			historyGo.SetActive(open && flag);
		}
		if ((Object)(object)tabGo != (Object)null)
		{
			tabGo.SetActive(flag);
		}
		if ((Object)(object)tabLabel != (Object)null)
		{
			((TMP_Text)tabLabel).text = (open ? (Lang.T("hudTab") + " ▼") : (Lang.T("hudTab") + " ▲"));
		}
		if (open && flag)
		{
			RefreshHistory();
		}
	}

	public void ToggleInput()
	{
		if (!((Object)(object)inputGo == (Object)null))
		{
			SetInputOpen(!inputGo.activeSelf);
		}
	}

	public void SetInputOpen(bool open)
	{
		if ((Object)(object)inputGo == (Object)null)
		{
			return;
		}
		string text = "?";
		try
		{
			text = new StackTrace().GetFrame(1).GetMethod().Name;
		}
		catch
		{
		}
		CompanionPlugin.Log.LogInfo((object)("[ui] SetInputOpen(" + open + ") 调用方=" + text + " 当前=" + (inputGo.activeSelf ? "开" : "关") + " 捕获=" + InputGuard.Capturing));
		if (!open)
		{
			if ((Object)(object)inputField != (Object)null)
			{
				inputField.Blur();
			}
			InputGuard.Set(on: false);
			inputGo.SetActive(false);
			return;
		}
		inputGo.SetActive(true);
		inputGo.transform.SetAsLastSibling();
		Canvas.ForceUpdateCanvases();
		if ((Object)(object)inputField != (Object)null)
		{
			inputField.SetText(inputText);
			inputField.Focus();
		}
		if ((Object)(object)inputField == (Object)null || !inputField.IsFocused)
		{
			CompanionPlugin.Log.LogWarning((object)"[ui] 输入框聚焦失败，重试一次");
			if ((Object)(object)inputField != (Object)null)
			{
				inputField.Focus();
			}
		}
	}

	private void ToggleApi()
	{
		if (!((Object)(object)apiGo == (Object)null))
		{
			apiGo.SetActive(!apiGo.activeSelf);
			apiGo.transform.SetAsLastSibling();
		}
	}

	public static void ShowHistory()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			Instance.SetHistory(open: true);
		}
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

	private void Panel(GameObject go, float w, float h, float alpha)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		Image obj = go.AddComponent<Image>();
		((Graphic)obj).color = new Color(0.015f, 0.017f, 0.022f, alpha);
		((Graphic)obj).raycastTarget = true;
		Box(go.transform, 0f, 0f, w, 1.6f, new Color(1f, 1f, 1f, 0.85f), "t");
		Box(go.transform, 0f, 0f - h, w, 1.6f, new Color(1f, 1f, 1f, 0.85f), "b");
		Box(go.transform, 0f, 0f, 1.6f, h, new Color(1f, 1f, 1f, 0.85f), "l");
		Box(go.transform, w - 1.6f, 0f, 1.6f, h, new Color(1f, 1f, 1f, 0.85f), "r");
	}

	private void Box(Transform parent, float x, float y, float w, float h, Color c, string name)
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
	}

	private TextMeshProUGUI Label(GameObject parent, string text, float size, Vector2 pos, float w, float h, TextAlignmentOptions align, Color color)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		TextMeshProUGUI obj = Node("label", parent.transform, pos.x, pos.y, w, h).AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = GameFont;
		((TMP_Text)obj).fontSize = size;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = color;
		((TMP_Text)obj).alignment = align;
		((TMP_Text)obj).enableWordWrapping = true;
		((Graphic)obj).raycastTarget = false;
		((TMP_Text)obj).text = text;
		return obj;
	}

	private GameObject Bar(GameObject parent, string text, float x, float y, float w, float h, Action onClick)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Node("bar_" + text, parent.transform, x, y, w, h);
		((Graphic)val.AddComponent<Image>()).color = new Color(1f, 1f, 1f, 0.06f);
		Box(val.transform, 0f, 0f, w, 1.2f, new Color(1f, 1f, 1f, 0.75f), "t");
		Box(val.transform, 0f, 0f - h, w, 1.2f, new Color(1f, 1f, 1f, 0.75f), "b");
		Box(val.transform, 0f, 0f, 1.2f, h, new Color(1f, 1f, 1f, 0.75f), "l");
		Box(val.transform, w - 1.2f, 0f, 1.2f, h, new Color(1f, 1f, 1f, 0.75f), "r");
		TextMeshProUGUI obj = Node("text", val.transform, 0f, -8f, w, h - 8f).AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = GameFont;
		((TMP_Text)obj).fontSize = 24f;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = Color.white;
		((TMP_Text)obj).alignment = (TextAlignmentOptions)514;
		((TMP_Text)obj).enableWordWrapping = false;
		((TMP_Text)obj).overflowMode = (TextOverflowModes)0;
		((TMP_Text)obj).text = text;
		((Graphic)obj).raycastTarget = false;
		val.AddComponent<NativeHit>().Init(onClick);
		val.AddComponent<BarFx>();
		return val;
	}

	private NativeField Field(GameObject parent, string title, float x, float y, float w, float h, Func<string> get, Action<string> set)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		Label(parent, title, 22f, new Vector2(x, y), w, 28f, (TextAlignmentOptions)513, new Color(1f, 1f, 1f, 0.6f));
		GameObject val = Node("field_" + title, parent.transform, x, y - 28f, w, h);
		((Graphic)val.AddComponent<Image>()).color = new Color(0f, 0f, 0f, 0.35f);
		Box(val.transform, 0f, 0f, w, 1.2f, new Color(1f, 1f, 1f, 0.6f), "t");
		Box(val.transform, 0f, 0f - h, w, 1.2f, new Color(1f, 1f, 1f, 0.6f), "b");
		NativeField nativeField = val.AddComponent<NativeField>();
		nativeField.Init(GameFont, get, set, multi: false);
		return nativeField;
	}
}
