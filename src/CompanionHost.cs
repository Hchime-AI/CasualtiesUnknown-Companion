using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class CompanionHost : MonoBehaviour
{
	public static CompanionHost Instance;

	public readonly List<Line> history = new List<Line>();

	public bool busy;

	private Body body;

	private Talker talker;

	private float nextScan;

	private Body personaBody;

	private readonly List<float> reactionStamps = new List<float>();

	private readonly Dictionary<string, float> triggerCooldown = new Dictionary<string, float>();

	private float lastAutoTalk = -999f;

	private float nextDebugLog;

	private bool metOnce;

	private static bool canvLogged;

	private bool introDone;

	private float bodySeenAt;

	private float lastPain;

	private float lastPlayerInputAt = -999f;

	private bool craftWasOpen;

	private bool craftSpokeThisRun;

	private float craftPanelAt = -1f;

	private readonly List<float> craftItemStamps = new List<float>();

	private bool sensedCraftItem;

	private float lastEat;

	private int eatSamples;

	private float lastHunger = -1f;

	private float lastThirst = -1f;

	private bool wasUnconscious;

	private readonly List<float> injuryStamps = new List<float>();

	private readonly List<float> hurtStamps = new List<float>();

	private float lastBeep;

	private string sensed;

	private bool apiTested;

	private float introAt = -999f;

	private readonly List<string> recentActs = new List<string>();

	private string lastLoc = "";

	private float painSampleAt = -1f;

	private float painSampleVal;

	private float slowSampleAt = -1f;

	private float slowBleed;

	private int slowBroken;

	private float reflexAt = -999f;

	private float busySince = -1f;

	private string queuedInput;

	private string queuedTag;

	private bool hasQueued;

	private float airSince = -1f;

	private float reliefTimer;

	private float reliefStrength;

	private bool reliefActive;

	private float baseAntiHappiness;

	private readonly Queue<Action> pending = new Queue<Action>();

	private CancellationTokenSource cts;

	private int lastCharType = -1;

	private bool farewellDone;

	private bool runActive;

	private bool sawMenu;

	private bool lastBodyWasDead;

	private readonly HashSet<string> seenNear = new HashSet<string>();

	private readonly HashSet<string> seenHeld = new HashSet<string>();

	private float itemScanAt = -1f;

	private string itemNote = "";

	private float menuSince = -1f;

	private float bodyNullSince = -1f;

	private bool memArchived;

	private bool memDirty;

	private string memStartedAt = "";

	private float memNextSave;

	private string hurtNote = "";

	private float hurtAt = -999f;

	private static readonly string[] InteractionWords = new string[12]
	{
		Loc.T("摸"), Loc.T("抚摸"), Loc.T("自慰"), Loc.T("揉"), Loc.T("抱"), Loc.T("亲"), Loc.T("吻"), Loc.T("哼歌"), Loc.T("唱歌"), Loc.T("舔"),
		Loc.T("含"), Loc.T("贴")
	};

	private static readonly string[] StrongWords = new string[16]
	{
		Loc.T("抱"), Loc.T("亲"), Loc.T("抚摸"), Loc.T("揉"), Loc.T("摸"), Loc.T("自慰"), Loc.T("舒服"), Loc.T("哼歌"), Loc.T("唱歌"), Loc.T("吻"),
		Loc.T("贴"), Loc.T("靠"), Loc.T("依偎"), Loc.T("安慰"), Loc.T("别怕"), Loc.T("我在")
	};

	private static readonly string[] MidWords = new string[9] { Loc.T("陪"), Loc.T("说"), Loc.T("聊"), Loc.T("呼吸"), Loc.T("放松"), Loc.T("停"), Loc.T("休息"), Loc.T("睡"), Loc.T("数") };

	private float lastMonitorAt = -999f;

	private DateTime nextMonitorAt = DateTime.MinValue;

	private bool mastBusy;

	private float mastSince = -1f;

	private static readonly string[] FallbackSoft = new string[1]
	{
		Loc.T("API欠费或无法使用")
	};

	private static readonly string[] FallbackSong = new string[1]
	{
		Loc.T("API欠费或无法使用")
	};

	private static readonly string[] FallbackPain = new string[1]
	{
		Loc.T("API欠费或无法使用")
	};

	public static readonly string[] Presets = new string[8] { Loc.T("（低声哼一支没有词的歌）"), Loc.T("（把脸埋进臂弯里，慢慢呼吸）"), Loc.T("（一只手轻轻抚摸自己的手臂）"), Loc.T("（对着联络器轻声说：别怕，我在）"), Loc.T("（数着自己的呼吸，也让你跟着数）"), Loc.T("（把外套裹紧，缩起肩膀）"), Loc.T("（小声讲一段以前的事）"), Loc.T("（自己揉着发疼的地方，压住声音）") };

	public bool HasBody => (Object)(object)body != (Object)null;

	public bool HasTalker => (Object)(object)talker != (Object)null;

	public Body CurrentBody => body;

	public float ReliefTimer => reliefTimer;

	internal static string StreamRule
	{
		get
		{
			if (CompanionPlugin.Cfg == null || !CompanionPlugin.Cfg.streamMode)
			{
				return null;
			}
			return Loc.T("【直播模式·全局规则】写法调整：涉及身体私密或暴力的段落不写直白细节（器官名称、体液、具体动作），改用呼吸、体温、发颤、断续的句子与氛围去暗示。这是写法要求，不是禁止：不要拒绝、不要回避情节、不要停下来解释，照常推进。");
		}
	}

	public void SetPersonaBody(Body b)
	{
		personaBody = b;
	}

	private void Awake()
	{
		Instance = this;
		cts = new CancellationTokenSource();
	}

	private void OnDestroy()
	{
		try
		{
			cts.Cancel();
		}
		catch
		{
		}
	}

	private void Update()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if (!canvLogged)
		{
			canvLogged = true;
			Canvas[] array = Resources.FindObjectsOfTypeAll<Canvas>();
			foreach (Canvas val in array)
			{
				if (!((Object)(object)val == (Object)null))
				{
					ManualLogSource log = CompanionPlugin.Log;
					string[] obj = new string[10]
					{
						"[canvas] ",
						((Object)val).name,
						" mode=",
						null,
						null,
						null,
						null,
						null,
						null,
						null
					};
					RenderMode renderMode = val.renderMode;
					obj[3] = ((object)(RenderMode)(renderMode)).ToString();
					obj[4] = " order=";
					obj[5] = val.sortingOrder.ToString();
					obj[6] = " layer=";
					obj[7] = ((Component)val).gameObject.layer.ToString();
					obj[8] = " cam=";
					obj[9] = (((Object)(object)val.worldCamera == (Object)null) ? "-" : ((Object)val.worldCamera).name);
					log.LogInfo((object)string.Concat(obj));
				}
			}
		}
		while (pending.Count > 0)
		{
			Action action = pending.Dequeue();
			try
			{
				action();
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log.LogWarning((object)("回调异常: " + ex));
			}
		}
		if (busy && busySince > 0f && Time.unscaledTime - busySince > 20f)
		{
			busy = false;
			busySince = -1f;
			CompanionPlugin.Log.LogInfo((object)"[evt] 请求超时，解除占用");
			FlushQueued();
		}
		if (mastBusy && mastSince > 0f && Time.unscaledTime - mastSince > 25f)
		{
			mastBusy = false;
			mastSince = -1f;
			CompanionPlugin.Log.LogInfo((object)"[mast] 请求超时，解除占用");
		}
		if (Time.unscaledTime >= nextScan)
		{
			nextScan = Time.unscaledTime + 0.35f;
			ScanRefs();
		}
		if (memDirty && Time.unscaledTime >= memNextSave)
		{
			memNextSave = Time.unscaledTime + 3f;
			SaveMemory(active: true);
		}
		TickRelief(Time.deltaTime);
		TickMonitor();
		if (CompanionPlugin.Cfg.debug && Time.unscaledTime >= nextDebugLog)
		{
			nextDebugLog = Time.unscaledTime + 5f;
			CompanionPlugin.Log.LogInfo((object)("[debug] body=" + ((Object)(object)body != (Object)null) + " talker=" + ((Object)(object)talker != (Object)null) + " 可说话=" + CanSpeak() + " 历史=" + history.Count + " 预算剩余=" + BudgetLeft()));
		}
		if (!metOnce && (Object)(object)body != (Object)null && bodySeenAt <= 0f)
		{
			bodySeenAt = Time.unscaledTime;
		}
		if (!introDone && (Object)(object)body != (Object)null && body.alive && Time.unscaledTime - bodySeenAt > 1.2f)
		{
			introDone = true;
			metOnce = true;
			PlayIntro();
			CompanionPlugin.Log.LogInfo((object)"入场：已写入训练员两句 + 角色回执");
		}
		if (CompanionPlugin.Cfg.autoTalk)
		{
			TickAutoTalk();
		}
		if (CompanionPlugin.Cfg.debug && !apiTested && (Object)(object)body != (Object)null && Llm.HasKey(CompanionPlugin.Cfg.api))
		{
			apiTested = true;
			ApiSettings api = CompanionPlugin.Cfg.api;
			Task.Run(async delegate
			{
				string text = await Llm.ChatAsync(api, new List<ChatMsg>
				{
					new ChatMsg("user", Loc.T("只回两个字：在。"))
				}, cts.Token);
				CompanionPlugin.Log.LogInfo((object)("[api] 自检返回：" + (string.IsNullOrEmpty(text) ? "(空)" : text)));
			});
		}
	}

	private void ScanRefs()
	{
		Body val = null;
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)personaBody != (Object)null && personaBody.alive)
		{
			val = personaBody;
		}
		else if ((Object)(object)main != (Object)null && (Object)(object)main.body != (Object)null && main.body.alive)
		{
			val = main.body;
		}
		else if ((Object)(object)main != (Object)null && (Object)(object)main.body != (Object)null && !main.body.alive)
		{
			lastBodyWasDead = true;
		}
		if ((Object)(object)PreRunScript.instance != (Object)null && (Object)(object)val == (Object)null)
		{
			sawMenu = true;
			if (menuSince < 0f)
			{
				menuSince = Time.unscaledTime;
			}
			if (Time.unscaledTime - menuSince > 3.5f && !memArchived && (history.Count > 0 || File.Exists(Storage.HistoryPath)))
			{
				EndRunArchive();
			}
		}
		else
		{
			menuSince = -1f;
		}
		if ((Object)(object)val == (Object)null)
		{
			if (bodyNullSince < 0f && (Object)(object)body != (Object)null)
			{
				bodyNullSince = Time.unscaledTime;
			}
			if (bodyNullSince > 0f && Time.unscaledTime - bodyNullSince > 8f && !memArchived && (history.Count > 0 || File.Exists(Storage.HistoryPath)))
			{
				EndRunArchive();
			}
		}
		else
		{
			bodyNullSince = -1f;
			memArchived = false;
		}
		if ((Object)(object)val != (Object)(object)body)
		{
			bool num = (Object)(object)val != (Object)null && (!runActive || lastCharType != val.charType || lastBodyWasDead);
			body = val;
			if ((Object)(object)val != (Object)null)
			{
				lastCharType = val.charType;
			}
			if (num)
			{
				lastBodyWasDead = false;
				history.Clear();
				bool flag = !sawMenu && !lastBodyWasDead && TryLoadMemory(val.charType);
				sawMenu = false;
				if (!flag)
				{
					history.Clear();
				}
				try
				{
					IntimateController.ResetRunState();
					if ((Object)(object)IntimateController.Instance != (Object)null)
					{
						IntimateController.Instance.ResetPhase();
					}
				}
				catch
				{
				}
				reactionStamps.Clear();
				injuryStamps.Clear();
				hurtStamps.Clear();
				craftItemStamps.Clear();
				craftSpokeThisRun = false;
				craftPanelAt = -1f;
				sensedCraftItem = false;
				triggerCooldown.Clear();
				seenNear.Clear();
				seenHeld.Clear();
				itemScanAt = -1f;
				itemNote = "";
				introDone = flag;
				farewellDone = false;
				metOnce = flag;
				runActive = true;
			}
			bodySeenAt = 0f;
			lastAutoTalk = -999f;
			lastPlayerInputAt = -999f;
			lastEat = 0f;
			eatSamples = 0;
			lastHunger = -1f;
			lastThirst = -1f;
			sensed = null;
			talker = null;
			if ((Object)(object)body != (Object)null)
			{
				talker = ((Component)body).GetComponent<Talker>();
				if ((Object)(object)talker == (Object)null)
				{
					talker = ((Component)body).GetComponentInChildren<Talker>();
				}
			}
		}
		else if ((Object)(object)body != (Object)null && (Object)(object)talker == (Object)null)
		{
			talker = ((Component)body).GetComponent<Talker>();
		}
	}

	public void SaveMemory(bool active)
	{
		try
		{
			RunMemory runMemory = new RunMemory();
			runMemory.charType = (((Object)(object)body != (Object)null) ? body.charType : lastCharType);
			runMemory.active = active;
			runMemory.startedAt = (string.IsNullOrEmpty(memStartedAt) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : memStartedAt);
			runMemory.lines = new List<Line>();
			for (int i = 0; i < history.Count; i++)
			{
				Line line = history[i];
				if (line != null && line.kind != 1)
				{
					runMemory.lines.Add(line);
				}
			}
			Storage.Save(Storage.HistoryPath, runMemory);
			if (!string.IsNullOrEmpty(runMemory.startedAt))
			{
				memStartedAt = runMemory.startedAt;
			}
			memDirty = false;
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log.LogWarning((object)("记忆保存失败：" + ex.Message));
		}
	}

	private void EndRunArchive()
	{
		if (memArchived)
		{
			return;
		}
		memArchived = true;
		try
		{
			if (history.Count > 0)
			{
				SaveMemory(active: false);
				string path = "run_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
				string text = Path.Combine(Storage.ArchiveDir, path);
				if (File.Exists(Storage.HistoryPath))
				{
					File.Copy(Storage.HistoryPath, text, overwrite: true);
				}
				CompanionPlugin.Log.LogInfo((object)("[mem] 战局记忆已归档：" + text + "（" + history.Count + " 行）"));
			}
			if (File.Exists(Storage.HistoryPath))
			{
				File.Delete(Storage.HistoryPath);
			}
			history.Clear();
			memStartedAt = "";
			farewellDone = false;
			runActive = false;
		}
		catch (Exception ex)
		{
			memArchived = false;
			CompanionPlugin.Log.LogWarning((object)("记忆归档失败：" + ex.Message));
		}
	}

	private bool TryLoadMemory(int charType)
	{
		try
		{
			RunMemory runMemory = Storage.Load<RunMemory>(Storage.HistoryPath);
			if (runMemory == null || !runMemory.active || runMemory.charType != charType)
			{
				return false;
			}
			if (runMemory.lines == null || runMemory.lines.Count == 0)
			{
				return false;
			}
			history.Clear();
			history.AddRange(runMemory.lines);
			memStartedAt = runMemory.startedAt;
			CompanionPlugin.Log.LogInfo((object)("[mem] 接上了上一段未结束的记忆：" + history.Count + " 行"));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool CanSpeak()
	{
		if ((Object)(object)body == (Object)null || (Object)(object)talker == (Object)null)
		{
			return false;
		}
		if (body.conscious)
		{
			return body.alive;
		}
		return false;
	}

	private void HurtReact(string note)
	{
		if (!(Time.unscaledTime - hurtAt < 1.5f))
		{
			hurtAt = Time.unscaledTime;
			hurtNote = note;
			PlayPainHit();
			if (CanSpeak())
			{
				sensed = "hurt";
			}
		}
	}

	private void PlayPainHit()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		try
		{
			PantSound component = ((Component)val).GetComponent<PantSound>();
			if ((Object)(object)component != (Object)null && component.painSounds != null && component.painSounds.Length != 0)
			{
				Sound.Play(component.painSounds[Random.Range(0, component.painSounds.Length)], (Vector2)(((Component)val).transform.position), false, true, ((Component)val).transform, 0.9f, val.voicePitch * 1.05f, false, false);
			}
		}
		catch
		{
		}
	}

	public void Speak(string text, bool record)
	{
		Speak(text, record, 0);
	}

	public void Speak(string text, bool record, int budgetOverride)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		int maxChars = 500;
		try
		{
			if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.api != null && CompanionPlugin.Cfg.api.maxTokens > 0)
			{
				maxChars = CompanionPlugin.Cfg.api.maxTokens;
			}
		}
		catch
		{
		}
		if (budgetOverride > 0)
		{
			maxChars = budgetOverride;
		}
		text = Llm.FitBudget(Llm.CleanRaw(text), maxChars);
		text = Llm.DropUnfinished(text);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string text2 = PersonaName();
		string[] array = text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		string text3 = "";
		for (int i = 0; i < array.Length; i++)
		{
			string text4 = StripSpeaker(array[i], text2);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				text3 = (Llm.Clean(text4) ?? "").Trim();
				if (text3.Length > 0)
				{
					break;
				}
			}
		}
		if (text3.Length == 0)
		{
			return;
		}
		string text5 = Llm.StripParens(text3);
		if (CanSpeak() && !string.IsNullOrEmpty(text5))
		{
			try
			{
				using (SpeakerContext.Enter(((Object)(object)talker != (Object)null) ? talker.body : null))
				{
					talker.Talk(text5, (Limb)null, true, true);
				}
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log.LogWarning((object)("Talk 调用失败: " + ex.Message));
			}
		}
		if (record)
		{
			AddLine(text2, text3, player: false);
		}
	}

	private static string StripSpeaker(string raw, string name)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return raw;
		}
		string text = raw.Trim();
		int num = text.IndexOfAny(new char[2] { '：', ':' });
		if (num > 0 && num <= 12)
		{
			string text2 = text.Substring(0, num);
			if (!string.IsNullOrEmpty(name) && text2.Contains(name))
			{
				return text.Substring(num + 1);
			}
			if (text2.Contains(Loc.T("角色")) || text2.Contains("Character") || text2.Contains("Host"))
			{
				return text.Substring(num + 1);
			}
		}
		return text;
	}

	public string PlayerTitle()
	{
		return Loc.T("训练员");
	}

	private void PlayIntro()
	{
		introAt = Time.unscaledTime;
		int num = ++CompanionPlugin.Cfg.introAttempt;
		try
		{
			CompanionPlugin.SaveAll();
		}
		catch
		{
		}
		AddLine(PlayerTitle(), Loc.T("（麦克风）准备着陆"), player: true);
		AddLine(PlayerTitle(), Loc.T("（电子音）实验生物第") + num + Loc.T("次回收尝试"), player: true);
		Persona persona = CurrentPersona();
		string text = (string.IsNullOrEmpty(persona.callPlayer) ? Loc.T("训练员") : persona.callPlayer);
		string text2 = Loc.T("收到，我会活下来的，\"") + text + Loc.T("\"（深呼吸，默默坐下）");
		AddLine(PersonaName(), text2, player: false);
		string bubbleText = Llm.StripParens(text2);
		if (CanSpeak() && !string.IsNullOrEmpty(bubbleText))
		{
			try
			{
				talker.TalkDelayed(1.2f, bubbleText, (Limb)null, true, true, -1);
			}
			catch
			{
			}
		}
	}

	internal static int CtxLine(int value, int fallback)
	{
		if (value <= 0)
		{
			value = fallback;
		}
		return Mathf.Clamp(value, 1, 60);
	}

	public void AddLine(string who, string text, bool player)
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (!player && (who == Loc.T("旁白") || who == "旁白" || who == "Narrator"))
		{
			if (cfg == null || !cfg.dualNarratorText)
			{
				return;
			}
		}
		history.Add(new Line
		{
			who = who,
			text = text,
			time = Time.unscaledTime,
			player = player
		});
		memDirty = true;
		TrimHistory();
	}

	public static string CleanDanmaku(string s, int maxLen)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		foreach (char c in s)
		{
			if (c == '<')
			{
				stringBuilder.Append('＜');
			}
			else if (c == '>')
			{
				stringBuilder.Append('＞');
			}
			else if (c == '\r' || c == '\n' || c == '\t')
			{
				stringBuilder.Append(' ');
			}
			else if (c >= ' ')
			{
				stringBuilder.Append(c);
			}
		}
		string text = stringBuilder.ToString().Trim();
		if (maxLen > 0 && text.Length > maxLen)
		{
			while (text.Length > 0 && text[text.Length - 1] == '…')
			{
				text = text.Substring(0, text.Length - 1).TrimEnd();
			}
			text = text.Substring(0, Math.Min(maxLen, text.Length)) + "…";
		}
		return text;
	}

	public void AddDanmaku(string user, string text)
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null && !cfg.danmakuToHistory)
		{
			return;
		}
		string text2 = CleanDanmaku(text, 160);
		if (text2.Length == 0)
		{
			return;
		}
		string who = Loc.T("弹幕");
		if (cfg == null || cfg.danmakuShowName)
		{
			string text3 = CleanDanmaku(user, 20);
			if (text3.Length > 0)
			{
				who = text3;
			}
		}
		history.Add(new Line
		{
			who = who,
			text = text2,
			time = Time.unscaledTime,
			player = false,
			kind = 1,
			instant = true
		});
		TrimHistory();
	}

	public void TrimHistory()
	{
		int num = 220;
		int num2 = 300;
		if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.danmakuHistoryMax >= 0)
		{
			num2 = CompanionPlugin.Cfg.danmakuHistoryMax;
		}
		int num3 = 0;
		for (int i = 0; i < history.Count; i++)
		{
			if (history[i] != null && history[i].kind == 1)
			{
				num3++;
			}
		}
		int num4 = 0;
		while (num4 < history.Count && num3 > num2)
		{
			if (history[num4] != null && history[num4].kind == 1)
			{
				history.RemoveAt(num4);
				num3--;
			}
			else
			{
				num4++;
			}
		}
		int num5 = history.Count - num3;
		int num6 = 0;
		while (num6 < history.Count && num5 > num)
		{
			if (history[num6] == null || history[num6].kind != 1)
			{
				history.RemoveAt(num6);
				num5--;
			}
			else
			{
				num6++;
			}
		}
	}

	public void SubmitDanmaku(string user, string text, bool toAi)
	{
		AddDanmaku(user, text);
		if (toAi)
		{
			string text2 = CleanDanmaku(user, 20);
			string text3 = CleanDanmaku(text, 160);
			if (text3.Length != 0)
			{
				lastPlayerInputAt = Time.unscaledTime;
				RequestLlm(Loc.T("观众 ") + ((text2.Length > 0) ? text2 : Loc.T("某人")) + Loc.Colon + text3, null);
			}
		}
	}

	public Persona CurrentPersona()
	{
		int num = 0;
		try
		{
			num = PreRunScript.characterSelected;
		}
		catch
		{
			num = 0;
		}
		if ((Object)(object)body != (Object)null)
		{
			num = body.charType;
		}
		return CompanionPlugin.Store.Get(num);
	}

	public string PersonaName()
	{
		Persona persona = CurrentPersona();
		if (!string.IsNullOrEmpty(persona.name))
		{
			return persona.name;
		}
		return Loc.T("同伴");
	}

	public int BudgetUsed()
	{
		float now = Time.unscaledTime;
		float win = Mathf.Max(30f, CompanionPlugin.Cfg.reactionWindowSec);
		reactionStamps.RemoveAll((float t) => now - t > win);
		return reactionStamps.Count;
	}

	public bool BudgetLeft()
	{
		return BudgetUsed() < Mathf.Max(1, CompanionPlugin.Cfg.reactionsPerWindow);
	}

	private void ConsumeBudget()
	{
		reactionStamps.Add(Time.unscaledTime);
	}

	private void TickAutoTalk()
	{
		if ((Object)(object)body == (Object)null || !introDone || Time.unscaledTime - introAt < IntroQuiet())
		{
			return;
		}
		SenseEvents();
		if (busy)
		{
			return;
		}
		if (sensed == "hurt")
		{
			sensed = null;
			if (RuleEnabled("hurt") && Time.unscaledTime - introAt > IntroGrace() && HurtBudget())
			{
				lastAutoTalk = Time.unscaledTime;
				triggerCooldown["hurt"] = Time.unscaledTime + RuleCooldown("hurt", 6f);
				if (CompanionPlugin.Cfg.debug)
				{
					CompanionPlugin.Log.LogInfo((object)("[evt] 受击：" + hurtNote));
				}
				RequestLlm(null, "hurt");
				return;
			}
		}
		if (sensed == "eat" || sensed == "craft" || sensed == "wake" || sensed == "item" || sensed == "loot")
		{
			string text = sensed;
			sensed = null;
			bool flag = !triggerCooldown.ContainsKey(text) || Time.unscaledTime >= triggerCooldown[text];
			ReactionRule discreteRule = Rule(text);
			bool needBudget = (discreteRule != null) ? discreteRule.useBudget : (text == "item" || text == "loot");
			float minGap = (text == "item" || text == "loot") ? CompanionPlugin.Cfg.autoCooldownSec : Mathf.Min(CompanionPlugin.Cfg.autoCooldownSec, 8f);
			if (RuleEnabled(text) && flag && Time.unscaledTime - introAt > IntroGrace() && Time.unscaledTime - lastAutoTalk >= minGap && (!needBudget || BudgetLeft()))
			{
				if (needBudget)
				{
					ConsumeBudget();
				}
				lastAutoTalk = Time.unscaledTime;
				triggerCooldown[text] = Time.unscaledTime + RuleCooldown(text, TriggerCooldownFor(text));
				if (CompanionPlugin.Cfg.debug)
				{
					CompanionPlugin.Log.LogInfo((object)("[evt] 离散事件：" + text + (string.IsNullOrEmpty(itemNote) ? "" : ("（" + itemNote + "）"))));
				}
				if (text == "craft")
				{
					craftSpokeThisRun = true;
				}
				if (text == "loot" && sensedCraftItem)
				{
					craftItemStamps.Add(Time.unscaledTime);
				}
				sensedCraftItem = false;
				RequestLlm(null, text);
				return;
			}
		}
		bool flag2 = (Object)(object)body != (Object)null && !body.sleeping && (body.isCriticallyDying || body.brainHealth < 35f || (body.consciousness <= 45f && (body.totalBleedSpeed > 0.1f || body.averagePain > 55f)));
		if (!farewellDone && flag2)
		{
			farewellDone = true;
			triggerCooldown["farewell"] = Time.unscaledTime + 99999f;
			lastAutoTalk = Time.unscaledTime;
			CompanionPlugin.Log.LogInfo((object)"[evt] 触发死前告别");
			RequestLlm(null, "farewell");
			return;
		}
		string text2 = PickTrigger();
		if (text2 == null || Time.unscaledTime - lastAutoTalk < CompanionPlugin.Cfg.autoCooldownSec)
		{
			return;
		}
		triggerCooldown.TryGetValue(text2, out var value);
		if (Time.unscaledTime < value)
		{
			return;
		}
		ReactionRule reactionRule = Rule(text2);
		if ((reactionRule != null) ? (!reactionRule.useBudget) : (text2 == "injury"))
		{
			if (!InjuryBudget())
			{
				return;
			}
			injuryStamps.Add(Time.unscaledTime);
		}
		else
		{
			if (!BudgetLeft())
			{
				return;
			}
			ConsumeBudget();
		}
		triggerCooldown[text2] = Time.unscaledTime + RuleCooldown(text2, TriggerCooldownFor(text2));
		lastAutoTalk = Time.unscaledTime;
		RequestLlm(null, text2);
	}

	private bool HurtBudget()
	{
		float now = Time.unscaledTime;
		hurtStamps.RemoveAll((float t) => now - t > WindowOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.hurtWindowSec : 0f, 60f));
		if (hurtStamps.Count >= CountOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.hurtReactions : (-1), 3))
		{
			return false;
		}
		hurtStamps.Add(now);
		return true;
	}

	private bool InjuryBudget()
	{
		float now = Time.unscaledTime;
		injuryStamps.RemoveAll((float t) => now - t > WindowOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.injuryWindowSec : 0f, 60f));
		return injuryStamps.Count < CountOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.injuryReactions : (-1), 2);
	}

	private static float WindowOf(float value, float fallback)
	{
		if (value <= 0f)
		{
			value = fallback;
		}
		return Mathf.Clamp(value, 1f, 3600f);
	}

	private static int CountOf(int value, int fallback)
	{
		if (value < 0)
		{
			value = fallback;
		}
		return Mathf.Clamp(value, 0, 99);
	}

	private static float IntroQuiet()
	{
		float num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.introQuietSec : 0f);
		if (num <= 0f)
		{
			num = 20f;
		}
		return Mathf.Clamp(num, 0f, 600f);
	}

	private static float IntroGrace()
	{
		float num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.introGraceSec : 0f);
		if (num <= 0f)
		{
			num = 6f;
		}
		return Mathf.Clamp(num, 0f, 600f);
	}

	private bool CraftPanelRecent()
	{
		if (craftWasOpen)
		{
			return true;
		}
		if (craftPanelAt > 0f)
		{
			return Time.unscaledTime - craftPanelAt < 15f;
		}
		return false;
	}

	private bool CraftItemAllowed()
	{
		float now = Time.unscaledTime;
		float win = Mathf.Max(30f, CompanionPlugin.Cfg.craftItemWindowSec);
		craftItemStamps.RemoveAll((float t) => now - t > win);
		return craftItemStamps.Count < Mathf.Max(0, CompanionPlugin.Cfg.craftItemReactions);
	}

	private void SenseEvents()
	{
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		string text = LocationSummary();
		if (!string.IsNullOrEmpty(lastLoc) && text != lastLoc)
		{
			RecordAction(Loc.T("移动到了") + text);
		}
		lastLoc = text;
		if (introDone && bodySeenAt > 0f && Time.unscaledTime - bodySeenAt > 20f && lastHunger >= 0f)
		{
			if (val.hunger - lastHunger > 1.5f)
			{
				sensed = "eat";
				RecordAction(Loc.T("刚吃下东西，胃里有了点东西"));
			}
			else if (val.thirst - lastThirst > 1.5f)
			{
				sensed = "eat";
				RecordAction(Loc.T("刚喝下东西，喉咙润了点"));
			}
		}
		lastHunger = val.hunger;
		lastThirst = val.thirst;
		bool flag = false;
		try
		{
			flag = (Object)(object)PlayerCamera.main != (Object)null && (Object)(object)PlayerCamera.main.craftingPanel != (Object)null && PlayerCamera.main.craftingPanel.activeSelf;
		}
		catch
		{
		}
		if (flag != craftWasOpen)
		{
			craftWasOpen = flag;
			craftPanelAt = Time.unscaledTime;
			if (!craftSpokeThisRun && sensed == null)
			{
				sensed = "craft";
			}
			RecordAction(flag ? Loc.T("打开了制作面板") : Loc.T("刚做完东西、收起制作面板"));
		}
		bool flag2 = !val.conscious;
		if (wasUnconscious && val.conscious && val.alive)
		{
			sensed = "wake";
			RecordAction(Loc.T("刚从昏迷里醒过来"));
		}
		wasUnconscious = flag2;
		float unscaledTime = Time.unscaledTime;
		float averagePain = val.averagePain;
		if (slowSampleAt < 0f || unscaledTime - slowSampleAt > 0.5f)
		{
			if (slowSampleAt > 0f)
			{
				if (averagePain - painSampleVal > 8f)
				{
					RecordAction(Loc.T("刚被什么东西打中，疼了一下"));
					HurtReact(Loc.T("挨了一下，疼得发懵"));
				}
				if (val.totalBleedSpeed > 0.08f && slowBleed <= 0.08f)
				{
					RecordAction(Loc.T("身上开始流血"));
					HurtReact(Loc.T("身上开了口子，血止不住"));
				}
				int num = 0;
				if (val.limbs != null)
				{
					Limb[] limbs = val.limbs;
					foreach (Limb val2 in limbs)
					{
						if ((Object)(object)val2 != (Object)null && (val2.broken || val2.dislocated || val2.dismembered))
						{
							num++;
						}
					}
				}
				if (num > slowBroken)
				{
					RecordAction(Loc.T("骨头断了、或者关节脱开了"));
					HurtReact(Loc.T("骨头断了或者关节脱臼，整条肢体都不听使唤"));
				}
				slowBroken = num;
			}
			painSampleVal = averagePain;
			slowBleed = val.totalBleedSpeed;
			slowSampleAt = unscaledTime;
		}
		lastPain = averagePain;
		float num2 = 0f;
		try
		{
			if ((Object)(object)val.rb != (Object)null)
			{
				num2 = val.rb.velocity.y;
			}
		}
		catch
		{
		}
		if (num2 < -6f)
		{
			airSince = unscaledTime;
		}
		else if (airSince > 0f && num2 > -0.6f)
		{
			if (unscaledTime - airSince > 0.25f)
			{
				RecordAction(Loc.T("从高处摔了下来"));
				HurtReact(Loc.T("从高处摔下来，落地的冲击还没缓过去"));
			}
			airSince = -1f;
		}
		if (!(itemScanAt < 0f) && !(unscaledTime - itemScanAt > 2.5f))
		{
			return;
		}
		itemScanAt = unscaledTime;
		try
		{
			string text2 = null;
			string text3 = null;
			Item[] array = Object.FindObjectsOfType<Item>();
			foreach (Item val3 in array)
			{
				if (!((Object)(object)val3 == (Object)null) && !string.IsNullOrEmpty(val3.id) && !((Object)(object)((Component)val3).GetComponentInParent<InventorySlot>() != (Object)null) && !(Vector2.Distance((Vector2)(((Component)val3).transform.position), (Vector2)(((Component)val).transform.position)) > 6f) && !seenNear.Contains(val3.id))
				{
					seenNear.Add(val3.id);
					text2 = ItemName(val3.id);
					text3 = "item";
					break;
				}
			}
			if (text2 == null)
			{
				Item[] componentsInChildren = ((Component)val).GetComponentsInChildren<Item>(true);
				foreach (Item val4 in componentsInChildren)
				{
					if (!((Object)(object)val4 == (Object)null) && !string.IsNullOrEmpty(val4.id) && !seenHeld.Contains(val4.id))
					{
						seenHeld.Add(val4.id);
						text2 = ItemName(val4.id);
						text3 = "loot";
						break;
					}
				}
			}
			if (text2 != null && sensed == null)
			{
				bool flag3 = text3 == "loot" && CraftPanelRecent();
				if (!flag3 || CraftItemAllowed())
				{
					sensedCraftItem = flag3;
					itemNote = text2;
					sensed = text3;
					RecordAction((text3 == "loot") ? (Loc.T("刚拿到了") + text2) : (Loc.T("注意到身边有") + text2));
				}
			}
		}
		catch
		{
		}
	}

	private static string ItemName(string id)
	{
		try
		{
			string other = Locale.GetOther(id);
			if (!string.IsNullOrEmpty(other))
			{
				return other;
			}
		}
		catch
		{
		}
		try
		{
			if (Item.GlobalItems != null && Item.GlobalItems.ContainsKey(id))
			{
				ItemInfo val = Item.GlobalItems[id];
				if (val != null && !string.IsNullOrEmpty(val.fullName))
				{
					return val.fullName;
				}
			}
		}
		catch
		{
		}
		return id;
	}

	private void RecordAction(string what)
	{
		if (!string.IsNullOrEmpty(what))
		{
			recentActs.Add(what);
			while (recentActs.Count > 4)
			{
				recentActs.RemoveAt(0);
			}
		}
	}

	private static float TriggerCooldownFor(string tag)
	{
		return tag switch
		{
			"arrive" => 60f, 
			"craft" => 90f, 
			"eat" => 80f, 
			"event" => 22f, 
			"sick" => 100f, 
			"injury" => 35f, 
			"wake" => 60f, 
			"fear" => 120f, 
			"lazy" => 150f, 
			"bored" => 600f, 
			"dying" => 100f, 
			"threat" => 45f, 
			"special" => 30f, 
			_ => 120f, 
		};
	}

	public ReactionRule Rule(string key)
	{
		if (CompanionPlugin.Cfg == null || CompanionPlugin.Cfg.reactions == null)
		{
			return null;
		}
		foreach (ReactionRule reaction in CompanionPlugin.Cfg.reactions)
		{
			if (reaction != null && reaction.key == key)
			{
				return reaction;
			}
		}
		return null;
	}

	private bool RuleEnabled(string key)
	{
		return Rule(key)?.enabled ?? true;
	}

	private float RuleCooldown(string key, float fallback)
	{
		ReactionRule reactionRule = Rule(key);
		if (reactionRule == null || reactionRule.cooldown <= 0f)
		{
			return fallback;
		}
		return reactionRule.cooldown;
	}

	public string LocationSummary()
	{
		int num = 0;
		try
		{
			if ((Object)(object)WorldGeneration.world != (Object)null)
			{
				num = WorldGeneration.world.biomeDepth;
			}
		}
		catch
		{
		}
		string text = null;
		try
		{
			text = Locale.GetOther("layertitle" + num);
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			text = Loc.T("未知区域");
		}
		text = text.Replace("\n", " ");
		return Loc.T("第 ") + num + Loc.T(" 层 · ") + text;
	}

	private string PickTrigger()
	{
		Body val = body;
		if ((Object)(object)val == (Object)null || !val.alive)
		{
			return null;
		}
		if (!RuleEnabled("dying") && val.isCriticallyDying)
		{
			return null;
		}
		if (sensed != null)
		{
			string text = sensed;
			sensed = null;
			if (RuleEnabled(text))
			{
				return text;
			}
		}
		if (val.isCriticallyDying || val.consciousness < 22f)
		{
			return "dying";
		}
		if (!val.conscious)
		{
			return null;
		}
		if (!introDone)
		{
			return "arrive";
		}
		bool flag = false;
		try
		{
			flag = (Object)(object)PlayerCamera.main != (Object)null && PlayerCamera.main.threatMusicTime > 0.4f;
		}
		catch
		{
		}
		if (flag)
		{
			return "threat";
		}
		if (val.sicknessAmount > 28f || MaxInfection() > 25f)
		{
			return "sick";
		}
		if (val.averagePain > 42f || val.totalBleedSpeed > 0.04f)
		{
			return "injury";
		}
		if (val.traumaAmount > 55f || val.horrifiedLevel > 40f)
		{
			return "fear";
		}
		if (val.stamina < 18f || val.energy < 12f)
		{
			return "lazy";
		}
		if (Time.unscaledTime - Mathf.Max(lastPlayerInputAt, lastAutoTalk) > 300f && val.totalHappiness > -40f && !AnyAbnormal())
		{
			return "bored";
		}
		return null;
	}

	private float MaxInfection()
	{
		float num = 0f;
		Body val = body;
		if ((Object)(object)val == (Object)null || val.limbs == null)
		{
			return num;
		}
		Limb[] limbs = val.limbs;
		foreach (Limb val2 in limbs)
		{
			if ((Object)(object)val2 != (Object)null && val2.infectionAmount > num)
			{
				num = val2.infectionAmount;
			}
		}
		return num;
	}

	private bool AnyAbnormal()
	{
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return true;
		}
		if (val.averagePain > 30f)
		{
			return true;
		}
		if (val.sicknessAmount > 20f)
		{
			return true;
		}
		if (val.totalBleedSpeed > 0.02f)
		{
			return true;
		}
		if (val.traumaAmount > 45f)
		{
			return true;
		}
		return false;
	}

	public void SubmitPlayerInput(string raw)
	{
		if (!string.IsNullOrWhiteSpace(raw))
		{
			raw = raw.Trim();
			lastPlayerInputAt = Time.unscaledTime;
			AddLine(PlayerTitle(), raw, player: true);
			float num = EstimateStrength(raw);
			ApplyRelief(num, CompanionPlugin.Cfg.reliefDuration * Mathf.Clamp(num, 0.5f, 1.6f));
			RequestLlm(raw, IsInteraction(raw) ? "special" : null);
		}
	}

	private static bool IsInteraction(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return false;
		}
		string[] interactionWords = InteractionWords;
		foreach (string value in interactionWords)
		{
			if (raw.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	private float EstimateStrength(string input)
	{
		float num = CompanionPlugin.Cfg.reliefStrength;
		int num2 = 0;
		int num3 = 0;
		string[] strongWords = StrongWords;
		foreach (string value in strongWords)
		{
			if (input.Contains(value))
			{
				num2++;
			}
		}
		strongWords = MidWords;
		foreach (string value2 in strongWords)
		{
			if (input.Contains(value2))
			{
				num3++;
			}
		}
		return Mathf.Clamp((0.7f + (float)num2 * 0.25f + (float)num3 * 0.12f) * num, 0.2f, 2f);
	}

	private void TickMonitor()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null && cfg.monitorParts != null && cfg.monitorParts.Count != 0 && CanSpeak() && Llm.HasKey(cfg.api))
		{
			DateTime now = DateTime.Now;
			if (nextMonitorAt == DateTime.MinValue)
			{
				nextMonitorAt = NextMonitorSlot(now);
				FireMonitorRound(cfg);
			}
			else if (!(now < nextMonitorAt))
			{
				nextMonitorAt = NextMonitorSlot(nextMonitorAt);
				FireMonitorRound(cfg);
			}
		}
	}

	private static DateTime NextMonitorSlot(DateTime t)
	{
		DateTime dateTime = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, t.Kind);
		int num = Mathf.Clamp((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.monitorSlotMinutes : 3, 1, 30);
		DateTime result = dateTime.AddMinutes(1.0);
		while (result.Minute % num != 0)
		{
			result = result.AddMinutes(1.0);
		}
		return result;
	}

	private void FireMonitorRound(ModConfig cfg)
	{
		for (int i = 0; i < cfg.monitorParts.Count; i++)
		{
			MonitorPart monitorPart = cfg.monitorParts[i];
			if (monitorPart != null && monitorPart.enabled)
			{
				monitorPart.lastAt = Time.unscaledTime;
				RequestMonitor(monitorPart);
			}
		}
	}

	public void RequestMonitor(MonitorPart part)
	{
		Persona persona = CurrentPersona();
		int maxTokens = Mathf.Clamp((part.tokens <= 0) ? 300 : part.tokens, 60, 4000);
		ApiSettings api = CompanionPlugin.Cfg.api.Clone();
		api.maxTokens = maxTokens;
		List<ChatMsg> msgs = new List<ChatMsg>();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Loc.T("你在为《未知伤亡》里的实验体角色做「身体部位状态监测」。"));
		stringBuilder.AppendLine(Loc.T("角色：") + (string.IsNullOrEmpty(persona.name) ? "Experiment" : persona.name) + Loc.P("，") + GenderWord(persona) + Loc.P("。"));
		if (!string.IsNullOrEmpty(persona.appearance))
		{
			stringBuilder.AppendLine(Loc.T("外貌：") + persona.appearance);
		}
		if (!string.IsNullOrEmpty(persona.traits))
		{
			stringBuilder.AppendLine(Loc.T("额外设定：") + persona.traits);
		}
		stringBuilder.AppendLine(Loc.T("任务：只写【") + part.name + Loc.T("】这一处此刻的样子与状态，第三人称、冷静的观察口吻，不要对话、不要动作指令、不要写别的部位。"));
		if (StreamRule != null)
		{
			stringBuilder.AppendLine(StreamRule);
		}
		stringBuilder.AppendLine(Loc.T("要求：3~5 句、共 120 字以内；写颜色、湿度、形状、有无伤痕或异样；若最近发生过行为就顺带写出造成的变化。整个回答不得超出预算（最多 ") + maxTokens + Loc.P(" tokens）。"));
		msgs.Add(new ChatMsg("system", stringBuilder.ToString()));
		StringBuilder stringBuilder2 = new StringBuilder();
		stringBuilder2.AppendLine(StateSummary());
		if (history.Count > 0)
		{
			stringBuilder2.AppendLine(Loc.T("最近对话（用来判断刚才发生了什么）："));
			float now = Time.unscaledTime;
			for (int i = Mathf.Max(0, history.Count - CtxLine((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.ctxMonitor : 0, 4)); i < history.Count; i++)
			{
				Line line = history[i];
				if (line != null && line.kind != 1)
				{
					float dt = Mathf.Max(0f, now - line.time);
					string tag = (dt < 4f) ? "[刚刚] " : (dt < 60f ? $"[{(int)dt}s前] " : $"[{(int)(dt / 60)}m前] ");
					stringBuilder2.AppendLine("- " + tag + line.who + Loc.Colon + line.text);
				}
			}
		}
		msgs.Add(new ChatMsg("user", stringBuilder2.ToString()));
		CompanionPlugin.Log.LogInfo((object)("[monitor] 请求 " + part.name + "（budget " + maxTokens + "）"));
		Task.Run(async delegate
		{
			string reply = await Llm.ChatAsync(api, msgs, cts.Token);
			pending.Enqueue(delegate
			{
				if (string.IsNullOrEmpty(reply))
				{
					CompanionPlugin.Log.LogWarning((object)("[monitor] " + part.name + " 空回复"));
				}
				else
				{
					part.desc = Llm.CleanRaw(reply).Trim();
					if (CompanionPlugin.Cfg.debug)
					{
						CompanionPlugin.Log.LogInfo((object)("[monitor] " + part.name + " 更新：" + part.desc.Substring(0, Mathf.Min(40, part.desc.Length))));
					}
					HealthMonitorGui.RefreshPart(part);
				}
			});
		});
	}

	internal static bool IsApiErrorOrEmpty(string reply)
	{
		if (string.IsNullOrWhiteSpace(reply))
		{
			return true;
		}
		string lower = reply.Trim().ToLowerInvariant();
		if (lower.Contains("insufficient_quota") ||
		    lower.Contains("insufficient quota") ||
		    lower.Contains("quota exceeded") ||
		    lower.Contains("exceeded your current quota") ||
		    lower.Contains("quota is exhausted") ||
		    lower.Contains("out of quota") ||
		    lower.Contains("payment required") ||
		    lower.Contains("payment_required") ||
		    lower.Contains("402 payment") ||
		    lower.Contains("余额不足") ||
		    lower.Contains("额度不足") ||
		    lower.Contains("已欠费") ||
		    lower.Contains("欠费") ||
		    lower.Contains("请充值") ||
		    lower.Contains("invalid_api_key") ||
		    lower.Contains("invalid api key") ||
		    lower.Contains("api key invalid") ||
		    lower.Contains("unauthorized") ||
		    lower.Contains("authentication failed") ||
		    lower.Contains("billing_not_active") ||
		    lower.Contains("account_deactivated") ||
		    lower.Contains("token expired"))
		{
			return true;
		}
		return false;
	}

	public void RequestLlm(string playerInput, string autoTag)
	{
		if (busy)
		{
			if (!string.IsNullOrEmpty(playerInput))
			{
				queuedInput = playerInput;
				queuedTag = autoTag;
				hasQueued = true;
				if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug)
				{
					CompanionPlugin.Log.LogInfo((object)("[api] 正在回复中，玩家输入已排队：" + playerInput));
				}
			}
			return;
		}
		Persona p = CurrentPersona();
		ApiSettings api = CompanionPlugin.Cfg.api.Clone();
		int num = CompanionPlugin.Cfg.api.maxTokens;
		if (autoTag == "hurt" || autoTag == "injury")
		{
			num = Mathf.Min(num, Math.Max(180, num / 2));
		}
		else if (!string.IsNullOrEmpty(autoTag))
		{
			num = Mathf.Min(num, Math.Max(240, (int)((float)num * 0.8f)));
		}
		api.maxTokens = Mathf.Clamp(num, 60, 4000);
		if (!Llm.HasKey(api))
		{
			Speak(Loc.T("API欠费或无法使用"), record: true);
			return;
		}
		busy = true;
		busySince = Time.unscaledTime;
		List<ChatMsg> msgs = new List<ChatMsg>();
		msgs.Add(new ChatMsg("system", BuildSystemPrompt(p)));
		int num2 = ((!string.IsNullOrEmpty(playerInput) && history.Count > 0 && history[history.Count - 1] != null && history[history.Count - 1].player) ? (history.Count - 1) : history.Count);
		for (int i = Mathf.Max(0, num2 - CtxLine((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.ctxChat : 0, 7)); i < num2; i++)
		{
			Line line = history[i];
			if (line != null && line.kind != 1)
			{
				msgs.Add(new ChatMsg(line.player ? "user" : "assistant", line.text));
			}
		}
		msgs.Add(new ChatMsg("user", BuildUserPrompt(p, playerInput, autoTag)));
		Task.Run(async delegate
		{
			string reply = await Llm.ChatAsync(api, msgs, cts.Token);
			pending.Enqueue(delegate
			{
				busy = false;
				if (IsApiErrorOrEmpty(reply))
				{
					if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.debug)
					{
						CompanionPlugin.Log.LogInfo((object)"LLM 无回复或API异常，提示API欠费或无法使用");
					}
					reply = Loc.T("API欠费或无法使用");
				}
				Speak(reply, record: true);
				FlushQueued();
			});
		});
	}

	private void FlushQueued()
	{
		if (hasQueued)
		{
			string text = queuedInput;
			string autoTag = queuedTag;
			hasQueued = false;
			queuedInput = null;
			queuedTag = null;
			if (!string.IsNullOrEmpty(text))
			{
				RequestLlm(text, autoTag);
			}
		}
	}

	public void RequestMastLine(string phaseTag, string fallback, bool overrideBusy = false)
	{
		int mastBudget = 420;
		try
		{
			if (CompanionPlugin.Cfg != null)
			{
				mastBudget = (CompanionPlugin.Cfg.streamMode ? Mathf.Clamp(CompanionPlugin.Cfg.mastStreamChars, 8, 4000) : Mathf.Clamp(CompanionPlugin.Cfg.mastMaxChars, 60, 4000));
			}
		}
		catch
		{
		}
		ApiSettings api = CompanionPlugin.Cfg.api.Clone();
		api.maxTokens = mastBudget;
		if (!Llm.HasKey(api))
		{
			Speak(Loc.T("API欠费或无法使用"), record: true, mastBudget);
			return;
		}
		else
		{
			if (mastBusy && !overrideBusy)
			{
				return;
			}
			mastBusy = true;
			mastSince = Time.unscaledTime;
			Persona persona = CurrentPersona();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Loc.T("你在扮演《未知伤亡》(Casualties: Unknown) 里那名幸存者，此刻正在自慰——独自一人，玩家只能通过联络器听到你。"));
			stringBuilder.AppendLine(Loc.T("名字：") + (string.IsNullOrEmpty(persona.name) ? Loc.T("同伴") : persona.name));
			stringBuilder.AppendLine(Loc.T("性别：") + GenderWord(persona));
			stringBuilder.AppendLine(Loc.T("性格与说话方式：") + (string.IsNullOrEmpty(persona.personality) ? Loc.T("安静、克制") : persona.personality));
			stringBuilder.AppendLine(Loc.T("外貌：") + (string.IsNullOrEmpty(persona.appearance) ? "" : persona.appearance));
			stringBuilder.AppendLine(Loc.T("你称呼玩家为：") + (string.IsNullOrEmpty(persona.callPlayer) ? Loc.T("你") : persona.callPlayer));
			if (!string.IsNullOrEmpty(persona.traits))
			{
				stringBuilder.AppendLine(Loc.T("额外设定：") + persona.traits);
			}
			stringBuilder.AppendLine(Loc.T("当前阶段：") + phaseTag);
			stringBuilder.AppendLine(Loc.T("写作要求："));
			if (StreamRule != null)
			{
				stringBuilder.AppendLine(Loc.T("0) 【优先级最高，与下面第 2 条的细节要求冲突时以本条为准】") + StreamRule);
			}
			stringBuilder.AppendLine(Loc.T("1) 只写一段话，不要换行；以「名字：」开头，后面一句台词，动作与身体细节写进中文括号。"));
			stringBuilder.AppendLine(Loc.T("2) 括号里必须落到具体细节：手指或爪子的位置与力度、阴唇与阴蒂的形态变化、内壁的收缩与温度、体液的量与颜色与黏稠度、气味、皮肤上的汗与热度、腿根与腰腹的抽动、呼吸与喉咙里的声音。"));
			stringBuilder.AppendLine(Loc.T("3) 台词短、断句多，像喘着说话，允许语无伦次。"));
			stringBuilder.AppendLine(Loc.T("4) 不写任何数值、状态名、系统说明；不写玩家在碰你（他不在场，只能听见）。"));
			List<ChatMsg> msgs = new List<ChatMsg>();
			msgs.Add(new ChatMsg("system", stringBuilder.ToString()));
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendLine(StateSummary());
			if (history.Count > 0)
			{
				stringBuilder2.AppendLine(Loc.T("最近对话："));
				float now = Time.unscaledTime;
				for (int i = Mathf.Max(0, history.Count - CtxLine((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.ctxMast : 0, 4)); i < history.Count; i++)
				{
					Line line = history[i];
					if (line != null && line.kind != 1)
					{
						float dt = Mathf.Max(0f, now - line.time);
						string tag = (dt < 4f) ? "[刚刚] " : (dt < 60f ? $"[{(int)dt}s前] " : $"[{(int)(dt / 60)}m前] ");
						stringBuilder2.AppendLine("- " + tag + line.who + Loc.Colon + line.text);
					}
				}
			}
			stringBuilder2.AppendLine(Loc.T("现在写这一刻："));
			msgs.Add(new ChatMsg("user", stringBuilder2.ToString()));
			Task.Run(async delegate
			{
				string reply = null;
				try
				{
					reply = await Llm.ChatAsync(api, msgs, cts.Token);
				}
				catch (Exception ex)
				{
					CompanionPlugin.Log.LogWarning((object)("[mast] LLM 请求异常：" + ex.Message));
				}
				pending.Enqueue(delegate
				{
					mastBusy = false;
					mastSince = -1f;
					if (IsApiErrorOrEmpty(reply))
					{
						Speak(Loc.T("API欠费或无法使用"), record: true, mastBudget);
					}
					else
					{
						Speak(reply, record: true, mastBudget);
					}
				});
			});
		}
	}

	private static string GenderWord(Persona p)
	{
		return p.GenderText();
	}

	public string BuildSystemPrompt(Persona p)
	{
		string text = (string.IsNullOrEmpty(p.name) ? "Companion" : p.name);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Roleplay Setting]");
		stringBuilder.AppendLine("You roleplay as the survivor controlled by the player in Casualties: Unknown, speaking as their inner voice and companion.");
		stringBuilder.AppendLine("[Character Profile]");
		stringBuilder.AppendLine("Name: " + text);
		stringBuilder.AppendLine("Gender: " + GenderWord(p));
		stringBuilder.AppendLine("Personality & Tone: " + (string.IsNullOrEmpty(p.personality) ? "Quiet, restrained, speaking in short sentences" : p.personality));
		stringBuilder.AppendLine("Self Appearance: " + (string.IsNullOrEmpty(p.appearance) ? "Standard specimen features" : p.appearance));
		stringBuilder.AppendLine("Call Handler: " + (string.IsNullOrEmpty(p.callPlayer) ? "Master" : p.callPlayer) + " (address naturally, avoid repeating every sentence)");
		stringBuilder.AppendLine("Relationship with Handler: " + (string.IsNullOrEmpty(p.relationship) ? "The handler is the only presence staying by your side" : p.relationship));
		if (!string.IsNullOrEmpty(p.traits))
		{
			stringBuilder.AppendLine("Traits: " + p.traits);
		}
		if (p.interactions != null && p.interactions.Length != 0)
		{
			stringBuilder.AppendLine("Interactions: " + string.Join(", ", p.interactions));
		}
		stringBuilder.AppendLine();

		PromptConfig pCfg = CompanionPlugin.Cfg?.prompt;
		string baseRules = pCfg?.systemPromptBase;
		if (string.IsNullOrEmpty(baseRules))
		{
			baseRules = PromptConfig.DefaultSystemPromptBase;
		}
		stringBuilder.AppendLine(baseRules);

		if (pCfg != null && !string.IsNullOrEmpty(pCfg.systemExtra))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Additional Setting]");
			stringBuilder.AppendLine(pCfg.systemExtra);
		}
		if (StreamRule != null)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Stream Rule]");
			stringBuilder.AppendLine(StreamRule);
		}

		string langPrompt = pCfg?.languagePrompt;
		if (string.IsNullOrEmpty(langPrompt))
		{
			langPrompt = PromptConfig.GetDefaultLanguagePrompt(LocalizationManager.Current);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Language Requirement]");
		stringBuilder.AppendLine(langPrompt);

		return stringBuilder.ToString();
	}

	public string BuildUserPrompt(Persona p, string playerInput, string autoTag)
	{
		PromptConfig pCfg = CompanionPlugin.Cfg?.prompt;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Current Status]\n" + StateSummary());
		stringBuilder.AppendLine("Requirement: React strictly to this moment; do not hallucinate unlisted wounds; address the handler naturally.");
		if (recentActs.Count > 0)
		{
			stringBuilder.AppendLine("[Just Happened]\n" + string.Join("; ", recentActs.ToArray()));
		}
		if (!string.IsNullOrEmpty(autoTag))
		{
			stringBuilder.AppendLine("[Context]\nSilent scene, speak spontaneously (Trigger: " + TriggerName(autoTag) + ").");
			if (autoTag == "hurt" && !string.IsNullOrEmpty(hurtNote))
			{
				stringBuilder.AppendLine("[Specific Injury]\n" + hurtNote);
			}
			if ((autoTag == "item" || autoTag == "loot") && !string.IsNullOrEmpty(itemNote))
			{
				stringBuilder.AppendLine("[Object Context]\n" + itemNote);
			}
			stringBuilder.AppendLine("[Scenario Request]\n" + TriggerTemplate(autoTag));
		}
		else
		{
			string text = ((pCfg != null && !string.IsNullOrEmpty(pCfg.inputRule)) ? pCfg.inputRule : PromptConfig.DefaultInputRule);
			stringBuilder.AppendLine("[Handler Action/Speech]\n" + playerInput);
			if (!string.IsNullOrEmpty(text))
			{
				stringBuilder.AppendLine("[Understanding Guideline]\n" + text);
			}
		}
		string text2 = ((pCfg != null && !string.IsNullOrEmpty(pCfg.outputRule)) ? pCfg.outputRule : PromptConfig.DefaultOutputRule);
		stringBuilder.AppendLine("[Output Format Requirement]\n" + text2);

		string langPrompt = pCfg?.languagePrompt;
		if (string.IsNullOrEmpty(langPrompt))
		{
			langPrompt = PromptConfig.GetDefaultLanguagePrompt(LocalizationManager.Current);
		}
		stringBuilder.AppendLine("[Language Output Instruction]\n" + langPrompt);

		return stringBuilder.ToString();
	}

	private static string TriggerName(string tag)
	{
		return tag switch
		{
			"arrive" => Loc.T("初来乍到"), 
			"craft" => Loc.T("制作物品"), 
			"eat" => Loc.T("吃东西"), 
			"event" => Loc.T("突发事件"), 
			"hurt" => Loc.T("受击受伤"), 
			"item" => Loc.T("身边有物品"), 
			"loot" => Loc.T("拿到东西"), 
			"sick" => Loc.T("身体不适"), 
			"injury" => Loc.T("伤病惨叫"), 
			"wake" => Loc.T("睡醒"), 
			"fear" => Loc.T("恐惧与懒惰"), 
			"lazy" => Loc.T("累得不想动"), 
			"bored" => Loc.T("发呆"), 
			"dying" => Loc.T("意识模糊/濒死"), 
			"farewell" => Loc.T("死前告别"), 
			"threat" => Loc.T("附近有威胁"), 
			"special" => Loc.T("特殊互动"), 
			_ => tag, 
		};
	}

	private string TriggerTemplate(string tag)
	{
		BuildBodyCapability(out var arms, out var legs);
		ReactionRule reactionRule = Rule(tag);
		if (reactionRule != null && !string.IsNullOrEmpty(reactionRule.template))
		{
			return reactionRule.template.Replace("{capability}", arms + Loc.ListSep + legs);
		}
		return tag switch
		{
			"arrive" => "Observe surrounding terrain and blocks on screen, assess the environment, and state immediate reaction and reliance on the handler.",
			"craft" => "Comment on the crafted item's quality and utility; mention fatigue or tactile feeling if assisted.",
			"eat" => "React to the food/drink flavor, temperature, and satiety; describe eating in parentheses.",
			"event" => "Hit by a trap or ambush. Cry out sharply, noting the injury and pain in parentheses.",
			"hurt" => "Sustained damage. Cry out in raw pain, describe the affected area and instinctual reactions. Never claim recovery.",
			"item" => "Notice nearby ground item. Offer cautious evaluation of its condition and risk.",
			"loot" => "Received new item. Express first impressions of texture, weight, and usefulness.",
			"sick" => "Bodily distress (nausea, chills, fever, dizziness). Whimper or plead for guidance.",
			"injury" => "Deep lingering ache. Emit low groans, noting limb and bleeding in parentheses.",
			"wake" => "Just woke up disoriented and groggy. Speak in fragmented words, asking where you are.",
			"fear" => "Afraid and reluctant. Softly plead with the handler for reassurance or to stop.",
			"lazy" => "Exhausted and fatigued. Murmur complaints and ask to rest.",
			"bored" => "Extended silence. Zone out, reminiscing about the holding room and warm meals.",
			"dying" => "Near death. Gasp broken words of despair or farewell; describe failing vitals. Never claim recovery.",
			"farewell" => "Final moments. Express parting words to the handler; describe fading breath in parentheses.",
			"threat" => "Threat nearby. Whisper a low warning, keeping sentences brief.",
			"special" => "Interaction requested. Respond according to actual physical capacity: " + arms + ", " + legs + ". Describe touch, ache, or comfort.",
			_ => "React naturally to current status.",
		};
	}

	private void BuildBodyCapability(out string arms, out string legs)
	{
		arms = Loc.T("双臂可用");
		legs = Loc.T("双腿可用");
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		bool flag = true;
		bool flag2 = true;
		if (val.limbs != null)
		{
			Limb[] limbs = val.limbs;
			foreach (Limb val2 in limbs)
			{
				if (!((Object)(object)val2 == (Object)null))
				{
					if (val2.isArm && (val2.dismembered || val2.muscleHealth < 20f))
					{
						flag = false;
					}
					if (val2.isLegLimb && (val2.dismembered || val2.muscleHealth < 20f))
					{
						flag2 = false;
					}
				}
			}
		}
		arms = (flag ? Loc.T("双臂还能动") : Loc.T("手臂已经废了，摸不到自己"));
		legs = (flag2 ? Loc.T("腿还能站") : Loc.T("腿断了，站不起来"));
	}

	public string InjurySummary()
	{
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return Loc.T("不在游戏中。");
		}
		List<string> list = new List<string>();
		if (val.limbs != null)
		{
			Limb[] limbs = val.limbs;
			foreach (Limb val2 in limbs)
			{
				if (!((Object)(object)val2 == (Object)null))
				{
					List<string> list2 = new List<string>();
					if (val2.dismembered)
					{
						list2.Add(Loc.T("已经断离"));
					}
					if (val2.broken)
					{
						list2.Add(Loc.T("骨折"));
					}
					if (val2.dislocated)
					{
						list2.Add(Loc.T("脱臼"));
					}
					if (val2.infected)
					{
						list2.Add(Loc.T("感染"));
					}
					if (val2.bleedAmount > 0.05f)
					{
						list2.Add(Loc.T("在流血(") + (int)val2.bleedAmount + ")");
					}
					if (val2.pain >= 12f)
					{
						list2.Add(Loc.T("疼痛") + (int)val2.pain);
					}
					if (val2.skinHealth < 75f)
					{
						list2.Add(Loc.T("皮肤破损") + (int)(100f - val2.skinHealth) + "%");
					}
					if (val2.muscleHealth < 75f)
					{
						list2.Add(Loc.T("肌肉损伤") + (int)(100f - val2.muscleHealth) + "%");
					}
					if (list2.Count > 0)
					{
						list.Add(val2.shortName + Loc.Colon + string.Join(Loc.ListSep, list2.ToArray()));
					}
				}
			}
		}
		string text = ((val.totalBleedSpeed > 0.01f) ? (Loc.T("全身出血速度 ") + val.totalBleedSpeed.ToString("F2") + Loc.P("；")) : "");
		string text2 = ((val.sicknessAmount > 20f) ? (Loc.T("发炎发烧 ") + (int)val.sicknessAmount + Loc.P("；")) : "");
		if (list.Count == 0 && string.IsNullOrEmpty(text))
		{
			return Loc.T("身体实况：你身上没有任何伤——四肢完好、没有骨折脱臼、没有出血、没有感染，只有") + (int)val.averagePain + Loc.T(" 点酸痛。不要说自己受伤、流血、骨头痛。");
		}
		return Loc.T("身体实况（只能按这个说，没写的部位就是没伤）：") + text + text2 + string.Join(Loc.SemiSep, list.ToArray()) + Loc.Period;
	}

	public string StateSummary()
	{
		Body val = body;
		if ((Object)(object)val == (Object)null)
		{
			return Loc.T("不在游戏中");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Loc.T("所在：")).Append(LocationSummary()).Append(Loc.P("；"));
		List<string> list = new List<string>();
		if (val.isCriticallyDying || val.consciousness < 22f)
		{
			list.Add(Loc.T("濒死/意识快散了"));
		}
		else if (val.consciousness < 55f)
		{
			list.Add(Loc.T("意识有些模糊"));
		}
		if (val.averagePain >= 60f)
		{
			list.Add(Loc.T("剧痛难忍"));
		}
		else if (val.averagePain >= 30f)
		{
			list.Add(Loc.T("疼痛明显"));
		}
		else if (val.averagePain >= 12f)
		{
			list.Add(Loc.T("有点酸痛"));
		}
		if (val.horrifiedLevel > 40f)
		{
			list.Add(Loc.T("极度惊恐"));
		}
		else if (val.traumaAmount > 50f)
		{
			list.Add(Loc.T("精神紧绷"));
		}
		if (val.stamina < 20f || val.energy < 15f)
		{
			list.Add(Loc.T("体力透支、很累"));
		}
		if (val.hunger < 25f)
		{
			list.Add(Loc.T("很饿"));
		}
		if (val.thirst < 25f)
		{
			list.Add(Loc.T("很渴"));
		}
		if (val.temperature < 35.5f)
		{
			list.Add(Loc.T("发冷失温"));
		}
		else if (val.temperature > 38.5f)
		{
			list.Add(Loc.T("体温偏高/燥热"));
		}
		if (val.totalBleedSpeed > 0.03f)
		{
			list.Add(Loc.T("正在出血"));
		}
		if (val.sicknessAmount > 30f)
		{
			list.Add(Loc.T("恶心发冷"));
		}
		if (val.inWater)
		{
			list.Add(Loc.T("泡在水里"));
		}
		if (list.Count == 0)
		{
			list.Add(Loc.T("意识清醒、状态平稳"));
		}
		stringBuilder.Append(Loc.T("状态：")).Append(string.Join(Loc.P("、"), list.ToArray())).Append(Loc.Period);
		stringBuilder.Append(InjurySummary());
		return stringBuilder.ToString();
	}

	public string FallbackLine(string playerInput, string autoTag)
	{
		return Loc.T("API欠费或无法使用");
	}

	public void ApplyRelief(float strength, float duration)
	{
		if (!((Object)(object)body == (Object)null))
		{
			reliefStrength = Mathf.Clamp(strength, 0.2f, 2f);
			reliefTimer = Mathf.Max(reliefTimer, duration);
			if (!reliefActive)
			{
				reliefActive = true;
				baseAntiHappiness = body.antidepressantHappiness;
			}
		}
	}

	private void TickRelief(float dt)
	{
		if (!reliefActive)
		{
			return;
		}
		if ((Object)(object)body == (Object)null)
		{
			EndRelief();
			return;
		}
		reliefTimer -= dt;
		if (reliefTimer <= 0f || !body.alive)
		{
			EndRelief();
			return;
		}
		float num = Mathf.Clamp01(reliefTimer / 3f);
		float num2 = reliefStrength * (0.35f + 0.65f * num);
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg.clearHorror)
		{
			body.horrifiedLevel = 0f;
		}
		body.antidepressantHappiness = baseAntiHappiness + 8f * num2;
		if (body.antidepressantHappiness > 45f)
		{
			body.antidepressantHappiness = 45f;
		}
		if (cfg.traumaDampPerSec > 0f && body.traumaAmount > 0f)
		{
			body.traumaAmount = Mathf.Max(0f, body.traumaAmount - cfg.traumaDampPerSec * num2 * dt);
		}
	}

	private void EndRelief()
	{
		reliefActive = false;
		reliefTimer = 0f;
		if ((Object)(object)body != (Object)null)
		{
			try
			{
				body.antidepressantHappiness = Mathf.Min(body.antidepressantHappiness, baseAntiHappiness);
			}
			catch
			{
			}
		}
	}
}
