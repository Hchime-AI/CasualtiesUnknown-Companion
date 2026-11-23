using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CUCompanion;

public class DualMode : MonoBehaviour
{
	public enum CarrySide
	{
		None,
		PartnerOnPlayer,
		PlayerOnPartner
	}

	private class BtnEntry
	{
		public RectTransform rt;

		public Image face;

		public TextMeshProUGUI txt;

		public Action act;
	}

	private class Pending
	{
		public Body body;

		public string id;

		public float at;

		public int tries;
	}

	public static DualMode Instance;

	public Body Player;

	public Body Partner;

	private GameObject playerGo;

	private GameObject partnerGo;

	private float playerBaseSpeed;

	private float playerBaseJump;

	private float partnerBaseSpeed;

	private float partnerBaseJump;

	private Body buffedBody;

	private bool buffedFar;

	private float buffedMult = 1f;

	private float nextScan;

	private float nextStuckAt;

	private float lastLogAt;

	private float carryLockUntil;

	private string sceneName = "";

	private readonly List<Pending> pending = new List<Pending>();

	private float nextForageScan;

	private float lastForageAt = -999f;

	private CarrySide carrySide;

	private Body carriedBody;

	private Body carrierBody;

	private bool carriedWasCrouch;

	private bool holdingArm;

	private float armNextRagdoll;

	private float nextIgnoreAt;

	private float nextRespawnAt = -1f;

	private readonly HashSet<Body> deadSet = new HashSet<Body>();

	private GameObject btnRoot;

	private float nextBtnBuild;

	private int btnPressed = -1;

	private readonly List<BtnEntry> btnList = new List<BtnEntry>();

	public static bool ShowDualStatus = false;

	private GameObject statusRoot;

	private TextMeshProUGUI statusLeft;

	private TextMeshProUGUI statusRight;

	private string lastStatusL = "";

	private string lastStatusR = "";

	private float nextStatus;

	private float nextStatusBuild;

	private float nextOverride;

	private float lastErrAt;

	private Action<ModConfig>[] steps;

	private static readonly string[] BackpackCandidates = new string[7] { "bigpack", "duffelbag", "slingbag", "backpack", "smallpack", "fannypack", "toolbox" };

	public CarrySide Carry => carrySide;

	public bool Carrying => carrySide != CarrySide.None;

	public Body CarriedBody => carriedBody;

	public Body CarrierBody => carrierBody;

	public bool HoldingArm => holdingArm;

	public bool Active
	{
		get
		{
			if ((Object)(object)Player != (Object)null)
			{
				return (Object)(object)Partner != (Object)null;
			}
			return false;
		}
	}

	private static WorldGeneration World
	{
		get
		{
			try
			{
				return WorldGeneration.world;
			}
			catch
			{
				return null;
			}
		}
	}

	private void Awake()
	{
		Instance = this;
		steps = new Action<ModConfig>[6]
		{
			TickAI,
			TickHoldArm,
			TickKeys,
			TickButtons,
			delegate
			{
				MachineCore.Tick();
			},
			TickStatus
		};
	}

	private void OnDestroy()
	{
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
	}

	private void Safe(Action<ModConfig> step, ModConfig cfg)
	{
		try
		{
			step(cfg);
		}
		catch (Exception ex)
		{
			if (Time.unscaledTime - lastErrAt > 2f)
			{
				lastErrAt = Time.unscaledTime;
				Log("步骤异常：" + ex.GetType().Name + " " + ex.Message);
			}
		}
	}

	private void Safe(Action step, ModConfig cfg)
	{
		try
		{
			step();
		}
		catch (Exception ex)
		{
			if (Time.unscaledTime - lastErrAt > 2f)
			{
				lastErrAt = Time.unscaledTime;
				Log("步骤异常：" + ex.GetType().Name + " " + ex.Message);
			}
		}
	}

	public bool PlayerControls()
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && (Object)(object)Player != (Object)null)
		{
			return main.body == Player;
		}
		return false;
	}

	private void Update()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null)
		{
			return;
		}
		if (!cfg.dualMode)
		{
			if (Active || (Object)(object)playerGo != (Object)null)
			{
				Teardown();
			}
			return;
		}
		if (Time.unscaledTime >= nextScan)
		{
			nextScan = Time.unscaledTime + 0.5f;
			Scan(cfg);
		}
		TickPending();
		if (!Active)
		{
			return;
		}
		if (Time.unscaledTime >= nextIgnoreAt)
		{
			nextIgnoreAt = Time.unscaledTime + 2f;
			IgnorePair();
			SyncMoodles();
		}
		if (steps != null)
		{
			for (int i = 0; i < steps.Length; i++)
			{
				Safe(steps[i], cfg);
			}
		}
	}

	private static bool WorldReady()
	{
		try
		{
			WorldGeneration w = World;
			if ((Object)(object)w == (Object)null)
			{
				return false;
			}
			if (!w.worldExists || w.generatingWorld)
			{
				return false;
			}
			if ((Object)(object)w.loadingObject != (Object)null && w.loadingObject.activeInHierarchy)
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void Scan(ModConfig cfg)
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		string text = SafeSceneName();
		if (text != sceneName)
		{
			sceneName = text;
			DropRefs();
			return;
		}
		Body body = main.body;
		if ((Object)(object)Player != (Object)null && !Player.alive)
		{
			HandleDeath(Player, "P1Alloy");
		}
		if ((Object)(object)Partner != (Object)null && !Partner.alive && Partner != Player)
		{
			HandleDeath(Partner, "P2EXpie");
		}
		if ((Object)(object)body == (Object)null || !body.alive)
		{
			return;
		}
		if (body != Partner && body != Player)
		{
			if (!WorldReady())
			{
				return;
			}
			Setup(body, cfg);
			return;
		}
		if (nextRespawnAt > 0f)
		{
			nextRespawnAt = -1f;
		}
		if (carrySide != 0 && ((Object)(object)carriedBody == (Object)null || (Object)(object)carrierBody == (Object)null || !carriedBody.alive || !carrierBody.alive))
		{
			SetCarry(CarrySide.None, speak: false);
		}
		EnsurePartner(cfg);
	}

	private static string SafeSceneName()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Scene activeScene = SceneManager.GetActiveScene();
			return ((Scene)activeScene).name;
		}
		catch
		{
			return "";
		}
	}

	private void ClearRefs()
	{
		Player = null;
		Partner = null;
		playerGo = null;
		partnerGo = null;
	}

	private void DestroyUi()
	{
		if ((Object)(object)btnRoot != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)btnRoot);
			}
			catch
			{
			}
		}
		btnRoot = null;
		btnList.Clear();
		btnPressed = -1;
		if ((Object)(object)statusRoot != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)statusRoot);
			}
			catch
			{
			}
		}
		statusRoot = null;
		statusLeft = null;
		statusRight = null;
		lastStatusL = "";
		lastStatusR = "";
	}

	private void DropRefs()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		ClearRefs();
		pending.Clear();
		carrySide = CarrySide.None;
		carriedBody = null;
		carrierBody = null;
		holdingArm = false;
		buffedBody = null;
		buffedFar = false;
		buffedMult = 1f;
		nextRespawnAt = -1f;
		DestroyUi();
		btnRoot = null;
		btnList.Clear();
		btnPressed = -1;
		statusRoot = null;
		statusLeft = null;
		statusRight = null;
		lastStatusL = "";
		lastStatusR = "";
	}

	private void Teardown()
	{
		MachineCore.Reset();
		try
		{
			if (carrySide != 0)
			{
				SetCarry(CarrySide.None, speak: true);
			}
		}
		catch
		{
		}
		try
		{
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main != (Object)null && (Object)(object)Player != (Object)null && main.body == Player && (Object)(object)Partner != (Object)null && Partner.alive)
			{
				main.body = Partner;
			}
		}
		catch
		{
		}
		try
		{
			CompanionHost instance = CompanionHost.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.SetPersonaBody(null);
			}
		}
		catch
		{
		}
		if ((Object)(object)playerGo != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)playerGo);
			}
			catch
			{
			}
		}
		playerGo = null;
		DestroyUi();
		statusLeft = null;
		statusRight = null;
		lastStatusL = "";
		lastStatusR = "";
		pending.Clear();
		ClearRefs();
		carrySide = CarrySide.None;
		carriedBody = null;
		carrierBody = null;
		holdingArm = false;
		buffedBody = null;
		buffedFar = false;
		buffedMult = 1f;
	}

	private void Setup(Body body, ModConfig cfg)
	{
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		if ((Object)(object)playerGo != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)playerGo);
			}
			catch
			{
			}
		}
		playerGo = null;
		Player = null;
		Partner = body;
		partnerGo = ((Component)body).gameObject;
		partnerBaseSpeed = ((body.maxSpeed > 0.3f) ? body.maxSpeed : 3f);
		partnerBaseJump = ((body.jumpSpeed > 0.3f) ? body.jumpSpeed : 7f);
		try
		{
			body.maxSpeed = Mathf.Clamp(partnerBaseSpeed * Mathf.Clamp(cfg.dualPartnerSpeed, 0.2f, 4f), 0.5f, 60f);
		}
		catch (Exception ex)
		{
			Log("设置 P2EXpie 移速失败：" + ex.Message);
		}
		if (cfg.dualPartnerChip && !RunIsUnchipped())
		{
			QueueItem(body, "aichip");
		}
		GameObject val = null;
		try
		{
			val = Resources.Load<GameObject>("Special/Characters/Milky");
		}
		catch
		{
		}
		if ((Object)(object)val == (Object)null)
		{
			Log("找不到 Special/Characters/Milky（游戏自带的那只实验体），无法生成 P1Alloy");
			return;
		}
		Vector3 position = ((Component)body).transform.position;
		Vector3 spawnPos = new Vector3(position.x - 0.45f * Facing(body), position.y + 0.05f, position.z);
		playerGo = Object.Instantiate<GameObject>(val, spawnPos, Quaternion.identity);
		if ((Object)(object)playerGo == (Object)null)
		{
			return;
		}
		((Object)playerGo).name = "CUWhitePlayer";
		Player = playerGo.GetComponentInChildren<Body>(true);
		if ((Object)(object)Player == (Object)null)
		{
			Object.Destroy((Object)(object)playerGo);
			playerGo = null;
			Log("P1Alloy 身上没有 Body 组件，装配失败");
			return;
		}
		IgnorePair();
		float speed;
		float jump;
		int skill;
		bool flag = DualAssets.LoadProfile(out speed, out jump, out skill);
		playerBaseSpeed = (flag ? speed : partnerBaseSpeed);
		playerBaseJump = (flag ? jump : partnerBaseJump);
		if (!flag)
		{
			DualAssets.SaveProfile(playerBaseSpeed, playerBaseJump, cfg.dualPlayerSkill);
		}
		if (cfg.dualPlayerSkill <= 0 && skill > 0)
		{
			cfg.dualPlayerSkill = skill;
		}
		try
		{
			Player.maxSpeed = Mathf.Clamp(playerBaseSpeed * Mathf.Clamp(cfg.dualPlayerSpeed, 0.2f, 4f), 0.5f, 60f);
			Player.jumpSpeed = Mathf.Max(1f, playerBaseJump);
		}
		catch (Exception ex2)
		{
			Log("设置 P1Alloy 性能失败：" + ex2.Message);
		}
		ApplySkills(Player, cfg.dualPlayerSkill);
		if (cfg.dualPlayerChip && !RunIsUnchipped())
		{
			QueueItem(Player, "aichip");
		}
		else
		{
			TryEnableChip();
		}
		if (cfg.dualPlayerKit)
		{
			GiveLoadout(Player);
		}
		try
		{
			ProstheticKit.ApplyAll(Player, cfg);
		}
		catch (Exception ex3)
		{
			Log("P1Alloy 义肢装配失败：" + ex3.Message);
		}
		PlayerCamera main = PlayerCamera.main;
		bool flag2 = false;
		if ((Object)(object)main != (Object)null && cfg.dualStartAsPlayer && (Object)(object)Partner != (Object)null && Partner.alive)
		{
			main.body = Player;
			flag2 = true;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			instance.SetPersonaBody(Partner);
			instance.AddLine(Loc.T("旁白"), Loc.T("（P1Alloy睁开眼，连接成功，这次任务会顺利完成）"), player: false);
		}
		Log("模板存档：" + DualAssets.Dir);
		Log("双人模式装配完成：P1Alloy " + ((Object)Player).name + "（外观=游戏自带 Milky，charType=" + SafeCharType(Player) + "）／P2EXpie " + ((Object)Partner).name + "（charType=" + SafeCharType(Partner) + "），开局操控=" + (flag2 ? "P1Alloy" : "P2EXpie（原版）"));
		Vector3 position2 = ((Component)Player).transform.position;
		string text = ((object)(Vector3)(position2)).ToString();
		position2 = ((Component)Partner).transform.position;
		Log("位置：P1Alloy " + text + " / P2EXpie " + ((object)(Vector3)(position2)).ToString());
	}

	private static bool RunIsUnchipped()
	{
		try
		{
			return WorldGeneration.unchipped;
		}
		catch
		{
			return false;
		}
	}

	private void TryEnableChip()
	{
		try
		{
			if (RunIsUnchipped())
			{
				Log("本局是无芯片模式：不发放芯片，也不改动本局模式设置");
			}
		}
		catch (Exception ex)
		{
			Log("检查无芯片模式失败：" + ex.Message);
		}
	}

	private void EnsurePartner(ModConfig cfg)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		if (((Object)(object)Partner != (Object)null && Partner.alive && (Object)(object)((Component)Partner).gameObject != (Object)null) || (Object)(object)Player == (Object)null || !Player.alive)
		{
			return;
		}
		Body val = FindExperimentBody();
		if ((Object)(object)val != (Object)null)
		{
			Partner = val;
			partnerGo = ((Component)val).gameObject;
			partnerBaseSpeed = ((val.maxSpeed > 0.3f) ? val.maxSpeed : 3f);
			partnerBaseJump = ((val.jumpSpeed > 0.3f) ? val.jumpSpeed : 7f);
			CompanionHost instance = CompanionHost.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.SetPersonaBody(val);
			}
			string name = ((Object)val).name;
			Vector3 position = ((Component)val).transform.position;
			Log("找回场上的 P2EXpie：" + name + " @" + ((object)(Vector3)(position)).ToString());
			return;
		}
		Log("P2EXpie 不在了，双人模式不会凭空再造一个");
	}

	private Body FindExperimentBody()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		Body result = null;
		float num = float.MaxValue;
		try
		{
			Body[] array = Object.FindObjectsOfType<Body>();
			Vector3 val = (((Object)(object)Player != (Object)null) ? ((Component)Player).transform.position : Vector3.zero);
			foreach (Body val2 in array)
			{
				if ((Object)(object)val2 == (Object)null || !val2.alive || val2 == Player)
				{
					continue;
				}
				string text = ((Object)((Component)val2).gameObject).name.ToLowerInvariant();
				if (!text.StartsWith("cu") && (text.Contains("experiment") || text.Contains("saw")))
				{
					float num2 = Vector2.Distance((Vector2)(((Component)val2).transform.position), (Vector2)(val));
					if (num2 < num)
					{
						num = num2;
						result = val2;
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static int SafeCharType(Body b)
	{
		try
		{
			return ((Object)(object)b != (Object)null) ? b.charType : (-1);
		}
		catch
		{
			return -1;
		}
	}

	private void IgnorePair()
	{
		if ((Object)(object)Player == (Object)null || (Object)(object)Partner == (Object)null)
		{
			return;
		}
		Body player = Player;
		Body partner = Partner;
		try
		{
			Collider2D[] componentsInChildren = ((Component)player).GetComponentsInChildren<Collider2D>(true);
			Collider2D[] componentsInChildren2 = ((Component)partner).GetComponentsInChildren<Collider2D>(true);
			if (componentsInChildren == null || componentsInChildren2 == null)
			{
				return;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if ((Object)(object)componentsInChildren[i] == (Object)null)
				{
					continue;
				}
				for (int j = 0; j < componentsInChildren2.Length; j++)
				{
					if (!((Object)(object)componentsInChildren2[j] == (Object)null))
					{
						try
						{
							Physics2D.IgnoreCollision(componentsInChildren[i], componentsInChildren2[j], true);
						}
						catch
						{
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log("关闭两人碰撞失败：" + ex.Message);
		}
	}

	private void SyncWoundView()
	{
		try
		{
			if (!MedicalPartnerAssistant.IsInspectingPartner)
			{
				PlayerCamera main = PlayerCamera.main;
				if (!((Object)(object)main == (Object)null) && !((Object)(object)main.body == (Object)null))
				{
					MedicalPanelDecoupler.SetInspectTarget(main.body);
					Log("医疗界面已跟随切换：" + ((Object)main.body).name);
				}
			}
		}
		catch (Exception ex)
		{
			Log("医疗界面跟随切换失败：" + ex.Message);
		}
	}

	private void SyncMoodles()
	{
		try
		{
			MoodleManager main = MoodleManager.main;
			if ((Object)(object)main == (Object)null)
			{
				return;
			}
			PlayerCamera main2 = PlayerCamera.main;
			if ((Object)(object)main2 == (Object)null || (Object)(object)main2.body == (Object)null)
			{
				return;
			}
			Type typeFromHandle = typeof(MoodleManager);
			FieldInfo field = typeFromHandle.GetField("body", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(field == null) && (Body)field.GetValue(main) != main2.body)
			{
				field.SetValue(main, main2.body);
				MethodInfo method = typeFromHandle.GetMethod("ClearMoodles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo method2 = typeFromHandle.GetMethod("AddAllMoodles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					method.Invoke(main, null);
				}
				if (method2 != null)
				{
					method2.Invoke(main, null);
				}
			}
		}
		catch (Exception ex)
		{
			Log("状态图标跟随切换失败：" + ex.Message);
		}
	}

	private void ApplySkills(Body b, int level)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		if ((Object)(object)b == (Object)null)
		{
			return;
		}
		try
		{
			Skills skills = b.skills;
			if (skills == null)
			{
				Log("Body.skills 为空，等级没写进去");
				return;
			}
			Skills val = new Skills();
			val.STR = skills.STR;
			val.RES = skills.RES;
			val.INT = skills.INT;
			val.expSTR = skills.expSTR;
			val.expRES = skills.expRES;
			val.expINT = skills.expINT;
			val.minSTR = skills.minSTR;
			val.maxSTR = skills.maxSTR;
			val.minRES = skills.minRES;
			val.maxRES = skills.maxRES;
			val.minINT = skills.minINT;
			val.maxINT = skills.maxINT;
			val.STRGainMult = skills.STRGainMult;
			val.RESGainMult = skills.RESGainMult;
			val.INTGainMult = skills.INTGainMult;
			b.skills = val;
			skills = val;
			skills.INT = (skills.RES = (skills.STR = Mathf.Clamp(level, 1, 100)));
			skills.UpdateExpBoundaries();
			skills.expSTR = skills.minSTR;
			skills.expRES = skills.minRES;
			skills.expINT = skills.minINT;
			Log("P1Alloy等级已写入：力 " + skills.STR + " / 韧 " + skills.RES + " / 智 " + skills.INT);
		}
		catch (Exception ex)
		{
			Log("写等级失败：" + ex.Message);
		}
	}

	private string BestBackpack(out float capacity)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		capacity = -1f;
		string result = null;
		for (int i = 0; i < BackpackCandidates.Length; i++)
		{
			string text = BackpackCandidates[i];
			GameObject val = null;
			GameObject val2 = null;
			try
			{
				val = Resources.Load<GameObject>(text);
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				val2 = Object.Instantiate<GameObject>(val, new Vector3(0f, -9999f, 0f), Quaternion.identity);
				if (!((Object)(object)val2 == (Object)null))
				{
					Container val3 = val2.GetComponent<Container>();
					if ((Object)(object)val3 == (Object)null)
					{
						val3 = val2.GetComponentInChildren<Container>(true);
					}
					if (!((Object)(object)val3 == (Object)null) && val3.maxWeight > capacity)
					{
						capacity = val3.maxWeight;
						result = text;
					}
				}
			}
			catch
			{
			}
			finally
			{
				if ((Object)(object)val2 != (Object)null)
				{
					Object.Destroy((Object)(object)val2);
				}
			}
		}
		return result;
	}

	private void GiveLoadout(Body b)
	{
		float capacity;
		string text = BestBackpack(out capacity);
		if (string.IsNullOrEmpty(text))
		{
			text = "bigpack";
		}
		Log("开局背包 = " + text + "（容量 " + capacity.ToString("F1") + "）");
		GiveItem(b, text, wear: true);
		GiveItem(b, "rifle");
		GiveFilledMagazine(b, "riflemagazine");
		GiveFilledMagazine(b, "riflemagazine");
		_ = new string[10] { "medkit", "bandage", "sterilizedbandage", "tourniquet", "splint", "painkillers", "morphine", "syringe", "antibiotics", "plasticbandage" };
		string[] array = new string[6] { "medkit", "sterilizedbandage", "tourniquet", "splint", "morphine", "antibiotics" };
		for (int i = 0; i < array.Length; i++)
		{
			GiveItem(b, array[i]);
		}
	}

	private void QueueItem(Body b, string id)
	{
		if (!((Object)(object)b == (Object)null) && !string.IsNullOrEmpty(id))
		{
			pending.Add(new Pending
			{
				body = b,
				id = id,
				at = Time.unscaledTime + 0.8f,
				tries = 0
			});
		}
	}

	private void TickPending()
	{
		if (this.pending.Count == 0)
		{
			return;
		}
		for (int num = this.pending.Count - 1; num >= 0; num--)
		{
			Pending pending = this.pending[num];
			if ((Object)(object)pending.body == (Object)null || !pending.body.alive)
			{
				this.pending.RemoveAt(num);
			}
			else if (!(Time.unscaledTime < pending.at))
			{
				pending.tries++;
				if (GiveItem(pending.body, pending.id))
				{
					Log("已交付 " + pending.id);
					this.pending.RemoveAt(num);
				}
				else if (pending.tries >= 6)
				{
					Log("给物品最终失败 " + pending.id);
					this.pending.RemoveAt(num);
				}
				else
				{
					pending.at = Time.unscaledTime + 1.2f;
				}
			}
		}
	}

	private void GiveFilledMagazine(Body b, string id)
	{
		Item val = MakeItem(id);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		try
		{
			AmmoScript val2 = ((Component)val).GetComponent<AmmoScript>();
			if ((Object)(object)val2 == (Object)null)
			{
				val2 = ((Component)val).GetComponentInChildren<AmmoScript>(true);
			}
			if ((Object)(object)val2 != (Object)null && val2.maxRounds > 0)
			{
				val2.rounds = val2.maxRounds;
			}
		}
		catch
		{
		}
		try
		{
			b.AutoPickUpItem(val);
		}
		catch
		{
		}
	}

	private Item MakeItem(string id)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GameObject val = Resources.Load<GameObject>(id);
			if ((Object)(object)val == (Object)null)
			{
				return null;
			}
			GameObject val2 = Object.Instantiate<GameObject>(val, new Vector3(0f, -9999f, 0f), Quaternion.identity);
			if ((Object)(object)val2 == (Object)null)
			{
				return null;
			}
			Item val3 = val2.GetComponent<Item>();
			if ((Object)(object)val3 == (Object)null)
			{
				val3 = val2.GetComponentInChildren<Item>(true);
			}
			if ((Object)(object)val3 == (Object)null)
			{
				Object.Destroy((Object)(object)val2);
				return null;
			}
			return val3;
		}
		catch (Exception ex)
		{
			Log("生成物品失败 " + id + "：" + ex.Message);
			return null;
		}
	}

	private bool GiveItem(Body b, string id)
	{
		return GiveItem(b, id, wear: false);
	}

	private bool GiveItem(Body b, string id, bool wear)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)b == (Object)null || string.IsNullOrEmpty(id))
		{
			return false;
		}
		if (wear)
		{
			Item val = MakeItem(id);
			if ((Object)(object)val != (Object)null)
			{
				try
				{
					if (val.Stats != null && val.Stats.wearable)
					{
						b.WearWearable(val);
						return true;
					}
				}
				catch
				{
				}
				try
				{
					b.AutoPickUpItem(val);
					return true;
				}
				catch
				{
				}
			}
		}
		try
		{
			GameObject val2 = Resources.Load<GameObject>(id);
			if ((Object)(object)val2 == (Object)null)
			{
				return false;
			}
			GameObject val3 = Object.Instantiate<GameObject>(val2, ((Component)b).transform.position, Quaternion.identity);
			if ((Object)(object)val3 == (Object)null)
			{
				return false;
			}
			Item val4 = val3.GetComponent<Item>();
			if ((Object)(object)val4 == (Object)null)
			{
				val4 = val3.GetComponentInChildren<Item>(true);
			}
			if ((Object)(object)val4 == (Object)null)
			{
				Object.Destroy((Object)(object)val3);
				return false;
			}
			b.AutoPickUpItem(val4);
			return true;
		}
		catch (Exception ex)
		{
			Log("给物品失败 " + id + "：" + ex.Message);
			return false;
		}
	}

	private void HandleDeath(Body dead, string who)
	{
		if ((Object)(object)dead == (Object)null || !deadSet.Add(dead))
		{
			return;
		}
		try
		{
			if (carrySide != 0)
			{
				SetCarry(CarrySide.None, speak: false);
			}
		}
		catch
		{
		}
		if (dead == Player)
		{
			holdingArm = false;
			buffedBody = null;
		}
		Body val = ((dead == Player) ? Partner : Player);
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main != (Object)null && ((Object)(object)main.body == (Object)null || main.body == dead) && (Object)(object)val != (Object)null && val.alive)
		{
			main.body = val;
		}

		Log("[Dual] " + who + " 死亡，双人模式不再自动重生");
	}

	private void ApplyFarBuff(Body b, bool far, ModConfig cfg)
	{
		if ((Object)(object)b == (Object)null)
		{
			return;
		}
		float num = (far ? Mathf.Clamp(cfg.dualFarSpeedMult, 1f, 6f) : 1f);
		if (buffedBody != b || buffedFar != far || !(Mathf.Abs(buffedMult - num) < 0.01f))
		{
			float num2 = ((b == Player) ? playerBaseSpeed : partnerBaseSpeed);
			float num3 = ((b == Player) ? playerBaseJump : partnerBaseJump);
			float num4 = ((b == Player) ? Mathf.Clamp(cfg.dualPlayerSpeed, 0.2f, 4f) : Mathf.Clamp(cfg.dualPartnerSpeed, 0.2f, 4f));
			try
			{
				b.maxSpeed = Mathf.Clamp(num2 * num4 * num, 0.5f, 60f);
				b.jumpSpeed = Mathf.Max(1f, num3 * (1f + (num - 1f) * 0.35f));
			}
			catch
			{
			}
			buffedBody = b;
			buffedFar = far;
			buffedMult = num;
			if (far)
			{
				Log(((b == Player) ? "P1Alloy" : "P2EXpie") + " 脱离玩家过远，远程强化 ×" + num.ToString("0.0"));
			}
		}
	}

	private static float Facing(Body b)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)b == (Object)null)
		{
			return 1f;
		}
		if (!(((Component)b).transform.localScale.x < 0f))
		{
			return 1f;
		}
		return -1f;
	}

	public void ToggleCarry()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (Active && !(Time.unscaledTime < carryLockUntil))
		{
			if (carrySide != 0)
			{
				SetCarry(CarrySide.None, speak: true);
				return;
			}
			if (Vector2.Distance((Vector2)(((Component)Player).transform.position), (Vector2)(((Component)Partner).transform.position)) > 3.2f)
			{
				Info(Loc.T("离得太远了，先走近一点。"));
				return;
			}
			PlayerCamera main = PlayerCamera.main;
			bool flag = (Object)(object)main != (Object)null && main.body == Player;
			SetCarry(flag ? CarrySide.PartnerOnPlayer : CarrySide.PlayerOnPartner, speak: true);
		}
	}

	public void SetCarry(CarrySide side, bool speak)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		if (!Active)
		{
			return;
		}
		ReleaseCarryInternal(side == CarrySide.None);
		Body obj;
		switch (side)
		{
		case CarrySide.None:
			return;
		default:
			obj = Partner;
			break;
		case CarrySide.PartnerOnPlayer:
			obj = Player;
			break;
		}
		Body val = obj;
		Body val2 = ((side == CarrySide.PartnerOnPlayer) ? Partner : Player);
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null || !val.alive || !val2.alive)
		{
			return;
		}
		carrySide = side;
		carrierBody = val;
		carriedBody = val2;
		carryLockUntil = Time.unscaledTime + 0.45f;
		carriedWasCrouch = val2.crouching;
		try
		{
			val2.crouching = true;
		}
		catch
		{
		}
		try
		{
			val2.crouchAmount = 1f;
		}
		catch
		{
		}
		try
		{
			if ((Object)(object)val2.rb != (Object)null)
			{
				val2.rb.velocity = Vector2.zero;
				val2.rb.simulated = false;
			}
			if ((Object)(object)val2.col != (Object)null)
			{
				((Behaviour)val2.col).enabled = false;
			}
		}
		catch (Exception ex)
		{
			Log("背负物理设置失败：" + ex.Message);
		}
		try
		{
			ApplyCarryPose();
		}
		catch (Exception ex2)
		{
			Log("背负摆位失败：" + ex2.Message);
		}
		if (speak)
		{
			ModConfig cfg2 = CompanionPlugin.Cfg;
			CompanionHost instance = CompanionHost.Instance;
			string text = (((Object)(object)instance != (Object)null) ? instance.PersonaName() : Loc.T("她"));
			if (side == CarrySide.PartnerOnPlayer)
			{
				if ((Object)(object)instance != (Object)null)
				{
					string speech = (cfg2 != null && cfg2.dualPartnerText)
						? Loc.T("（被P1Alloy一把捞到背上，下巴抵着它的肩）……这样就不用自己走了。")
						: "...";
					instance.AddLine(text, speech, player: false);
				}
			}
			else
			{
				if ((Object)(object)instance != (Object)null)
				{
					instance.AddLine(Loc.T("旁白"), Loc.P("（") + text + Loc.T("把P1Alloy拽到背上，两只手扣着它的腿弯）"), player: false);
				}
			}
		}
	}

	private void ApplyCarryPose()
	{
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)carriedBody == (Object)null || (Object)(object)carrierBody == (Object)null)
		{
			return;
		}
		ModConfig cfg = CompanionPlugin.Cfg;
		float num = ((cfg != null) ? Mathf.Clamp(cfg.dualCarryOffsetY, -0.5f, 1.5f) : 0.42f);
		float num2 = ((cfg != null) ? Mathf.Clamp(cfg.dualCarryBack, -0.5f, 2f) : 0.55f);
		float num3 = Facing(carrierBody);
		Transform transform = ((Component)carriedBody).transform;
		Vector3 position = ((Component)carrierBody).transform.position;
		transform.position = new Vector3(position.x - num2 * num3, position.y + num, position.z);
		transform.rotation = Quaternion.identity;
		Vector3 localScale = transform.localScale;
		transform.localScale = new Vector3(Mathf.Abs(localScale.x) * num3, Mathf.Abs(localScale.y), Mathf.Abs(localScale.z));
		try
		{
			carriedBody.isRight = carrierBody.isRight;
		}
		catch
		{
		}
		try
		{
			carriedBody.crouching = true;
			carriedBody.crouchAmount = 1f;
			carriedBody.moveDir = Vector2.zero;
			carriedBody.standing = true;
			carriedBody.shock = 0f;
		}
		catch
		{
		}
	}

	private void TickCarryPose(ModConfig cfg)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)carriedBody == (Object)null || (Object)(object)carrierBody == (Object)null)
		{
			carrySide = CarrySide.None;
			return;
		}
		try
		{
			carriedBody.standing = true;
			carriedBody.shock = 0f;
			if ((Object)(object)carriedBody.rb != (Object)null)
			{
				if (carriedBody.rb.simulated)
				{
					carriedBody.rb.simulated = false;
				}
				carriedBody.rb.velocity = Vector2.zero;
			}
			if ((Object)(object)carriedBody.col != (Object)null && ((Behaviour)carriedBody.col).enabled)
			{
				((Behaviour)carriedBody.col).enabled = false;
			}
			if (carriedBody.limbs != null)
			{
				for (int i = 0; i < carriedBody.limbs.Length; i++)
				{
					Limb val = carriedBody.limbs[i];
					if ((Object)(object)val != (Object)null && (Object)(object)val.rb != (Object)null)
					{
						if (val.rb.simulated)
						{
							val.rb.simulated = false;
						}
						val.rb.velocity = Vector2.zero;
						val.rb.angularVelocity = 0f;
					}
				}
			}
		}
		catch
		{
		}
		ApplyCarryPose();
	}

	private void ReleaseCarryInternal(bool placeBeside)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)carriedBody == (Object)null)
		{
			carrySide = CarrySide.None;
			carrierBody = null;
			return;
		}
		Body val = carriedBody;
		Body val2 = carrierBody;
		carriedBody = null;
		carrierBody = null;
		carrySide = CarrySide.None;
		try
		{
			((Component)val).transform.SetParent((Transform)null, true);
		}
		catch
		{
		}
		try
		{
			val.crouching = false;
			val.crouchAmount = 0f;
			val.moveDir = Vector2.zero;
		}
		catch
		{
		}
		try
		{
			if ((Object)(object)val.rb != (Object)null)
			{
				val.rb.simulated = true;
				val.rb.velocity = Vector2.zero;
				val.rb.angularVelocity = 0f;
			}
			if ((Object)(object)val.col != (Object)null)
			{
				((Behaviour)val.col).enabled = true;
			}
			if (val.alive)
			{
				val.standing = true;
				val.shock = 0f;
				val.Stand(true);
				if (val.limbs != null)
				{
					for (int i = 0; i < val.limbs.Length; i++)
					{
						Limb val3 = val.limbs[i];
						if ((Object)(object)val3 != (Object)null && (Object)(object)val3.rb != (Object)null)
						{
							val3.rb.simulated = false;
							val3.rb.velocity = Vector2.zero;
							val3.rb.angularVelocity = 0f;
						}
					}
				}
			}
			else
			{
				val.Ragdoll();
			}
		}
		catch
		{
		}
		if ((Object)(object)val2 != (Object)null)
		{
			Vector3 position = ((Component)val2).transform.position;
			float num = Facing(val2);
			try
			{
				((Component)val).transform.position = new Vector3(position.x + 0.85f * num, position.y, position.z);
			}
			catch
			{
			}
		}
	}

	public void ToggleHoldArm()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (!Active)
		{
			return;
		}
		if (holdingArm)
		{
			ReleaseArm(bySwitch: false);
			return;
		}
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null || main.body != Player)
		{
			Info(Loc.T("只有操控P1Alloy时才抓得住P2EXpie的胳膊。"));
			return;
		}
		if (carrySide != 0)
		{
			Info(Loc.T("先把她放下来。"));
			return;
		}
		if (Vector2.Distance((Vector2)(((Component)Player).transform.position), (Vector2)(((Component)Partner).transform.position)) > 2.8f)
		{
			Info(Loc.T("太远了，够不到。"));
			return;
		}
		holdingArm = true;
		armNextRagdoll = 0f;
		try
		{
			Partner.Ragdoll();
		}
		catch
		{
		}
		CompanionHost instance = CompanionHost.Instance;
		string text = (((Object)(object)instance != (Object)null) ? instance.PersonaName() : Loc.T("她"));
		if ((Object)(object)instance != (Object)null)
		{
			instance.AddLine(Loc.T("旁白"), Loc.T("（P1Alloy一把扣住") + text + Loc.T("的胳膊，力气大得她整个人被拽得失去平衡）"), player: false);
		}
	}

	private void ReleaseArm(bool bySwitch)
	{
		if (!holdingArm)
		{
			return;
		}
		holdingArm = false;
		if ((Object)(object)Partner != (Object)null)
		{
			if (Partner.alive)
			{
				try
				{
					if (Partner.shock > 9f)
					{
						Partner.shock = 9f;
					}
				}
				catch
				{
				}
				try
				{
					Partner.Stand(true);
				}
				catch
				{
				}
			}
			try
			{
				if ((Object)(object)Partner.col != (Object)null)
				{
					((Behaviour)Partner.col).enabled = true;
				}
				if ((Object)(object)Partner.rb != (Object)null)
				{
					Partner.rb.simulated = true;
				}
			}
			catch
			{
			}
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			instance.AddLine(Loc.T("旁白"), bySwitch ? Loc.T("（切换的时候松开了手）") : Loc.T("（P1Alloy松开手，她的胳膊垂回地上）"), player: false);
		}
	}

	private void TickHoldArm(ModConfig cfg)
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		if (!holdingArm)
		{
			return;
		}
		if ((Object)(object)Partner == (Object)null || !Partner.alive || carrySide != 0)
		{
			ReleaseArm(bySwitch: false);
			return;
		}
		if ((Object)(object)Player == (Object)null)
			{
				return;
			}
			try
			{
				if ((Object)(object)Partner.col != (Object)null && ((Behaviour)Partner.col).enabled)
				{
					((Behaviour)Partner.col).enabled = false;
				}
				if ((Object)(object)Partner.rb != (Object)null && Partner.rb.simulated)
				{
					Partner.rb.simulated = false;
				}
			}
			catch
			{
			}
			float num = Facing(Player);
			Vector3 val = ((Player.limbs == null || Player.limbs.Length <= 5 || !((Object)(object)Player.limbs[5] != (Object)null)) ? (((Component)Player).transform.position + new Vector3(-0.45f * num, -0.15f, 0f)) : ((Component)Player.limbs[5]).transform.position);
			Limb val2 = ((Partner.limbs != null && Partner.limbs.Length > 5 && (Object)(object)Partner.limbs[5] != (Object)null) ? Partner.limbs[5] : null);
			if ((Object)(object)val2 != (Object)null && (Object)(object)val2.rb != (Object)null)
			{
				val2.rb.position = (Vector2)(val);
				val2.rb.velocity = (((Object)(object)Player.rb != (Object)null) ? Player.rb.velocity : Vector2.zero);
				val2.rb.angularVelocity = 0f;
				((Component)val2).transform.position = val;
			}
			if (Partner.limbs != null)
			{
				for (int i = 0; i < Partner.limbs.Length; i++)
				{
					Limb val3 = Partner.limbs[i];
					if (!((Object)(object)val3 == (Object)null) && !((Object)(object)val3.rb == (Object)null) && val3 != val2)
					{
						val3.rb.angularVelocity = Mathf.Clamp(val3.rb.angularVelocity, -60f, 60f);
						val3.rb.velocity = Vector2.ClampMagnitude(val3.rb.velocity, 7.5f);
						Vector2 val4 = val3.rb.position - (Vector2)(val);
						if (((Vector2)val4).magnitude > 1.6f)
						{
							val3.rb.position = (Vector2)(val) + ((Vector2)val4).normalized * 1.6f;
						}
					}
				}
			}
			Vector3 val5 = val - new Vector3(0.55f * num, 0.35f, 0f);
			((Component)Partner).transform.position = Vector3.Lerp(((Component)Partner).transform.position, val5, 15f * Time.deltaTime);
			((Component)Partner).transform.rotation = Quaternion.identity;
	}

	public void Recall()
	{
		if (!Active)
		{
			return;
		}
		if (carrySide != 0)
		{
			Info(Loc.T("先把P2EXpie放下来再叫。"));
			return;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			instance.AddLine(Loc.T("旁白"), Loc.T("（P1Alloy朝") + instance.PersonaName() + Loc.T("招手，叫她过来）"), player: false);
		}
	}

	public void Switch()
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null || !Active || Time.unscaledTime < carryLockUntil)
		{
			return;
		}
		bool flag = main.body == Player;
		Body val = (flag ? Partner : Player);
		if ((Object)(object)val == (Object)null || !val.alive)
		{
			Info(flag ? Loc.T("P2EXpie 已经不能动了。") : Loc.T("P1Alloy 已经不能动了。"));
			return;
		}
		if (carrySide != 0)
		{
			SetCarry((!flag) ? CarrySide.PartnerOnPlayer : CarrySide.PlayerOnPartner, speak: false);
		}
		if (holdingArm)
		{
			ReleaseArm(bySwitch: true);
		}
		main.body = val;
		SyncMoodles();
		SyncWoundView();
		MedicalPanelDecoupler.SetInspectTarget(val);
		ScreenDistortionIsolator.ResetRun();
		carryLockUntil = Time.unscaledTime + 0.5f;
		IgnorePair();
		SyncMoodles();
		Info(flag ? Loc.T("（脑机切换到 P2EXpie）") : Loc.T("（脑机切回 P1Alloy）"));
	}

	private void TickKeys(ModConfig cfg)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		int dualSwitchKey = cfg.dualSwitchKey;
		if (dualSwitchKey <= 0)
		{
			return;
		}
		KeyCode val = (KeyCode)dualSwitchKey;
		try
		{
			if (Input.GetKeyDown(val))
			{
				bool num = Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307);
				bool flag = Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305);
				bool flag2 = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
				if (num)
				{
					ToggleCarry();
				}
				else if (flag)
				{
					ToggleHoldArm();
				}
				else if (flag2)
				{
					Recall();
				}
				else
				{
					Switch();
				}
			}
		}
		catch
		{
		}
	}

	private void Info(string text)
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null && !cfg.dualNarratorText)
		{
			return;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			instance.AddLine(Loc.T("旁白"), text, player: false);
		}
	}

	private void Log(string msg)
	{
		if (!(Time.unscaledTime - lastLogAt < 0.2f))
		{
			lastLogAt = Time.unscaledTime;
			Debug.Log((object)("[dual] " + msg));
		}
	}

	private void TickStatus(ModConfig cfg)
	{
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		PlayerCamera main = PlayerCamera.main;
		bool flag = (Object)(object)main != (Object)null && (Object)(object)main.woundView != (Object)null && main.woundView.activeSelf;
		if (!ShowDualStatus || !flag || !cfg.dualShowStatus)
		{
			if ((Object)(object)statusRoot != (Object)null && statusRoot.activeSelf)
			{
				statusRoot.SetActive(false);
			}
			return;
		}
		if ((Object)(object)statusRoot == (Object)null)
		{
			if (!(Time.unscaledTime < nextStatusBuild))
			{
				nextStatusBuild = Time.unscaledTime + 1f;
				BuildStatus(cfg);
			}
			return;
		}
		if (!statusRoot.activeSelf)
		{
			statusRoot.SetActive(true);
		}
		float num = Mathf.Clamp(cfg.dualStatusX, -2000f, 2000f);
		// 状态表整体下移 200px（pivot 在右上角，y 负方向为下）
		float num2 = Mathf.Clamp(cfg.dualStatusY, -2000f, 2000f) + 200f;
		RectTransform component = statusRoot.GetComponent<RectTransform>();
		Vector2 val = default(Vector2);
		val = new Vector2(0f - num, 0f - num2);
		if ((Object)(object)component != (Object)null)
		{
			// 状态表文字变长、或偏移量被改到画面外时，整块会顶出屏幕右上角。
			// 这里按画布尺寸把整块夹回可视区内，保证它永远完整留在屏幕里。
			RectTransform parentRect = ((Transform)component).parent as RectTransform;
			float num3 = component.rect.width;
			float num4 = component.rect.height;
			float num5 = (((Object)(object)parentRect != (Object)null) ? parentRect.rect.width : ((float)Screen.width));
			float num6 = (((Object)(object)parentRect != (Object)null) ? parentRect.rect.height : ((float)Screen.height));
			val.x = Mathf.Clamp(val.x, num3 - num5, 0f);
			val.y = Mathf.Clamp(val.y, num4 - num6, 0f);
		}
		if ((Object)(object)component != (Object)null && (Mathf.Abs(component.anchoredPosition.x - val.x) > 0.5f || Mathf.Abs(component.anchoredPosition.y - val.y) > 0.5f))
		{
			component.anchoredPosition = val;
		}
		if (!(Time.unscaledTime < nextStatus))
		{
			nextStatus = Time.unscaledTime + 0.25f;
			string text = StatusText(Player, Loc.T("P1Alloy · 脑机接管"), machine: true);
			string text2 = StatusText(Partner, Loc.T("P2EXpie · 原体"), machine: false);
			if ((Object)(object)statusLeft != (Object)null && text != lastStatusL)
			{
				((TMP_Text)statusLeft).text = text;
				lastStatusL = text;
			}
			if ((Object)(object)statusRight != (Object)null && text2 != lastStatusR)
			{
				((TMP_Text)statusRight).text = text2;
				lastStatusR = text2;
			}
		}
	}

	private static string StatusText(Body b, string title, bool machine)
	{
		if ((Object)(object)b == (Object)null)
		{
			return title + Loc.T("\n（不在场）");
		}
		return title + Loc.T("\n意识 ") + F(b.consciousness, 0) + Loc.T("\u3000脑 ") + F(b.brainHealth, 0) + Loc.T("\n血量 ") + F(b.bloodVolume, 0) + Loc.T("\u3000出血 ") + F(b.totalBleedSpeed, 2) + Loc.T("\n疼痛 ") + F(b.averagePain, 0) + Loc.T("\u3000休克 ") + F(b.shock, 0) + Loc.T("\n心率 ") + F(b.heartRate, 0) + Loc.T("\u3000血氧 ") + F(b.bloodOxygen, 0) + Loc.T("\n体温 ") + F(b.temperature, 1) + Loc.T("\u3000感染 ") + F(b.sicknessAmount, 0) + Loc.T("\n饥饿 ") + F(b.hunger, 0) + Loc.T("\u3000口渴 ") + F(b.thirst, 0) + Loc.T("\n体力 ") + F(b.stamina, 0) + Loc.T("\u3000精力 ") + F(b.energy, 0) + Loc.T("\n情绪 ") + F(b.happiness, 0) + Loc.T("\u3000创伤 ") + F(b.traumaAmount, 0);
	}

	private static string F(float v, int digits)
	{
		if (float.IsNaN(v) || float.IsInfinity(v))
		{
			return "-";
		}
		return v.ToString((digits == 0) ? "0" : "0.##");
	}

	private void BuildStatus(ModConfig cfg)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Transform canvasTransform = NativeHudController.CanvasTransform;
			if ((Object)(object)canvasTransform == (Object)null)
			{
				return;
			}
			statusRoot = new GameObject("CU_DualStatus", new Type[1] { typeof(RectTransform) });
			int num = LayerMask.NameToLayer("UI");
			if (num >= 0)
			{
				statusRoot.layer = num;
			}
			RectTransform component = statusRoot.GetComponent<RectTransform>();
			((Transform)component).SetParent(canvasTransform, false);
			component.anchorMin = new Vector2(1f, 1f);
			component.anchorMax = new Vector2(1f, 1f);
			component.pivot = new Vector2(1f, 1f);
			component.sizeDelta = new Vector2(770f, 230f);
			component.anchoredPosition = new Vector2(0f - Mathf.Clamp(cfg.dualStatusX, -2000f, 2000f), 0f - (Mathf.Clamp(cfg.dualStatusY, -2000f, 2000f) + 200f));
			Image obj = statusRoot.AddComponent<Image>();
			((Graphic)obj).color = new Color(0.04f, 0.06f, 0.09f, 0.88f);
			((Graphic)obj).raycastTarget = false;
			for (int i = 0; i < 4; i++)
			{
				GameObject val = new GameObject("line", new Type[1] { typeof(RectTransform) });
				if (num >= 0)
				{
					val.layer = num;
				}
				RectTransform component2 = val.GetComponent<RectTransform>();
				((Transform)component2).SetParent((Transform)(object)component, false);
				component2.anchorMin = new Vector2(0f, 1f);
				component2.anchorMax = new Vector2(0f, 1f);
				component2.pivot = new Vector2(0f, 1f);
				bool flag = i >= 2;
				component2.sizeDelta = (flag ? new Vector2(1.4f, 230f) : new Vector2(770f, 1.4f));
				component2.anchoredPosition = new Vector2(flag ? ((i == 3) ? 768.6f : 0f) : 0f, flag ? 0f : ((i == 1) ? (-228.6f) : 0f));
				Image obj2 = val.AddComponent<Image>();
				((Graphic)obj2).color = new Color(0.35f, 0.65f, 0.9f, 0.65f);
				((Graphic)obj2).raycastTarget = false;
			}
			statusLeft = MakeStatusText(component, 12f);
			statusRight = MakeStatusText(component, 392f);
			lastStatusL = "";
			lastStatusR = "";
		}
		catch (Exception ex)
		{
			Log("状态表创建失败：" + ex.Message);
			if ((Object)(object)statusRoot != (Object)null)
			{
				try
				{
					Object.Destroy((Object)(object)statusRoot);
				}
				catch
				{
				}
			}
			statusRoot = null;
			statusLeft = null;
			statusRight = null;
			lastStatusL = "";
			lastStatusR = "";
		}
	}

	private static TextMeshProUGUI MakeStatusText(RectTransform parent, float x)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("col", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)parent, false);
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(0f, 0f);
		component.pivot = new Vector2(0f, 0f);
		component.sizeDelta = new Vector2(360f, 292f);
		component.anchoredPosition = new Vector2(x, 4f);
		TextMeshProUGUI obj = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = NativeHudController.GameFont;
		((TMP_Text)obj).fontSize = 20f;
		((Graphic)obj).color = new Color(0.88f, 0.92f, 0.98f, 0.95f);
		((TMP_Text)obj).alignment = (TextAlignmentOptions)257;
		((TMP_Text)obj).enableWordWrapping = false;
		// 文字被改长后不许撑出面板：允许自动缩到 9 号字，最大仍是 20。
		((TMP_Text)obj).enableAutoSizing = true;
		((TMP_Text)obj).fontSizeMin = 9f;
		((TMP_Text)obj).fontSizeMax = 20f;
		((Graphic)obj).raycastTarget = false;
		((TMP_Text)obj).text = "";
		return obj;
	}

	private void TickButtons(ModConfig cfg)
	{
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)btnRoot == (Object)null || btnList.Count == 0)
		{
			if (!(Time.unscaledTime < nextBtnBuild))
			{
				nextBtnBuild = Time.unscaledTime + 1f;
				BuildButtons();
			}
			return;
		}
		if (btnList.Count >= 2 && (Object)(object)btnList[1].txt != (Object)null)
		{
			string text = ((carrySide == CarrySide.None) ? Loc.T("背起") : Loc.T("放下"));
			if (((TMP_Text)btnList[1].txt).text != text)
			{
				((TMP_Text)btnList[1].txt).text = text;
			}
		}
		if (btnList.Count >= 3 && (Object)(object)btnList[2].txt != (Object)null)
		{
			string text2 = (holdingArm ? Loc.T("松手") : Loc.T("抓胳膊"));
			if (((TMP_Text)btnList[2].txt).text != text2)
			{
				((TMP_Text)btnList[2].txt).text = text2;
			}
		}
		bool flag = Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307);
		float num = (flag ? 0.14f : 0.05f);
		for (int i = 0; i < btnList.Count; i++)
		{
			Image face = btnList[i].face;
			if ((Object)(object)face != (Object)null && Mathf.Abs(((Graphic)face).color.a - num) > 0.01f)
			{
				((Graphic)face).color = new Color(1f, 1f, 1f, num);
			}
		}
		int num2 = (flag ? HitTest() : (-1));
		if (Input.GetMouseButtonDown(0))
		{
			btnPressed = num2;
		}
		if (!Input.GetMouseButtonUp(0))
		{
			return;
		}
		if (btnPressed >= 0 && btnPressed == num2 && btnPressed < btnList.Count)
		{
			try
			{
				PlayerCamera.PlayUISound("click", 1f);
			}
			catch
			{
			}
			btnList[btnPressed].act?.Invoke();
		}
		btnPressed = -1;
	}

	private int HitTest()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = (Vector2)(Input.mousePosition);
		for (int i = 0; i < btnList.Count; i++)
		{
			RectTransform rt = btnList[i].rt;
			if ((Object)(object)rt == (Object)null)
			{
				continue;
			}
			try
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(rt, val, (Camera)null))
				{
					return i;
				}
			}
			catch
			{
			}
		}
		return -1;
	}

	private void BuildButtons()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			NativeHudController instance = NativeHudController.Instance;
			if (!((Object)(object)instance == (Object)null) && !((Object)(object)instance.HistoryRect == (Object)null) && !((Object)(object)btnRoot != (Object)null))
			{
				btnRoot = new GameObject("CU_DualBar", new Type[1] { typeof(RectTransform) });
				RectTransform component = btnRoot.GetComponent<RectTransform>();
				((Transform)component).SetParent((Transform)(object)instance.HistoryRect, false);
				component.anchorMin = new Vector2(0f, 1f);
				component.anchorMax = new Vector2(0f, 1f);
				component.pivot = new Vector2(0f, 1f);
				component.sizeDelta = new Vector2(600f, 40f);
				component.anchoredPosition = new Vector2(140f, -566f);
				AddBtn(Loc.T("切换 F9"), 0f, 126f, delegate
				{
					Switch();
				});
				AddBtn(Loc.T("背起"), 132f, 96f, delegate
				{
					ToggleCarry();
				});
				AddBtn(Loc.T("抓胳膊"), 234f, 110f, delegate
				{
					ToggleHoldArm();
				});
				AddBtn(Loc.T("召回"), 350f, 96f, delegate
				{
					Recall();
				});
			}
		}
		catch (Exception ex)
		{
			Log("按钮创建失败：" + ex.Message);
			btnRoot = null;
		}
	}

	private void AddBtn(string text, float x, float w, Action act)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		RectTransform component = btnRoot.GetComponent<RectTransform>();
		GameObject val = new GameObject("CU_btn_" + text, new Type[1] { typeof(RectTransform) });
		RectTransform component2 = val.GetComponent<RectTransform>();
		((Transform)component2).SetParent((Transform)(object)component, false);
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(0f, 1f);
		component2.pivot = new Vector2(0f, 1f);
		component2.sizeDelta = new Vector2(w, 40f);
		component2.anchoredPosition = new Vector2(x, 0f);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = new Color(1f, 1f, 1f, 0.05f);
		((Graphic)val2).raycastTarget = false;
		for (int i = 0; i < 4; i++)
		{
			GameObject val3 = new GameObject("line", new Type[1] { typeof(RectTransform) });
			RectTransform component3 = val3.GetComponent<RectTransform>();
			((Transform)component3).SetParent(val.transform, false);
			component3.anchorMin = new Vector2(0f, 1f);
			component3.anchorMax = new Vector2(0f, 1f);
			component3.pivot = new Vector2(0f, 1f);
			float num = i switch
			{
				3 => w - 1.6f, 
				2 => 0f, 
				_ => 0f, 
			};
			float num2 = ((i == 1) ? (-38.4f) : 0f);
			float num3 = ((i >= 2) ? 1.6f : w);
			float num4 = ((i >= 2) ? 40f : 1.6f);
			component3.sizeDelta = new Vector2(num3, num4);
			component3.anchoredPosition = new Vector2(num, num2);
			Image obj = val3.AddComponent<Image>();
			((Graphic)obj).color = new Color(1f, 1f, 1f, 0.7f);
			((Graphic)obj).raycastTarget = false;
		}
		TextMeshProUGUI txt = AddText(component2, text, 0f, w);
		btnList.Add(new BtnEntry
		{
			rt = component2,
			face = val2,
			txt = txt,
			act = act
		});
	}

	private static TextMeshProUGUI AddText(RectTransform parent, string text, float x, float w)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("txt", new Type[1] { typeof(RectTransform) });
		RectTransform component = val.GetComponent<RectTransform>();
		((Transform)component).SetParent((Transform)(object)parent, false);
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(w, 40f);
		component.anchoredPosition = new Vector2(x, 0f);
		TextMeshProUGUI obj = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)obj).font = NativeHudController.GameFont;
		((TMP_Text)obj).fontSize = 24f;
		Loc.Fit((TMP_Text)obj);
		((Graphic)obj).color = Color.white;
		((TMP_Text)obj).alignment = (TextAlignmentOptions)514;
		((TMP_Text)obj).enableWordWrapping = false;
		((Graphic)obj).raycastTarget = false;
		((TMP_Text)obj).text = text;
		return obj;
	}

	private void TickAI(ModConfig cfg)
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		Body body = main.body;
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		if (carrySide != 0)
		{
			TickCarryPose(cfg);
		}
		else
		{
			if (holdingArm || (Object)(object)Partner == (Object)null || !Partner.alive || !((Component)Partner).gameObject.activeInHierarchy)
			{
				return;
			}
			if (!PlayerControls())
			{
				if ((Object)(object)Player != (Object)null && Player.alive)
				{
					try
					{
						Player.moveDir = Vector2.zero;
					}
					catch
					{
					}
				}
				ApplyFarBuff(Player, far: false, cfg);
			}
			else
			{
				if (!cfg.dualAi)
				{
					FreezePartner();
				}
			}
		}
	}

	/// <summary>
	/// P2 非手操状态：清零残余移动输入，不做任何自动行为，
	/// 与 P1Alloy 的「无 AI」状态完全一致。
	/// </summary>
	private void FreezePartner()
	{
		if ((Object)(object)Partner == (Object)null || !Partner.alive)
		{
			return;
		}
		try
		{
			Partner.moveDir = Vector2.zero;
		}
		catch
		{
		}
	}
}
