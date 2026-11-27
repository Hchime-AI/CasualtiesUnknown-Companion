using System;
using System.Collections.Generic;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CUCompanion;

public class IntimateController : MonoBehaviour
{
	public static IntimateController Instance;

	public const int PhaseNone = 0;

	public const int PhaseAccept = 1;

	public const int PhaseActing = 2;

	public const int PhaseFaster = 3;

	public const int PhasePeak = 4;

	public const int PhaseOrgasm = 5;

	private int phase;

	private float startTime;

	private float nextBeat;

	private float endAt;

	private float lingerUntil = -1f;

	private float zoomBase = -1f;

	private float nextPant;

	private float nextHeal;

	private bool lying;

	private bool sitting;

	private TextMeshProUGUI hintText;

	private RectTransform historyRt;

	private GameObject interactBtn;

	private GameObject panel;

	private GameObject optRow;

	private GameObject orgasmBtn;

	private bool optionsOpen;

	private int debtCaloriesAt;

	private float debtSleepAccum;

	private bool attached;

	private readonly List<Transform> tilted = new List<Transform>();

	private RenderTexture zoomRt;

	private Canvas zoomCanvas;

	private RawImage zoomImg;

	private float zoomT;

	private readonly Dictionary<Canvas, RenderMode> savedModes = new Dictionary<Canvas, RenderMode>();

	public bool Active
	{
		get
		{
			if (phase != 0)
			{
				return phase != 5;
			}
			return false;
		}
	}

	public bool InMast
	{
		get
		{
			if (phase >= 1)
			{
				return phase != 5;
			}
			return false;
		}
	}

	public bool InAfterglow => phase == 5;

	public bool AtPeak => phase == 4;

	public bool BrainOff
	{
		get
		{
			if (phase >= 4)
			{
				return !Debt;
			}
			return false;
		}
	}

	public static bool Debt { get; private set; }

	public static int DebtCalorieNeed => Mathf.Clamp(CompanionPlugin.Cfg?.debtCalorieNeed ?? 200, 0, 20000);

	public static float DebtSleepNeed => Mathf.Clamp(CompanionPlugin.Cfg?.debtSleepNeed ?? 60f, 1f, 3600f);

	private static float ActingAt => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastActingAt : 0f, 20f, 1f, 3600f);

	private static float FasterAt => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastFasterAt : 0f, 40f, 1f, 3600f);

	private static float PeakAt => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastPeakAt : 0f, 60f, 1f, 3600f);

	private static float EndAt => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastEndAt : 0f, 600f, 5f, 7200f);

	private static float BeatEvery => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastBeatEvery : 0f, 30f, 1f, 600f);

	private static float LingerSec => CfgTime((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.mastLingerSec : 0f, 30f, 0f, 600f);

	private static ManualLogSource Log => CompanionPlugin.Log;

	private static float CfgTime(float value, float fallback, float min, float max)
	{
		if (value <= 0f)
		{
			value = fallback;
		}
		return Mathf.Clamp(value, min, max);
	}

	public static void ResetRunState()
	{
		Debt = false;
		ManualLogSource log = CompanionPlugin.Log;
		if (log != null)
		{
			log.LogInfo((object)"[Intimate] New run: Physical debt reset to false.");
		}
	}

	public void ResetPhase()
	{
		phase = 0;
		lingerUntil = -1f;
		nextPant = float.MaxValue;
		nextBeat = 0f;
		optionsOpen = false;
		debtSleepAccum = 0f;
		debtCaloriesAt = 0;
		try
		{
			ClosePanel();
		}
		catch
		{
		}
		try
		{
			if ((Object)(object)interactBtn != (Object)null)
			{
				interactBtn.SetActive(true);
			}
		}
		catch
		{
		}
		try
		{
			InputGuard.HardLock = false;
			InputGuard.Set(on: false);
		}
		catch
		{
		}
		sitting = false;
		lying = false;
		try
		{
			StandUp();
		}
		catch
		{
		}
		try
		{
			RestoreZoom(1f);
		}
		catch
		{
		}
		if (zoomT == 0f && !((Object)(object)zoomRt != (Object)null))
		{
			return;
		}
		zoomT = 0f;
		try
		{
			Camera main = Camera.main;
			if ((Object)(object)main != (Object)null)
			{
				main.targetTexture = null;
			}
			RestoreUiModes();
			if ((Object)(object)zoomCanvas != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)zoomCanvas).gameObject);
			}
			if ((Object)(object)zoomRt != (Object)null)
			{
				zoomRt.Release();
				Object.Destroy((Object)(object)zoomRt);
			}
			zoomCanvas = null;
			zoomImg = null;
			zoomRt = null;
		}
		catch
		{
		}
	}

	public static void ClearDebt(string reason)
	{
		if (Debt)
		{
			Debt = false;
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogInfo((object)("[Intimate] Physical debt cleared: " + reason));
			}
		}
	}

	private void Awake()
	{
		Instance = this;
	}

	public void Attach(RectTransform hist)
	{
		if (!((Object)(object)hist == (Object)null))
		{
			historyRt = hist;
			if (!attached)
			{
				attached = true;
				BuildInteractButton();
			}
		}
	}

	public void SetVisible(bool v)
	{
		if ((Object)(object)interactBtn != (Object)null && interactBtn.activeSelf != (v && !Active))
		{
			interactBtn.SetActive(v && !Active);
		}
		if (!v)
		{
			optionsOpen = false;
			if (!Active)
			{
				ClosePanel();
			}
		}
	}

	private static GameObject Node(string name, Transform parent, float x, float y, float w, float h)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
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

	private static GameObject BarIn(Transform parent, string text, float x, float y, float w, float h, Action onClick)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Node("bar_" + text, parent, x, y, w, h);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = new Color(1f, 1f, 1f, 0.06f);
		((Graphic)obj).raycastTarget = true;
		Button obj2 = val.AddComponent<Button>();
		((Selectable)obj2).transition = (Selectable.Transition)0;
		obj2.onClick = new Button.ButtonClickedEvent();
		((UnityEvent)obj2.onClick).AddListener((UnityAction)delegate
		{
			try
			{
				PlayerCamera.PlayUISound("click", 1f);
			}
			catch
			{
			}
			onClick?.Invoke();
		});
		TextMeshProUGUI obj3 = Node("txt", val.transform, 0f, 0f, w, h).AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj3).font = NativeHudController.GameFont;
		((TMP_Text)obj3).fontSize = 24f;
		Loc.Fit((TMP_Text)obj3);
		((Graphic)obj3).color = Color.white;
		((TMP_Text)obj3).alignment = (TextAlignmentOptions)514;
		((TMP_Text)obj3).enableWordWrapping = false;
		((Graphic)obj3).raycastTarget = false;
		((TMP_Text)obj3).text = text;
		Color c = default(Color);
		c = new Color(1f, 1f, 1f, 0.7f);
		AddLine(val.transform, 0f, 0f, w, 1.6f, c);
		AddLine(val.transform, 0f, 0f - (h - 1.6f), w, 1.6f, c);
		AddLine(val.transform, 0f, 0f, 1.6f, h, c);
		AddLine(val.transform, w - 1.6f, 0f, 1.6f, h, c);
		val.AddComponent<BarFx>();
		return val;
	}

	private static void AddLine(Transform parent, float x, float y, float w, float h, Color c)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Image obj = Node("line", parent, x, y, w, h).AddComponent<Image>();
		((Graphic)obj).color = c;
		((Graphic)obj).raycastTarget = false;
	}

	private void BuildInteractButton()
	{
		if (!((Object)(object)historyRt == (Object)null))
		{
			if ((Object)(object)interactBtn != (Object)null)
			{
				Object.Destroy((Object)(object)interactBtn);
			}
			interactBtn = BarIn((Transform)(object)historyRt, Loc.T("互动 ▼"), 0f, -566f, 132f, 40f, delegate
			{
				ToggleOptions();
			});
		}
	}

	public void ToggleOptions()
	{
		optionsOpen = !optionsOpen;
		CompanionPlugin.Log.LogInfo(optionsOpen ? "[ui] 互动菜单：打开" : "[ui] 互动菜单：关闭");
		if (optionsOpen)
		{
			ShowOptions();
		}
		else
		{
			ClosePanel();
		}
		TextMeshProUGUI val = (((Object)(object)interactBtn == (Object)null) ? null : interactBtn.GetComponentInChildren<TextMeshProUGUI>(true));
		if ((Object)(object)val != (Object)null)
		{
			((TMP_Text)val).text = (optionsOpen ? Loc.T("互动 ▲") : Loc.T("互动 ▼"));
		}
	}

	private void ClosePanel()
	{
		if ((Object)(object)panel != (Object)null)
		{
			Object.Destroy((Object)(object)panel);
		}
		panel = null;
		optRow = null;
		orgasmBtn = null;
	}

	private void ShowOptions()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		ClosePanel();
		CompanionHost instance = CompanionHost.Instance;
		if (!((Object)(object)instance == (Object)null) && instance.HasBody)
		{
			Transform canvasTransform = NativeHudController.CanvasTransform;
			if (!((Object)(object)canvasTransform == (Object)null))
			{
				panel = Node("MastPanel", canvasTransform, 0f, 0f, 520f, 150f);
				RectTransform component = panel.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0f);
				component.anchorMax = new Vector2(0.5f, 0f);
				component.pivot = new Vector2(0.5f, 0f);
				component.anchoredPosition = new Vector2(0f, 132f);
				((Transform)component).SetAsLastSibling();
				optRow = Node("row", panel.transform, 0f, 0f, 520f, 120f);
				RebuildOptions();
			}
		}
	}

	private void RebuildOptions()
	{
		if ((Object)(object)optRow == (Object)null)
		{
			return;
		}
		for (int num = optRow.transform.childCount - 1; num >= 0; num--)
		{
			Object.Destroy((Object)(object)((Component)optRow.transform.GetChild(num)).gameObject);
		}
		CompanionHost instance = CompanionHost.Instance;
		Persona persona = (((Object)(object)instance == (Object)null) ? null : instance.CurrentPersona());
		string[] array = ((persona != null && persona.interactions != null && persona.interactions.Length != 0) ? persona.interactions : new string[3] { Loc.T("自慰"), Loc.T("哼歌"), Loc.T("抚摸自身") });
		int num2 = 3;
		for (int i = 0; i < array.Length; i++)
		{
			string label = array[i];
			int num3 = i % num2;
			int num4 = i / num2;
			string text = IsMasturbationLabel(label) ? Loc.T("自慰") : label;
			BarIn(optRow.transform, text, (float)num3 * 172f, 0f - (float)num4 * 50f, 164f, 44f, delegate
			{
				OnPick(label);
			});
		}
	}

	private static bool IsMasturbationLabel(string label)
	{
		if (string.IsNullOrEmpty(label))
		{
			return false;
		}
		string text = label.Trim().ToLowerInvariant();
		if (text == "自慰" || text == "自渎" || text == "zw" || text == "masturbation")
		{
			return true;
		}
		if (text == Loc.T("自慰").Trim().ToLowerInvariant())
		{
			return true;
		}
		return text.StartsWith("masturbat");
	}

	private void OnPick(string label)
	{
		CompanionHost instance = CompanionHost.Instance;
		if (!((Object)(object)instance == (Object)null))
		{
			if (IsMasturbationLabel(label))
			{
				StartMasturbation();
				return;
			}
			ClosePanel();
			optionsOpen = false;
			instance.SubmitPlayerInput(label);
		}
	}

	public void StartMasturbation()
	{
		CompanionHost instance = CompanionHost.Instance;
		if (!((Object)(object)instance == (Object)null) && instance.HasBody && phase == 0)
		{
			phase = 1;
			startTime = Time.unscaledTime;
			nextBeat = startTime + PeakAt + BeatEvery;
			endAt = startTime + EndAt;
			nextPant = 0f;
			nextHeal = 0f;
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogInfo((object)"[Intimate] Started: Command accepted.");
			}
			ClosePanel();
			optionsOpen = false;
			BuildOrgasmButton();
			if ((Object)(object)interactBtn != (Object)null)
			{
				interactBtn.SetActive(false);
			}
			sitting = false;
			lying = false;
			try
			{
				InputGuard.HardLock = true;
				InputGuard.Set(on: true);
			}
			catch
			{
			}
			Say(Loc.T("（靠着墙壁慢慢坐下，调整呼吸）……好，我听你的。"));
			instance.AddLine(instance.PersonaName(), Loc.T("（接受指令，开始调整呼吸放松身体）"), player: false);
			if (Debt)
			{
				instance.AddLine(instance.PersonaName(), LocalizationManager.T("debtHint"), player: false);
			}
			return;
		}
		ManualLogSource log2 = Log;
		if (log2 != null)
		{
			log2.LogInfo((object)("[Intimate] Masturbation skipped: hasHost=" + (!((Object)(object)instance == (Object)null)) + " hasBody=" + (((Object)(object)instance != (Object)null) ? instance.HasBody.ToString() : "?") + " phase=" + phase));
		}
	}

	private void BuildOrgasmButton()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)panel != (Object)null)
		{
			ClosePanel();
		}
		Transform canvasTransform = NativeHudController.CanvasTransform;
		panel = Node("MastPanel", canvasTransform, 0f, 0f, 520f, 120f);
		RectTransform component = panel.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0f);
		component.anchorMax = new Vector2(0.5f, 0f);
		component.pivot = new Vector2(0.5f, 0f);
		component.anchoredPosition = new Vector2(0f, 132f);
		((Transform)component).SetAsLastSibling();
		float armedAt = Time.unscaledTime + 0.35f;
		orgasmBtn = BarIn(panel.transform, Loc.T("高  潮"), 0f, 0f, 520f, 84f, delegate
		{
			if (!(Time.unscaledTime < armedAt))
			{
				Orgasm();
			}
		});
		hintText = Node("hint", panel.transform, 0f, -92f, 520f, 26f).AddComponent<TextMeshProUGUI>();
		((TMP_Text)hintText).font = NativeHudController.GameFont;
		((TMP_Text)hintText).fontSize = 18f;
		((Graphic)hintText).color = new Color(1f, 1f, 1f, 0.5f);
		((TMP_Text)hintText).alignment = (TextAlignmentOptions)514;
		((Graphic)hintText).raycastTarget = false;
		((TMP_Text)hintText).text = Loc.T("点这里才结束；不点的话 10 分钟后会自动结束");
	}

	private void Say(string text)
	{
		CompanionHost.Instance?.Speak(text, record: true);
	}

	private void Beat()
	{
		CompanionHost instance = CompanionHost.Instance;
		if (!((Object)(object)instance == (Object)null) && instance.HasBody && instance.CanSpeak())
		{
			string[] array = ((phase < 4) ? new string[4]
			{
				Loc.T("（闭上眼睛深呼吸，声音微颤）……主人，我在照做。"),
				Loc.T("（轻靠着墙面，呼吸渐渐散乱）这样……感觉稍微好一些了。"),
				Loc.T("（小声地喘息）……身上有点热，心跳好快。"),
				Loc.T("（停顿了一下，呼吸放缓）……有你在通讯器那头，感觉安心很多。")
			} : new string[4]
			{
				Loc.T("（呼吸变得急促，手指紧紧抓着衣服）……呼……心跳得好剧烈……"),
				Loc.T("（身体发软慢慢靠下去，声音断断续续）……感觉头脑有点放空……"),
				Loc.T("（眼眶发热，轻喘着闭上眼睛）……让我缓一下……别挂断……"),
				Loc.T("（整个人完全松弛下来，长长地呼出一口气）……嗯……终于平复些了……")
			});
			string fallback = array[Random.Range(0, array.Length)];
			instance.RequestMastLine((phase >= 4) ? Loc.T("情绪剧烈释放，身体彻底松弛前夕，呼吸急促") : Loc.T("刚开始调整呼吸，尝试放松平复"), fallback);
		}
	}

	private void BeginActing()
	{
		phase = 2;
		ManualLogSource log = Log;
		if (log != null)
		{
			log.LogInfo((object)"[Intimate] Actions begun: Non-intrusive animation and cinematic zoom enabled.");
		}
		Say(Loc.T("（闭上眼睛，慢慢放松紧绷的神经）……开始了。"));
		zoomBase = 1f;
	}

	private void LayDown()
	{
		lying = false;
	}

	private void StandUp()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			for (int i = 0; i < tilted.Count; i++)
			{
				if ((Object)(object)tilted[i] != (Object)null)
				{
					tilted[i].rotation = Quaternion.identity;
				}
			}
		}
		catch
		{
		}
		tilted.Clear();
		lying = false;
	}

	private void ApplyPose()
	{
	}

	private void HideUiFromRt()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		savedModes.Clear();
		try
		{
			Canvas[] array = Resources.FindObjectsOfTypeAll<Canvas>();
			foreach (Canvas val in array)
			{
				if (!((Object)(object)val == (Object)null) && !(((Object)val).name == "CUZoomCanvas") && (int)val.renderMode == 1)
				{
					savedModes[val] = val.renderMode;
					val.renderMode = (RenderMode)0;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogWarning((object)("[IntimateZoom] HideUiFromRt failed: " + ex.Message));
			}
		}
	}

	private void RestoreUiModes()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			foreach (KeyValuePair<Canvas, RenderMode> savedMode in savedModes)
			{
				if ((Object)(object)savedMode.Key != (Object)null)
				{
					savedMode.Key.renderMode = savedMode.Value;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogWarning((object)("[IntimateZoom] RestoreUiModes failed: " + ex.Message));
			}
		}
		savedModes.Clear();
	}

	private void Zoom(float dt)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Camera main = Camera.main;
			if (!((Object)(object)main == (Object)null))
			{
				if ((Object)(object)zoomRt == (Object)null)
				{
					int num = Mathf.Max(320, Screen.width);
					int num2 = Mathf.Max(240, Screen.height);
					zoomRt = new RenderTexture(num, num2, 16, (RenderTextureFormat)0);
					zoomRt.Create();
					HideUiFromRt();
					main.targetTexture = zoomRt;
					GameObject val = new GameObject("CUZoomCanvas");
					zoomCanvas = val.AddComponent<Canvas>();
					zoomCanvas.renderMode = (RenderMode)0;
					zoomCanvas.sortingOrder = -30000;
					val.AddComponent<CanvasScaler>();
					GameObject val2 = new GameObject("CUZoomImage");
					val2.transform.SetParent(val.transform, false);
					zoomImg = val2.AddComponent<RawImage>();
					zoomImg.texture = (Texture)(object)zoomRt;
					((Graphic)zoomImg).raycastTarget = false;
					RectTransform rectTransform = ((Graphic)zoomImg).rectTransform;
					rectTransform.anchorMin = Vector2.zero;
					rectTransform.anchorMax = Vector2.one;
					rectTransform.offsetMin = Vector2.zero;
					rectTransform.offsetMax = Vector2.zero;
					((Graphic)zoomImg).color = Color.white;
				}
				zoomT = Mathf.MoveTowards(zoomT, 1f, dt * 1.2f);
				float num3 = Mathf.Lerp(1f, 0.55f, zoomT);
				float num4 = (1f - num3) * 0.5f;
				if ((Object)(object)zoomImg != (Object)null)
				{
					zoomImg.uvRect = new Rect(num4, num4, num3, num3);
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogWarning((object)("[IntimateZoom] Zoom error: " + ex.Message));
			}
		}
	}

	private void RestoreZoom(float dt)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)zoomRt == (Object)null)
			{
				return;
			}
			zoomT = Mathf.MoveTowards(zoomT, 0f, dt * 2.2f);
			float num = Mathf.Lerp(1f, 0.55f, zoomT);
			float num2 = (1f - num) * 0.5f;
			if ((Object)(object)zoomImg != (Object)null)
			{
				zoomImg.uvRect = new Rect(num2, num2, num, num);
			}
			if (!(zoomT > 0f))
			{
				Camera main = Camera.main;
				if ((Object)(object)main != (Object)null)
				{
					main.targetTexture = null;
				}
				RestoreUiModes();
				if ((Object)(object)zoomCanvas != (Object)null)
				{
					Object.Destroy((Object)(object)((Component)zoomCanvas).gameObject);
				}
				if ((Object)(object)zoomRt != (Object)null)
				{
					zoomRt.Release();
					Object.Destroy((Object)(object)zoomRt);
				}
				zoomCanvas = null;
				zoomImg = null;
				zoomRt = null;
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogWarning((object)("[IntimateZoom] Restore error: " + ex.Message));
			}
		}
	}

	private void Pant(float now, float vol, float pitch)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance == (Object)null || !instance.HasBody || now < nextPant)
		{
			return;
		}
		nextPant = now + ((phase >= 4) ? 0.9f : 1.6f);
		try
		{
			Body currentBody = instance.CurrentBody;
			PantSound component = ((Component)currentBody).GetComponent<PantSound>();
			if ((Object)(object)component != (Object)null && component.painSounds != null && component.painSounds.Length != 0)
			{
				Sound.Play(component.painSounds[Random.Range(0, component.painSounds.Length)], (Vector2)(((Component)currentBody).transform.position), false, true, ((Component)currentBody).transform, vol, currentBody.voicePitch * pitch, false, false);
			}
			else
			{
				Sound.Play("pant", (Vector2)(((Component)currentBody).transform.position), false, true, ((Component)currentBody).transform, vol, pitch, false, false);
			}
		}
		catch
		{
		}
	}

	private void EnterDebt()
	{
		bool debt = Debt;
		Debt = true;
		debtSleepAccum = 0f;
		if (!debt)
		{
			try
			{
				PlayerCamera main = PlayerCamera.main;
				debtCaloriesAt = ((!((Object)(object)main == (Object)null)) ? main.caloriesConsumed : 0);
			}
			catch
			{
				debtCaloriesAt = 0;
			}
		}
		ManualLogSource log = Log;
		if (log != null)
		{
			log.LogInfo((object)$"[Intimate] Entered physical debt: Requires {DebtCalorieNeed} calories or {DebtSleepNeed:F0}s continuous sleep.");
		}
	}

	private void TickDebt(float dt)
	{
		if (!Debt)
		{
			return;
		}
		try
		{
			PlayerCamera main = PlayerCamera.main;
			int debtCalorieNeed = DebtCalorieNeed;
			if ((Object)(object)main != (Object)null && debtCalorieNeed > 0 && main.caloriesConsumed - debtCaloriesAt >= debtCalorieNeed)
			{
				ClearDebt("Consumed " + (main.caloriesConsumed - debtCaloriesAt) + " calories");
				return;
			}
			CompanionHost instance = CompanionHost.Instance;
			Body val = (((Object)(object)instance == (Object)null) ? null : instance.CurrentBody);
			if ((Object)(object)val != (Object)null && val.sleeping)
			{
				debtSleepAccum += dt;
				if (debtSleepAccum >= DebtSleepNeed)
				{
					ClearDebt("Rested via sleep cycle");
				}
			}
			else
			{
				debtSleepAccum = 0f;
			}
		}
		catch
		{
		}
	}

	private void Heal(float dt)
	{
		if (Debt)
		{
			return;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance == (Object)null || !instance.HasBody)
		{
			return;
		}
		Body currentBody = instance.CurrentBody;
		float num = dt;
		try
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			if (cfg != null)
			{
				num = dt * Mathf.Clamp(cfg.mastHealScale, 0f, 20f);
			}
		}
		catch
		{
		}
		try
		{
			currentBody.horrifiedLevel = 0f;
			currentBody.traumaAmount = Mathf.Max(0f, currentBody.traumaAmount - num * 2.2f);
			currentBody.immunity = Mathf.Min(100f, currentBody.immunity + num * 0.8f);
			if (currentBody.temperature < 37.6f)
			{
				currentBody.temperature = Mathf.Min(37.6f, currentBody.temperature + num * 0.02f);
			}
			if (currentBody.wetness < 100f)
			{
				currentBody.wetness = Mathf.Min(100f, currentBody.wetness + num * 0.15f);
			}
			if (currentBody.consciousness < 100f)
			{
				currentBody.consciousness = Mathf.Min(100f, currentBody.consciousness + num * 0.05f);
			}
			if (currentBody.sicknessAmount > 0f)
			{
				currentBody.sicknessAmount = Mathf.Max(0f, currentBody.sicknessAmount - num * 2.4f);
			}
			if (currentBody.internalBleeding > 0f)
			{
				currentBody.internalBleeding = Mathf.Max(0f, currentBody.internalBleeding - num * 1.6f);
			}
			if (currentBody.hemothorax > 0f)
			{
				currentBody.hemothorax = Mathf.Max(0f, currentBody.hemothorax - num * 1.6f);
			}
			if (currentBody.strokeAmount > 0f)
			{
				currentBody.strokeAmount = Mathf.Max(0f, currentBody.strokeAmount - num * 1.2f);
			}
			if (currentBody.septicShock > 0f)
			{
				currentBody.septicShock = Mathf.Max(0f, currentBody.septicShock - num * 1.2f);
			}
			if (currentBody.limbs != null)
			{
				for (int i = 0; i < currentBody.limbs.Length; i++)
				{
					Limb val = currentBody.limbs[i];
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					val.pain = Mathf.Max(0f, val.pain - num * 5f);
					if (val.infectionAmount > 0f)
					{
						val.infectionAmount = Mathf.Max(0f, val.infectionAmount - num * 4f);
						if (val.infectionAmount <= 0f)
						{
							val.infected = false;
						}
					}
					val.bleedAmount = Mathf.Max(0f, val.bleedAmount - num * 2f);
					if (val.skinHealth < 100f)
					{
						val.skinHealth = Mathf.Min(100f, val.skinHealth + num * 1.8f);
					}
					if (val.muscleHealth < 100f)
					{
						val.muscleHealth = Mathf.Min(100f, val.muscleHealth + num * 1.2f);
					}
				}
			}
			if (currentBody.fibrillationProgress > 0f)
			{
				currentBody.fibrillationProgress = 0f;
			}
			currentBody.fibrillationForced = false;
			if (currentBody.heartRate < 60f)
			{
				currentBody.heartRate = 60f;
			}
			if (currentBody.bloodOxygen < 60f)
			{
				currentBody.bloodOxygen = 60f;
			}
			if (currentBody.bloodPressure < 85f)
			{
				currentBody.bloodPressure = 85f;
			}
			if (currentBody.bloodVolume < 35f)
			{
				currentBody.bloodVolume = 35f;
			}
			if (currentBody.temperature < 29f)
			{
				currentBody.temperature = 29f;
			}
		}
		catch
		{
		}
	}

	public void Orgasm()
	{
		if (phase != 0 && phase != 5)
		{
			phase = 5;
			try
			{
				InputGuard.HardLock = false;
				InputGuard.Set(on: false);
			}
			catch
			{
			}
			sitting = false;
			nextPant = float.MaxValue;
			lingerUntil = Time.unscaledTime + LingerSec;
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogInfo((object)"[Intimate] Orgasm reached.");
			}
			CompanionHost instance = CompanionHost.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.RequestMastLine(Loc.T("释放平复的瞬间：整个人彻底放松下来，长舒一口气，疲惫但感到被治愈"), Loc.T("（整个人彻底松弛地瘫软下来，大口呼吸着）……呼……终于缓过来了……感觉身体在恢复……"), overrideBusy: true);
			}
			ClosePanel();
			optionsOpen = false;
			if ((Object)(object)interactBtn != (Object)null)
			{
				interactBtn.SetActive(true);
			}
		}
	}

	private void StopAll(bool restore)
	{
		phase = 0;
		lingerUntil = -1f;
		ClosePanel();
		optionsOpen = false;
		if ((Object)(object)interactBtn != (Object)null)
		{
			interactBtn.SetActive(true);
		}
		ManualLogSource log = Log;
		if (log != null)
		{
			log.LogInfo((object)"[Intimate] Intimate cycle finished.");
		}
		sitting = false;
		try
		{
			InputGuard.HardLock = false;
			InputGuard.Set(on: false);
		}
		catch
		{
		}
		StandUp();
		if (restore)
		{
			RestoreZoom(1f);
		}
	}

	private void Update()
	{
		if (phase == 0 && InputGuard.HardLock)
		{
			InputGuard.HardLock = false;
			try
			{
				InputGuard.Set(on: false);
			}
			catch
			{
			}
		}
		if ((Object)(object)zoomRt != (Object)null && (phase == 0 || phase == 5))
		{
			RestoreZoom(Time.unscaledDeltaTime);
		}
		TickDebt(Time.unscaledDeltaTime);
		if (phase == 0)
		{
			return;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance == (Object)null || !instance.HasBody)
		{
			if (phase != 5)
			{
				StopAll(restore: true);
			}
			return;
		}
		if (Active && (Object)(object)panel == (Object)null)
		{
			BuildOrgasmButton();
		}
		float unscaledTime = Time.unscaledTime;
		float num = unscaledTime - startTime;
		if (phase == 1 && num >= ActingAt)
		{
			BeginActing();
		}
		if (phase == 2 && num >= FasterAt)
		{
			phase = 3;
			Say(Loc.T("（动作快起来，喘得接不上话）……快了、快了……"));
		}
		if (phase == 3 && num >= PeakAt)
		{
			phase = 4;
			Say(Loc.T("（眼神散掉，嘴角松开）……脑子……空了……好舒服……"));
		}
		if (phase >= 2)
		{
			ApplyPose();
			if (phase != 5)
			{
				Zoom(Time.unscaledDeltaTime);
			}
			if (phase != 5)
			{
				Pant(unscaledTime, (phase >= 4) ? 0.55f : 0.38f, (phase >= 4) ? 1.15f : 1f);
			}
		}
		if (phase >= 4)
		{
			nextHeal -= Time.unscaledDeltaTime;
			if (nextHeal <= 0f)
			{
				nextHeal = 0.5f;
				Heal(0.5f);
			}
		}
		if (phase >= 2 && phase != 5 && unscaledTime >= nextBeat)
		{
			nextBeat = unscaledTime + BeatEvery;
			Beat();
		}
		if (phase != 5 && unscaledTime >= endAt)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogInfo((object)"[Intimate] Max duration reached, completing automatically.");
			}
			StopAll(restore: true);
		}
		else if (phase == 5 && lingerUntil > 0f && unscaledTime >= lingerUntil)
		{
			EnterDebt();
			StopAll(restore: true);
		}
		else if ((Object)(object)hintText != (Object)null && phase != 5)
		{
			int num2 = Mathf.Max(0, Mathf.CeilToInt(endAt - unscaledTime));
			((TMP_Text)hintText).text = Loc.T("已持续 ") + num2 / 60 + ":" + (num2 % 60).ToString("00") + Loc.T(" 剩余 ；点「高潮」结束");
		}
	}
}
