using System;
using System.Collections.Generic;
using UnityEngine;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 物块网格分类
	/// </summary>
	public enum BlockCellType : byte
	{
		Air = 0,          // 空气网格
		FragileSolid = 1, // 绿色网格：脆弱实心物块（普通岩石、泥土、木材、植物等）
		HardSolid = 2,    // 红色网格：强硬实心物块（以钢铁、合金、高耐久金属为基准）
		Liquid = 3        // 蓝色网格：液体物块（水、酸液、流体）
	}

	/// <summary>
	/// 单个扫描感知到的方块数据结构
	/// </summary>
	public struct ScannedCell
	{
		public Vector2Int gridPos;
		public BlockCellType type;
		public float health;
		public string name;
		public Rect worldBounds;
	}

	/// <summary>
	/// 感知神经元：以角色脚底为中心全程实时扫描周边地形网格，
	/// 识别并分类脆弱实心(绿)、强硬实心(红)、液体(蓝)及空气。
	/// 支持最高全图 50000 范围扫描，精准识别低矮天花板蹲下通道与各级坡度。
	/// </summary>
	public class P2PerceptionNeuron
	{
		private readonly Dictionary<Vector2Int, ScannedCell> scannedCells = new Dictionary<Vector2Int, ScannedCell>();
		private Vector2Int lastCenterBlock;
		private float lastScanTime;
		private float scanInterval = 0.12f; // 约 8Hz 刷新网格缓存，兼顾实时性与极低性能开销

		public IReadOnlyDictionary<Vector2Int, ScannedCell> ScannedCells => scannedCells;
		public Vector2Int CenterBlock => lastCenterBlock;
		public float LastScanTime => lastScanTime;

		/// <summary>
		/// 实时更新以指定角色脚底为中心的周边物块感知网格
		/// </summary>
		public void Scan(Body targetBody, WorldGeneration world, float radius)
		{
			if ((UnityEngine.Object)(object)targetBody == (UnityEngine.Object)null || (UnityEngine.Object)(object)world == (UnityEngine.Object)null)
			{
				scannedCells.Clear();
				return;
			}

			// 精确计算脚底接触面方块
			Vector3 bodyPos = ((Component)targetBody).transform.position;
			float feetY = (targetBody.col != null) ? targetBody.col.bounds.min.y : (bodyPos.y - 0.7f);
			Vector2 feetWorldPos = new Vector2(bodyPos.x, feetY + 0.1f);
			lastCenterBlock = world.WorldToBlockPos(feetWorldPos);

			// 本地快速缓存半径上限收敛为 25（约 50x50 格，完全满足视野与局部高精展示，杜绝大半径卡顿）
			int cacheRad = Mathf.Clamp(Mathf.RoundToInt(radius), 8, 25);
			float now = Time.unscaledTime;
			if (now - lastScanTime < scanInterval && scannedCells.Count > 0)
			{
				return;
			}
			lastScanTime = now;

			float halfW = world.halfWidth;
			float halfH = world.halfHeight;

			int minX = Mathf.Max(0, lastCenterBlock.x - cacheRad);
			int maxX = Mathf.Min((int)world.width - 1, lastCenterBlock.x + cacheRad);
			int minY = Mathf.Max(0, lastCenterBlock.y - cacheRad);
			int maxY = Mathf.Min((int)world.height - 1, lastCenterBlock.y + cacheRad);

			long radSq = (long)cacheRad * cacheRad;

			scannedCells.Clear();

			for (int y = minY; y <= maxY; y++)
			{
				int dy = y - lastCenterBlock.y;
				long dySq = (long)dy * dy;

				for (int x = minX; x <= maxX; x++)
				{
					int dx = x - lastCenterBlock.x;
					if ((long)dx * dx + dySq > radSq)
					{
						continue;
					}

					Vector2Int gp = new Vector2Int(x, y);
					BlockCellType cellType = ClassifyBlock(world, gp, out float health, out string blockName);

					// 世界坐标对齐：方块 (x, y) 的左下角为 (x - halfW, y - halfH)，长宽各 1.0
					Rect bounds = new Rect(
						(float)x - halfW,
						(float)y - halfH,
						1f,
						1f
					);

					scannedCells[gp] = new ScannedCell
					{
						gridPos = gp,
						type = cellType,
						health = health,
						name = blockName,
						worldBounds = bounds
					};
				}
			}
		}

		private static readonly Dictionary<ushort, (BlockCellType type, float health, string name)> blockMetaCache =
			new Dictionary<ushort, (BlockCellType, float, string)>();

		/// <summary>
		/// 根据物块属性、硬度标准及液体属性分类物块
		/// </summary>
		public static BlockCellType ClassifyBlock(WorldGeneration w, Vector2Int pos, out float health, out string name)
		{
			health = 0f;
			name = string.Empty;

			if ((UnityEngine.Object)(object)w == (UnityEngine.Object)null)
			{
				return BlockCellType.Air;
			}

			// 地图世界边界外部视为不可逾越的强硬基岩实心阻挡
			if (pos.x < 0 || pos.x >= (int)w.width || pos.y < 0 || pos.y >= (int)w.height)
			{
				return BlockCellType.HardSolid;
			}

			ushort blockId = 0;
			try
			{
				blockId = w.GetBlock(pos);
			}
			catch
			{
				return BlockCellType.Air;
			}

			// 1. 空气方块：检查是否存在液体
			if (blockId == 0)
			{
				if (CheckHasLiquid(pos))
				{
					return BlockCellType.Liquid;
				}
				return BlockCellType.Air;
			}

			// 2. 实心方块：快速元数据缓存直查
			if (blockMetaCache.TryGetValue(blockId, out var cached))
			{
				health = cached.health;
				name = cached.name;
				return cached.type;
			}

			BlockCellType resolvedType = BlockCellType.FragileSolid;
			try
			{
				BlockInfo info = w.GetBlockInfo(blockId);
				if (info != null)
				{
					health = info.health;
					name = info.name ?? string.Empty;

					if (info.metallic || info.health >= 150f || IsHardMaterialName(name))
					{
						resolvedType = BlockCellType.HardSolid;
					}
				}
			}
			catch
			{
			}

			blockMetaCache[blockId] = (resolvedType, health, name);
			return resolvedType;
		}

		private static bool CheckHasLiquid(Vector2Int pos)
		{
			try
			{
				FluidManager fm = FluidManager.main;
				if ((UnityEngine.Object)(object)fm != (UnityEngine.Object)null)
				{
					return fm.HasLiquid(pos);
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool IsHardMaterialName(string n)
		{
			if (string.IsNullOrEmpty(n)) return false;
			string lower = n.ToLowerInvariant();
			return lower.Contains("steel") ||
				   lower.Contains("iron") ||
				   lower.Contains("metal") ||
				   lower.Contains("alloy") ||
				   lower.Contains("titanium") ||
				   lower.Contains("dense") ||
				   lower.Contains("reinforced") ||
				   lower.Contains("bedrock") ||
				   lower.Contains("concrete") ||
				   lower.Contains("钢") ||
				   lower.Contains("铁") ||
				   lower.Contains("合金") ||
				   lower.Contains("金属");
		}

		// ── 供寻路与动作系统消费的高速判定接口 ──

		public BlockCellType GetCellType(Vector2Int gp)
		{
			WorldGeneration w = WorldGeneration.world;
			if ((UnityEngine.Object)(object)w != (UnityEngine.Object)null)
			{
				return ClassifyBlock(w, gp, out _, out _);
			}
			if (scannedCells.TryGetValue(gp, out ScannedCell cell))
			{
				return cell.type;
			}
			return BlockCellType.Air;
		}

		public bool IsSolid(Vector2Int gp)
		{
			BlockCellType t = GetCellType(gp);
			return t == BlockCellType.FragileSolid || t == BlockCellType.HardSolid;
		}

		public bool IsPassable(Vector2Int gp)
		{
			return !IsSolid(gp);
		}

		public bool IsAir(Vector2Int gp)
		{
			return GetCellType(gp) == BlockCellType.Air;
		}

		public bool IsLiquid(Vector2Int gp)
		{
			return GetCellType(gp) == BlockCellType.Liquid;
		}

		public bool IsFragile(Vector2Int gp)
		{
			return GetCellType(gp) == BlockCellType.FragileSolid;
		}

		public bool IsHard(Vector2Int gp)
		{
			return GetCellType(gp) == BlockCellType.HardSolid;
		}

		/// <summary>
		/// 判定该格是否为可落脚行走的地面：
		/// 脚下 (gp.y - 1) 必须为实心方块，且上方必须拥有至少 2 格可通行的站立身位高度 (gp 与 gp.y + 1)。
		/// 支持非实心液体（浅水滩）正常行走，杜绝因积水误判为坚硬障碍导致路线中断。
		/// </summary>
		public bool IsWalkableGround(Vector2Int gp)
		{
			// 脚底 (y - 1) 必须是实心方块支撑
			if (!IsSolid(new Vector2Int(gp.x, gp.y - 1)))
			{
				return false;
			}
			// 角色身位高度：正常站立需要脚底上方至少 2 格连通非实心空间 (gp 与 gp.y + 1)
			if (!IsPassable(gp) || !IsPassable(new Vector2Int(gp.x, gp.y + 1)))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// 判定该格是否为低矮通道（高度仅 2 格空气，上方第 3 格为实心天花板阻挡，需自动蹲下通过）：
		/// 脚底必须为实心地面，上方有 2 格连通空气，但第 3 格 (gp.y + 2) 为实心天花板。
		/// 若上方第 3 格仍为空气，则属于开阔正常站立行走高度。
		/// </summary>
		public bool IsCrouchPassage(Vector2Int gp)
		{
			if (!IsWalkableGround(gp)) return false;
			// 检查头顶第 3 格是否为实心阻挡
			return IsSolid(new Vector2Int(gp.x, gp.y + 2));
		}
	}
}