using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 单个战术指令槽位视图控制器：
	/// 完全脱胎于原版物品栏圆环槽位资产，保留原版 Sprite、材质、动画与光晕，
	/// 彻底斩断原版物品栏逻辑，仅响应左键单击触发战术指令。
	/// </summary>
	public class P2CommandSlotView : MonoBehaviour
	{
		public int SlotIndex { get; private set; }
		public bool HasCommand { get; private set; }
		public P2CommandState Command { get; private set; }
		public string Label { get; private set; }
		public string Description { get; private set; }

		private RectTransform rectTransform;
		private Image bgImage;
		private Image iconImage;
		private Sprite normalSprite;
		private Sprite mutedSprite;
		private TextMeshProUGUI labelTmp;
		private bool isHovered;

		public void Setup(int slotIdx, Image bg, Image icon, Sprite normSp, Sprite muteSp, TextMeshProUGUI fontSource)
		{
			SlotIndex = slotIdx;
			bgImage = bg;
			iconImage = icon;
			normalSprite = normSp;
			mutedSprite = muteSp;
			rectTransform = GetComponent<RectTransform>();

			// 隐藏原版物品耐久条/锁扣/水填充等子对象
			foreach (Transform child in transform)
			{
				string cName = child.name.ToLowerInvariant();
				if (cName.Contains("cond") || cName.Contains("lock") || cName.Contains("water") || cName.Contains("fill"))
				{
					child.gameObject.SetActive(false);
				}
			}

			// 配置对应槽位的指令属性
			switch (slotIdx)
			{
				case 0:
					HasCommand = true;
					Command = P2CommandState.Recall;
					Label = Loc.T("召回");
					Description = Loc.T("【召回 / 跟随】\n命令 P2 立即规划最近安全路线赶往训练员身边并保持跟随");
					break;
				case 2:
					HasCommand = true;
					Command = P2CommandState.Stop;
					Label = Loc.T("停下");
					Description = Loc.T("【停下 / 待命】\n命令 P2 清空行动路线，在当前位置原地驻留待命");
					break;
				case 4:
					HasCommand = true;
					Command = P2CommandState.Play;
					Label = Loc.T("玩耍");
					Description = Loc.T("【玩耍 / 发呆】\n命令 P2 放松警戒，保持当前待命状态自由发呆闲逛");
					break;
				default:
					HasCommand = false;
					Label = "";
					Description = Loc.T("（空置战术槽位）");
					break;
			}

			// 如果该槽位有指令，隐藏原版物品图标并植入文本标签
			if (iconImage != null)
			{
				iconImage.gameObject.SetActive(false);
			}

			// 植入基于原版 TextMeshPro 样式的清晰文字标识
			GameObject labelGo = new GameObject("CommandLabel");
			labelGo.transform.SetParent(transform, false);
			RectTransform lblRt = labelGo.AddComponent<RectTransform>();
			lblRt.anchorMin = Vector2.zero;
			lblRt.anchorMax = Vector2.one;
			lblRt.sizeDelta = Vector2.zero;

			labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
			if (fontSource != null)
			{
				labelTmp.font = fontSource.font;
				labelTmp.fontSharedMaterial = fontSource.fontSharedMaterial;
			}
			labelTmp.fontSize = 17f;
			labelTmp.fontStyle = FontStyles.Bold;
			labelTmp.alignment = TextAlignmentOptions.Center;
			labelTmp.raycastTarget = false;

			RefreshVisuals();
		}

		public void RefreshVisuals()
		{
			if (labelTmp != null)
			{
				if (HasCommand)
				{
					P2BrainHub brain = P2BrainHub.Instance;
					bool isActive = brain != null && brain.Motor.CurrentCommand == Command;
					if (isActive)
					{
						labelTmp.text = $"<color=#55FF77>● {Label}</color>";
					}
					else
					{
						labelTmp.text = $"<color=#FFFFFF>{Label}</color>";
					}
				}
				else
				{
					labelTmp.text = "<color=#556677>--</color>";
				}
			}

			if (bgImage != null)
			{
				if (HasCommand)
				{
					if (normalSprite != null) bgImage.sprite = normalSprite;
					bgImage.color = isHovered ? new Color(1f, 1f, 1f, 1f) : new Color(0.85f, 0.9f, 0.95f, 0.85f);
				}
				else
				{
					if (mutedSprite != null) bgImage.sprite = mutedSprite;
					bgImage.color = new Color(0.6f, 0.65f, 0.7f, 0.35f);
				}
			}
		}

		public bool CheckHover(Vector2 mousePos)
		{
			if (rectTransform == null) return false;
			Vector2 slotCenter = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
			float radius = 38f * (Screen.height / 1080f);
			if (radius < 26f) radius = 26f;
			return Vector2.Distance(mousePos, slotCenter) <= radius;
		}

		public void SetHovered(bool hovered)
		{
			if (isHovered == hovered) return;
			isHovered = hovered;
			RefreshVisuals();
		}

		private void Update()
		{
			if (rectTransform != null)
			{
				float targetScale = isHovered && HasCommand ? 1.14f : 1.0f;
				rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 18f);
			}
		}
	}

	/// <summary>
	/// 战术指令圆环菜单管理器：
	/// 彻底复制游戏本身的物品栏资产 (PlayerCamera.main.radialMenu) 并在其基础上重新编译为 mod 本身的独立资产，
	/// 保证 100% 贴合原版美术、字体、边框、光环与动效，独立且绝不影响原版物品栏。
	/// 通过键盘 4 键呼出，ESC 退出，内部按钮全部支持鼠标左键单击交互。
	/// </summary>
	public class P2CommandRadialMenu : MonoBehaviour
	{
		public static P2CommandRadialMenu Instance { get; private set; }

		private GameObject clonedRadialObj;
		private TextMeshProUGUI centerTextTmp;
		private Image centerCircleImg;
		private readonly List<P2CommandSlotView> slots = new List<P2CommandSlotView>();

		private bool isOpen;
		private float targetScale;
		private float currentScale;
		private P2CommandSlotView currentlyHoveredSlot;

		public bool IsOpen => isOpen;

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this) Instance = null;
			if (clonedRadialObj != null)
			{
				Destroy(clonedRadialObj);
				clonedRadialObj = null;
			}
		}

		private void Update()
		{
			PlayerCamera pc = PlayerCamera.main;
			if ((UnityEngine.Object)(object)pc == (UnityEngine.Object)null || (UnityEngine.Object)(object)pc.radialMenu == (UnityEngine.Object)null)
			{
				return;
			}

			// 1. 若尚未克隆原版物品栏圆环资产，执行独立克隆与资产重编译
			if (clonedRadialObj == null)
			{
				TryCloneNativeRadial(pc);
				if (clonedRadialObj == null) return;
			}

			// 2. 键盘 4 键触发与 ESC 退出响应
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				ToggleMenu();
			}
			else if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || (DualMode.Instance != null && !DualMode.Instance.Active)))
			{
				CloseMenu();
			}

			// 3. 仿原版光滑展开/收起缩放插值动画
			if (clonedRadialObj != null)
			{
				currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * 15f);
				clonedRadialObj.transform.localScale = Vector3.one * currentScale;

				if (!isOpen && currentScale < 0.03f && clonedRadialObj.activeSelf)
				{
					clonedRadialObj.SetActive(false);
				}
			}

			if (!isOpen || !clonedRadialObj.activeSelf)
			{
				return;
			}

			// 保持菜单呼出时鼠标光标可用
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;

			// 4. 槽位鼠标悬停检测与提示文案刷新
			Vector2 mousePos = Input.mousePosition;
			P2CommandSlotView hovered = null;
			for (int i = 0; i < slots.Count; i++)
			{
				if (slots[i].CheckHover(mousePos))
				{
					hovered = slots[i];
					break;
				}
			}

			if (hovered != currentlyHoveredSlot)
			{
				currentlyHoveredSlot = hovered;
				for (int i = 0; i < slots.Count; i++)
				{
					slots[i].SetHovered(slots[i] == currentlyHoveredSlot);
				}
			}

			// 5. 刷新圆环中心文字
			if (centerTextTmp != null)
			{
				if (currentlyHoveredSlot != null && !string.IsNullOrEmpty(currentlyHoveredSlot.Description))
				{
					centerTextTmp.text = currentlyHoveredSlot.Description;
				}
				else
				{
					P2BrainHub brain = P2BrainHub.Instance;
					string curCmdStr = brain?.Motor != null ? brain.Motor.CommandName : Loc.T("未知");
					centerTextTmp.text = $"<size=20><b>{Loc.T("P2 战术指令")}</b></size>\n<color=#66FFAA>{Loc.T("当前：")}{curCmdStr}</color>\n<size=13><color=#99AABB>{Loc.T("[4] 开启/收起  [ESC] 退出")}</color></size>";
				}
			}

			// 6. 响应左键单击交互：“内部按钮全部是左键单击就就行”
			if (Input.GetMouseButtonDown(0))
			{
				if (currentlyHoveredSlot != null && currentlyHoveredSlot.HasCommand)
				{
					ExecuteSlotCommand(currentlyHoveredSlot.Command);
					PlayNativeUiSound();
					CloseMenu();
				}
			}
		}

		public void ToggleMenu()
		{
			if (isOpen)
			{
				CloseMenu();
			}
			else
			{
				OpenMenu();
			}
		}

		public void OpenMenu()
		{
			if (clonedRadialObj == null) return;
			isOpen = true;
			targetScale = 1.0f;
			clonedRadialObj.SetActive(true);

			for (int i = 0; i < slots.Count; i++)
			{
				slots[i].RefreshVisuals();
			}
		}

		public void CloseMenu()
		{
			isOpen = false;
			targetScale = 0f;
			if (currentlyHoveredSlot != null)
			{
				currentlyHoveredSlot.SetHovered(false);
				currentlyHoveredSlot = null;
			}
		}

		private void ExecuteSlotCommand(P2CommandState cmd)
		{
			P2BrainHub brain = P2BrainHub.Instance;
			DualMode dm = DualMode.Instance;
			WorldGeneration world = WorldGeneration.world;
			if (brain == null || dm == null || world == null) return;

			brain.Motor.SetCommand(cmd, brain.Nav, dm.Partner, dm.Player, world, brain.Perception);
		}

		private void PlayNativeUiSound()
		{
			try
			{
				PlayerCamera pc = PlayerCamera.main;
				if ((UnityEngine.Object)(object)pc != (UnityEngine.Object)null && pc.uiSounds != null && pc.uiSounds.Length > 0 && pc.uiSounds[0] != null)
				{
					AudioSource.PlayClipAtPoint(pc.uiSounds[0], Camera.main ? Camera.main.transform.position : Vector3.zero);
				}
			}
			catch { }
		}

		/// <summary>
		/// 从游戏原生物品栏直接克隆 UI 架构，剥离物品逻辑，注入战术指令
		/// </summary>
		private void TryCloneNativeRadial(PlayerCamera pc)
		{
			try
			{
				GameObject original = pc.radialMenu.gameObject;
				clonedRadialObj = Instantiate(original);
				clonedRadialObj.name = "CU_P2CommandRadialMenu";

				// 挂载至相同父 Canvas 下，继承同等分辨率适配与坐标系
				clonedRadialObj.transform.SetParent(original.transform.parent, false);
				RectTransform rt = clonedRadialObj.GetComponent<RectTransform>();
				if (rt != null)
				{
					rt.anchorMin = new Vector2(0.5f, 0.5f);
					rt.anchorMax = new Vector2(0.5f, 0.5f);
					rt.pivot = new Vector2(0.5f, 0.5f);
					rt.anchoredPosition = Vector2.zero;
				}
				clonedRadialObj.transform.localScale = Vector3.zero;
				clonedRadialObj.SetActive(false);

				// 摘除 RadialCenter 标签，彻底杜绝游戏原生背包拖拽交互混淆
				foreach (Transform t in clonedRadialObj.GetComponentsInChildren<Transform>(true))
				{
					if (t.CompareTag("RadialCenter"))
					{
						t.tag = "Untagged";
					}
				}

				// 提取中央文字与背景圆环
				centerTextTmp = clonedRadialObj.GetComponentInChildren<TextMeshProUGUI>();
				centerCircleImg = pc.radialCircle != null ? clonedRadialObj.GetComponentInChildren<Image>() : null;

				// 彻底剔除所有原生 InvButton，并编译为战术槽位
				slots.Clear();
				InvButton[] nativeButtons = clonedRadialObj.GetComponentsInChildren<InvButton>(true);
				foreach (InvButton inv in nativeButtons)
				{
					int slotIdx = inv.slot;
					GameObject go = inv.gameObject;
					Image bg = go.GetComponent<Image>();
					Image icon = inv.itemImg;
					Sprite norm = inv.normalSprite;
					Sprite mute = inv.mutedSprite;

					// 销毁原生脚本
					Destroy(inv);

					// 挂载全新独立槽位控制器
					P2CommandSlotView view = go.AddComponent<P2CommandSlotView>();
					view.Setup(slotIdx, bg, icon, norm, mute, centerTextTmp);
					slots.Add(view);
				}

				// 按槽位索引排序 (0 ~ 7)
				slots.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

				CompanionPlugin.Log?.LogInfo($"[P2CommandRadialMenu] Cloned native radial UI successfully with {slots.Count} command slots.");
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log?.LogError($"[P2CommandRadialMenu] Failed to clone native radial: {ex.Message}");
			}
		}
	}
}