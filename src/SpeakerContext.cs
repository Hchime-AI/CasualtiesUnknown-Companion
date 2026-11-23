using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CUCompanion
{
	/// <summary>
	/// 游戏取台词表时默认按"当前被操控那具躯体"的 charType 查（Locale.GetCharacter(str) 内部读 PlayerCamera.main.body）。
	/// 陪伴体说话时它是另一具躯体，于是会读到玩家的物种台词。
	/// 这里在说话期间把正在说话的躯体钉住，让 GetCharacter 按它的 charType 查表。
	/// </summary>
	internal static class SpeakerContext
	{
		public static Body speaker;
		public static int scope;

		public static int Resolve()
		{
			if (scope > 0 && (UnityEngine.Object)speaker != (UnityEngine.Object)null)
			{
				return Mathf.Clamp(speaker.charType, 0, 2);
			}
			PlayerCamera main = PlayerCamera.main;
			if ((UnityEngine.Object)main != (UnityEngine.Object)null && (UnityEngine.Object)main.body != (UnityEngine.Object)null)
			{
				return Mathf.Clamp(main.body.charType, 0, 2);
			}
			return 0;
		}

		public static Pin Enter(Body body)
		{
			Pin result = new Pin(body);
			return result;
		}

		public struct Pin : IDisposable
		{
			private readonly Body previous;
			private readonly int previousScope;
			private readonly bool run;

			public Pin(Body body)
			{
				previous = speaker;
				previousScope = scope;
				run = (UnityEngine.Object)body != (UnityEngine.Object)null;
				if (run)
				{
					speaker = body;
					scope = previousScope + 1;
				}
			}

			public void Dispose()
			{
				if (run)
				{
					speaker = previous;
					scope = previousScope;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Locale), "GetCharacter", new Type[] { typeof(string) })]
	internal static class Locale_GetCharacter_Speaker_Patch
	{
		private static bool Prefix(string str, ref List<string> __result)
		{
			if (SpeakerContext.scope <= 0)
			{
				return true;
			}
			try
			{
				__result = Locale.GetCharacter(str, SpeakerContext.Resolve());
			}
			catch
			{
				return true;
			}
			return false;
		}
	}
}
