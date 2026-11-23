using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public static class IntimateBoost
{
	public static bool On
	{
		get
		{
			IntimateController instance = IntimateController.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				return instance.BrainOff;
			}
			return false;
		}
	}

	public static bool Applies(Body b)
	{
		if ((Object)(object)b == (Object)null || !On)
		{
			return false;
		}
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance != (Object)null && instance.HasBody)
		{
			return instance.CurrentBody == b;
		}
		return false;
	}
}
