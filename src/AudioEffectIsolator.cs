using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public static class AudioEffectIsolator
{
	[HarmonyPatch(typeof(Body), "HandleCirculation")]
	internal static class BodyCirculationPatch
	{
		[HarmonyPrefix]
		private static void Prefix(Body __instance)
		{
			ActiveCirculationBody = __instance;
		}

		[HarmonyPostfix]
		private static void Postfix()
		{
			ActiveCirculationBody = null;
		}
	}

	[HarmonyPatch(typeof(Vomiter), "Vomit")]
	internal static class VomiterVomitPatch
	{
		[HarmonyPrefix]
		private static void Prefix(Vomiter __instance)
		{
			ActiveVomiter = __instance;
		}

		[HarmonyPostfix]
		private static void Postfix()
		{
			ActiveVomiter = null;
		}
	}

	[HarmonyPatch(typeof(Vomiter), "VomitBlood")]
	internal static class VomiterVomitBloodPatch
	{
		[HarmonyPrefix]
		private static void Prefix(Vomiter __instance)
		{
			ActiveVomiter = __instance;
		}

		[HarmonyPostfix]
		private static void Postfix()
		{
			ActiveVomiter = null;
		}
	}

	[HarmonyPatch(typeof(PantSound), "Update")]
	internal static class PantSoundIsolationPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(PantSound __instance)
		{
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main == (Object)null || (Object)(object)main.body == (Object)null || (Object)(object)__instance == (Object)null)
			{
				return true;
			}
			Body component = ((Component)__instance).GetComponent<Body>();
			if ((Object)(object)component == (Object)null)
			{
				return true;
			}
			if (component != main.body)
			{
				AudioSource component2 = ((Component)__instance).GetComponent<AudioSource>();
				if ((Object)(object)component2 != (Object)null && component2.volume > 0f)
				{
					component2.volume = 0f;
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Sound), "Play", new Type[]
	{
		typeof(string),
		typeof(Vector2),
		typeof(bool),
		typeof(bool),
		typeof(Transform),
		typeof(float),
		typeof(float),
		typeof(bool),
		typeof(bool)
	})]
	internal static class SoundPlayStringIsolationPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(string clip)
		{
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main == (Object)null || (Object)(object)main.body == (Object)null)
			{
				return true;
			}
			if ((Object)(object)ActiveCirculationBody != (Object)null && ActiveCirculationBody != main.body && clip != null && clip.StartsWith("heartthump", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if ((Object)(object)ActiveVomiter != (Object)null)
			{
				Body component = ((Component)ActiveVomiter).GetComponent<Body>();
				if ((Object)(object)component != (Object)null && component != main.body && clip != null && (clip.Contains("vomitwarning") || clip.Contains("bloodvomitwarning")))
				{
					return false;
				}
			}
			return true;
		}
	}

	public static Body ActiveCirculationBody;

	public static Vomiter ActiveVomiter;
}
