using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class SettingsEditorGui : MonoBehaviour
{
	public static SettingsEditorGui Instance;

	public void OpenMenu()
	{
		open = true;
		LegacyCompanionUi.CloseWindows();
	}

	public void CloseMenu()
	{
		open = false;
	}

	public bool IsOpen
	{
		get
		{
			return open;
		}
	}

	private bool open;

	private int mainTab;

	private int subTabPrompt;

	private int subTabReact;

	private int subTabCodex;

	private int subTabUi;

	private int subTabDual;

	private int curPersonaIdx;

	private Vector2 scrollRules;

	private Vector2 scrollTimers;

	private Vector2 scrollQuotas;

	private Vector2 scrollDual;

	private Vector2 scrollDualAi;

	private Vector2 scrollReplyWay;

	private Vector2 scrollPreset;

	private string p2ScanRadiusInputStr;

	private Vector2 scrollPlayerIdentity;

	private Vector2 scrollSense;

	private Vector2 scrollUiSettings;

	private Vector2 scrollApi;

	private float contentH = 600f;

	private float ScrollHWithSub => Mathf.Max(120f, contentH - 110f);

	private float ScrollHNoSub => Mathf.Max(120f, contentH - 80f);

	private Font font;

	private GUIStyle styleLabel;

	private GUIStyle styleSmall;

	private GUIStyle styleHead;

	private GUIStyle styleArea;

	private GUIStyle styleField;

	private GUIStyle styleBtn;

	private GUIStyle styleBox;

	private string apiMsg = "";

	private bool inited;

	private readonly Dictionary<string, string> editBuf = new Dictionary<string, string>();

	private static readonly string[] MainTabTitles = new string[7]
	{
		"LLM预设",
		"LLM反应",
		"LLM接口",
		"角色图鉴",
		"MOD界面",
		"双人模式",
		"关于"
	};

	private static readonly string[] SubTabsPrompt = new string[4]
	{
		"【回复偏好】",
		"【底层提示词】",
		"【人设与身份】",
		"【LLM感知】"
	};

	private static readonly string[] SubTabsReact = new string[3]
	{
		"【反应模板】",
		"【计时】",
		"【额度】"
	};

	private static readonly string[] SubTabsCodex = new string[2]
	{
		"【角色人设】",
		"【角色图鉴】"
	};

	private static readonly string[] SubTabsUi = new string[2]
	{
		"【界面设置】",
		"【语言】"
	};

	private static readonly string[] SubTabsDual = new string[2]
	{
		"【常规与操控】",
		"【P2 AI】"
	};

	private float lastPreviewAt;

	private void Awake()
	{
		Instance = this;
	}

	private static bool InGame()
	{
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			return instance.HasBody;
		}
		return false;
	}

	private void Update()
	{
		if (open && !InGame())
		{
			if (GUIUtility.keyboardControl == 0)
			{
				string inputString = Input.inputString;
				if (!string.IsNullOrEmpty(inputString))
				{
					if (inputString.Contains("1"))
					{
						mainTab = 0;
					}
					else if (inputString.Contains("2"))
					{
						mainTab = 1;
					}
					else if (inputString.Contains("3"))
					{
						mainTab = 2;
					}
					else if (inputString.Contains("4"))
					{
						mainTab = 3;
					}
					else if (inputString.Contains("5"))
					{
						mainTab = 4;
					}
					else if (inputString.Contains("6"))
					{
						mainTab = 5;
					}
				}
			}
		}
		if (Input.GetKeyDown((KeyCode)285) && !InGame())
		{
			open = !open;
			if (open)
			{
				LegacyCompanionUi.CloseWindows();
				PlayerCamera.PlayUISound("click", 1f);
			}
		}
	}

	private void InitStyles()
	{
		if (!inited)
		{
			inited = true;
			font = Font.CreateDynamicFontFromOSFont(new string[5] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Microsoft YaHei UI", "Arial Unicode MS" }, 15);
			if ((Object)(object)font == (Object)null)
			{
				font = GUI.skin.font;
			}
			else if ((Object)(object)GUI.skin != (Object)null)
			{
				GUI.skin.font = font;
			}
			styleBox = new GUIStyle(GUI.skin.box);
			styleBox.normal.background = MakeTex(new Color(0.035f, 0.04f, 0.05f, 1f));
			styleBox.border = new RectOffset(1, 1, 1, 1);
			styleBox.clipping = TextClipping.Overflow;
			styleLabel = new GUIStyle(GUI.skin.label)
			{
				font = font,
				fontSize = 14,
				wordWrap = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			styleLabel.normal.textColor = new Color(0.88f, 0.9f, 0.94f);
			styleSmall = new GUIStyle(styleLabel)
			{
				fontSize = 12,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			styleSmall.normal.textColor = new Color(0.62f, 0.68f, 0.78f);
			styleHead = new GUIStyle(styleLabel)
			{
				fontSize = 17,
				fontStyle = (FontStyle)1,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			styleHead.normal.textColor = new Color(0.72f, 0.86f, 1f);
			styleArea = new GUIStyle(GUI.skin.textArea)
			{
				font = font,
				fontSize = 13,
				wordWrap = true,
				clipping = TextClipping.Overflow
			};
			styleField = new GUIStyle(GUI.skin.textField)
			{
				font = font,
				fontSize = 13,
				clipping = TextClipping.Overflow
			};
			styleBtn = new GUIStyle(GUI.skin.button)
			{
				font = font,
				fontSize = 13,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(6, 6, 2, 2)
			};
		}
	}

	private static Texture2D MakeTex(Color c)
	{
		Texture2D val = new Texture2D(1, 1);
		val.SetPixel(0, 0, c);
		val.Apply();
		return val;
	}

	private void OnGUI()
	{
		UiScale.Begin();
		try
		{
			DrawGui();
		}
		finally
		{
			UiScale.End();
		}
	}

	private void DrawGui()
	{
		if (InGame())
		{
			open = false;
			MenuBlocker.Require("settings", false);
			MenuBlocker.Sync();
			return;
		}
		InitStyles();
		if (!open)
		{
			MenuBlocker.Require("settings", false);
			MenuBlocker.Sync();
			if (GUI.Button(new Rect(12f, 12f, 132f, 34f), Lang.T("openMenu"), styleBtn))
			{
				OpenMenu();
			}
			return;
		}
		MenuBlocker.Require("settings", true);
		MenuBlocker.Sync();
		MenuBlocker.Paint();
		float num = Mathf.Max(360f, (float)UiScale.SW - 24f);
		float num2 = Mathf.Max(260f, (float)UiScale.SH - 24f);
		Rect val = new Rect(((float)UiScale.SW - num) * 0.5f, ((float)UiScale.SH - num2) * 0.5f, num, num2);
		UiPointerGuard.Mark(val);
		contentH = Mathf.Max(240f, val.height - 20f - 52f);
		CodexView.SetArea(val.width - 24f, contentH - 45f);
		GUI.Box(val, GUIContent.none, styleBox);
		GUILayout.BeginArea(new Rect(val.x + 12f, val.y + 10f, val.width - 24f, val.height - 20f));

		// Top Main Tab Bar
		GUILayout.BeginHorizontal();
		GUILayout.Label(Lang.T("menuTitle") + "  " + CompanionPlugin.VersionTag, styleHead, GUILayout.Width(180f), GUILayout.Height(32f));
		GUILayout.FlexibleSpace();
		for (int i = 0; i < MainTabTitles.Length; i++)
		{
			bool isCurrent = (mainTab == i);
			Color color = GUI.color;
			GUI.color = isCurrent ? new Color(0.45f, 0.85f, 1f) : Color.white;
			if (GUILayout.Button(Loc.T(MainTabTitles[i]), styleBtn, GUILayout.Width(92f), GUILayout.Height(32f)))
			{
				mainTab = i;
			}
			GUI.color = color;
			GUILayout.Space(2f);
		}
		GUILayout.Space(6f);
		if (GUILayout.Button("×", styleBtn, GUILayout.Width(36f), GUILayout.Height(32f)))
		{
			open = false;
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(6f);

		// Tab dispatch
		if (mainTab == 0)
		{
			DrawMainPrompt();
		}
		else if (mainTab == 1)
		{
			DrawMainReact();
		}
		else if (mainTab == 2)
		{
			DrawApi();
		}
		else if (mainTab == 3)
		{
			DrawMainCodex();
		}
		else if (mainTab == 4)
		{
			DrawMainUi();
		}
		else if (mainTab == 5)
		{
			DrawDual();
			if (subTabDual == 1)
			{
				DrawP2AiWatermark(new Rect(val.x + 16f, val.y + 75f, val.width - 32f, contentH));
			}
		}
		else if (mainTab == 6)
		{
			DrawAbout();
		}

		GUILayout.EndArea();
	}

	private void DrawSubTabBar(string[] subTabs, ref int currentIdx)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(2f);
		for (int i = 0; i < subTabs.Length; i++)
		{
			bool isCurrent = (currentIdx == i);
			Color prev = GUI.color;
			GUI.color = isCurrent ? new Color(0.45f, 0.85f, 1f, 1f) : new Color(0.85f, 0.88f, 0.92f, 0.85f);
			if (GUILayout.Button(Loc.T(subTabs[i]), styleBtn, GUILayout.Height(30f), GUILayout.MinWidth(95f)))
			{
				currentIdx = i;
			}
			GUI.color = prev;
			GUILayout.Space(4f);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(6f);
	}

	#region Main Tab 0: LLM预设
	private void DrawMainPrompt()
	{
		DrawSubTabBar(SubTabsPrompt, ref subTabPrompt);
		switch (subTabPrompt)
		{
			case 0:
				DrawPrompt_ReplyWay();
				break;
			case 1:
				DrawPrompt_Preset();
				break;
			case 2:
				DrawPrompt_PlayerIdentity();
				break;
			case 3:
				DrawPrompt_Sense();
				break;
		}
	}

	private void DrawPrompt_ReplyWay()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;
		if (cfg.prompt == null) cfg.prompt = new PromptConfig();

		GUILayout.Label(Loc.T("配置大语言模型的回复规则、单行字数截断、打字机逐字出字速度与音效。"), styleSmall);
		scrollReplyWay = GUILayout.BeginScrollView(scrollReplyWay, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、输入与输出规则"));
		GUILayout.Label(Loc.T("输入规则（告诉模型怎么理解你打的字）"), styleLabel);
		cfg.prompt.inputRule = GUILayout.TextArea(cfg.prompt.inputRule ?? "", styleArea, GUILayout.MinHeight(55f));
		GUILayout.Space(6f);
		GUILayout.Label(Loc.T("输出规则（拼进最后的输出格式要求）"), styleLabel);
		cfg.prompt.outputRule = GUILayout.TextArea(cfg.prompt.outputRule ?? "", styleArea, GUILayout.MinHeight(55f));
		GUILayout.Space(6f);
		cfg.lineMaxChars = GenInt("lineMaxChars", Loc.T("单行字数上限（") + cfg.lineMaxChars + Loc.P("）"), Loc.T("作用：历史框里每一行最多显示多少字，超了退到最近的句末整句截断（防刷屏）。默认 160，范围 40–1000。"), cfg.lineMaxChars, 40, 1000);

		GenHead(Loc.T("二、打字机逐字显示"));
		cfg.typingCharsPerSec = GenFloat(Loc.T("打字速度 ") + cfg.typingCharsPerSec.ToString("F1") + Loc.T(" 字/秒"), Loc.T("作用：历史框里逐字显示的速度。调大=出字快；调小=更有逐字表演感。默认 22。"), cfg.typingCharsPerSec, 2f, 200f);
		cfg.typingLineGap = GenFloat(Loc.T("每行打完停顿 ") + cfg.typingLineGap.ToString("F2") + Loc.T(" 秒"), Loc.T("作用：一条消息显示完后停多久才轮到下一条。默认 0.45 秒。"), cfg.typingLineGap, 0f, 5f);
		cfg.typingSoundGrace = GenFloat(Loc.T("打字音收尾容差 ") + cfg.typingSoundGrace.ToString("F2") + Loc.T(" 秒"), Loc.T("作用：文字打完后音效再多响多久。默认 0.05 秒。"), cfg.typingSoundGrace, 0f, 2f);

		GenHead(Loc.T("三、打字音效"));
		GUILayout.BeginHorizontal();
		GUILayout.Label(Lang.T("soundTyping"), styleLabel, GUILayout.Width(130f));
		float vol = GUILayout.HorizontalSlider(Mathf.Clamp01(cfg.typingVolume), 0f, 1f, GUILayout.Width(360f));
		if (Mathf.Abs(vol - cfg.typingVolume) > 0.0001f)
		{
			cfg.typingVolume = vol;
			PlayTypingPreview(cfg.typingVolume);
		}
		GUILayout.Label(Mathf.RoundToInt(cfg.typingVolume * 100f) + "%", styleLabel, GUILayout.Width(50f));
		if (GUILayout.Button("−", styleBtn, GUILayout.Width(28f)))
		{
			cfg.typingVolume = Mathf.Clamp01(cfg.typingVolume - 0.05f);
			PlayTypingPreview(cfg.typingVolume);
		}
		if (GUILayout.Button("+", styleBtn, GUILayout.Width(28f)))
		{
			cfg.typingVolume = Mathf.Clamp01(cfg.typingVolume + 0.05f);
			PlayTypingPreview(cfg.typingVolume);
		}
		if (GUILayout.Button(Lang.T("soundTest"), styleBtn, GUILayout.Width(80f)))
		{
			PlayTypingPreview(cfg.typingVolume);
		}
		GUILayout.EndHorizontal();

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(100f), GUILayout.Height(30f)))
		{
			cfg.prompt.inputRule = Loc.T("用第三人称简报训练员刚做的事，不要写成对白。");
			cfg.prompt.outputRule = Loc.T("每段一行，行首「名字：」，动作与身体状态一律放进中文圆括号。");
			cfg.lineMaxChars = 160;
			cfg.typingCharsPerSec = 22f;
			cfg.typingLineGap = 0.45f;
			cfg.typingSoundGrace = 0.05f;
			cfg.typingVolume = 0.22f;
			editBuf.Clear();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认回复方式");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}

	private void DrawPrompt_Preset()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;
		if (cfg.prompt == null) cfg.prompt = new PromptConfig();
		if (cfg.api == null) cfg.api = new ApiSettings();

		GUILayout.Label(Loc.T("底层系统提示词、输入输出规则、输出语言指令、采样参数及记忆上下文全公开面板。"), styleSmall);
		scrollPreset = GUILayout.BeginScrollView(scrollPreset, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、底层系统提示词基准规则 (System Prompt Base)"));
		GUILayout.Label(Loc.T("角色扮演核心原则、世界观、生理能力限制与防出戏铁律（以英文交付模型，保证逻辑严谨）。完全公开无任何隐藏，可在此直接修改："), styleSmall);
		cfg.prompt.systemPromptBase = GUILayout.TextArea(cfg.prompt.systemPromptBase ?? PromptConfig.DefaultSystemPromptBase, styleArea, GUILayout.MinHeight(120f));

		GenHead(Loc.T("二、输入理解要求 (Input Understanding Guideline)"));
		GUILayout.Label(Loc.T("指导模型如何理解玩家输入或动作指令（以英文交付模型）："), styleSmall);
		cfg.prompt.inputRule = GUILayout.TextArea(cfg.prompt.inputRule ?? PromptConfig.DefaultInputRule, styleArea, GUILayout.MinHeight(50f));

		GenHead(Loc.T("三、输出格式规范 (Output Format Requirement)"));
		GUILayout.Label(Loc.T("要求模型输出的固定格式规范（如单行输出、括号描写动作）（以英文交付模型）："), styleSmall);
		cfg.prompt.outputRule = GUILayout.TextArea(cfg.prompt.outputRule ?? PromptConfig.DefaultOutputRule, styleArea, GUILayout.MinHeight(50f));

		GenHead(Loc.T("四、模型回复输出语言指令 (Language Instruction)"));
		GUILayout.Label(Loc.T("直接指定模型输出哪种语言。跟随模组界面显示语言自动切换，在此处修改与在【语言】设置中修改完全实时同步："), styleSmall);
		cfg.prompt.languagePrompt = GUILayout.TextArea(cfg.prompt.languagePrompt ?? PromptConfig.GetDefaultLanguagePrompt(Lang.Current), styleArea, GUILayout.MinHeight(50f));

		GenHead(Loc.T("五、附加自定义设定 (System Extra)"));
		GUILayout.Label(Loc.T("写进系统提示词尾部的额外用户自定义设定（世界观、性格微调、特殊口癖或禁忌）："), styleSmall);
		cfg.prompt.systemExtra = GUILayout.TextArea(cfg.prompt.systemExtra ?? "", styleArea, GUILayout.MinHeight(60f));

		GenHead(Loc.T("六、模型采样与生成参数"));
		cfg.api.temperature = GenFloat(Loc.T("温度 ") + cfg.api.temperature.ToString("F2"), Loc.T("作用：说话的多变程度。0 = 每次几乎同一句，0.9 = 正常有人味，1.5 = 很放飞。默认 0.9。"), cfg.api.temperature, 0f, 1.5f);
		cfg.api.maxTokens = GenInt("maxTokens", Loc.T("单次发言 token 上限（") + cfg.api.maxTokens + Loc.P("）"), Loc.T("作用：角色每说一次话，模型最多能吐多少 token，超了按句末截断。范围 60–4000，默认 300。"), cfg.api.maxTokens, 60, 4000);
		cfg.mastMaxChars = GenInt("mastMaxChars", Loc.T("自慰台词 token 上限（") + cfg.mastMaxChars + Loc.P("）"), Loc.T("作用：只管自慰阶段那几句台词，独立于上面的全局值。范围 60–4000，默认 420。"), cfg.mastMaxChars, 60, 4000);
		cfg.mastStreamChars = GenInt("mastStreamChars", Loc.T("直播模式自慰台词 token 上限（") + cfg.mastStreamChars + Loc.P("）"), Loc.T("作用：直播模式生效时每句自慰反应的上限，短促省钱。默认 20，范围 8–4000。"), cfg.mastStreamChars, 8, 4000);

		GenHead(Loc.T("七、记忆（进提示词的历史条数）"));
		cfg.ctxChat = GenInt("ctxChat", Loc.T("普通说话带几条（") + cfg.ctxChat + Loc.P("）"), Loc.T("作用：你打字说话时，给模型当上下文的最近历史条数。默认 7，范围 1–60。"), cfg.ctxChat, 1, 60);
		cfg.ctxPrompt = GenInt("ctxPrompt", Loc.T("提示词【刚才的对话】带几条（") + cfg.ctxPrompt + Loc.P("）"), Loc.T("作用：拼进系统提示的「从早到晚」摘要条数。默认 10，范围 1–60。"), cfg.ctxPrompt, 1, 60);
		cfg.ctxMast = GenInt("ctxMast", Loc.T("自慰台词参考几条（") + cfg.ctxMast + Loc.P("）"), Loc.T("作用：自慰阶段写台词时参考的最近条数。默认 4，范围 1–60。"), cfg.ctxMast, 1, 60);
		cfg.ctxMonitor = GenInt("ctxMonitor", Loc.T("部位监测参考几条（") + cfg.ctxMonitor + Loc.P("）"), Loc.T("作用：监测窗口写身体状态时参考的最近条数。默认 4，范围 1–60。"), cfg.ctxMonitor, 1, 60);

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认底层提示词"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
		{
			cfg.prompt.systemPromptBase = PromptConfig.DefaultSystemPromptBase;
			cfg.prompt.inputRule = PromptConfig.DefaultInputRule;
			cfg.prompt.outputRule = PromptConfig.DefaultOutputRule;
			cfg.prompt.languagePrompt = PromptConfig.GetDefaultLanguagePrompt(Lang.Current);
			cfg.prompt.systemExtra = "";
			cfg.api.temperature = 0.9f;
			cfg.api.maxTokens = 300;
			cfg.mastMaxChars = 420;
			cfg.mastStreamChars = 20;
			cfg.ctxChat = 7;
			cfg.ctxPrompt = 10;
			cfg.ctxMast = 4;
			cfg.ctxMonitor = 4;
			editBuf.Clear();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认底层提示词与预设");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}

	private void DrawPrompt_PlayerIdentity()
	{
		PersonaStore store = CompanionPlugin.Store;
		Persona p = store?.Get(curPersonaIdx);

		GUILayout.Label(Loc.T("设置伴侣角色对玩家的称呼、你与角色之间的身份关系以及当前角色姓名。"), styleSmall);
		scrollPlayerIdentity = GUILayout.BeginScrollView(scrollPlayerIdentity, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、角色槽位选择"));
		GUILayout.BeginHorizontal();
		string[] slotNames = new string[3] { Loc.T("角色 1 (SAW-01)"), Loc.T("角色 2 (Dune)"), Loc.T("角色 3 (Milky)") };
		for (int k = 0; k < 3; k++)
		{
			bool isSel = (curPersonaIdx == k);
			Color prev = GUI.color;
			GUI.color = isSel ? new Color(0.45f, 0.85f, 1f) : Color.white;
			if (GUILayout.Button(slotNames[k], styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
			{
				curPersonaIdx = k;
			}
			GUI.color = prev;
			GUILayout.Space(6f);
		}
		GUILayout.EndHorizontal();

		if (p != null)
		{
			GenHead(Loc.T("二、称谓与身份设定"));
			GUILayout.Label(Loc.T("称呼玩家为（角色对你的称呼）"), styleLabel);
			p.callPlayer = GUILayout.TextField(p.callPlayer ?? "", styleField, GUILayout.Width(240f));

			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.Label(Loc.T("快速预设："), styleSmall, GUILayout.Width(65f));
			string[] quickCalls = new string[6] { "主人", "训练员", "长官", "指挥官", "搭档", "你" };
			for (int q = 0; q < quickCalls.Length; q++)
			{
				if (GUILayout.Button(Loc.T(quickCalls[q]), styleBtn, GUILayout.Width(65f), GUILayout.Height(24f)))
				{
					p.callPlayer = quickCalls[q];
				}
				GUILayout.Space(4f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(8f);
			GUILayout.Label(Loc.T("玩家身份 / 与角色的关系（写入系统提示词，决定角色如何看待你）"), styleLabel);
			p.relationship = GUILayout.TextField(p.relationship ?? "", styleField);

			GenHead(Loc.T("三、角色基本资料"));
			GUILayout.Label(Loc.T("角色姓名"), styleLabel);
			p.name = GUILayout.TextField(p.name ?? "", styleField, GUILayout.Width(240f));

			GUILayout.Space(6f);
			GUILayout.Label(Loc.T("角色性格特质描述"), styleLabel);
			p.personality = GUILayout.TextArea(p.personality ?? "", styleArea, GUILayout.MinHeight(55f));

			GUILayout.Space(6f);
			GUILayout.Label(Loc.T("角色外观特征描述"), styleLabel);
			p.appearance = GUILayout.TextArea(p.appearance ?? "", styleArea, GUILayout.MinHeight(50f));

			GUILayout.Space(6f);
			GUILayout.Label(Loc.T("可用互动：") + ((p.interactions == null || p.interactions.Length == 0) ? Loc.T("（无）") : string.Join(Loc.P("、"), p.interactions)), styleSmall);

			GenHead(Loc.T("四、高级自定义面板"));
			GUILayout.Label(Loc.T("立绘外观颜色、语音频谱与按键动作互动可在原生人设面板中精细调节："), styleSmall);
			GUILayout.Space(4f);
			if (GUILayout.Button(Lang.T("openPersona") + " (F7)", styleBtn, GUILayout.Width(240f), GUILayout.Height(34f)))
			{
				open = false;
				NativePanel.OpenAnywhere();
			}
		}

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存人设与身份"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
		{
			store?.Save();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("人设与身份已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复本角色默认人设"), styleBtn, GUILayout.Width(160f), GUILayout.Height(30f)))
		{
			if (p != null)
			{
				LocalizationManager.DefaultPersona(curPersonaIdx, out var callPlayer, out var interactions, out var personality, out var appearance, out var relationship, out var traits);
				p.callPlayer = callPlayer;
				p.interactions = interactions;
				p.personality = personality;
				p.appearance = appearance;
				p.relationship = relationship;
				p.traits = traits;
				store?.Save();
				CompanionPlugin.SaveAll();
				apiMsg = Loc.T("已恢复角色 ") + (curPersonaIdx + 1) + Loc.T(" 的默认人设");
			}
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}

	private void DrawPrompt_Sense()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;

		GUILayout.Label(Loc.T("控制 LLM 对游戏事件的自动感知、主动开口、互动安抚与身体部位状态监测。"), styleSmall);
		scrollSense = GUILayout.BeginScrollView(scrollSense, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、主动开口感知"));
		cfg.autoTalk = GenBool(Loc.T("主动说话"), Loc.T("作用：关掉后只有你打字、或触发事件（受击/进食等）才会生成；角色不会自己找话说。具体频率与额度请在【LLM反应】中调节。"), cfg.autoTalk);

		GenHead(Loc.T("二、安抚互动感知（你说一句话带来的镇定效果）"));
		cfg.reliefStrength = GenFloat(Loc.T("安抚强度 ") + cfg.reliefStrength.ToString("F2") + Loc.T(" 倍"), Loc.T("作用：每次互动给角色带来的镇定/止痛幅度倍数。1 = 原版手感，调大=一句话压更多恐惧。默认 1。"), cfg.reliefStrength, 0.2f, 2f);
		cfg.reliefDuration = GenFloat(Loc.T("安抚持续 ") + cfg.reliefDuration.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：一次互动后镇定效果维持多久（实际还会乘上强度）。默认 18 秒。"), cfg.reliefDuration, 1f, 600f);
		cfg.traumaDampPerSec = GenFloat(Loc.T("创伤消退 ") + cfg.traumaDampPerSec.ToString("F2") + Loc.T(" /秒"), Loc.T("作用：安抚生效期间每秒消掉多少创伤值；0 = 安抚不再压创伤。默认 0.85。"), cfg.traumaDampPerSec, 0f, 10f);

		GenHead(Loc.T("三、身体部位监测感知"));
		cfg.monitorSlotMinutes = GenInt("monitorSlotMinutes", Loc.T("每 N 分钟刷新一轮（") + cfg.monitorSlotMinutes + Loc.P("）"), Loc.T("作用：监测窗口里启用的部位每隔这么久重新生成一次描述（按整点分钟对齐）。默认 3 分钟，范围 1–30。"), cfg.monitorSlotMinutes, 1, 30);
		if (cfg.monitorParts != null)
		{
			for (int i = 0; i < cfg.monitorParts.Count; i++)
			{
				MonitorPart monitorPart = cfg.monitorParts[i];
				if (monitorPart != null)
				{
					string text = (string.IsNullOrEmpty(monitorPart.name) ? ("#" + (i + 1)) : monitorPart.name);
					monitorPart.tokens = GenInt("partTokens" + i, Loc.T("部位「") + text + Loc.T("」token 上限（") + monitorPart.tokens + Loc.P("）"), Loc.T("作用：这一个部位每次描述最多多少 token。范围 60–4000。"), monitorPart.tokens, 60, 4000);
				}
			}
		}

		GenHead(Loc.T("四、直播弹幕感知"));
		cfg.danmakuToAi = GenBool(Loc.T("让角色回应弹幕"), Loc.T("作用：关掉 = 弹幕只在历史框显示，永不触发模型（不烧 API）；开着才按额度抽签让它开口。详细限制在【LLM反应 -> 额度】中。"), cfg.danmakuToAi);

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(100f), GUILayout.Height(30f)))
		{
			cfg.autoTalk = true;
			cfg.reliefStrength = 1f;
			cfg.reliefDuration = 18f;
			cfg.traumaDampPerSec = 0.85f;
			cfg.monitorSlotMinutes = 3;
			cfg.danmakuToAi = true;
			editBuf.Clear();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认感知设置");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}
	#endregion

	#region Main Tab 1: LLM反应
	private void DrawMainReact()
	{
		DrawSubTabBar(SubTabsReact, ref subTabReact);
		switch (subTabReact)
		{
			case 0:
				DrawReact_Rules();
				break;
			case 1:
				DrawReact_Timers();
				break;
			case 2:
				DrawReact_Quotas();
				break;
		}
	}

	private void DrawReact_Rules()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null || cfg.reactions == null)
		{
			return;
		}
		GUILayout.Label(Loc.T("每条模板 = 触发条件 + 给模型的表演指令。勾选控制是否启用；冷却单位秒；「独立计时」= 不占时间窗反应次数预算。"), styleSmall);
		GUILayout.Space(4f);
		scrollRules = GUILayout.BeginScrollView(scrollRules, GUILayout.Height(ScrollHWithSub));
		for (int i = 0; i < cfg.reactions.Count; i++)
		{
			ReactionRule reactionRule = cfg.reactions[i];
			if (reactionRule != null)
			{
				GUILayout.BeginVertical(styleBox);
				GUILayout.BeginHorizontal();
				reactionRule.enabled = GUILayout.Toggle(reactionRule.enabled, "", GUILayout.Width(20f));
				GUILayout.Label(reactionRule.title + "  (" + reactionRule.key + ")", styleLabel, GUILayout.Width(220f));
				GUILayout.Label(Loc.T("冷却"), styleSmall, GUILayout.Width(32f));
				if (float.TryParse(GUILayout.TextField(reactionRule.cooldown.ToString("F0"), styleField, GUILayout.Width(56f)), out var result))
				{
					reactionRule.cooldown = Mathf.Clamp(result, 0f, 3600f);
				}
				reactionRule.useBudget = !GUILayout.Toggle(!reactionRule.useBudget, Loc.T("独立计时"), GUILayout.Width(80f));
				GUILayout.FlexibleSpace();
				GUILayout.Label(reactionRule.enabled ? Loc.T("启用") : Loc.T("停用"), styleSmall, GUILayout.Width(40f));
				GUILayout.EndHorizontal();
				GUILayout.Label(Loc.T("触发：") + reactionRule.condition, styleSmall);
				reactionRule.template = GUILayout.TextArea(reactionRule.template ?? "", styleArea, GUILayout.MinHeight(56f));
				GUILayout.Space(4f);
				GUILayout.EndVertical();
			}
		}
		GUILayout.EndScrollView();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认模板"), styleBtn, GUILayout.Width(120f), GUILayout.Height(30f)))
		{
			cfg.reactions = ModConfig.DefaultReactions();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认模板");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}

	private void DrawReact_Timers()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;

		GUILayout.Label(Loc.T("控制所有自动反应、事件响应、开场与自慰流程的节拍、时长与冷却时间（单位：秒）。填 0 = 无间隔立即生效。"), styleSmall);
		scrollTimers = GUILayout.BeginScrollView(scrollTimers, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、自发与事件反应窗口"));
		cfg.reactionWindowSec = GenFloat(Loc.T("自发开口统计窗口 ") + cfg.reactionWindowSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：上面自发开口按多少秒为一轮统计。默认 60 秒。"), cfg.reactionWindowSec, 30f, 3600f);
		cfg.autoCooldownSec = GenFloat(Loc.T("两次自动开口最小间隔 ") + cfg.autoCooldownSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：防刷屏，两次自动开口之间至少隔这么久；0 = 只要额度还在就一直说。默认 18 秒。"), cfg.autoCooldownSec, 0f, 600f);
		cfg.hurtWindowSec = GenFloat(Loc.T("受击反应统计窗口 ") + cfg.hurtWindowSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：受击反应次数按多少秒统计一轮。默认 60 秒。"), cfg.hurtWindowSec, 5f, 3600f);
		cfg.injuryWindowSec = GenFloat(Loc.T("伤病惨叫统计窗口 ") + cfg.injuryWindowSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：骨折感染伤病惨叫按多少秒统计一轮。默认 60 秒。"), cfg.injuryWindowSec, 5f, 3600f);
		cfg.craftItemWindowSec = GenFloat(Loc.T("制作反应窗口 ") + cfg.craftItemWindowSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：对制作产物的反应按多少秒统计。默认 300 秒（5 分钟）。"), cfg.craftItemWindowSec, 30f, 3600f);

		GenHead(Loc.T("二、开场静默与缓冲"));
		cfg.introQuietSec = GenFloat(Loc.T("进场后静默 ") + cfg.introQuietSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：进游戏后先安静这么久，再开始自动说话。默认 20 秒。"), cfg.introQuietSec, 0f, 600f);
		cfg.introGraceSec = GenFloat(Loc.T("开场台词后缓冲 ") + cfg.introGraceSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：入场台词说完后，这段时间内不触发受击/事件反应。默认 6 秒。"), cfg.introGraceSec, 0f, 600f);

		GenHead(Loc.T("三、自慰与生理时间轴"));
		cfg.mastActingAt = GenFloat(Loc.T("接受指令 → 开始动作 ") + cfg.mastActingAt.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：点下按钮后先酝酿多久才动手。默认 20 秒。"), cfg.mastActingAt, 1f, 600f);
		cfg.mastFasterAt = GenFloat(Loc.T("开始动作 → 加快 ") + cfg.mastFasterAt.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：开始动作到这个时间点转入加快阶段。默认 40 秒。"), cfg.mastFasterAt, 2f, 900f);
		cfg.mastPeakAt = GenFloat(Loc.T("加快 → 快感上脑 ") + cfg.mastPeakAt.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：到这个时间点进入放空大脑（每秒回血）。默认 60 秒。"), cfg.mastPeakAt, 3f, 1800f);
		cfg.mastBeatEvery = GenFloat(Loc.T("反应间隔 ") + cfg.mastBeatEvery.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：放空大脑后每隔多久说一句反应。默认 30 秒。"), cfg.mastBeatEvery, 1f, 600f);
		cfg.mastEndAt = GenFloat(Loc.T("硬性结束 ") + cfg.mastEndAt.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：一直不点高潮的话，到这个时间自动结束。默认 600 秒。"), cfg.mastEndAt, 5f, 7200f);
		cfg.mastLingerSec = GenFloat(Loc.T("高潮后保留放空 ") + cfg.mastLingerSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：结束后放空大脑再多维持这么久（继续回血）。默认 30 秒。"), cfg.mastLingerSec, 0f, 600f);
		cfg.debtSleepNeed = GenFloat(Loc.T("亏空解除所需连续睡眠 ") + cfg.debtSleepNeed.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：连续睡够这么久解除性亏空；中途醒来重新计时。默认 60 秒。"), cfg.debtSleepNeed, 5f, 600f);

		GenHead(Loc.T("四、直播弹幕与查询计时"));
		cfg.danmakuResponseWindowSec = GenFloat(Loc.T("弹幕回应统计窗口 ") + cfg.danmakuResponseWindowSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：弹幕回应额度按多少秒一轮统计。默认 60 秒。"), cfg.danmakuResponseWindowSec, 5f, 3600f);
		cfg.danmakuDisplayWindowSec = GenFloat(Loc.T("弹幕显示限速窗口 ") + cfg.danmakuDisplayWindowSec.ToString("F2") + Loc.T(" 秒"), Loc.T("作用：显示限速按多少秒统计。默认 1 秒。"), cfg.danmakuDisplayWindowSec, 0.1f, 60f);
		cfg.danmakuDedupSec = GenFloat(Loc.T("同一句去重记忆 ") + cfg.danmakuDedupSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：同一句话在此时间内只显示一次。默认 30 秒。"), cfg.danmakuDedupSec, 0f, 3600f);
		cfg.balanceRefreshSec = GenFloat(Loc.T("余额刷新间隔 ") + cfg.balanceRefreshSec.ToString("F0") + Loc.T(" 秒"), Loc.T("作用：右下角小窗多久查一次 API 余额。默认 60 秒。"), cfg.balanceRefreshSec, 15f, 3600f);

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(100f), GUILayout.Height(30f)))
		{
			cfg.reactionWindowSec = 60f;
			cfg.autoCooldownSec = 18f;
			cfg.hurtWindowSec = 60f;
			cfg.injuryWindowSec = 60f;
			cfg.craftItemWindowSec = 300f;
			cfg.introQuietSec = 20f;
			cfg.introGraceSec = 6f;
			cfg.mastActingAt = 20f;
			cfg.mastFasterAt = 40f;
			cfg.mastPeakAt = 60f;
			cfg.mastEndAt = 600f;
			cfg.mastBeatEvery = 30f;
			cfg.mastLingerSec = 30f;
			cfg.debtSleepNeed = 60f;
			cfg.danmakuResponseWindowSec = 60f;
			cfg.danmakuDisplayWindowSec = 1f;
			cfg.danmakuDedupSec = 30f;
			cfg.balanceRefreshSec = 60f;
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认计时");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}

	private void DrawReact_Quotas()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;

		GUILayout.Label(Loc.T("控制各触发通道在统计时间窗内允许消耗的发言次数上限与生理热量门槛。填 0 = 彻底关闭该类自发反应。"), styleSmall);
		scrollQuotas = GUILayout.BeginScrollView(scrollQuotas, GUILayout.Height(ScrollHWithSub));

		GenHead(Loc.T("一、自发与互动开口额度"));
		cfg.reactionsPerWindow = GenInt("reactionsPerWindow", Loc.T("时间窗内最多开口次数（") + cfg.reactionsPerWindow + Loc.P("）"), Loc.T("作用：一个时间窗里允许多少次「自发」生成。默认 5，范围 0–99；0 = 只剩你打字才说话。"), cfg.reactionsPerWindow, 0, 99);
		cfg.hurtReactions = GenInt("hurtReactions", Loc.T("受击反应：窗口内最多次数（") + cfg.hurtReactions + Loc.P("）"), Loc.T("作用：受击反应的独立额度。默认 3，范围 0–99；0 = 挨打也不吭声。"), cfg.hurtReactions, 0, 99);
		cfg.injuryReactions = GenInt("injuryReactions", Loc.T("伤病惨叫：窗口内最多次数（") + cfg.injuryReactions + Loc.P("）"), Loc.T("作用：骨折、感染等伤病反应的独立额度。默认 2，范围 0–99。"), cfg.injuryReactions, 0, 99);
		cfg.craftItemReactions = GenInt("craftItemReactions", Loc.T("制作物品反应上限（") + cfg.craftItemReactions + Loc.T(" 次）"), Loc.T("作用：一个窗口内最多对「制作产物」反应几次。默认 2；0 = 完全不反应。"), cfg.craftItemReactions, 0, 20);

		GenHead(Loc.T("二、生理亏空额度"));
		cfg.debtCalorieNeed = GenInt("debtCal", Loc.T("解除所需热量 ") + cfg.debtCalorieNeed + Loc.T(" 千卡"), Loc.T("作用：从进入性亏空算起，累计吃进这么多热量就解除。0 = 只能靠睡觉解除。默认 200。"), cfg.debtCalorieNeed, 0, 5000);

		GenHead(Loc.T("三、直播弹幕额度"));
		cfg.danmakuPerMin = GenInt("danmakuPerMin", Loc.T("每分钟最多回应几条（") + cfg.danmakuPerMin + Loc.P("）"), Loc.T("作用：每分钟最多挑几条弹幕让角色开口；0 = 全部只显示不回应。默认 3，范围 0–60。"), cfg.danmakuPerMin, 0, 60);
		cfg.danmakuMaxPerSec = GenInt("danmakuMaxPerSec", Loc.T("显示限速（") + cfg.danmakuMaxPerSec + Loc.T(" 条/窗口）"), Loc.T("作用：一个窗口内最多显示几条，超出的直接丢弃防刷屏。默认 5。"), cfg.danmakuMaxPerSec, 1, 200);
		cfg.danmakuHistoryMax = GenInt("danmakuHistoryMax", Loc.T("历史框最多留几条弹幕（") + cfg.danmakuHistoryMax + Loc.P("）"), Loc.T("作用：弹幕在历史框里的独立额度，和对话记录分开算。默认 300。"), cfg.danmakuHistoryMax, 0, 2000);

		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(140f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(100f), GUILayout.Height(30f)))
		{
			cfg.reactionsPerWindow = 5;
			cfg.hurtReactions = 3;
			cfg.injuryReactions = 2;
			cfg.craftItemReactions = 2;
			cfg.debtCalorieNeed = 200;
			cfg.danmakuPerMin = 3;
			cfg.danmakuMaxPerSec = 5;
			cfg.danmakuHistoryMax = 300;
			editBuf.Clear();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("已恢复默认额度");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}
	#endregion

	#region Main Tab 2: LLM接口
	private void DrawApi()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null)
		{
			if (cfg.api == null)
			{
				cfg.api = new ApiSettings();
			}
			scrollApi = GUILayout.BeginScrollView(scrollApi, GUILayout.Height(Mathf.Max(120f, contentH - 45f)));
			GUILayout.Label(Loc.T("接口类型"), styleLabel);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button((cfg.api.provider == "openai") ? Loc.T("● OpenAI 兼容") : Loc.T("○ OpenAI 兼容"), styleBtn, GUILayout.Width(150f)))
			{
				cfg.api.provider = "openai";
			}
			if (GUILayout.Button((cfg.api.provider == "anthropic") ? "● Anthropic" : "○ Anthropic", styleBtn, GUILayout.Width(120f)))
			{
				cfg.api.provider = "anthropic";
			}
			GUILayout.EndHorizontal();
			GUILayout.Label(Loc.T("接口地址 baseUrl"), styleLabel);
			cfg.api.baseUrl = GUILayout.TextField(cfg.api.baseUrl ?? "", styleField);
			GUILayout.Label("API Key", styleLabel);
			cfg.api.apiKey = GUILayout.TextField(cfg.api.apiKey ?? "", styleField);
			GUILayout.Label(Loc.T("模型 model"), styleLabel);
			cfg.api.model = GUILayout.TextField(cfg.api.model ?? "", styleField);
			GUILayout.Space(6f);
			GUILayout.Label(Loc.T("超时（秒）"), styleLabel);
			GUILayout.Label(Loc.T("作用：一次请求最多等多久，超时就放弃这次发言（回落到本地兜底台词），不会卡住游戏。"), styleSmall);
			cfg.api.timeoutSec = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)cfg.api.timeoutSec, 5f, 120f));
			GUILayout.Space(6f);
			cfg.api.maxTokens = GenInt("apiMaxTokens", Loc.T("单次发言 token 上限（") + cfg.api.maxTokens + Loc.P("）"), Loc.T("作用：单次回复最多生成的 token 数。范围 60–4000。"), cfg.api.maxTokens, 60, 4000);
			cfg.api.showThinking = GenBool(Loc.T("显示「思考中…」提示"), Loc.T("作用：等待模型回复时在历史框右上角显示「思考中…」。"), cfg.api.showThinking);
			cfg.streamMode = GenBool(Loc.T("直播模式"), Loc.T("作用：开启后涉及私密或暴力的描写改用氛围与动作暗示，不写直白细节。"), cfg.streamMode);
			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(Loc.T("保存"), styleBtn, GUILayout.Width(90f), GUILayout.Height(30f)))
			{
				CompanionPlugin.SaveAll();
				apiMsg = Loc.T("已保存：") + DateTime.Now.ToString("HH:mm:ss");
			}
			if (GUILayout.Button(Loc.T("测试连接"), styleBtn, GUILayout.Width(90f), GUILayout.Height(30f)))
			{
				apiMsg = Loc.T("测试中…");
				TestApi(cfg.api);
			}
			GUILayout.Label(apiMsg, styleSmall);
			GUILayout.EndHorizontal();
		}
	}

	private async void TestApi(ApiSettings api)
	{
		try
		{
			List<ChatMsg> list = new List<ChatMsg>();
			list.Add(new ChatMsg("user", Loc.T("只回两个字：在。")));
			string text = await Llm.ChatAsync(api, list, CancellationToken.None);
			apiMsg = (string.IsNullOrEmpty(text) ? Loc.T("无回复（看日志）") : (Loc.T("返回：") + text));
			CompanionPlugin.Log.LogInfo("[api] 编辑器测试返回：" + text);
		}
		catch (Exception ex)
		{
			apiMsg = Loc.T("失败：") + ex.Message;
			CompanionPlugin.Log.LogWarning("[api] 编辑器测试失败：" + ex.Message);
		}
	}
	#endregion

	#region Main Tab 3: 角色图鉴
	private void DrawMainCodex()
	{
		DrawSubTabBar(SubTabsCodex, ref subTabCodex);
		switch (subTabCodex)
		{
			case 0:
				DrawCodex_Persona();
				break;
			case 1:
				DrawCodex_Codex();
				break;
		}
	}

	private void DrawCodex_Persona()
	{
		PersonaStore store = CompanionPlugin.Store;
		int num = 0;
		try
		{
			if ((Object)(object)PreRunScript.instance != (Object)null)
			{
				num = Mathf.Clamp(PreRunScript.characterSelected, 0, 2);
			}
			else if ((Object)(object)CompanionHost.Instance != (Object)null && (Object)(object)CompanionHost.Instance.CurrentBody != (Object)null)
			{
				num = Mathf.Clamp(CompanionHost.Instance.CurrentBody.charType, 0, 2);
			}
		}
		catch
		{
		}
		Persona persona = store?.Get(num);
		GUILayout.Label(Loc.T("当前激活角色槽位（角色 ") + (num + 1) + Loc.T("）：人设、互动、颜色滑块可在原生面板里精细调节。"), styleSmall);
		GUILayout.Space(6f);
		if (persona != null)
		{
			GUILayout.Label(Loc.T("名字：") + (string.IsNullOrEmpty(persona.name) ? Loc.T("（未设置）") : persona.name), styleLabel);
			GUILayout.Label(Loc.T("称呼玩家为：") + (string.IsNullOrEmpty(persona.callPlayer) ? Loc.T("你") : persona.callPlayer), styleLabel);
			GUILayout.Label(Loc.T("性格概述：") + (string.IsNullOrEmpty(persona.personality) ? Loc.T("（未设置）") : persona.personality), styleSmall);
			GUILayout.Label(Loc.T("可用互动：") + ((persona.interactions == null || persona.interactions.Length == 0) ? Loc.T("（无）") : string.Join(Loc.P("、"), persona.interactions)), styleSmall);
		}
		GUILayout.Space(12f);
		if (GUILayout.Button(Lang.T("openPersona") + " (F7)", styleBtn, GUILayout.Width(260f), GUILayout.Height(36f)))
		{
			open = false;
			NativePanel.OpenAnywhere();
		}
		GUILayout.Space(8f);
		GUILayout.Label(Loc.T("提示：主菜单按 F4 可开关本设置菜单；F7 可随时呼出原生人设面板。"), styleSmall);
	}

	private void DrawCodex_Codex()
	{
		CodexView.Draw(styleHead, styleLabel, styleSmall, styleField, styleBtn);
	}
	#endregion

	#region Main Tab 4: MOD界面
	private void DrawMainUi()
	{
		DrawSubTabBar(SubTabsUi, ref subTabUi);
		switch (subTabUi)
		{
			case 0:
				DrawUi();
				break;
			case 1:
				DrawLanguage();
				break;
		}
	}

	private void DrawUi()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null)
		{
			scrollUiSettings = GUILayout.BeginScrollView(scrollUiSettings, GUILayout.Height(ScrollHWithSub));
			GenHead(Loc.T("界面比例"));
			GUILayout.Label(Loc.T("界面比例 ") + Mathf.RoundToInt(cfg.uiScale * 100f) + "%", styleLabel);
			GUILayout.Label(Loc.T("作用：整体放大或缩小模组自己的文字和 UI（设置窗、人设窗、历史对话面板、输入条、互动栏、状态表一起变），游戏本体的界面不受影响。100% = 原样；字太小就往上调，觉得挤就往下调。"), styleSmall);
			cfg.uiScale = Mathf.Round(GUILayout.HorizontalSlider(cfg.uiScale, 0.7f, 2f) * 20f) / 20f;
			GenHead(Loc.T("历史对话面板"));
			cfg.hudHistoryTop = GenFloat(Loc.T("距屏幕顶部（像素）"), Loc.T("作用：面板顶边到屏幕上边缘的距离。默认 200，正好让开左上角的物品栏。"), cfg.hudHistoryTop, 0f, 1200f);
			cfg.hudHistoryRight = GenFloat(Loc.T("距屏幕右侧（像素）"), Loc.T("作用：面板右边到屏幕右边缘的距离；拖到 0 = 自动（保持贴左侧 40 的位置）。往右拖面板整体右移。"), cfg.hudHistoryRight, 0f, 2000f);
			GUILayout.Label(Loc.T("背景不透明度 ") + Mathf.RoundToInt(cfg.hudHistoryAlpha * 100f) + "%", styleLabel);
			GUILayout.Label(Loc.T("作用：面板底色和四条边框的透明度。0% = 只剩文字浮在画面上；文字颜色、按钮、滚动条都不受影响。"), styleSmall);
			cfg.hudHistoryAlpha = GUILayout.HorizontalSlider(cfg.hudHistoryAlpha, 0f, 1f);
			GenHead(Loc.T("呼出按钮"));
			cfg.hudTabBottom = GenFloat(Loc.T("距屏幕底边（像素）"), Loc.T("作用：「陪伴对话」按钮到底边的距离，数字越小按钮越靠下。"), cfg.hudTabBottom, 0f, 1500f);
			GUILayout.EndScrollView();
			GUILayout.Space(8f);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
			{
				CompanionPlugin.SaveAll();
				apiMsg = Loc.T("界面设置已保存 ") + DateTime.Now.ToString("HH:mm:ss");
			}
			if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
			{
				cfg.hudHistoryTop = 200f;
				cfg.hudHistoryRight = 0f;
				cfg.hudHistoryAlpha = 0.93f;
				cfg.hudTabBottom = 380f;
				cfg.uiScale = 1f;
				CompanionPlugin.SaveAll();
				apiMsg = Loc.T("界面设置已恢复默认");
			}
			GUILayout.Label(apiMsg, styleSmall);
			GUILayout.EndHorizontal();
		}
	}

	private void DrawLanguage()
	{
		GUILayout.Label(Loc.T("界面与角色数据的语言。每种语言各自一套 settings / personas 文件；切走再切回来，你在那个语言下改过的内容还在。"), styleSmall);
		GUILayout.Space(10f);
		GUILayout.Label(Loc.T("当前：") + Lang.NativeName(Lang.Current) + Loc.P("（") + Lang.Code(Lang.Current) + Loc.P("）"), styleHead);
		GUILayout.Space(10f);
		int num = 4;
		LangId[] all = Lang.All;
		for (int i = 0; i < all.Length; i += num)
		{
			GUILayout.BeginHorizontal();
			for (int j = i; j < all.Length && j < i + num; j++)
			{
				LangId langId = all[j];
				bool flag = langId == Lang.Current;
				Color color = GUI.color;
				GUI.color = flag ? new Color(0.6f, 0.8f, 1f) : Color.white;
				if (GUILayout.Button(Lang.NativeName(langId), styleBtn, GUILayout.Width(200f), GUILayout.Height(38f)) && !flag)
				{
					CompanionPlugin.SwitchLanguage(langId);
				}
				GUI.color = color;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(6f);
		}
		GUILayout.Space(12f);
		GenHead(Loc.T("LLM 回复输出语言指令 (可在此手动修改，点击上方语言按钮会自动覆盖为该语言默认值)"));
		GUILayout.Label(Loc.T("作用：直接作为最终系统指令传给模型，要求模型按指定语言生成台词。切换上方语言会自动同步重置，也可在此输入你自定义的方言、口音或特殊语言要求："), styleSmall);
		if (CompanionPlugin.Cfg != null)
		{
			if (CompanionPlugin.Cfg.prompt == null) CompanionPlugin.Cfg.prompt = new PromptConfig();
			CompanionPlugin.Cfg.prompt.languagePrompt = GUILayout.TextArea(CompanionPlugin.Cfg.prompt.languagePrompt ?? PromptConfig.GetDefaultLanguagePrompt(Lang.Current), styleArea, GUILayout.MinHeight(55f));
		}

		GUILayout.Space(10f);
		GUILayout.Label(Loc.T("切换后局内 HUD 会重建；若界面文字没变，按 F4 关掉菜单再开一次。"), styleSmall);
	}
	#endregion

	#region Main Tab 5: 双人模式
	private void DrawDual()
	{
		DrawSubTabBar(SubTabsDual, ref subTabDual);
		switch (subTabDual)
		{
		case 0:
			DrawDual_General();
			break;
		case 1:
			DrawDual_Ai();
			break;
		}
	}

	private void DrawDual_General()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;
		scrollDual = GUILayout.BeginScrollView(scrollDual, GUILayout.Height(ScrollHWithSub));
		GUILayout.BeginVertical(styleBox);
		GUILayout.Label(Loc.T("该模式固定方案：P1Alloy（它被改造之前是一只优秀的Milky，现在它是一具优秀的遥控机器人和保镖，据它自己所言，它并不抗拒）P2EXX（你在选定投送物种时选择的物种，它们并不知道自己的主人废了多大的努力将P1安排在它们身边，但无论如何，总体来说，Alloy是个麻烦的家伙）"), styleLabel);
		GUILayout.EndVertical();
		GUILayout.Space(6f);
		GenHead(Loc.T("总开关"));
		cfg.dualMode = GenBool(Loc.T("启用双人模式"), Loc.T("开启后，同步投送一只Alloy改进型Milky协助你的P2生物，注意，这个行为并不会让任务变得更加简单，Alloy型生物只改造了大脑和脊柱，这让其拥有巨大的力量和智慧，以及被远程操控的能力，但其本质终究是一个生物体，在完全被摧毁行动能力之前，它同样需要补给来保持生命进程"), cfg.dualMode);
		GUILayout.Label(Loc.T("操作提示：历史框下方会显示「切换 / 背起 / 抓胳膊 / 召回」四个按钮，按住 Alt 点击生效。快捷键：F9 切换操控、Alt+F9 背起/放下、Ctrl+F9 抓胳膊、Shift+F9 召回。"), styleSmall);

		cfg.dualStartAsPlayer = GenBool(Loc.T("开局操控 P1Alloy"), Loc.T("作用：开 = 进图先控 P1Alloy；关 = 先控 P2EXX（选定的原版身体），P1Alloy 在旁边跟着。任何时候都能按切换键换过去。"), cfg.dualStartAsPlayer);
		GenHead(Loc.T("文案与旁白"));
		cfg.dualNarratorText = GenBool(Loc.T("启用旁白文案"), Loc.T("作用：开启后，显示双人模式中的动作、切换及状态等【旁白】提示文案（默认关）。"), cfg.dualNarratorText);
		cfg.dualPartnerText = GenBool(Loc.T("启用 P2 硬编码文案"), Loc.T("作用：开启后，P2 角色在召回、被背起等动作时触发内置硬编码对白；关闭时发言替换为「...」（默认关）。"), cfg.dualPartnerText);
		GenHead(Loc.T("机器核心"));
		cfg.dualMachineCore = GenBool(Loc.T("机器核心：锁定 P1Alloy 的状态"), Loc.T("作用：打开后，「机器核心」这条状态会挂在 P1Alloy 身上，同时锁住它的意识=100、脑状态=100（不会脑死亡）、情绪=100、精力≥96、睡眠与睡眠债清零、休克与疼痛休克=0、心颤复位；你操控它倒地时会自动站起来。关掉 = 这些全都不生效，P1Alloy 和普通实验体一样会疼、会睡、会累、会因脑损伤而死，状态图标也不再显示，只有死亡不会自动重生仍然有效。"), cfg.dualMachineCore);
		GenHead(Loc.T("性能"));
		GUILayout.Label(Loc.T("P1Alloy 移速倍率 ") + cfg.dualPlayerSpeed.ToString("0.00") + Loc.T(" × P2 基础速度"), styleLabel);
		cfg.dualPlayerSpeed = GUILayout.HorizontalSlider(cfg.dualPlayerSpeed, 0.2f, 3f);
		GUILayout.Label(Loc.T("P2 移速倍率 ") + cfg.dualPartnerSpeed.ToString("0.00") + Loc.T(" × 原速"), styleLabel);
		cfg.dualPartnerSpeed = GUILayout.HorizontalSlider(cfg.dualPartnerSpeed, 0.2f, 3f);
		cfg.dualPlayerSkill = GenInt("dualSkill", Loc.T("P1Alloy 力 / 韧 / 智等级"), Loc.T("作用：把这三项一次性设成同一个等级（默认 15）。改完经验条会重新起算，不影响后续正常升级。"), cfg.dualPlayerSkill, 1, 100);
		GenHead(Loc.T("芯片"));
		cfg.dualPlayerChip = GenBool(Loc.T("P1Alloy 植入芯片"), Loc.T("作用：P1Alloy 开局自带一枚芯片（背包里的「aichip」）。训练员能通过脑机看到它的状态，就是靠这个。"), cfg.dualPlayerChip);
		cfg.dualPartnerChip = GenBool(Loc.T("P2 植入芯片"), Loc.T("作用：决定 P2 开局有没有芯片。关掉 = 它没有脑机接口，只能靠自己的眼睛和耳朵跟着你。"), cfg.dualPartnerChip);
		cfg.dualPlayerKit = GenBool(Loc.T("P1Alloy 开局带背包 + 步枪"), Loc.T("作用：额外给 P1Alloy 一个大背包、一把步枪、一个弹匣和 3 个随机医疗物品。默认关（它只是另一只实验体）。"), cfg.dualPlayerKit);
		GenHead(Loc.T("改造（落地自带义肢）"));
		cfg.p1ProstheticArmA = GenCycle(Loc.T("上臂 A"), cfg.p1ProstheticArmA);
		cfg.p1ProstheticArmB = GenCycle(Loc.T("上臂 B"), cfg.p1ProstheticArmB);
		cfg.p1ProstheticLegA = GenCycle(Loc.T("大腿 A"), cfg.p1ProstheticLegA);
		cfg.p1ProstheticLegB = GenCycle(Loc.T("大腿 B"), cfg.p1ProstheticLegB);
		GUILayout.Label(Loc.T("A / B 对应游戏里的 UpperArmA、UpperArmB、UpperLegA、UpperLegB 部件名。"), styleSmall);
		GenHead(Loc.T("背负与按键"));
		cfg.dualCarryOffsetY = GenFloat(Loc.T("背在背上时的高度偏移"), Loc.T("作用：被背起来以后，相对背负者身体的垂直位置。默认 0.42（肩背高度）；看起来悬空或陷进身体就调这里。"), cfg.dualCarryOffsetY, -0.5f, 1.5f);
		cfg.dualCarryBack = GenFloat(Loc.T("背在背上时往身后的偏移"), Loc.T("作用：被背者往背负者身后偏移的距离，防止两具身体叠在一起。默认 0.55；负值会往前挪。"), cfg.dualCarryBack, -0.5f, 2f);
		cfg.dualSwitchKey = GenInt("dualKey", Loc.T("切换操控的按键代码"), Loc.T("作用：Unity KeyCode 数值。298 = F9（默认）。Alt+F9 背起/放下、Ctrl+F9 抓胳膊、Shift+F9 召回。常用：F7=288、F9=298、F10=299、Tab=9、Q=113。"), cfg.dualSwitchKey, 0, 400);
		GenHead(Loc.T("状态表"));
		cfg.dualShowStatus = GenBool(Loc.T("显示两名实验体的状态表（右上角）"), Loc.T("作用：右上角两列分别显示 P1Alloy（脑机接管）与 P2（原生生理）的意识、血量、疼痛、心率、体温、饥饿、体力、情绪、骨折脱臼感染。两者各自独立结算。游戏自带的左下角本体状态不受影响，它会跟着你当前操控的那一位走。"), cfg.dualShowStatus);
		cfg.dualStatusX = GenFloat(Loc.T("状态表距屏幕右边的距离"), Loc.T("作用：像素，默认 40。表右边缘贴到屏幕右侧减去这个值。"), cfg.dualStatusX, -500f, 1600f);
		cfg.dualStatusY = GenFloat(Loc.T("状态表距屏幕顶部的距离"), Loc.T("作用：像素，默认 40。表顶边距离屏幕顶部这个值；往下挪调大。"), cfg.dualStatusY, -200f, 1200f);
		GUILayout.EndScrollView();
		DrawDualFooter();
	}

	private void DrawDual_Ai()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null) return;

		scrollDualAi = GUILayout.BeginScrollView(scrollDualAi, GUILayout.Height(ScrollHWithSub));

		// 1. P2 AI 开关
		GenHead(Loc.T("P2 AI 总开关"));
		cfg.dualAi = GenBool(Loc.T("启用 P2 AI (神经首脑调度)"), Loc.T("开启后，P2EXpie 将由首脑神经网络调度系统接管物理与行为驱动；关闭后清空一切自动行为，保持完全静止。"), cfg.dualAi);

		GUILayout.Space(8f);

		// 2. [等待加入更多功能的UI空] -> 已接入神经元参数面板
		GUILayout.BeginVertical(styleBox);
		GUILayout.Label(Loc.T("【P2 神经网络实时参数与调试】"), styleHead);

		cfg.p2AiDebug = GenBool(Loc.T("AI Debug 画面可视化"), Loc.T("开启后，将在玩家视野中常态高亮渲染感知物块网格（绿色脆弱/红色强硬/蓝色液体/空气框）及多级寻路行动路线（最新绿、备选红、历史灰）。"), cfg.p2AiDebug);

		GUILayout.Label(Loc.T("感知扫描半径：") + cfg.p2ScanRadius.ToString("F0") + Loc.T(" 格（最高全图 50000）"), styleLabel);
		GUILayout.Label(Loc.T("以 P2 角色为中心全程实时扫描周围地形的半径格数。可拖动滑块，亦可直接在输入框中键入数值。"), styleSmall);
		GUILayout.BeginHorizontal();
		float newRad = GUILayout.HorizontalSlider(Mathf.Clamp(cfg.p2ScanRadius, 4f, 50000f), 4f, 50000f, GUILayout.Width(280f));
		if (Mathf.Abs(newRad - cfg.p2ScanRadius) > 0.5f)
		{
			cfg.p2ScanRadius = Mathf.Round(newRad);
			p2ScanRadiusInputStr = cfg.p2ScanRadius.ToString("F0");
		}
		if (p2ScanRadiusInputStr == null)
		{
			p2ScanRadiusInputStr = cfg.p2ScanRadius.ToString("F0");
		}
		GUILayout.Space(8f);
		string inputStr = GUILayout.TextField(p2ScanRadiusInputStr, GUILayout.Width(75f));
		if (inputStr != p2ScanRadiusInputStr)
		{
			p2ScanRadiusInputStr = inputStr;
			if (float.TryParse(inputStr, out float parsed) && parsed >= 4f)
			{
				cfg.p2ScanRadius = Mathf.Clamp(parsed, 4f, 50000f);
			}
		}
		GUILayout.Label(Loc.T("格"), styleLabel, GUILayout.Width(25f));
		if (GUILayout.Button(Loc.T("全图 50000"), styleBtn, GUILayout.Width(90f)))
		{
			cfg.p2ScanRadius = 50000f;
			p2ScanRadiusInputStr = "50000";
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(4f);
		GenHead(Loc.T("影子路径观察器 (Shadow Jump Observer)"));
		cfg.p2ShadowJumpRender = GenBool(Loc.T("玩家跳跃轨迹渲染 (亮蓝绿线)"), Loc.T("开启后，在游戏画面中实时高亮渲染玩家跳跃的亮蓝绿色物理抛物线轨迹。"), cfg.p2ShadowJumpRender);
		cfg.p2ShadowJumpRecord = GenBool(Loc.T("玩家跳跃数据持久化记录"), Loc.T("开启后，在每次跳跃落地时自动采集跳跃动力学参数与周围区格地貌，并追加保存到记录文件中。"), cfg.p2ShadowJumpRecord);
		GUILayout.Label(Loc.T("影子记录文件存储地址："), styleLabel);
		cfg.p2ShadowRecordPath = GUILayout.TextField(cfg.p2ShadowRecordPath ?? "BepInEx/config/CUCompanion/player_jump_observations.jsonl", styleField);

		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("· 战术控制控件：游戏中随时按 [键盘 4] 打开仿物品栏圆环【P2 战术指令菜单】(停下/召回/玩耍)\n· 视觉系统：集成视场快照能力 (P2VisionNeuron)\n· 感知系统：物块分类实时探测 (P2PerceptionNeuron)\n· 寻路系统：多级路线、水平直线、跳跃曲线、贴墙下坠 (P2NavNeuron)\n· 动作系统：方向锚定、下坠贴墙推移、虚拟输入注入 (P2MotorNeuron)\n· 影子观察器：亮蓝绿色记录玩家实机跳跃物理轨迹与周围地貌 (P2ShadowObserver)"), styleSmall);
		GUILayout.EndVertical();

		GUILayout.Space(8f);

		// 3. AI 首脑代码框
		GenHead(Loc.T("AI 首脑代码框"));
		GUILayout.Label(Loc.T("说明：在此配置/查看所有组件调用和总控代码，未来根据要求添加组件组装整套 AI 系统，按照神经网络来构建并由首脑统一互相驱动。"), styleSmall);

		if (string.IsNullOrEmpty(cfg.p2BrainCode))
		{
			cfg.p2BrainCode = ModConfig.DefaultP2BrainCode;
		}

		cfg.p2BrainCode = GUILayout.TextArea(cfg.p2BrainCode, styleArea, GUILayout.MinHeight(340f));

		GUILayout.Space(6f);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("恢复首脑默认代码"), styleBtn, GUILayout.Width(160f), GUILayout.Height(26f)))
		{
			cfg.p2BrainCode = ModConfig.DefaultP2BrainCode;
			apiMsg = Loc.T("已重置首脑调度代码模板");
		}
		GUILayout.EndHorizontal();

		GUILayout.EndScrollView();
		DrawDualFooter();
	}

	private void DrawDualFooter()
	{
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Loc.T("保存到配置文件"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
		{
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("双人模式设置已保存 ") + DateTime.Now.ToString("HH:mm:ss");
		}
		if (GUILayout.Button(Loc.T("恢复默认"), styleBtn, GUILayout.Width(150f), GUILayout.Height(30f)))
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			cfg.dualMode = false; cfg.dualStartAsPlayer = false; cfg.dualPlayerSpeed = 1f; cfg.dualPartnerSpeed = 1f; cfg.dualPlayerSkill = 15; cfg.dualPlayerChip = true; cfg.dualPartnerChip = true; cfg.dualPlayerKit = false; cfg.p1ProstheticArmA = 0; cfg.p1ProstheticArmB = 0; cfg.p1ProstheticLegA = 0; cfg.p1ProstheticLegB = 0; cfg.dualCarryOffsetY = 0.42f; cfg.dualCarryBack = 0.55f; cfg.dualMachineCore = true; cfg.dualShowStatus = true; cfg.dualStatusX = 40f; cfg.dualStatusY = 40f; cfg.dualSwitchKey = 298;
			cfg.dualNarratorText = false; cfg.dualPartnerText = false;
			cfg.dualAiWander = true;
			cfg.p2BrainCode = ModConfig.DefaultP2BrainCode;
			editBuf.Clear();
			CompanionPlugin.SaveAll();
			apiMsg = Loc.T("双人模式已恢复默认");
		}
		GUILayout.Label(apiMsg, styleSmall);
		GUILayout.EndHorizontal();
	}
	#endregion

	#region Helper UI Elements
	private void GenHead(string t)
	{
		GUILayout.Space(10f);
		GUILayout.Label(t, styleHead, GUILayout.MinHeight(26f));
	}

	private int GenInt(string id, string label, string desc, int value, int min, int max)
	{
		GUILayout.Label(label, styleLabel);
		GUILayout.Label(desc, styleSmall);
		if (!editBuf.TryGetValue(id, out var value2) || value2 == null)
		{
			value2 = value.ToString();
		}
		value2 = GUILayout.TextField(value2, styleField, GUILayout.Width(110f));
		editBuf[id] = value2;
		if (int.TryParse(value2, out var result))
		{
			return Mathf.Clamp(result, min, max);
		}
		return value;
	}

	private float GenFloat(string label, string desc, float value, float min, float max)
	{
		GUILayout.Label(label, styleLabel);
		GUILayout.Label(desc, styleSmall);
		return GUILayout.HorizontalSlider(value, min, max);
	}

	/// <summary>点一下循环：不装 → 木桩 → 钢制 → 强化 → 不装。</summary>
	private int GenCycle(string label, int value)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, styleLabel, GUILayout.Width(90f));
		if (GUILayout.Button(ProstheticKit.TierLabel(value), styleBtn, GUILayout.Width(150f)))
		{
			value = (value + 1) % 4;
		}
		GUILayout.EndHorizontal();
		return value;
	}

	private bool GenBool(string label, string desc, bool value)
	{
		GUILayout.Label(label, styleLabel);
		GUILayout.Label(desc, styleSmall);
		return GUILayout.Toggle(value, value ? Loc.T("  开") : Loc.T("  关"), styleLabel, GUILayout.Width(90f));
	}

	private void PlayTypingPreview(float vol)
	{
		if (vol <= 0.001f || Time.unscaledTime - lastPreviewAt < 0.14f)
		{
			return;
		}
		lastPreviewAt = Time.unscaledTime;
		try
		{
			Camera main = Camera.main;
			Vector3 val = (((Object)(object)main != (Object)null) ? ((Component)main).transform.position : Vector3.zero);
			int num = 0;
			try
			{
				if ((Object)(object)PreRunScript.instance != (Object)null)
				{
					num = PreRunScript.characterSelected;
				}
				else if ((Object)(object)CompanionHost.Instance != (Object)null && (Object)(object)CompanionHost.Instance.CurrentBody != (Object)null)
				{
					num = CompanionHost.Instance.CurrentBody.charType;
				}
			}
			catch
			{
			}
			Sound.Play(num switch
			{
				2 => "speechdune",
				1 => "milspeech",
				_ => "speech",
			}, (Vector2)val, false, true, null, Mathf.Clamp(vol, 0f, 1f), 1f, false, false);
		}
		catch
		{
		}
	}
	#endregion

	#region Main Tab 6: 关于 (About & Credits)
	private Vector2 scrollAbout;
	private static Texture2D aboutArtTexture1;
	private static Texture2D aboutArtTexture2;
	private static bool aboutArtLoaded;

	private static byte[] LoadResourceBytes(string filename)
	{
		try
		{
			var assm = typeof(SettingsEditorGui).Assembly;
			foreach (var name in assm.GetManifestResourceNames())
			{
				if (name.EndsWith(filename, StringComparison.OrdinalIgnoreCase))
				{
					using (var s = assm.GetManifestResourceStream(name))
					{
						if (s != null)
						{
							byte[] b = new byte[s.Length];
							s.Read(b, 0, b.Length);
							return b;
						}
					}
				}
			}
			string p1 = Path.Combine(BepInEx.Paths.PluginPath ?? ".", "CUCompanion", filename);
			string p2 = Path.Combine(Application.dataPath ?? ".", filename);
			if (File.Exists(p1)) return File.ReadAllBytes(p1);
			if (File.Exists(p2)) return File.ReadAllBytes(p2);
		}
		catch { }
		return null;
	}

	private static void EnsureAboutArt()
	{
		if (aboutArtLoaded) return;
		aboutArtLoaded = true;
		try
		{
			byte[] data1 = LoadResourceBytes("about_artwork1.png") ?? LoadResourceBytes("about_artwork.png");
			if (data1 != null && data1.Length > 0)
			{
				Texture2D tex1 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				if (ImageConversion.LoadImage(tex1, data1)) aboutArtTexture1 = tex1;
			}

			byte[] data2 = LoadResourceBytes("about_artwork2.png");
			if (data2 != null && data2.Length > 0)
			{
				Texture2D tex2 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				if (ImageConversion.LoadImage(tex2, data2)) aboutArtTexture2 = tex2;
			}
		}
		catch { }
	}

	private void DrawAbout()
	{
		scrollAbout = GUILayout.BeginScrollView(scrollAbout, GUILayout.Height(ScrollHWithSub + 36f));

		GUILayout.BeginVertical(styleBox);
		GUILayout.Label("About & Credits", styleHead);
		GUILayout.Label("Casualties: Unknown · Companion Mod (CUCompanion)", styleLabel);
		GUILayout.EndVertical();

		GUILayout.Space(10f);

		// Project Creators
		GUILayout.BeginVertical(styleBox);
		GenHead("Project Creators");
		GUILayout.Label("Creator 1:  Hchime-ai", styleLabel);
		GUILayout.Label("Creator 2:  meowgreen", styleLabel);
		GUILayout.Label("Creator 3:  小褐XHchime-ai", styleLabel);
		GUILayout.Space(4f);
		GUILayout.Label("Development Tooling:  Codex-type tools", styleSmall);
		GUILayout.EndVertical();

		// Official Links & Support
		GUILayout.BeginVertical(styleBox);
		GenHead("Official Links & Support");
		GUILayout.Label("Click any button below to open the official link in your browser:", styleSmall);
		GUILayout.Space(8f);

		DrawLinkButton("Creator Support (Patreon)", "https://www.patreon.com/cw/MeowgreenXiaoLu67");
		DrawLinkButton("Developer GitHub", "https://github.com/Hchime-AI");
		DrawLinkButton("Developer X (Twitter)", "https://x.com/Lgreen_mie67");
		DrawLinkButton("Developer YouTube", "https://www.youtube.com/@Hchime-ai");
		DrawLinkButton("Developer Discord", "https://discord.gg/4F7cYhKpex");
		GUILayout.EndVertical();

		// Artwork Showcase (Two Concept Artworks at bottom / 垫底 & 放大)
		EnsureAboutArt();
		if (aboutArtTexture1 != null || aboutArtTexture2 != null)
		{
			GUILayout.Space(14f);
			GUILayout.BeginVertical(styleBox);
			GenHead("Original Concept Artwork");
			GUILayout.Space(8f);

			if (aboutArtTexture1 != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				Rect imgRect1 = GUILayoutUtility.GetRect(720f, 583f, GUILayout.Width(720f), GUILayout.Height(583f));
				GUI.DrawTexture(imgRect1, aboutArtTexture1, ScaleMode.ScaleToFit);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(16f);
			}

			if (aboutArtTexture2 != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				Rect imgRect2 = GUILayoutUtility.GetRect(620f, 613f, GUILayout.Width(620f), GUILayout.Height(613f));
				GUI.DrawTexture(imgRect2, aboutArtTexture2, ScaleMode.ScaleToFit);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(8f);
			}

			GUILayout.EndVertical();
		}

		GUILayout.EndScrollView();
	}

	private void DrawLinkButton(string label, string url)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, styleLabel, GUILayout.Width(240f));
		if (GUILayout.Button("Open Link ↗", styleBtn, GUILayout.Width(130f), GUILayout.Height(28f)))
		{
			try
			{
				Application.OpenURL(url);
			}
			catch { }
		}
		GUILayout.Space(8f);
		GUILayout.Label(url, styleSmall);
		GUILayout.EndHorizontal();
		GUILayout.Space(4f);
	}

	/// <summary>
	/// 柿黄色喷溅式中央半透明水印：UNFINISHED - WORK IN PROGRESS
	/// </summary>
	private static void DrawP2AiWatermark(Rect areaRect)
	{
		Color prevColor = GUI.color;
		Matrix4x4 prevMatrix = GUI.matrix;

		Vector2 center = new Vector2(areaRect.x + areaRect.width * 0.5f, areaRect.y + areaRect.height * 0.45f);
		GUIUtility.RotateAroundPivot(-22f, center);

		GUIStyle watermarkStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 44,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter
		};

		Rect textRect = new Rect(center.x - 340f, center.y - 70f, 680f, 140f);

		// 柿黄色喷溅式阴影重影 (暗红血溅底色)
		GUI.color = new Color(0.85f, 0.22f, 0.05f, 0.28f);
		GUI.Label(new Rect(textRect.x + 3f, textRect.y + 3f, textRect.width, textRect.height), "/// UNFINISHED - WORK IN PROGRESS ///", watermarkStyle);
		GUI.Label(new Rect(textRect.x - 2f, textRect.y + 1f, textRect.width, textRect.height), "/// UNFINISHED - WORK IN PROGRESS ///", watermarkStyle);

		// 柿黄色半透明主体 (大字号、斜向、居中覆盖)
		GUI.color = new Color(1.0f, 0.48f, 0.12f, 0.40f);
		GUI.Label(textRect, "/// UNFINISHED - WORK IN PROGRESS ///", watermarkStyle);

		GUI.matrix = prevMatrix;
		GUI.color = prevColor;
	}
	#endregion
}
