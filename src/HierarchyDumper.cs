using System;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class HierarchyDumper : MonoBehaviour
{
	public static HierarchyDumper Instance;

	private float nextDumpAt;

	private const float DumpInterval = 30f;

	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg != null && cfg.debug && Time.unscaledTime >= nextDumpAt)
		{
			nextDumpAt = Time.unscaledTime + 30f;
			DumpTextMeshProHierarchy();
		}
	}

	public void DumpTextMeshProHierarchy()
	{
		try
		{
			TextMeshProUGUI[] array = Object.FindObjectsOfType<TextMeshProUGUI>();
			if (array == null || array.Length == 0)
			{
				return;
			}
			int num = Math.Min(array.Length, 30);
			for (int i = 0; i < num; i++)
			{
				TextMeshProUGUI val = array[i];
				if (!((Object)(object)val == (Object)null))
				{
					string arg = BuildHierarchyPath(((TMP_Text)val).transform);
					string text = (((TMP_Text)val).text ?? "").Replace("\n", " ");
					if (text.Length > 25)
					{
						text = text.Substring(0, 25);
					}
					ManualLogSource log = CompanionPlugin.Log;
					if (log != null)
					{
						log.LogInfo((object)$"[HierarchyDumper] '{text}' -> {arg}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = CompanionPlugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("[HierarchyDumper] Scan error: " + ex.Message));
			}
		}
	}

	private static string BuildHierarchyPath(Transform t)
	{
		if ((Object)(object)t == (Object)null)
		{
			return "";
		}
		string text = ((Object)t).name;
		while ((Object)(object)t.parent != (Object)null)
		{
			t = t.parent;
			text = ((Object)t).name + "/" + text;
		}
		return text;
	}
}
