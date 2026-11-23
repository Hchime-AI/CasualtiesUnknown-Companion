using System.Collections.Generic;

namespace CUCompanion;

public class ModConfig
{
	public ApiSettings api = new ApiSettings();

	public bool historyOpen = true;

	public float histX = 28f;

	public float histY = 28f;

	public float histW = 470f;

	public float histH = 330f;

	public bool inputOpen;

	public float inputOffsetX;

	public float inputOffsetY = 22f;

	public int reactionsPerWindow = 5;

	public int craftItemReactions = 2;

	public float craftItemWindowSec = 300f;

	public float reactionWindowSec = 60f;

	public float autoCooldownSec = 18f;

	public bool autoTalk = true;

	public float reliefStrength = 1f;

	public float reliefDuration = 18f;

	public float traumaDampPerSec = 0.85f;

	public bool clearHorror = true;

	public bool showMoodles = true;

	public bool streamMode;

	public string streamHubPath = "";

	public int streamPort = 8768;

	public int danmakuPerMin = 3;

	public bool danmakuToHistory = true;

	public bool danmakuToAi = true;

	public int danmakuHistoryMax = 300;

	public int danmakuMaxPerSec = 5;

	public bool danmakuShowName = true;

	public float balanceRefreshSec = 60f;

	public float priceInPerM = 2f;

	public float priceOutPerM = 8f;

	public string currencySymbol = "¥";

	public float typingVolume = 0.22f;

	public float hudHistoryTop = 200f;

	/// <summary>界面比例：0.7–2.0，文字和 UI 一起缩放。</summary>
	public float uiScale = 1f;

	public float hudHistoryRight;

	public float hudHistoryAlpha = 0.93f;

	public float hudTabBottom = 380f;

	public int mastMaxChars = 420;

	public int mastStreamChars = 20;

	public int debtCalorieNeed = 200;

	public float debtSleepNeed = 60f;

	public float mastHealScale = 2.3f;

	public int ctxChat = 7;

	public int ctxPrompt = 10;

	public int ctxMast = 4;

	public int ctxMonitor = 4;

	public int lineMaxChars = 160;

	public int hurtReactions = 3;

	public float hurtWindowSec = 60f;

	public int injuryReactions = 2;

	public float injuryWindowSec = 60f;

	public float introQuietSec = 20f;

	public float introGraceSec = 6f;

	public float mastActingAt = 20f;

	public float mastFasterAt = 40f;

	public float mastPeakAt = 60f;

	public float mastEndAt = 600f;

	public float mastBeatEvery = 30f;

	public float mastLingerSec = 30f;

	public float typingCharsPerSec = 22f;

	public float typingLineGap = 0.45f;

	public float typingSoundGrace = 0.05f;

	public float danmakuDisplayWindowSec = 1f;

	public float danmakuDedupSec = 30f;

	public float danmakuResponseWindowSec = 60f;

	/// <summary>双人模式开关，默认关。</summary>
	public bool dualMode = false;

	public bool dualStartAsPlayer;

	/// <summary>双人模式旁白文案开关，默认关。</summary>
	public bool dualNarratorText = false;

	/// <summary>双人模式 P2 硬编码文案开关，默认关。</summary>
	public bool dualPartnerText = false;

	public float dualPlayerSpeed = 1f;

	public float dualPartnerSpeed = 1f;

	public bool dualAi = true;

	/// <summary>AI Debug 画面可视化：常态显示感知物块网格(绿/红/蓝)与多级寻路路线</summary>
	public bool p2AiDebug = true;

	/// <summary>感知神经元：物块网格扫描半径 (4 ~ 50000 格，支持全图，默认建议 25 格避免卡顿)</summary>
	public float p2ScanRadius = 25f;

	/// <summary>P2AI 影子路径观察器：玩家跳跃轨迹渲染开关（亮蓝绿色折线，默认开）</summary>
	public bool p2ShadowJumpRender = true;

	/// <summary>P2AI 影子路径观察器：玩家跳跃数据持久化记录开关（默认开）</summary>
	public bool p2ShadowJumpRecord = true;

	/// <summary>P2AI 影子路径观察器：跳跃观察记录本地持久化路径（默认保存在 BepInEx/config/CUCompanion/player_jump_observations.jsonl）</summary>
	public string p2ShadowRecordPath = "BepInEx/config/CUCompanion/player_jump_observations.jsonl";

	/// <summary>P2 AI 神经网络首脑总控代码</summary>
	public string p2BrainCode = DefaultP2BrainCode;

	public static readonly string DefaultP2BrainCode = @"// =========================================================================
// [P2-AI 首脑中枢与神经网络总控调度器 (P2 Neural Core Dispatcher)]
// 说明：由首脑统一调度各神经元组件，形成感知 -> 决策 -> 驱动闭环。
// =========================================================================

public class P2BrainHub
{
    // ── 1. 感觉神经元群 (Sensory Neurons) ──
    public VisualNeuron   Vision;    // 视觉神经：方块网格、障碍检测、射线探坑
    public SenseNeuron    Sense;     // 感知神经：生理状态(疼痛/体力/失血)、环境温湿度
    public AuditoryNeuron Audio;     // 听力神经：环境声源、脚步声、威胁警告

    // ── 2. 运动与行动神经元群 (Motor & Action Neurons) ──
    public NavNeuron      Nav;       // 寻路神经：A* 地形网格寻路、跳点计算
    public MotorNeuron    Motor;     // 动作神经：虚拟按键注入、跳跃冲量、蹲伏潜行
    public BreakNeuron    Break;     // 破坏神经：方块开凿、障碍破拆判定

    // ── 3. 情绪与心智神经元群 (Mental & Behavioral Neurons) ──
    public ChaosNeuron    Chaos;     // 迷乱神经：惊恐应激、失控抽搐、恐慌逃跑
    public PlayNeuron     Play;      // 娱乐神经：发呆闲逛、拟人微动作、好奇张望

    // ── 4. 首脑总控主时钟 (Brain Master Tick) ──
    public void Tick(Body partner, Body player, WorldGeneration world, float dt)
    {
        // 步骤1：激活感知系统 (采集视/感/听输入数据)
        // 步骤2：心智与状态评估 (根据生理及情绪权重调度)
        // 步骤3：行动决策与路径规划 (寻路/破拆/动作协调)
        // 步骤4：底层虚拟输入驱动 (注入 Body 物理执行)
    }
};";

	public int dualPlayerSkill = 15;

	public bool dualPlayerChip = true;

	public bool dualPartnerChip = true;

	public bool dualPlayerKit = true;

	/// <summary>机器核心：P1Alloy 的意识/情绪/精力/脑状态等锁定是否生效（也就是图标上那条「机器核心」状态）。</summary>
	public bool dualMachineCore = true;

	/// <summary>开局义肢：0 不装 / 1 木桩 / 2 钢制 / 3 强化。顺序 = 上臂A、上臂B、大腿A、大腿B。</summary>
	public int p1ProstheticArmA;

	public int p1ProstheticArmB;

	public int p1ProstheticLegA;

	public int p1ProstheticLegB;

	public float dualFollowDistance = 2.2f;

	public float dualRecallDistance = 14f;

	public float dualFarDistance = 8f;

	public float dualFarSpeedMult = 2.4f;

	public bool dualAiWander = true;

	public float dualCarryOffsetY = 0.42f;

	public float dualCarryBack = 0.55f;

	public bool dualShowStatus = true;

	public float dualStatusX = 40f;

	public float dualStatusY = 40f;

	public int dualSwitchKey = 298;

	public int dualVersion = 3;

	public bool debug;

	public int introAttempt = 11;

	public List<ReactionRule> reactions = new List<ReactionRule>();

	public List<MonitorPart> monitorParts = new List<MonitorPart>();

	public bool monitorOpen;

	public float monitorIntervalSec = 20f;

	public int monitorSlotMinutes = 3;

	public PromptConfig prompt = new PromptConfig();

	public static List<MonitorPart> DefaultMonitorParts()
	{
		return new List<MonitorPart>();
	}

	public static List<ReactionRule> DefaultReactions()
	{
		return new List<ReactionRule>
		{
			new ReactionRule
			{
				key = "arrive",
				title = Loc.T("初来乍到"),
				cooldown = 60f,
				condition = Loc.T("刚被投放 / 首次进入本局；看身边物块判断环境"),
				template = "You have just arrived at this site. First observe the surrounding blocks and terrain on screen, assess the environment (enclosed room, ruins, snow, submerged, cave), and state your immediate reaction in one or two sentences."
			},
			new ReactionRule
			{
				key = "craft",
				title = Loc.T("制作物品"),
				cooldown = 90f,
				condition = Loc.T("制作面板打开或关闭的瞬间"),
				template = "The handler is crafting or just finished an item. Comment on what was made and its utility; if you assisted, express how it felt and your physical fatigue."
			},
			new ReactionRule
			{
				key = "eat",
				title = Loc.T("吃东西"),
				cooldown = 80f,
				condition = Loc.T("body.eatTime 由 0 跳到正数（吃下 / 喝下）"),
				template = "You are consuming food or drink. React to its taste, temperature, texture, and whether it satisfies your hunger; describe chewing and swallowing in parentheses."
			},
			new ReactionRule
			{
				key = "event",
				title = Loc.T("突发事件"),
				cooldown = 45f,
				useBudget = false,
				condition = Loc.T("平均疼痛 1 秒内骤增 > 15（被打中 / 踩陷阱）"),
				template = "You were hit by a trap or sudden ambush. Cry out sharply, cursing in shock, and clearly describe the injured limb and the immediate sharp pain in parentheses."
			},
			new ReactionRule
			{
				key = "hurt",
				title = Loc.T("受击受伤"),
				cooldown = 6f,
				useBudget = false,
				condition = Loc.T("边沿触发：疼痛骤增 / 新出血 / 新骨折脱臼 / 跌落；不吃日常反应次数"),
				template = "You just sustained an injury. Cry out in sharp, raw pain, then describe where it hurts and your instinctual bodily reaction (flinching, clutching the wound, trembling, shallow breathing). Never claim recovery."
			},
			new ReactionRule
			{
				key = "item",
				title = Loc.T("身边有物品"),
				cooldown = 25f,
				useBudget = true,
				condition = Loc.T("6 格内出现从没提过的地上的物品（同一种每局只提一次）"),
				template = "You notice an item on the ground nearby ({item}). Offer a cautious thought on what it appears to be, its condition, and whether it looks safe to approach. Do not pick it up."
			},
			new ReactionRule
			{
				key = "loot",
				title = Loc.T("拿到物品"),
				cooldown = 20f,
				useBudget = true,
				condition = Loc.T("背包里出现从没拿到过的东西（同一种每局只提一次）"),
				template = "You just received a new item ({item}). Express your first impression upon holding it: texture, weight, utility, and ask the handler about its purpose if unsure."
			},
			new ReactionRule
			{
				key = "sick",
				title = Loc.T("不适"),
				cooldown = 100f,
				condition = Loc.T("sicknessAmount > 28 或任一肢体 infectionAmount > 25"),
				template = "You are suffering from bodily illness (nausea, fever/chills, infection, dizziness). Groan or plead for guidance according to the severity."
			},
			new ReactionRule
			{
				key = "injury",
				title = Loc.T("伤病惨叫"),
				cooldown = 90f,
				useBudget = false,
				condition = Loc.T("averagePain > 42 或 totalBleedSpeed > 0.04；独立计时 5 分钟最多 3 次"),
				template = "A persistent wound aches deeply without letting up. Emit a muffled whimper or groan, noting the affected limb, type of wound, and bleeding in parentheses."
			},
			new ReactionRule
			{
				key = "wake",
				title = Loc.T("睡醒"),
				cooldown = 60f,
				condition = Loc.T("昏迷(consciousness<30) -> 苏醒(conscious)"),
				template = "You just regained consciousness. Your voice is groggy and disoriented; speak in fragmented, slightly confused phrases, asking where you are."
			},
			new ReactionRule
			{
				key = "fear",
				title = Loc.T("恐惧与懒惰"),
				cooldown = 120f,
				condition = Loc.T("traumaAmount > 55 或 horrifiedLevel > 40"),
				template = "You are frightened and reluctant to proceed. Softly plead with the handler for reassurance or to pause; sound meek and submissive."
			},
			new ReactionRule
			{
				key = "lazy",
				title = Loc.T("累到不想动"),
				cooldown = 150f,
				condition = Loc.T("stamina < 18 或 energy < 12"),
				template = "Exhausted and drained of energy. Murmur quiet complaints and beg to rest for a moment."
			},
			new ReactionRule
			{
				key = "bored",
				title = Loc.T("发呆"),
				cooldown = 600f,
				condition = Loc.T("闲置 5 分钟、无异常状态"),
				template = "Left idle for an extended period with nothing happening. Zone out, reminiscing about quiet days back in the holding room: the handler's presence and warm meals. Nostalgic tone, but clearly framed as a memory."
			},
			new ReactionRule
			{
				key = "dying",
				title = Loc.T("濒死"),
				cooldown = 100f,
				condition = Loc.T("isCriticallyDying 或 consciousness < 22"),
				template = "At the brink of death (failing consciousness or critical damage). Gasp out desperate cries or farewells; describe your failing body in parentheses. Never imply recovery."
			},
			new ReactionRule
			{
				key = "threat",
				title = Loc.T("附近有威胁"),
				cooldown = 90f,
				condition = Loc.T("威胁音乐 threatMusicTime > 0.4"),
				template = "Threat detected nearby. Lower your voice to alert the handler, keeping phrases short and urgent."
			},
			new ReactionRule
			{
				key = "farewell",
				title = Loc.T("死前告别"),
				cooldown = 99999f,
				useBudget = false,
				condition = Loc.T("consciousness <= 45 或 isCriticallyDying 或 brainHealth < 30（每局一次）"),
				template = "You realize you cannot survive. This is your final chance to speak: express your parting words to the handler. Describe your fading breath and failing senses in parentheses."
			},
			new ReactionRule
			{
				key = "special",
				title = Loc.T("特殊互动"),
				cooldown = 30f,
				useBudget = false,
				condition = Loc.T("训练员输入含 摸/抚摸/自慰/哼歌/抱/亲 等互动词"),
				template = "The handler issued an interaction command. Respond according to your actual physical capacity: {capability}. If unable, explain why candidly; if able, describe tactile sensations, ache, or relief."
			}
		};
	}
}
