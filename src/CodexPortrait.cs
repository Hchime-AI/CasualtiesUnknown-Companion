using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

/// <summary>
/// 图鉴右侧的角色快照。
/// 不 Instantiate 角色预制体（主菜单里没有 WorldGeneration，Body.Awake 会炸），
/// 而是读取预制体里每个 SpriteRenderer 的贴图 / 摆位 / 旋转 / 缩放，直接画到 IMGUI 上——
/// 得到的是这只角色模型本身的站姿，不经过任何运行时系统。
/// </summary>
internal static class CodexPortrait
{
	private sealed class Part
	{
		internal Sprite sprite;

		internal Color color;

		internal bool flipX;

		internal Rect rect;

		internal Vector2 pivot;

		internal float rotation;

		internal int order;
	}

	private static readonly Dictionary<string, List<Part>> cache = new Dictionary<string, List<Part>>();

	private static readonly HashSet<string> missing = new HashSet<string>();

	private static GUIStyle captionStyleCache;

	private static List<Part> Load(string prefabPath)
	{
		if (cache.TryGetValue(prefabPath, out var value))
		{
			return value;
		}
		if (missing.Contains(prefabPath))
		{
			return null;
		}
		try
		{
			GameObject val = Resources.Load<GameObject>(prefabPath);
			if ((Object)(object)val == (Object)null)
			{
				missing.Add(prefabPath);
				return null;
			}
			SpriteRenderer[] array = val.GetComponentsInChildren<SpriteRenderer>(true);
			Vector3 position = val.transform.position;
			List<Part> list = new List<Part>();
			if (array != null)
			{
				foreach (SpriteRenderer val2 in array)
				{
					if ((Object)(object)val2 == (Object)null || (Object)(object)val2.sprite == (Object)null)
					{
						continue;
					}
					Sprite sprite = val2.sprite;
					Transform transform = ((Component)val2).transform;
					string text = (((Object)val2).name + "|" + (((Object)(object)transform.parent != (Object)null) ? ((Object)transform.parent).name : "")).ToLowerInvariant();
					if (text.Contains("anim") || text.Contains("particle") || text.Contains("bone"))
					{
						continue;
					}
					Vector3 position2 = transform.position - position;
					Vector3 lossyScale = transform.lossyScale;
					float num = Mathf.Max(0.001f, Mathf.Abs(lossyScale.x));
					float num2 = Mathf.Max(0.001f, Mathf.Abs(lossyScale.y));
					Bounds bounds = sprite.bounds;
					Vector3 center = bounds.center;
					Vector3 size = bounds.size;
					float num3 = position2.x + center.x * num;
					float num4 = position2.y + center.y * num2;
					float num5 = size.x * num;
					float num6 = size.y * num2;
					if (num5 < 0.002f || num6 < 0.002f || val2.color.a < 0.02f)
					{
						continue;
					}
					Part part = new Part();
					part.sprite = sprite;
					part.color = val2.color;
					part.flipX = val2.flipX;
					part.rect = new Rect(num3 - num5 * 0.5f, num4 - num6 * 0.5f, num5, num6);
					part.pivot = new Vector2(position2.x, position2.y);
					part.rotation = transform.eulerAngles.z;
					part.order = val2.sortingOrder;
					list.Add(part);
				}
			}
			if (list.Count == 0)
			{
				missing.Add(prefabPath);
				cache[prefabPath] = null;
				return null;
			}
			list.Sort(delegate (Part a, Part b)
			{
				if (a.order != b.order)
				{
					return a.order.CompareTo(b.order);
				}
				return b.rect.y.CompareTo(a.rect.y);
			});
			cache[prefabPath] = list;
			return list;
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log?.LogWarning((object)("[Codex] 角色快照加载失败（" + prefabPath + "）：" + ex.Message));
			missing.Add(prefabPath);
			return null;
		}
	}

	internal static void Draw(Rect area, string prefabPath, string caption)
	{
		Color color = GUI.color;
		Color backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = Color.white;
		GUI.color = Color.white;
		DrawFrame(area);
		Rect rect = new Rect(area.x + 10f, area.y + 10f, area.width - 20f, area.height - 44f);
		List<Part> list = Load(prefabPath);
		if (list != null && list.Count > 0)
		{
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			float num4 = float.MinValue;
			foreach (Part item in list)
			{
				Rect boundsOf = RotatedBounds(item);
				num = Mathf.Min(num, boundsOf.xMin);
				num2 = Mathf.Min(num2, boundsOf.yMin);
				num3 = Mathf.Max(num3, boundsOf.xMax);
				num4 = Mathf.Max(num4, boundsOf.yMax);
			}
			float num5 = Mathf.Max(0.001f, num3 - num);
			float num6 = Mathf.Max(0.001f, num4 - num2);
			float num7 = Mathf.Min(rect.width / num5, rect.height / num6) * 0.92f;
			float num8 = rect.x + (rect.width - num5 * num7) * 0.5f - num * num7;
			float num9 = rect.y + (rect.height - num6 * num7) * 0.5f + num4 * num7;
			Matrix4x4 matrix = GUI.matrix;
			foreach (Part item2 in list)
			{
				Texture texture = item2.sprite.texture;
				if ((Object)(object)texture == (Object)null)
				{
					continue;
				}
				Rect rect2 = ToScreen(item2.rect, num8, num9, num7);
				Rect textureRect = item2.sprite.textureRect;
				float num10 = (float)texture.width;
				float num11 = (float)texture.height;
				Rect texCoords = new Rect(textureRect.x / num10, textureRect.y / num11, textureRect.width / num10, textureRect.height / num11);
				if (item2.flipX)
				{
					texCoords = new Rect(texCoords.x + texCoords.width, texCoords.y, 0f - texCoords.width, texCoords.height);
				}
				if (Mathf.Abs(item2.rotation) > 0.01f)
				{
					Vector2 pivotScreen = new Vector2(num8 + item2.pivot.x * num7, num9 - item2.pivot.y * num7);
					GUIUtility.RotateAroundPivot(item2.rotation, pivotScreen);
				}
				GUI.color = item2.color;
				GUI.DrawTextureWithTexCoords(rect2, texture, texCoords, true);
				GUI.matrix = matrix;
			}
			GUI.color = Color.white;
		}
		else
		{
			GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.5f - 30f, rect.width, 28f), Loc.T("（模型快照暂不可用，进入一局后重开本页会更稳）"), Caption());
		}
		GUI.Label(new Rect(area.x + 12f, area.yMax - 34f, area.width - 24f, 26f), caption, Caption());
		GUI.color = color;
		GUI.backgroundColor = backgroundColor;
	}

	private static Rect ToScreen(Rect world, float originX, float originY, float scale)
	{
		float num = originX + world.xMin * scale;
		float num2 = originY - world.yMax * scale;
		return new Rect(num, num2, world.width * scale, world.height * scale);
	}

	private static Rect RotatedBounds(Part part)
	{
		if (Mathf.Abs(part.rotation) < 0.01f)
		{
			return part.rect;
		}
		float num = part.rotation * ((float)Math.PI / 180f);
		float num2 = Mathf.Cos(num);
		float num3 = Mathf.Sin(num);
		Vector2 vector = part.rect.center - part.pivot;
		Vector2 vector2 = new Vector2(vector.x * num2 - vector.y * num3, vector.x * num3 + vector.y * num2) + part.pivot;
		float num4 = Mathf.Abs(part.rect.width * num2) + Mathf.Abs(part.rect.height * num3);
		float num5 = Mathf.Abs(part.rect.width * num3) + Mathf.Abs(part.rect.height * num2);
		return new Rect(vector2.x - num4 * 0.5f, vector2.y - num5 * 0.5f, num4, num5);
	}

	private static GUIStyle Caption()
	{
		if (captionStyleCache == null)
		{
			captionStyleCache = new GUIStyle(GUI.skin.label);
			captionStyleCache.alignment = TextAnchor.MiddleCenter;
			captionStyleCache.wordWrap = true;
			captionStyleCache.clipping = TextClipping.Overflow;
		}
		return captionStyleCache;
	}

	private static void DrawFrame(Rect area)
	{
		Color color = GUI.color;
		GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.92f);
		GUI.DrawTexture(area, (Texture)Texture2D.whiteTexture);
		GUI.color = new Color(0.24f, 0.44f, 0.5f, 0.85f);
		GUI.DrawTexture(new Rect(area.x, area.y, area.width, 1f), (Texture)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(area.x, area.yMax - 1f, area.width, 1f), (Texture)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(area.x, area.y, 1f, area.height), (Texture)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(area.xMax - 1f, area.y, 1f, area.height), (Texture)Texture2D.whiteTexture);
		GUI.color = color;
	}
}
