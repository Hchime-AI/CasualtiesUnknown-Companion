using System;
using UnityEngine;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// P2-AI 首脑中枢与神经网络总控调度器 (P2 Neural Core Dispatcher)
	/// 由首脑统一调度各神经元组件，形成感知 -> 决策 -> 驱动闭环。
	/// </summary>
	public class P2BrainHub : MonoBehaviour
	{
		public static P2BrainHub Instance { get; private set; }

		// ── 1. 感觉神经元群 (Sensory Neurons) ──
		public P2VisionNeuron Vision { get; private set; }
		public P2PerceptionNeuron Perception { get; private set; }
		public P2AudioNeuron Audio { get; private set; }

		// ── 2. 运动与行动神经元群 (Motor & Action Neurons) ──
		public P2NavNeuron Nav { get; private set; }
		public P2MotorNeuron Motor { get; private set; }
		public P2BreakNeuron Break { get; private set; }

		// ── 3. 情绪与心智神经元群 (Mental & Behavioral Neurons) ──
		public P2ChaosNeuron Chaos { get; private set; }
		public P2PlayNeuron Play { get; private set; }

		// ── 4. 调试渲染器、战术圆环与影子路径观察器 ──
		public P2DebugRenderer DebugRenderer { get; private set; }
		public P2CommandRadialMenu CommandMenu { get; private set; }
		public P2ShadowObserver ShadowObserver { get; private set; }

		private void Awake()
		{
			Instance = this;

			// 初始化各大神经元系统
			Vision = new P2VisionNeuron();
			Perception = new P2PerceptionNeuron();
			Audio = new P2AudioNeuron();

			Nav = new P2NavNeuron();
			Motor = new P2MotorNeuron();
			Break = new P2BreakNeuron();

			Chaos = new P2ChaosNeuron();
			Play = new P2PlayNeuron();

			// 挂载画面 Debug 渲染器、战术圆环与影子路径观察器
			DebugRenderer = gameObject.AddComponent<P2DebugRenderer>();
			CommandMenu = gameObject.AddComponent<P2CommandRadialMenu>();
			ShadowObserver = gameObject.AddComponent<P2ShadowObserver>();
		}

		private void OnDestroy()
		{
			if (Instance == this) Instance = null;
		}

		private void Update()
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			if (cfg == null) return;

			DualMode dm = DualMode.Instance;
			WorldGeneration world = WorldGeneration.world;
			if ((UnityEngine.Object)(object)world == (UnityEngine.Object)null || !world.worldExists || world.generatingWorld || ((UnityEngine.Object)(object)world.loadingObject != (UnityEngine.Object)null && world.loadingObject.activeInHierarchy))
			{
				return;
			}

			Body partner = dm?.Partner;
			Body player = dm?.Player;

			float scanRad = Mathf.Clamp(cfg.p2ScanRadius, 4f, 50000f);

			// ── 常态感知与 AIdebug 网格渲染保障 ──
			// "AIdebug时这个物块网格界面会常态显示在玩家的眼中，关闭时仅AI接管时会将扫描数据反馈给AI神经系统"
			if (cfg.p2AiDebug)
			{
				Body scanCenter = ((UnityEngine.Object)(object)partner != (UnityEngine.Object)null && partner.alive) ? partner : player;
				if ((UnityEngine.Object)(object)scanCenter != (UnityEngine.Object)null)
				{
					Perception.Scan(scanCenter, world, scanRad);
				}
			}

			// ── AI 接管状态下的完整闭环驱动 ──
			if (!cfg.dualAi || (UnityEngine.Object)(object)dm == (UnityEngine.Object)null || !dm.Active)
			{
				return;
			}

			if ((UnityEngine.Object)(object)partner == (UnityEngine.Object)null || !partner.alive || (UnityEngine.Object)(object)player == (UnityEngine.Object)null)
			{
				return;
			}

			// 当训练员正在手操 P1，且 P2 处于未被背负、未被牵手的独自活动状态
			if (dm.PlayerControls() && !dm.Carrying && !dm.HoldingArm)
			{
				// 若非 debug 模式，此时仍需确保感知扫描正常反馈数据给寻路和动作
				if (!cfg.p2AiDebug)
				{
					Perception.Scan(partner, world, scanRad);
				}

				float dt = Time.deltaTime;

				// 步骤 1：若当前为跟随/召回状态，实时规划路线（终点锚定玩家脚底）
				if (Motor.CurrentCommand == P2CommandState.Recall)
				{
					Vector3 pPos = ((Component)player).transform.position;
					float pfY = (player.col != null) ? player.col.bounds.min.y : (pPos.y - 0.7f);
					Vector2 playerFeet = new Vector2(pPos.x, pfY + 0.05f);
					Nav.PlanPath(partner, playerFeet, Perception, world, force: false);
				}

				// 步骤 2：动作系统驱动物理执行
				Motor.Tick(partner, player, Nav, world, dt);
			}
		}
	}
}