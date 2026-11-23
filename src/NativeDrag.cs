using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class NativeDrag : MonoBehaviour
{
	private float w;

	private float min;

	private float max;

	private Func<float> get;

	private Action<float> set;

	public void Init(float width, float lo, float hi, Func<float> g, Action<float> s)
	{
		w = width;
		min = lo;
		max = hi;
		get = g;
		set = s;
	}

	private void Update()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if (get == null || !Input.GetMouseButton(0))
		{
			return;
		}
		Transform transform = ((Component)this).transform;
		RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		Vector2 val2 = default(Vector2);
		if ((Object)(object)val == (Object)null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(val, (Vector2)(Input.mousePosition), (Camera)null, out val2))
		{
			return;
		}
		Rect rect = val.rect;
		if (((Rect)rect).Contains(val2) || Input.GetMouseButtonDown(0))
		{
			float num = Mathf.Clamp01((val2.x - ((Rect)rect).xMin) / Mathf.Max(((Rect)rect).width, 1f));
			set(Mathf.Lerp(min, max, num));
			Transform val3 = ((Component)this).transform.Find("knob");
			if ((Object)(object)val3 != (Object)null)
			{
				val3.localPosition = new Vector3(((Rect)rect).width * num - 6f, 0f, 0f);
			}
		}
	}
}
