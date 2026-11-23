using System;
using UnityEngine;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 动作系统指令状态
	/// </summary>
	public enum P2CommandState : byte
	{
		Recall, // 召回 / 跟随：寻路启动并使用最近路线跟随 P1
		Stop,   // 停下 / 待命：寻路停止，原地不动
		Play    // 玩耍 / 闲逛：保持待命与发呆
	}

	/// <summary>
	/// 动作神经元：以物理寻路线为绝对指令，驱动原版 Body 物理组件进行点到点执行。
	/// 核心解决：
	/// 1. 彻底杜绝平地乱跳：Walk 节点仅提供水平推进，由游戏底层原生的 TryStepUp() 自动吸附平滑上台阶；
	/// 2. 彻底杜绝左右抽搐：航点单向递增消费，路点投影死区锁定，换路时保持前向路点不回退；
	/// 3. 冲刺助跑起跳：JumpTakeoff 冲刺至起跳边缘起跳，并在空中持续保持水平冲量，跨越深坑与高台；
	/// 4. 垂直下坠模式：垂直高度 > 3 格时迈出悬崖并在空中向侧墙贴附滑落；
	/// 5. 低矮隧道自动蹲伏通行。
	/// </summary>
	public class P2MotorNeuron
	{
		private P2CommandState currentCommand = P2CommandState.Recall;
		private NavPath currentTrackedPath;
		private int currentWaypointIndex;
		private float nextJumpAt;
		private float currentFacingDir = 1f;

		public P2CommandState CurrentCommand => currentCommand;
		public string CommandName => currentCommand switch
		{
			P2CommandState.Recall => Loc.T("跟随 / 召回"),
			P2CommandState.Stop => Loc.T("停下 / 待命"),
			P2CommandState.Play => Loc.T("玩耍 / 发呆"),
			_ => "未知"
		};

		/// <summary>
		/// 接收并应用战术指令
		/// </summary>
		public void SetCommand(P2CommandState cmd, P2NavNeuron nav, Body partner, Body player, WorldGeneration world, P2PerceptionNeuron perception)
		{
			currentCommand = cmd;
			currentTrackedPath = null;
			currentWaypointIndex = 0;

			switch (cmd)
			{
				case P2CommandState.Stop:
					if (nav != null) nav.ClearPaths();
					if ((UnityEngine.Object)(object)partner != (UnityEngine.Object)null)
					{
						try
						{
							partner.moveDir = Vector2.zero;
							partner.crouching = false;
						}
						catch { }
					}
					CompanionHost.Instance?.AddLine(Loc.T("旁白"), Loc.T("（P2收到待命指令，在原地停下）"), player: false);
					break;

				case P2CommandState.Play:
					if (nav != null) nav.ClearPaths();
					if ((UnityEngine.Object)(object)partner != (UnityEngine.Object)null)
					{
						try
						{
							partner.moveDir = Vector2.zero;
							partner.crouching = false;
						}
						catch { }
					}
					CompanionHost.Instance?.AddLine(Loc.T("旁白"), Loc.T("（P2在附近放松下来，开始发呆玩耍）"), player: false);
					break;

				case P2CommandState.Recall:
					if (nav != null && (UnityEngine.Object)(object)partner != (UnityEngine.Object)null && (UnityEngine.Object)(object)player != (UnityEngine.Object)null)
					{
						Vector3 pPos = ((Component)player).transform.position;
						float pfY = (player.col != null) ? player.col.bounds.min.y : (pPos.y - 0.7f);
						Vector2 playerFeet = new Vector2(pPos.x, pfY + 0.05f);
						nav.PlanPath(partner, playerFeet, perception, world, force: true);
					}
					CompanionHost.Instance?.AddLine(Loc.T("旁白"), Loc.T("（P2收到召回指令，立刻规划路线赶向训练员身边）"), player: false);
					break;
			}
		}

		/// <summary>
		/// 动作主步进时钟：以寻路线为指令，完成点到点驱动
		/// </summary>
		public void Tick(Body partner, Body player, P2NavNeuron nav, WorldGeneration world, float dt)
		{
			if ((UnityEngine.Object)(object)partner == (UnityEngine.Object)null || !partner.alive)
			{
				return;
			}

			// 1. 停下与玩耍指令：原地不动
			if (currentCommand == P2CommandState.Stop || currentCommand == P2CommandState.Play)
			{
				try
				{
					partner.moveDir = Vector2.zero;
					partner.crouching = false;
				}
				catch { }
				return;
			}

			// 2. 目标玩家存在性检查
			if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null || !player.alive)
			{
				try
				{
					partner.moveDir = Vector2.zero;
					partner.crouching = false;
				}
				catch { }
				return;
			}

			Vector3 pPos = ((Component)player).transform.position;
			Vector3 partPos = ((Component)partner).transform.position;

			float feetY = (partner.col != null) ? partner.col.bounds.min.y : (partPos.y - 0.7f);
			Vector2 feetWorldPos = new Vector2(partPos.x, feetY + 0.05f);
			Vector2Int feetGp = world.WorldToBlockPos(feetWorldPos);

			float distToPlayer = Vector2.Distance(pPos, partPos);

			// 若已在训练员身旁 (<= 1.4m)，平稳待命，面朝训练员
			if (distToPlayer <= 1.4f)
			{
				try
				{
					partner.moveDir = Vector2.zero;
					partner.crouching = false;
				}
				catch { }
				AnchorFacing(partner, pPos.x - partPos.x);
				return;
			}

			NavPath path = nav?.PrimaryPath;
			if (path == null || path.waypoints.Count == 0)
			{
				try
				{
					partner.moveDir = Vector2.zero;
					partner.crouching = false;
				}
				catch { }
				AnchorFacing(partner, pPos.x - partPos.x);
				return;
			}

			// 3. 动态换路平滑过渡：若更换新路径，寻找当前位置前方第一个有效路点，严禁回退！
			if (path != currentTrackedPath)
			{
				currentTrackedPath = path;
				currentWaypointIndex = AlignToNewPath(path, partPos, currentFacingDir);
			}

			// 4. 消费已到达的路点，向前单向推进（杜绝回退死锁）
			while (currentWaypointIndex < path.waypoints.Count)
			{
				NavWaypoint wp = path.waypoints[currentWaypointIndex];
				float wpDx = wp.worldPos.x - partPos.x;
				float wpDy = wp.worldPos.y - feetY;
				float absWpDx = Mathf.Abs(wpDx);

				bool reached = false;
				if (wp.nodeType == NavNodeType.Walk)
				{
					// 水平走动与台阶：已接近路点，或沿行进方向已越过路点
					bool passedForward = (partPos.x - wp.worldPos.x) * currentFacingDir > 0.05f;
					if ((absWpDx < 0.38f || passedForward) && Mathf.Abs(wpDy) < 1.30f)
					{
						reached = true;
					}
				}
				else if (wp.nodeType == NavNodeType.JumpTakeoff)
				{
					// 助跑起跳点：逼近起跳点或已越过起跳边缘
					float fwdSign = (absWpDx > 0.05f) ? Mathf.Sign(wpDx) : currentFacingDir;
					if (absWpDx < 0.45f || (partPos.x - wp.worldPos.x) * fwdSign >= 0f)
					{
						if (partner.grounded && Time.unscaledTime >= nextJumpAt)
						{
							partner.Jump();
							nextJumpAt = Time.unscaledTime + 0.35f;
						}
						reached = true;
					}
				}
				else if (wp.nodeType == NavNodeType.Jump)
				{
					// 跳跃落地：横向接近或越过着陆点，且已落地，立即消费该路点，绝不在台阶上中途驻留
					bool horizontallyNear = absWpDx < 0.65f || (partPos.x - wp.worldPos.x) * currentFacingDir > 0.05f;
					if (partner.grounded && horizontallyNear)
					{
						reached = true;
					}
				}
				else if (wp.nodeType == NavNodeType.DescentBrink)
				{
					// 悬崖边缘前跨点：接近边缘、或脚下离开地面（已迈入空中）
					if (absWpDx < 0.35f || !partner.grounded || (partPos.x - wp.worldPos.x) * currentFacingDir > 0.05f)
					{
						reached = true;
					}
				}
				else if (wp.nodeType == NavNodeType.Descent)
				{
					// 下坠着落：落地且接近地面路点
					if (partner.grounded && absWpDx < 0.65f && feetY <= wp.worldPos.y + 0.6f)
					{
						reached = true;
					}
				}

				if (reached && currentWaypointIndex < path.waypoints.Count - 1)
				{
					currentWaypointIndex++;
				}
				else
				{
					break;
				}
			}

			if (currentWaypointIndex >= path.waypoints.Count)
			{
				try
				{
					partner.moveDir = Vector2.zero;
					partner.crouching = false;
				}
				catch { }
				AnchorFacing(partner, pPos.x - partPos.x);
				return;
			}

			NavWaypoint targetWp = path.waypoints[currentWaypointIndex];
			float dx = targetWp.worldPos.x - partPos.x;
			float dy = targetWp.worldPos.y - feetY;
			float absDx = Mathf.Abs(dx);

			// 5. 自动蹲下判定与执行
			P2PerceptionNeuron perception = P2BrainHub.Instance?.Perception;
			bool currentLowCeiling = perception != null && perception.IsCrouchPassage(feetGp);
			bool nextLowCeiling = targetWp.requiresCrouch;
			bool wantCrouch = currentLowCeiling || nextLowCeiling;
			try
			{
				partner.crouching = wantCrouch;
				if (wantCrouch && partner.crouchAmount < 0.85f)
				{
					partner.crouchAmount = 1.0f;
				}
			}
			catch { }

			// 6. 面朝方向迟滞死区（0.22m 范围内不翻转，消除原地抽动）
			if (absDx > 0.22f)
			{
				currentFacingDir = Mathf.Sign(dx);
			}
			AnchorFacing(partner, currentFacingDir);

			// 7. 节点类型底层物理驱动
			if (targetWp.nodeType == NavNodeType.Walk)
			{
				// 水平走动与 1 格台阶平滑推进：纯走平推，依靠游戏原生 TryStepUp() 自动跨上台阶，坚决不跳跃！
				float sign = (absDx > 0.06f) ? Mathf.Sign(dx) : currentFacingDir;
				float moveX = (absDx > 0.10f) ? sign * Mathf.Clamp(absDx * 2.2f, 0.45f, 1.0f) : 0f;
				try
				{
					partner.moveDir = new Vector2(moveX, 0f);
				}
				catch { }
			}
			else if (targetWp.nodeType == NavNodeType.JumpTakeoff)
			{
				// 助跑起跳点：全速冲刺，到达起跳线瞬间起跳
				float jumpDir = (absDx > 0.06f) ? Mathf.Sign(dx) : currentFacingDir;
				try
				{
					partner.moveDir = new Vector2(jumpDir * 1.0f, 0f);
					if (partner.grounded && Time.unscaledTime >= nextJumpAt)
					{
						partner.Jump();
						nextJumpAt = Time.unscaledTime + 0.35f;
						currentWaypointIndex++;
					}
				}
				catch { }
			}
			else if (targetWp.nodeType == NavNodeType.Jump)
			{
				// 动能跳跃空中推进：在空中持续保持全速向前冲刺推力，一次性跃上目标高台，坚决不中途碎跳
				float jumpDir = (absDx > 0.06f) ? Mathf.Sign(dx) : currentFacingDir;
				try
				{
					partner.moveDir = new Vector2(jumpDir * 1.0f, 0f);
				}
				catch { }
			}
			else if (targetWp.nodeType == NavNodeType.DescentBrink)
			{
				// 悬崖边缘前迈点：果断向前迈步跨落，绝不在边缘犹豫徘徊
				float sign = (absDx > 0.05f) ? Mathf.Sign(dx) : currentFacingDir;
				try
				{
					partner.moveDir = new Vector2(sign * 0.85f, 0f);
				}
				catch { }
			}
			else if (targetWp.nodeType == NavNodeType.Descent)
			{
				// 下坠模式 (> 3格)
				try
				{
					float vy = (partner.rb != null) ? partner.rb.velocity.y : 0f;
					bool isFalling = !partner.grounded && (vy < -0.2f);

					if (!isFalling)
					{
						// 尚未迈入空中：向前跨下
						float fwd = (absDx > 0.06f) ? Mathf.Sign(dx) : currentFacingDir;
						partner.moveDir = new Vector2(fwd * 0.85f, 0f);
					}
					else
					{
						// 空中垂直下落：向侧面墙面持续微施力贴附滑落
						if (targetWp.wallDirection != 0f)
						{
							partner.moveDir = new Vector2(targetWp.wallDirection * 0.90f, 0f);
						}
						else
						{
							float fwd = (absDx > 0.06f) ? Mathf.Sign(dx) : currentFacingDir;
							partner.moveDir = new Vector2(fwd * 0.35f, 0f);
						}
					}
				}
				catch { }
			}
		}

		private static void AnchorFacing(Body b, float dirX)
		{
			if ((UnityEngine.Object)(object)b == (UnityEngine.Object)null || Mathf.Abs(dirX) < 0.05f) return;
			try
			{
				Vector3 pos = ((Component)b).transform.position;
				float sign = Mathf.Sign(dirX);
				b.targetLookPos = new Vector3(pos.x + sign * 2.8f, pos.y + 0.1f, 0f);

				bool wantRight = dirX > 0f;
				if (b.standing && b.isRight != wantRight)
				{
					b.SwitchDir();
				}
			}
			catch { }
		}

		/// <summary>
		/// 新路径对齐：寻找新路径上离当前角色最近且位于前方的路点，严禁粗暴回退
		/// </summary>
		private static int AlignToNewPath(NavPath path, Vector3 partPos, float facingDir)
		{
			if (path == null || path.waypoints.Count <= 1) return 0;
			int bestIdx = 0;
			float minAheadDist = float.MaxValue;

			for (int i = 0; i < path.waypoints.Count; i++)
			{
				NavWaypoint wp = path.waypoints[i];
				float dx = (wp.worldPos.x - partPos.x) * facingDir;
				float dist = Vector2.Distance(wp.worldPos, partPos);

				// 优先选择前方路点（dx > -0.05m）
				if (dx > -0.05f && dist < minAheadDist)
				{
					minAheadDist = dist;
					bestIdx = i;
				}
			}

			// 若角色已几乎到达所选路点 (< 0.25m)，直接进入下一个路点
			if (bestIdx < path.waypoints.Count - 1 && Vector2.Distance(path.waypoints[bestIdx].worldPos, partPos) < 0.25f)
			{
				bestIdx++;
			}

			return bestIdx;
		}
	}
}
