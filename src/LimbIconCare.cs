using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CUCompanion
{
	/// <summary>
	/// 医疗面板的「残留图标」处理。
	///
	/// 游戏里 WoundView.AddImageToLimb() 会把止血带、夹板、注射创口、脚底玻璃、自残创口
	/// 这类贴图直接挂到共享的 limbImages[i] 上，靠 OnLimbWound.cond 决定什么时候消失，
	/// 而 cond 绑的是「当时那具躯体」的 Limb。模组把 WoundView.body 借给同伴看伤势时，
	/// 这些图标挂在同一套 limbImages 上，切回来就留在玩家面板上：只有图标、没有任何实际效果。
	///
	/// 做法：给每枚图标记住归属 Body，切换时只开关它自己的 Image.enabled。
	/// 节点不搬家、不改父级，所以 OnLimbWound.Update 一直在跑、cond 到点自然会把它删掉；
	/// 隐藏期间也不需要模组代跑条件。
	/// </summary>
	internal static class LimbIconCare
	{
		private static readonly string[] KnownIconNames = new string[6]
		{
			"tourniquetIcon",
			"splintIcon",
			"injectionWound",
			"footglasshealthpanel",
			"selfHarmWound",
			"suicideWound"
		};

		private sealed class Entry
		{
			public GameObject go;

			public Body owner;

			public int limbIndex;

			public Image image;

			public bool visible;
		}

		private static readonly Dictionary<GameObject, Entry> icons = new Dictionary<GameObject, Entry>();

		private static readonly List<GameObject> scratch = new List<GameObject>();

		private static bool IsIconCandidate(GameObject go)
		{
			if ((Object)(object)go == (Object)null)
			{
				return false;
			}
			if ((Object)(object)go.GetComponent<OnLimbWound>() != (Object)null)
			{
				return true;
			}
			for (int i = 0; i < KnownIconNames.Length; i++)
			{
				if (go.name == KnownIconNames[i])
				{
					return true;
				}
			}
			return false;
		}

		public static void Clear()
		{
			icons.Clear();
			scratch.Clear();
		}

		private static void Sweep()
		{
			if (icons.Count == 0)
			{
				return;
			}
			scratch.Clear();
			foreach (KeyValuePair<GameObject, Entry> icon in icons)
			{
				if ((Object)(object)icon.Value.go == (Object)null || (Object)(object)icon.Value.owner == (Object)null)
				{
					scratch.Add(icon.Key);
				}
			}
			for (int i = 0; i < scratch.Count; i++)
			{
				icons.Remove(scratch[i]);
			}
			scratch.Clear();
		}

		/// <summary>把面板上还没登记归属的图标，记到 displayed 这具躯体名下。</summary>
		public static void Reconcile(WoundView view, Body displayed)
		{
			if ((Object)(object)view == (Object)null || view.limbImages == null)
			{
				return;
			}
			Sweep();
			for (int i = 0; i < view.limbImages.Length; i++)
			{
				Image slot = view.limbImages[i];
				if ((Object)(object)slot == (Object)null)
				{
					continue;
				}
				Transform parent = ((Component)slot).transform;
				for (int j = parent.childCount - 1; j >= 0; j--)
				{
					GameObject child = ((Component)parent.GetChild(j)).gameObject;
					if (icons.ContainsKey(child) || !IsIconCandidate(child))
					{
						continue;
					}
					icons[child] = new Entry
					{
						go = child,
						owner = displayed,
						limbIndex = i,
						image = child.GetComponent<Image>(),
						visible = true
					};
				}
			}
		}

		/// <summary>只让属于 visible 这具躯体的图标可见。</summary>
		public static void Apply(WoundView view, Body visible)
		{
			if ((Object)(object)view == (Object)null || icons.Count == 0)
			{
				return;
			}
			Sweep();
			foreach (KeyValuePair<GameObject, Entry> icon in icons)
			{
				Entry value = icon.Value;
				if ((Object)(object)value.go == (Object)null)
				{
					continue;
				}
				bool show = value.owner == visible;
				if ((Object)(object)value.image != (Object)null)
				{
					if (value.image.enabled != show)
					{
						value.image.enabled = show;
					}
				}
				else if (value.go.activeSelf != show)
				{
					value.go.SetActive(show);
				}
				value.visible = show;
			}
		}
	}
}
