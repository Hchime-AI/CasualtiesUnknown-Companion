using System;
using System.Collections.Generic;
using UnityEngine;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 路径节点类型
	/// </summary>
	public enum NavNodeType : byte
	{
		Walk,          // 水平走动 / 1格平滑台阶（利用游戏原生 TryStepUp 贴地行进，绝不突兀起跳）
		JumpTakeoff,   // 助跑起跳点（全速冲刺至此点触发起跳，保持前向水平动能）
		Jump,          // 动能跳跃空中与着地点
		DescentBrink,  // 悬崖边缘前跨点（果断前迈下落，禁止边缘来回犹豫）
		Descent        // 下坠模式（垂直高度 > 3格，贴墙垂直滑落下落）
	}

	/// <summary>
	/// 单个寻路路点
	/// </summary>
	public struct NavWaypoint
	{
		public Vector2 worldPos;
		public Vector2Int gridPos;
		public NavNodeType nodeType;
		public float wallDirection;   // 贴墙方向：-1 为左侧墙面，+1 为右侧墙面，0 为无墙
		public bool requiresCrouch;   // 低矮空间自动蹲下通行
		public int slopeHeight;       // Y轴相差格数：1格为台阶，2至4格为跳跃高台，5格以上为下落
		public bool isAerialGap;      // 是否为空中缺口跳跃
	}

	/// <summary>
	/// 完整行动规划路线
	/// </summary>
	public class NavPath
	{
		public List<NavWaypoint> waypoints = new List<NavWaypoint>();
		public List<Vector2> renderPoints = new List<Vector2>(); // 供 Debug 渲染的高精动能抛物线与折线点集
		public bool isCandidate;   // true = 红色备选路线（需破坏/挖掘）
		public bool isPrimary;     // true = 绿色主选路线（动作系统正在使用）
		public float createTime;
		public float totalCost;
		public bool requiresDigging;
		public List<Vector2Int> breakBlocks = new List<Vector2Int>();
	}

	/// <summary>
	/// 平台动力学寻路神经元 (Platformer A* Engine)
	/// 核心特性：
	/// 1. 真实物理抛物线验障：跳跃轨迹沿线进行角色 Bounding Box 采样，100% 杜绝穿实心块画线；
	/// 2. 坡度分级：1格为平滑台阶（纯水平推力由原版 TryStepUp 顺畅上阶），2~4格为助跑跳跃，>=5格为下坠；
	/// 3. 终点强力锚定到玩家脚底接触面，严禁未寻通时向远处拉直穿墙直线；
	/// 4. 路径生命周期防抖：目标未大幅移动时保持连续航线，彻底消除 0.3s 重构引发的左右抽搐。
	/// </summary>
	public class P2NavNeuron
	{
		private const int MaxHistoryPaths = 6;
		private readonly List<NavPath> activePaths = new List<NavPath>(MaxHistoryPaths);
		private NavPath candidatePath;
		private float lastPlanTime;
		private Vector2 lastTargetPos;
		private Vector2 lastStartFeetPos;

		public IReadOnlyList<NavPath> ActivePaths => activePaths;
		public NavPath PrimaryPath => activePaths.Count > 0 ? activePaths[0] : null;
		public NavPath CandidatePath => candidatePath;

		public void ClearPaths()
		{
			activePaths.Clear();
			candidatePath = null;
		}

		/// <summary>
		/// 根据目标位置与当前感知网格规划行动路径
		/// </summary>
		public bool PlanPath(Body p2, Vector2 targetWorldPos, P2PerceptionNeuron perception, WorldGeneration world, bool force = false)
		{
			if ((UnityEngine.Object)(object)p2 == (UnityEngine.Object)null || (UnityEngine.Object)(object)world == (UnityEngine.Object)null || perception == null)
			{
				ClearPaths();
				return false;
			}

			// 空中滞空飞跃或垂直下坠过程中，绝不打断正在执行的物理轨迹
			if (!force && !p2.grounded && activePaths.Count > 0 && activePaths[0] != null)
			{
				return false;
			}

			float now = Time.unscaledTime;

			// 精确获取角色脚底接触面世界坐标
			Vector3 p2Pos = ((Component)p2).transform.position;
			float p2FeetY = (p2.col != null) ? p2.col.bounds.min.y : (p2Pos.y - 0.7f);
			Vector2 p2FeetWorld = new Vector2(p2Pos.x, p2FeetY + 0.05f);

			// 精确获取玩家脚底接触面世界坐标（锚定真实地面）
			Body player = DualMode.Instance?.Player;
			Vector2 playerFeetWorld;
			if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.alive)
			{
				float pFeetY = (player.col != null) ? player.col.bounds.min.y : (((Component)player).transform.position.y - 0.7f);
				playerFeetWorld = new Vector2(((Component)player).transform.position.x, pFeetY + 0.05f);
			}
			else
			{
				playerFeetWorld = targetWorldPos;
			}

			// ── 路径生命周期稳定性防抖：避免每 0.3s 重构导致的左右抽搐 ──
			if (!force && activePaths.Count > 0 && activePaths[0] != null && activePaths[0].waypoints.Count > 0)
			{
				float targetDelta = Vector2.Distance(playerFeetWorld, lastTargetPos);
				float startDelta = Vector2.Distance(p2FeetWorld, lastStartFeetPos);
				// 玩家未大幅移动 (< 1.6m) 且当前路径创建未超时 (< 1.0s) 时，保持当前有效路径
				if (targetDelta < 1.6f && (now - lastPlanTime < 1.0f) && startDelta < 2.5f)
				{
					return false;
				}
			}

			// 规划节流：至少间隔 0.25s
			if (!force && now - lastPlanTime < 0.25f)
			{
				return false;
			}
			lastPlanTime = now;
			lastTargetPos = playerFeetWorld;
			lastStartFeetPos = p2FeetWorld;

			// 若角色与目标已经在身旁极近处 (<= 1.4m)，直接维持平滑近距连接，保持待命
			if (Vector2.Distance(p2FeetWorld, playerFeetWorld) <= 1.4f)
			{
				Vector2Int p2Gp = world.WorldToBlockPos(p2FeetWorld);
				Vector2Int pGp = world.WorldToBlockPos(playerFeetWorld);
				var directPath = new NavPath
				{
					waypoints = new List<NavWaypoint>
					{
						new NavWaypoint { worldPos = p2FeetWorld, gridPos = p2Gp, nodeType = NavNodeType.Walk },
						new NavWaypoint { worldPos = playerFeetWorld, gridPos = pGp, nodeType = NavNodeType.Walk }
					},
					renderPoints = new List<Vector2> { p2FeetWorld, playerFeetWorld },
					createTime = now,
					isPrimary = true
				};
				activePaths.Clear();
				activePaths.Add(directPath);
				return true;
			}

			Vector2Int startGp = FindCharacterGround(p2FeetWorld, p2.grounded, perception, world);
			Vector2Int goalGp = FindNearestGround(world.WorldToBlockPos(playerFeetWorld), perception);

			// 提取角色跳跃动能参数
			float jumpSpeed = 9.2f;
			float maxRunSpeed = 4.5f;
			try
			{
				if (p2.actualJumpSpeed > 1f) jumpSpeed = p2.actualJumpSpeed;
				if (p2.actualMaxSpeed > 1f) maxRunSpeed = p2.actualMaxSpeed;
			}
			catch { }

			// 1. 规划正常物理行动路线
			NavPath mainPath = SearchAStar(startGp, goalGp, perception, world, jumpSpeed, maxRunSpeed, allowDigging: false, playerFeetWorld);

			// 2. 备选破坏路线（仅当正常主路径未能完全寻通时）
			NavPath digPath = (mainPath == null) ? SearchAStar(startGp, goalGp, perception, world, jumpSpeed, maxRunSpeed, allowDigging: true, playerFeetWorld) : null;
			if (digPath != null && digPath.requiresDigging)
			{
				digPath.isCandidate = true;
				digPath.isPrimary = false;
				candidatePath = digPath;
			}
			else
			{
				candidatePath = null;
			}

			if (mainPath == null && candidatePath != null)
			{
				mainPath = candidatePath;
			}

			if (mainPath == null || mainPath.waypoints.Count == 0)
			{
				return false;
			}

			mainPath.isPrimary = true;
			mainPath.isCandidate = false;

			activePaths.Clear();
			activePaths.Add(mainPath);
			return true;
		}

		private Vector2Int FindCharacterGround(Vector2 feetWorldPos, bool grounded, P2PerceptionNeuron perception, WorldGeneration world)
		{
			Vector2Int centerGp = world.WorldToBlockPos(feetWorldPos);
			if (perception.IsWalkableGround(centerGp)) return centerGp;

			if (grounded)
			{
				Vector2Int leftGp = world.WorldToBlockPos(new Vector2(feetWorldPos.x - 0.28f, feetWorldPos.y));
				if (perception.IsWalkableGround(leftGp)) return leftGp;

				Vector2Int rightGp = world.WorldToBlockPos(new Vector2(feetWorldPos.x + 0.28f, feetWorldPos.y));
				if (perception.IsWalkableGround(rightGp)) return rightGp;

				for (int dy = -1; dy <= 1; dy++)
				{
					if (dy == 0) continue;
					Vector2Int testC = new Vector2Int(centerGp.x, centerGp.y + dy);
					if (perception.IsWalkableGround(testC)) return testC;
					Vector2Int testL = new Vector2Int(leftGp.x, leftGp.y + dy);
					if (perception.IsWalkableGround(testL)) return testL;
					Vector2Int testR = new Vector2Int(rightGp.x, rightGp.y + dy);
					if (perception.IsWalkableGround(testR)) return testR;
				}
				return centerGp;
			}

			// 空中状态：向下探测地面
			for (int dy = -1; dy >= -16; dy--)
			{
				Vector2Int test = new Vector2Int(centerGp.x, centerGp.y + dy);
				if (perception.IsWalkableGround(test)) return test;
			}
			for (int dy = 1; dy <= 3; dy++)
			{
				Vector2Int test = new Vector2Int(centerGp.x, centerGp.y + dy);
				if (perception.IsWalkableGround(test)) return test;
			}
			return centerGp;
		}

		private Vector2Int FindNearestGround(Vector2Int gp, P2PerceptionNeuron perception)
		{
			if (perception.IsWalkableGround(gp)) return gp;
			// 1. 脚底 ±1 格
			for (int dy = -1; dy <= 1; dy++)
			{
				Vector2Int test = new Vector2Int(gp.x, gp.y + dy);
				if (perception.IsWalkableGround(test)) return test;
			}
			// 2. 左右两侧
			for (int dx = -2; dx <= 2; dx++)
			{
				if (dx == 0) continue;
				for (int dy = -1; dy <= 1; dy++)
				{
					Vector2Int test = new Vector2Int(gp.x + dx, gp.y + dy);
					if (perception.IsWalkableGround(test)) return test;
				}
			}
			// 3. 向下探测
			for (int dy = -2; dy >= -16; dy--)
			{
				Vector2Int test = new Vector2Int(gp.x, gp.y + dy);
				if (perception.IsWalkableGround(test)) return test;
			}
			// 4. 向上探测
			for (int dy = 2; dy <= 6; dy++)
			{
				Vector2Int test = new Vector2Int(gp.x, gp.y + dy);
				if (perception.IsWalkableGround(test)) return test;
			}
			return gp;
		}

		// ── A* 核心实现 ──

		private class SearchNode
		{
			public Vector2Int pos;
			public float gCost;
			public float hCost;
			public float fCost => gCost + hCost;
			public NavNodeType moveType;
			public float wallDir;
			public int slopeHeight;
			public bool isAerial;
			public float jumpApexLift;
			public SearchNode parent;
			public bool isDigNode;
			public int heapIndex;
		}

		private class FastMinHeap
		{
			private SearchNode[] heap;
			public int Count { get; private set; }

			public FastMinHeap(int capacity)
			{
				heap = new SearchNode[capacity];
				Count = 0;
			}

			public void Push(SearchNode node)
			{
				if (Count >= heap.Length)
				{
					Array.Resize(ref heap, heap.Length * 2);
				}
				int i = Count++;
				heap[i] = node;
				node.heapIndex = i;
				Up(i);
			}

			public SearchNode Pop()
			{
				SearchNode root = heap[0];
				Count--;
				if (Count > 0)
				{
					SearchNode last = heap[Count];
					heap[0] = last;
					last.heapIndex = 0;
					Down(0);
				}
				return root;
			}

			public void Update(SearchNode node)
			{
				Up(node.heapIndex);
			}

			private void Up(int i)
			{
				SearchNode node = heap[i];
				while (i > 0)
				{
					int p = (i - 1) >> 1;
					SearchNode parent = heap[p];
					if (node.fCost < parent.fCost)
					{
						heap[i] = parent;
						parent.heapIndex = i;
						i = p;
					}
					else break;
				}
				heap[i] = node;
				node.heapIndex = i;
			}

			private void Down(int i)
			{
				SearchNode node = heap[i];
				int half = Count >> 1;
				while (i < half)
				{
					int left = (i << 1) + 1;
					int right = left + 1;
					int bestChild = (right < Count && heap[right].fCost < heap[left].fCost) ? right : left;
					if (heap[bestChild].fCost < node.fCost)
					{
						heap[i] = heap[bestChild];
						heap[bestChild].heapIndex = i;
						i = bestChild;
					}
					else break;
				}
				heap[i] = node;
				node.heapIndex = i;
			}
		}

		private NavPath SearchAStar(Vector2Int start, Vector2Int goal, P2PerceptionNeuron perception, WorldGeneration world, float jumpSpeed, float runSpeed, bool allowDigging, Vector2 targetFeetPos)
		{
			var openHeap = new FastMinHeap(1024);
			var closedSet = new HashSet<Vector2Int>();
			var allNodes = new Dictionary<Vector2Int, SearchNode>(2048);

			SearchNode startNode = new SearchNode
			{
				pos = start,
				gCost = 0f,
				hCost = Heuristic(start, goal),
				moveType = NavNodeType.Walk,
				slopeHeight = 0,
				parent = null
			};
			openHeap.Push(startNode);
			allNodes[start] = startNode;

			int maxSteps = 4000;
			SearchNode bestNode = startNode;
			float bestDist = Heuristic(start, goal);

			while (openHeap.Count > 0 && maxSteps-- > 0)
			{
				SearchNode current = openHeap.Pop();

				if (current.pos == goal || (Mathf.Abs(current.pos.x - goal.x) <= 1 && Mathf.Abs(current.pos.y - goal.y) <= 1))
				{
					return ReconstructPath(current, world, perception, jumpSpeed, runSpeed, targetFeetPos, goal, reachedGoal: true);
				}

				closedSet.Add(current.pos);

				float distToGoal = Heuristic(current.pos, goal);
				if (distToGoal < bestDist)
				{
					bestDist = distToGoal;
					bestNode = current;
				}

				ExpandNeighbors(current, goal, perception, openHeap, closedSet, allNodes, jumpSpeed, runSpeed, allowDigging);
			}

			// 未完全到达终点但有有效进展时，截断至最近可达节点，杜绝穿墙直线
			if (bestNode != null && bestNode != startNode && bestDist < Heuristic(start, goal) * 0.75f)
			{
				return ReconstructPath(bestNode, world, perception, jumpSpeed, runSpeed, targetFeetPos, goal, reachedGoal: false);
			}

			return null;
		}

		/// <summary>
		/// 核心物理验障：沿真实抛物线对角色 Bounding Box 进行高密采样，
		/// 只要抛物线上任意一帧的角色身位触碰实心方块或天花板，立即判定为阻断，100% 杜绝穿墙画线！
		/// </summary>
		private static bool CheckJumpTrajectoryClear(Vector2Int from, Vector2Int to, P2PerceptionNeuron p, float jumpApexLift)
		{
			// 1. 起跳点头顶必须通畅
			if (!p.IsPassable(new Vector2Int(from.x, from.y + 1)) || !p.IsPassable(new Vector2Int(from.x, from.y + 2)))
			{
				return false;
			}
			// 2. 着地点身位必须通畅
			if (!p.IsPassable(to) || !p.IsPassable(new Vector2Int(to.x, to.y + 1)))
			{
				return false;
			}

			float apexY = Mathf.Max(from.y, to.y) + jumpApexLift;
			int samples = Mathf.Max(10, (Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y)) * 4);

			for (int i = 1; i < samples; i++)
			{
				float t = (float)i / (float)samples;
				float curX = Mathf.Lerp(from.x, to.x, t);
				float arcOffset = 4f * (apexY - Mathf.Max(from.y, to.y)) * t * (1f - t);
				float curY = Mathf.Lerp(from.y, to.y, t) + arcOffset;

				int blockX = Mathf.RoundToInt(curX);
				int blockY = Mathf.FloorToInt(curY);

				// 角色身高 2 格：检查脚底 blockY 与头部 blockY + 1
				if (p.IsSolid(new Vector2Int(blockX, blockY)) || p.IsSolid(new Vector2Int(blockX, blockY + 1)))
				{
					return false; // 撞到实心岩石或天花板！
				}
			}
			return true;
		}

		private void ExpandNeighbors(SearchNode current, Vector2Int goal, P2PerceptionNeuron p, FastMinHeap openHeap, HashSet<Vector2Int> closed, Dictionary<Vector2Int, SearchNode> all, float jumpSpeed, float runSpeed, bool allowDig)
		{
			int cx = current.pos.x;
			int cy = current.pos.y;
			int[] dirs = new int[] { -1, 1 };

			// ── 1. 水平行走与 1 格平滑台阶 ──
			// 利用游戏原生 TryStepUp 机制：1格高度差纯走平推即可自动上台阶，坚决不跳跃！
			foreach (int dx in dirs)
			{
				// A. 纯水平行走 (dy = 0)
				Vector2Int hPos = new Vector2Int(cx + dx, cy);
				if (!closed.Contains(hPos))
				{
					if (p.IsWalkableGround(hPos))
					{
						float cost = 1.0f;
						if (p.IsLiquid(hPos)) cost += 2.0f;
						TryAddNode(current, hPos, goal, cost, NavNodeType.Walk, 0f, 0, false, 0f, false, openHeap, all);
					}
					else if (allowDig)
					{
						if (p.IsFragile(hPos) || p.IsFragile(hPos + Vector2Int.up))
						{
							TryAddNode(current, hPos, goal, 8.0f, NavNodeType.Walk, 0f, 0, false, 0f, true, openHeap, all);
						}
					}
				}

				// B. 1 格台阶平滑上升 (dy = +1)：前方地面是实心台阶，头顶空间通畅
				Vector2Int stepUpPos = new Vector2Int(cx + dx, cy + 1);
				if (!closed.Contains(stepUpPos) && p.IsWalkableGround(stepUpPos))
				{
					// 起步瞬间头顶通畅，不碰天花板
					if (p.IsPassable(new Vector2Int(cx, cy + 2)))
					{
						TryAddNode(current, stepUpPos, goal, 1.15f, NavNodeType.Walk, 0f, 1, false, 0f, false, openHeap, all);
					}
				}

				// C. 1 格台阶平滑下降 (dy = -1)
				Vector2Int stepDownPos = new Vector2Int(cx + dx, cy - 1);
				if (!closed.Contains(stepDownPos) && p.IsWalkableGround(stepDownPos))
				{
					TryAddNode(current, stepDownPos, goal, 1.10f, NavNodeType.Walk, 0f, 1, false, 0f, false, openHeap, all);
				}
			}

			// ── 2. 高于 1 格的高台跳跃攀登 (dy = 2 ~ 4, dx = -2 ~ 2) ──
			// 仅当面对高度 >= 2 的实心障碍壁、无法通过 1 格台阶平推上去时才允许触发跳跃！
			// 若前方存在可平步上阶的连续台阶 (cx + stepDir, cy + 1)，严禁跳跃，由 Walk 平滑顺畅走上去！
			for (int dx = -2; dx <= 2; dx++)
			{
				int stepDir = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
				if (stepDir != 0 && p.IsWalkableGround(new Vector2Int(cx + stepDir, cy + 1)))
				{
					continue; // 前方是连续阶梯斜坡，坚决走上去，绝不碎跳！
				}

				for (int dy = 2; dy <= 4; dy++)
				{
					Vector2Int jumpPos = new Vector2Int(cx + dx, cy + dy);
					if (closed.Contains(jumpPos)) continue;
					if (!p.IsWalkableGround(jumpPos)) continue;

					float apexLift = 1.0f + dy * 0.2f;
					if (!CheckJumpTrajectoryClear(current.pos, jumpPos, p, apexLift))
					{
						continue; // 物理阻断，绝不穿墙！
					}

					float cost = 3.6f + Mathf.Abs(dx) * 0.5f + dy * 0.6f;
					TryAddNode(current, jumpPos, goal, cost, NavNodeType.Jump, 0f, dy, false, apexLift, false, openHeap, all);
				}
			}

			// ── 3. 跨越垂直高障碍与空中深坑飞跃 (span = 2 ~ 4, dy = -2 ~ 1) ──
			foreach (int dx in dirs)
			{
				for (int span = 2; span <= 4; span++)
				{
					for (int dy = -2; dy <= 1; dy++)
					{
						Vector2Int landingPos = new Vector2Int(cx + dx * span, cy + dy);
						if (closed.Contains(landingPos)) continue;
						if (!p.IsWalkableGround(landingPos)) continue;

						// 严格确认：中间必须存在真正的深坑断裂（无地面支撑）或高度 >= 2 的实心高墙！
						// 若中间全是连续地面或 1 格平缓台阶，坚决禁止 Jump！
						bool hasRealAbyssOrTallWall = false;
						for (int s = 1; s < span; s++)
						{
							int colX = cx + dx * s;
							// 真正的悬空坑洼（下方深坑无地面支撑）
							if (!p.IsSolid(new Vector2Int(colX, cy - 1)) && !p.IsSolid(new Vector2Int(colX, cy - 2)))
							{
								hasRealAbyssOrTallWall = true;
							}
							// 高度 >= 2 的实心高墙阻挡（无法平推上阶）
							if (p.IsSolid(new Vector2Int(colX, cy)) && p.IsSolid(new Vector2Int(colX, cy + 1)))
							{
								hasRealAbyssOrTallWall = true;
							}
						}
						if (!hasRealAbyssOrTallWall) continue;

						float apexLift = (dy > 0) ? 1.2f : 0.85f;
						if (!CheckJumpTrajectoryClear(current.pos, landingPos, p, apexLift))
						{
							continue; // 抛物线撞墙/撞顶，剔除！
						}

						float cost = 4.0f + span * 0.5f + Mathf.Max(0, dy) * 0.6f;
						TryAddNode(current, landingPos, goal, cost, NavNodeType.Jump, 0f, Mathf.Abs(dy), isAerial: true, jumpApexLift: apexLift, isDig: false, openHeap, all);
					}
				}
			}

			// ── 4. 悬崖下落与下坠模式 (drop = 2 ~ 16) ──
			foreach (int dx in dirs)
			{
				// 前方脚下必须悬空才进入下落探测
				if (p.IsSolid(new Vector2Int(cx + dx, cy - 1))) continue;

				for (int drop = 2; drop <= 16; drop++)
				{
					Vector2Int dropPos = new Vector2Int(cx + dx, cy - drop);
					if (closed.Contains(dropPos)) continue;

					// 确保垂直下落竖井内部无实心阻挡
					bool shaftBlocked = false;
					for (int sy = 0; sy > -drop; sy--)
					{
						if (p.IsSolid(new Vector2Int(cx + dx, cy + sy)))
						{
							shaftBlocked = true;
							break;
						}
					}
					if (shaftBlocked) break;

					if (p.IsWalkableGround(dropPos))
					{
						// 寻找侧面墙壁提供贴墙引导
						float wallDir = 0f;
						for (int sy = 0; sy >= -drop; sy--)
						{
							if (p.IsSolid(new Vector2Int(cx + dx - 1, cy + sy))) { wallDir = -1f; break; }
							if (p.IsSolid(new Vector2Int(cx + dx + 1, cy + sy))) { wallDir = 1f; break; }
						}

						NavNodeType nodeType = (drop > 3) ? NavNodeType.Descent : NavNodeType.Walk;
						float dropCost = 1.1f + drop * 0.2f;
						TryAddNode(current, dropPos, goal, dropCost, nodeType, wallDir, drop, false, 0f, false, openHeap, all);
						break;
					}
				}
			}
		}

		private void TryAddNode(SearchNode parent, Vector2Int pos, Vector2Int goal, float stepCost, NavNodeType type, float wallDir, int slope, bool isAerial, float jumpApexLift, bool isDig, FastMinHeap openHeap, Dictionary<Vector2Int, SearchNode> all)
		{
			float newG = parent.gCost + stepCost;
			if (all.TryGetValue(pos, out SearchNode existing))
			{
				if (newG < existing.gCost)
				{
					existing.gCost = newG;
					existing.parent = parent;
					existing.moveType = type;
					existing.wallDir = wallDir;
					existing.slopeHeight = slope;
					existing.isAerial = isAerial;
					existing.jumpApexLift = jumpApexLift;
					existing.isDigNode = isDig || parent.isDigNode;
					openHeap.Update(existing);
				}
			}
			else
			{
				SearchNode node = new SearchNode
				{
					pos = pos,
					gCost = newG,
					hCost = Heuristic(pos, goal),
					moveType = type,
					wallDir = wallDir,
					slopeHeight = slope,
					isAerial = isAerial,
					jumpApexLift = jumpApexLift,
					isDigNode = isDig || parent.isDigNode,
					parent = parent
				};
				openHeap.Push(node);
				all[pos] = node;
			}
		}

		private static float Heuristic(Vector2Int a, Vector2Int b)
		{
			float dx = a.x - b.x;
			float dy = a.y - b.y;
			return Mathf.Sqrt(dx * dx + dy * dy);
		}

		private NavPath ReconstructPath(SearchNode endNode, WorldGeneration world, P2PerceptionNeuron p, float jumpSpeed, float runSpeed, Vector2 targetFeetPos, Vector2Int goalGp, bool reachedGoal)
		{
			var waypoints = new List<NavWaypoint>();
			var renderPoints = new List<Vector2>();
			var breakBlocks = new List<Vector2Int>();
			bool requiresDig = false;

			float halfW = world.halfWidth;
			float halfH = world.halfHeight;

			List<SearchNode> chain = new List<SearchNode>();
			SearchNode curr = endNode;
			while (curr != null)
			{
				chain.Insert(0, curr);
				if (curr.isDigNode)
				{
					requiresDig = true;
					breakBlocks.Add(curr.pos);
				}
				curr = curr.parent;
			}

			for (int i = 0; i < chain.Count; i++)
			{
				SearchNode node = chain[i];
				Vector2 nodeWorld = new Vector2((float)node.pos.x - halfW + 0.5f, (float)node.pos.y - halfH);

				if (i == 0)
				{
					waypoints.Add(new NavWaypoint
					{
						gridPos = node.pos,
						worldPos = nodeWorld,
						nodeType = NavNodeType.Walk,
						wallDirection = 0f,
						requiresCrouch = p.IsCrouchPassage(node.pos),
						slopeHeight = 0,
						isAerialGap = false
					});
					renderPoints.Add(nodeWorld);
					continue;
				}

				SearchNode prev = chain[i - 1];
				Vector2 prevWorld = new Vector2((float)prev.pos.x - halfW + 0.5f, (float)prev.pos.y - halfH);
				float dxSign = Mathf.Sign(node.pos.x - prev.pos.x);
				if (dxSign == 0f) dxSign = 1f;

				if (node.moveType == NavNodeType.Walk)
				{
					// 水平走动与 1 格台阶平滑过渡（贴合方块边缘，直角阶梯折线，不斜切方块）
					if (node.pos.y > prev.pos.y) // 上台阶 1 格
					{
						Vector2 stepBase = new Vector2(nodeWorld.x - dxSign * 0.50f, prevWorld.y);
						Vector2 stepTop = new Vector2(nodeWorld.x - dxSign * 0.50f, nodeWorld.y);
						renderPoints.Add(stepBase);
						renderPoints.Add(stepTop);
					}
					else if (node.pos.y < prev.pos.y) // 下台阶 1 格
					{
						Vector2 stepEdge = new Vector2(prevWorld.x + dxSign * 0.50f, prevWorld.y);
						Vector2 stepBottom = new Vector2(prevWorld.x + dxSign * 0.50f, nodeWorld.y);
						renderPoints.Add(stepEdge);
						renderPoints.Add(stepBottom);
					}
					renderPoints.Add(nodeWorld);

					waypoints.Add(new NavWaypoint
					{
						gridPos = node.pos,
						worldPos = nodeWorld,
						nodeType = NavNodeType.Walk,
						wallDirection = 0f,
						requiresCrouch = p.IsCrouchPassage(node.pos),
						slopeHeight = node.slopeHeight,
						isAerialGap = false
					});
				}
				else if (node.moveType == NavNodeType.Jump)
				{
					// 助跑起跳：起跳点设置在距前一位置边缘约 0.4 米处，保留冲刺距离
					float takeoffOffsetX = (node.pos.x == prev.pos.x) ? 0f : dxSign * 0.40f;
					Vector2 takeoffWorld = new Vector2(prevWorld.x + takeoffOffsetX, prevWorld.y);

					// 1. 助跑起跳路点 (Takeoff)
					waypoints.Add(new NavWaypoint
					{
						gridPos = prev.pos,
						worldPos = takeoffWorld,
						nodeType = NavNodeType.JumpTakeoff,
						wallDirection = 0f,
						requiresCrouch = p.IsCrouchPassage(prev.pos),
						slopeHeight = 0,
						isAerialGap = node.isAerial
					});

					// 2. 着陆目标路点 (Landing)
					waypoints.Add(new NavWaypoint
					{
						gridPos = node.pos,
						worldPos = nodeWorld,
						nodeType = NavNodeType.Jump,
						wallDirection = 0f,
						requiresCrouch = p.IsCrouchPassage(node.pos),
						slopeHeight = node.slopeHeight,
						isAerialGap = node.isAerial
					});

					// 绘制通过验证的精确抛物线（绝不穿墙）
					renderPoints.Add(takeoffWorld);
					float apexY = Mathf.Max(takeoffWorld.y, nodeWorld.y) + node.jumpApexLift;
					int samples = node.isAerial ? 12 : 8;
					for (int s = 1; s <= samples; s++)
					{
						float t = (float)s / (float)samples;
						float curX = Mathf.Lerp(takeoffWorld.x, nodeWorld.x, t);
						float arcOffset = 4f * (apexY - Mathf.Max(takeoffWorld.y, nodeWorld.y)) * t * (1f - t);
						float curY = Mathf.Lerp(takeoffWorld.y, nodeWorld.y, t) + arcOffset;
						renderPoints.Add(new Vector2(curX, curY));
					}
				}
				else if (node.moveType == NavNodeType.Descent)
				{
					// 下坠模式 (> 3 格)
					Vector2 brinkWorld = new Vector2(prevWorld.x + dxSign * 0.48f, prevWorld.y);

					// 1. 悬崖边缘前迈点
					waypoints.Add(new NavWaypoint
					{
						gridPos = prev.pos,
						worldPos = brinkWorld,
						nodeType = NavNodeType.DescentBrink,
						wallDirection = 0f,
						requiresCrouch = p.IsCrouchPassage(prev.pos),
						slopeHeight = 0,
						isAerialGap = false
					});

					// 2. 地面着落路点
					waypoints.Add(new NavWaypoint
					{
						gridPos = node.pos,
						worldPos = nodeWorld,
						nodeType = NavNodeType.Descent,
						wallDirection = node.wallDir,
						requiresCrouch = p.IsCrouchPassage(node.pos),
						slopeHeight = node.slopeHeight,
						isAerialGap = false
					});

					// 贴墙垂直下落折线
					float wallBias = node.wallDir * 0.30f;
					Vector2 wallShaftTop = new Vector2(brinkWorld.x + wallBias, prevWorld.y - 0.25f);
					Vector2 wallShaftBottom = new Vector2(brinkWorld.x + wallBias, nodeWorld.y + 0.15f);

					renderPoints.Add(brinkWorld);
					renderPoints.Add(wallShaftTop);
					renderPoints.Add(wallShaftBottom);
					renderPoints.Add(nodeWorld);
				}
			}

			// 终点强力锚定：仅当成功寻通至玩家脚下地面格时，连接最后一小段至玩家站立脚底
			if (reachedGoal && renderPoints.Count > 0 && Vector2.Distance(renderPoints[renderPoints.Count - 1], targetFeetPos) <= 1.4f)
			{
				waypoints.Add(new NavWaypoint
				{
					gridPos = goalGp,
					worldPos = targetFeetPos,
					nodeType = NavNodeType.Walk,
					wallDirection = 0f,
					requiresCrouch = false,
					slopeHeight = 0,
					isAerialGap = false
				});
				renderPoints.Add(targetFeetPos);
			}

			return new NavPath
			{
				waypoints = waypoints,
				renderPoints = renderPoints,
				createTime = Time.unscaledTime,
				totalCost = endNode.gCost,
				requiresDigging = requiresDig,
				breakBlocks = breakBlocks
			};
		}
	}
}
