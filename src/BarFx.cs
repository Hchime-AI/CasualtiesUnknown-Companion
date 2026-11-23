using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace CUCompanion;

public class BarFx : MonoBehaviour
{
	private Image face;

	private Image[] borders;

	private RectTransform rt;

	private bool hover;

	private bool down;

	private void Awake()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		face = ((Component)this).GetComponent<Image>();
		ref RectTransform reference = ref rt;
		Transform transform = ((Component)this).transform;
		reference = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		List<Image> list = new List<Image>();
		foreach (Transform item in ((Component)this).transform)
		{
			Image component = ((Component)item).GetComponent<Image>();
			if ((Object)(object)component != (Object)null && (Object)(object)component != (Object)(object)face)
			{
				list.Add(component);
			}
		}
		borders = list.ToArray();
	}

	private void Update()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rt == (Object)null))
		{
			Canvas componentInParent = ((Component)this).GetComponentInParent<Canvas>();
			Camera val = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
			Vector2 val2 = (Vector2)(Input.mousePosition);
			bool flag = RectTransformUtility.RectangleContainsScreenPoint(rt, val2, val);
			if (flag != hover)
			{
				hover = flag;
				Apply();
			}
			if (hover && Input.GetMouseButtonDown(0))
			{
				down = true;
				Apply();
			}
			else if (down && Input.GetMouseButtonUp(0))
			{
				down = false;
				Apply();
			}
		}
	}

	private void Apply()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		float num = (down ? 0.34f : (hover ? 0.17f : 0.06f));
		if ((Object)(object)face != (Object)null)
		{
			((Graphic)face).color = new Color(1f, 1f, 1f, num);
		}
		float num2 = (down ? 1f : (hover ? 0.95f : 0.72f));
		if (borders != null)
		{
			for (int i = 0; i < borders.Length; i++)
			{
				((Graphic)borders[i]).color = new Color(1f, 1f, 1f, num2);
			}
		}
	}
}
