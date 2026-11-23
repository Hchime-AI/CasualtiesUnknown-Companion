using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class NativeHit : MonoBehaviour
{
	private Action action;

	private int lastFrame = -1;

	public void Init(Action a)
	{
		action = a;
	}

	private void Update()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (action == null || !Input.GetMouseButtonDown(0))
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
		if (((Rect)rect).Contains(val2) && Time.frameCount != lastFrame)
		{
			lastFrame = Time.frameCount;
			try
			{
				PlayerCamera.PlayUISound("click", 1f);
			}
			catch
			{
			}
			CompanionPlugin.Log.LogInfo((object)("[ui] 点击 " + ((Object)((Component)this).gameObject).name));
			action();
		}
	}
}
