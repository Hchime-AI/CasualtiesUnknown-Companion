using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using BepInEx;
using Newtonsoft.Json;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 物理轨迹采样点
	/// </summary>
	[Serializable]
	public struct ShadowTrajectoryPoint
	{
		public float x;
		public float y;
		public float vx;
		public float vy;
		public float t;
	}

	/// <summary>
	/// 跳跃周围区格地形数据
	/// </summary>
	[Serializable]
	public struct ShadowTerrainCell
	{
		public int x;
		public int y;
		public ushort blockId;
		public string name;
		public byte cellType;
		public bool isSolid;
	}

	/// <summary>
	/// 完整跳跃观察数据包
	/// </summary>
	[Serializable]
	public class PlayerJumpRecord
	{
		public int jumpId;
		public string timestamp;
		public string character;
		public float takeoffX;
		public float takeoffY;
		public float takeoffVx;
		public float takeoffVy;
		public float landingX;
		public float landingY;
		public float landingVx;
		public float landingVy;
		public float apexX;
		public float apexY;
		public float duration;
		public float distanceX;
		public float deltaY;
		public bool wasWallJump;
		public List<ShadowTrajectoryPoint> trajectory = new List<ShadowTrajectoryPoint>();
		public List<ShadowTerrainCell> surroundingGrid = new List<ShadowTerrainCell>();
	}

	/// <summary>
	/// 影子路径观察器 (Shadow Jump Observer)
	/// 专职记录玩家在实机中的真实跳跃物理轨迹与周围区格地貌，
	/// 配合 P2DebugRenderer 绘制亮蓝绿色的历史动作线，并将数据流式持久化到本地观察记录中。
	/// 
	/// 【开关与配置标注】：
	/// 1. 轨迹画面渲染开关：ModConfig.p2ShadowJumpRender（可在 F4 设置面板【P2 神经网络实时参数】中随时切换开/关，默认开）
	/// 2. 数据持久化记录开关：ModConfig.p2ShadowJumpRecord（可在 F4 设置面板中切换开/关，默认开）
	/// 3. 本地存储文件地址：ModConfig.p2ShadowRecordPath（默认 BepInEx/config/CUCompanion/player_jump_observations.jsonl，同时在工作区同步双写一份便于直读分析）
	/// </summary>
	public class P2ShadowObserver : MonoBehaviour
	{
		public static P2ShadowObserver Instance { get; private set; }

		// 【默认文件路径标注】
		public const string DefaultLogRelativePath = "BepInEx/config/CUCompanion/player_jump_observations.jsonl";
		public const string DefaultWorkspaceLogPath = "D:/DSH工作区/未知伤亡_陪伴Mod/player_jump_observations.jsonl";

		private bool wasGrounded = true;
		private bool isTrackingJump = false;
		private float jumpStartTime;
		private Vector2 takeoffPos;
		private Vector2 takeoffVel;
		private float maxApexY;
		private Vector2 apexPos;
		private bool wasWallJump;

		private readonly List<ShadowTrajectoryPoint> currentTrajectory = new List<ShadowTrajectoryPoint>(64);
		private readonly List<Vector2> currentRenderPoints = new List<Vector2>(64);
		private readonly List<Vector2> lastJumpRenderPoints = new List<Vector2>(64);

		private int jumpCounter = 0;
		private string workspaceLogPath;

		public IReadOnlyList<Vector2> LastJumpRenderPoints => lastJumpRenderPoints;
		public IReadOnlyList<Vector2> ActiveDisplayPoints => isTrackingJump ? currentRenderPoints : lastJumpRenderPoints;
		public bool IsTrackingJump => isTrackingJump;

		/// <summary>
		/// 获取当前配置的观察记录文件物理绝对路径
		/// </summary>
		public string GetConfiguredLogPath()
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			string path = cfg?.p2ShadowRecordPath;
			if (string.IsNullOrEmpty(path))
			{
				path = DefaultLogRelativePath;
			}
			if (Path.IsPathRooted(path))
			{
				return path;
			}
			string root = Paths.GameRootPath ?? ".";
			return Path.Combine(root, path);
		}

		private void Awake()
		{
			Instance = this;
			workspaceLogPath = DefaultWorkspaceLogPath;
			EnsureDirectory(GetConfiguredLogPath());
		}

		private void OnDestroy()
		{
			if (Instance == this) Instance = null;
		}

		private static void EnsureDirectory(string path)
		{
			try
			{
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
			}
			catch { }
		}

		private Body GetActivePlayerBody()
		{
			DualMode dm = DualMode.Instance;
			if (dm != null && dm.Active)
			{
				return dm.PlayerControls() ? dm.Player : dm.Partner;
			}
			return (PlayerCamera.main != null) ? PlayerCamera.main.body : null;
		}

		private void FixedUpdate()
		{
			Body player = GetActivePlayerBody();
			if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null || !player.alive)
			{
				if (isTrackingJump) EndJumpTracking(null, aborted: true);
				wasGrounded = true;
				return;
			}

			bool grounded = player.grounded;
			Vector2 pos = ((Component)player).transform.position;
			Vector2 vel = (player.rb != null) ? player.rb.velocity : Vector2.zero;

			// ── 1. 探测起跳瞬间 ──
			if (!isTrackingJump)
			{
				// 从地面离地，且垂直速度显著向上 (> 0.4m/s)
				if (wasGrounded && !grounded && vel.y > 0.4f)
				{
					StartJumpTracking(player, pos, vel);
				}
			}
			else
			{
				// ── 2. 跳跃滞空中采样 ──
				float t = Time.unscaledTime - jumpStartTime;
				currentTrajectory.Add(new ShadowTrajectoryPoint
				{
					x = pos.x,
					y = pos.y,
					vx = vel.x,
					vy = vel.y,
					t = t
				});
				currentRenderPoints.Add(pos);

				if (pos.y > maxApexY)
				{
					maxApexY = pos.y;
					apexPos = pos;
				}

				bool touchingWall = Physics2D.Raycast(pos, Vector2.left, 0.45f, LayerMask.GetMask("Ground")) ||
				                    Physics2D.Raycast(pos, Vector2.right, 0.45f, LayerMask.GetMask("Ground"));
				if (touchingWall)
				{
					wasWallJump = true;
				}

				// ── 3. 探测落地（跳跃结束） ──
				if (grounded && vel.y <= 0.2f && t > 0.08f)
				{
					EndJumpTracking(player, aborted: false);
				}
				else if (t > 4.5f) // 滞空超时容错
				{
					EndJumpTracking(player, aborted: false);
				}
			}

			wasGrounded = grounded;
		}

		private void StartJumpTracking(Body player, Vector2 pos, Vector2 vel)
		{
			isTrackingJump = true;
			jumpStartTime = Time.unscaledTime;
			takeoffPos = pos;
			takeoffVel = vel;
			maxApexY = pos.y;
			apexPos = pos;
			wasWallJump = false;

			currentTrajectory.Clear();
			currentRenderPoints.Clear();

			currentTrajectory.Add(new ShadowTrajectoryPoint
			{
				x = pos.x,
				y = pos.y,
				vx = vel.x,
				vy = vel.y,
				t = 0f
			});
			currentRenderPoints.Add(pos);
		}

		private void EndJumpTracking(Body player, bool aborted)
		{
			isTrackingJump = false;
			if (aborted || currentTrajectory.Count < 3)
			{
				return;
			}

			Vector2 landPos = ((UnityEngine.Object)(object)player != (UnityEngine.Object)null) ? (Vector2)((Component)player).transform.position : takeoffPos;
			Vector2 landVel = ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.rb != null) ? player.rb.velocity : Vector2.zero;
			float duration = Time.unscaledTime - jumpStartTime;

			// 跨层传送或大距离位移瞬移，不记录异常跳跃数据，防止遍历成千上万网格冲垮内存
			if (Mathf.Abs(landPos.x - takeoffPos.x) > 30f || Mathf.Abs(landPos.y - takeoffPos.y) > 30f)
			{
				return;
			}

			// 缓存显示轨迹供渲染器绘制亮蓝绿色参考线
			lastJumpRenderPoints.Clear();
			lastJumpRenderPoints.AddRange(currentRenderPoints);

			jumpCounter++;

			// 采集周围区格地貌（仅当开启记录开关时采集）
			ModConfig cfg = CompanionPlugin.Cfg;
			WorldGeneration world = WorldGeneration.world;
			var terrainCells = new List<ShadowTerrainCell>();
			if (cfg != null && cfg.p2ShadowJumpRecord && (UnityEngine.Object)(object)world != (UnityEngine.Object)null && world.worldExists && !world.generatingWorld)
			{
				float minWorldX = Mathf.Min(takeoffPos.x, landPos.x) - 4f;
				float maxWorldX = Mathf.Max(takeoffPos.x, landPos.x) + 4f;
				float minWorldY = Mathf.Min(takeoffPos.y, landPos.y) - 3f;
				float maxWorldY = Mathf.Max(maxApexY, landPos.y) + 3f;

				Vector2Int minGp = world.WorldToBlockPos(new Vector2(minWorldX, minWorldY));
				Vector2Int maxGp = world.WorldToBlockPos(new Vector2(maxWorldX, maxWorldY));

				for (int y = minGp.y; y <= maxGp.y; y++)
				{
					for (int x = minGp.x; x <= maxGp.x; x++)
					{
						Vector2Int gp = new Vector2Int(x, y);
						P2PerceptionNeuron p = P2BrainHub.Instance?.Perception;
						BlockCellType cType = (p != null) ? p.GetCellType(gp) : BlockCellType.Air;
						ushort bId = world.GetBlock(gp);
						string bName = "";
						try
						{
							BlockInfo bInfo = world.GetBlockInfo(bId);
							if (bInfo != null) bName = bInfo.name ?? "";
						}
						catch { }

						terrainCells.Add(new ShadowTerrainCell
						{
							x = x,
							y = y,
							blockId = bId,
							name = bName,
							cellType = (byte)cType,
							isSolid = (cType == BlockCellType.FragileSolid || cType == BlockCellType.HardSolid)
						});
					}
				}
			}

			string who = ((UnityEngine.Object)(object)player != (UnityEngine.Object)null) ? ((UnityEngine.Object)player).name : "Player";

			var record = new PlayerJumpRecord
			{
				jumpId = jumpCounter,
				timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
				character = who,
				takeoffX = takeoffPos.x,
				takeoffY = takeoffPos.y,
				takeoffVx = takeoffVel.x,
				takeoffVy = takeoffVel.y,
				landingX = landPos.x,
				landingY = landPos.y,
				landingVx = landVel.x,
				landingVy = landVel.y,
				apexX = apexPos.x,
				apexY = maxApexY,
				duration = duration,
				distanceX = landPos.x - takeoffPos.x,
				deltaY = landPos.y - takeoffPos.y,
				wasWallJump = wasWallJump,
				trajectory = new List<ShadowTrajectoryPoint>(currentTrajectory),
				surroundingGrid = terrainCells
			};

			SaveRecordJsonL(record);
		}

		private void SaveRecordJsonL(PlayerJumpRecord record)
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			// 【记录持久化开关】：受 ModConfig.p2ShadowJumpRecord 控制
			if (cfg != null && !cfg.p2ShadowJumpRecord)
			{
				return;
			}

			try
			{
				string json = JsonConvert.SerializeObject(record, Formatting.None);
				if (string.IsNullOrEmpty(json)) return;

				string targetPath = GetConfiguredLogPath();
				if (!string.IsNullOrEmpty(targetPath))
				{
					EnsureDirectory(targetPath);
					File.AppendAllText(targetPath, json + "\n", Encoding.UTF8);
				}

				if (!string.IsNullOrEmpty(workspaceLogPath))
				{
					EnsureDirectory(workspaceLogPath);
					File.AppendAllText(workspaceLogPath, json + "\n", Encoding.UTF8);
				}

				CompanionPlugin.Log?.LogInfo($"[ShadowObserver] 已记录玩家跳跃 #{record.jumpId} 到 [{targetPath}]: 距离={record.distanceX:F2}m, 高差={record.deltaY:F2}m, 滞空={record.duration:F2}s, 轨迹点={record.trajectory.Count}, 地貌格={record.surroundingGrid.Count}");
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log?.LogWarning($"[ShadowObserver] 保存跳跃记录失败: {ex.Message}");
			}
		}
	}
}
