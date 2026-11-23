using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace CUCompanion;

public class NativeBarButton : MonoBehaviour
{
	private Image face;

	private TextMeshProUGUI text;

	private RectTransform rect;

	private RectTransform reference;

	private Color baseColor = Color.white;

	public void Bind(Image faceImage, RectTransform referenceCell, TextMeshProUGUI tx)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		face = faceImage;
		text = tx;
		ref RectTransform reference = ref rect;
		Transform transform = ((Component)this).transform;
		reference = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		this.reference = referenceCell;
		if ((Object)(object)face != (Object)null)
		{
			baseColor = ((Graphic)face).color;
		}
	}

	private void Update()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)rect == (Object)null)
		{
			ref RectTransform reference = ref rect;
			Transform transform = ((Component)this).transform;
			reference = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		}
		if ((Object)(object)rect == (Object)null)
		{
			return;
		}
		float num = 0f;
		try
		{
			Vector2 val = default(Vector2);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, (Vector2)(Input.mousePosition), (Camera)null, out val);
			Rect val2 = rect.rect;
			if (!((Rect)val2).Contains(val))
			{
				float num2 = Mathf.Clamp(val.x, ((Rect)val2).xMin, ((Rect)val2).xMax);
				float num3 = Mathf.Clamp(val.y, ((Rect)val2).yMin, ((Rect)val2).yMax);
				num = Vector2.Distance(new Vector2(num2, num3), val);
			}
		}
		catch
		{
		}
		if ((Object)(object)face != (Object)null)
		{
			float num4 = ((num <= 0f) ? Mathf.Min(baseColor.a + 0.22f, 0.6f) : baseColor.a);
			((Graphic)face).color = Color.Lerp(((Graphic)face).color, new Color(baseColor.r, baseColor.g, baseColor.b, num4), Time.unscaledDeltaTime * 6f);
		}
		if ((Object)(object)text != (Object)null && (Object)(object)this.reference != (Object)null)
		{
			TextMeshProUGUI componentInChildren = ((Component)this.reference).GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				Color color = ((Graphic)componentInChildren).color;
				color.a = Mathf.Max(color.a, 0.85f);
				((Graphic)text).color = Color.Lerp(((Graphic)text).color, color, Time.unscaledDeltaTime * 6f);
			}
		}
	}
}
