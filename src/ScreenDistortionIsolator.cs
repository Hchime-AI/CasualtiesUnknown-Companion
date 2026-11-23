using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[HarmonyPatch(typeof(PlayerCamera), "HandleScreenShaders")]
public static class ScreenDistortionIsolator
{
	private struct Snapshot
	{
		public float pain;

		public float hunger;

		public float thirst;

		public float sick;

		public float water;

		public float droplet;

		public float dropletAmount;

		public float cold;

		public float dirt;

		public float sad;

		public float sat;

		public float oxy;

		public float bonusAbber;
	}

	private static readonly Dictionary<int, Snapshot> cache = new Dictionary<int, Snapshot>();

	private static int lastId;

	private static bool hasLast;

	private static Snapshot Read(PlayerCamera pc)
	{
		Snapshot result = default(Snapshot);
		result.pain = pc.painSmooth;
		result.hunger = pc.hungerSmooth;
		result.thirst = pc.thirstSmooth;
		result.sick = pc.sickSmooth;
		result.water = pc.waterSmooth;
		result.droplet = pc.dropletSmooth;
		result.dropletAmount = pc.dropletAmount;
		result.cold = pc.coldSmooth;
		result.dirt = pc.dirtSmooth;
		result.sad = pc.sadSmooth;
		result.sat = pc.satSmooth;
		result.oxy = pc.oxySmooth;
		result.bonusAbber = pc.bonusAbber;
		return result;
	}

	private static void Write(PlayerCamera pc, Snapshot s)
	{
		pc.painSmooth = s.pain;
		pc.hungerSmooth = s.hunger;
		pc.thirstSmooth = s.thirst;
		pc.sickSmooth = s.sick;
		pc.waterSmooth = s.water;
		pc.dropletSmooth = s.droplet;
		pc.dropletAmount = s.dropletAmount;
		pc.coldSmooth = s.cold;
		pc.dirtSmooth = s.dirt;
		pc.sadSmooth = s.sad;
		pc.satSmooth = s.sat;
		pc.oxySmooth = s.oxy;
		pc.bonusAbber = s.bonusAbber;
	}

	private static void Prefix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		Body body = __instance.body;
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		DualMode instance = DualMode.Instance;
		bool flag = !((Object)(object)instance == (Object)null) && instance.Active;
		int instanceID = ((Object)body).GetInstanceID();
		if (flag && hasLast && lastId != 0 && lastId != instanceID)
		{
			cache[lastId] = Read(__instance);
			if (cache.TryGetValue(instanceID, out var value))
			{
				Write(__instance, value);
			}
			else
			{
				Snapshot snapshot = default(Snapshot);
				snapshot.sad = 100f;
				Write(__instance, snapshot);
			}
		}
		lastId = instanceID;
		hasLast = true;
	}

	private static void Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		Body body = __instance.body;
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		DualMode instance = DualMode.Instance;
		if ((Object)(object)instance == (Object)null || !instance.Active)
		{
			return;
		}
		cache[((Object)body).GetInstanceID()] = Read(__instance);
		if (body != instance.Player)
		{
			return;
		}
		try
		{
			if ((Object)(object)__instance.mainScreenPass != (Object)null)
			{
				__instance.mainScreenPass.SetFloat("_InsanityFX", 0f);
			}
			if ((Object)(object)__instance.blurPass != (Object)null)
			{
				__instance.blurPass.SetFloat("_Insanity", 0f);
			}
			if ((Object)(object)__instance.bonusPass != (Object)null)
			{
				__instance.bonusPass.SetFloat("_Distort", 0f);
			}
		}
		catch
		{
		}
	}

	public static void ResetRun()
	{
		cache.Clear();
		lastId = 0;
		hasLast = false;
	}
}
