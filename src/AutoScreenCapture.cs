using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class AutoScreenCapture : MonoBehaviour
{
	public static AutoScreenCapture Instance;

	private float nextSnapAt;

	private float nextPruneAt;

	private const int MaxKeptShots = 60;

	private const float SnapInterval = 12f;

	private const float PruneInterval = 120f;

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
		if (cfg != null && cfg.debug)
		{
			float unscaledTime = Time.unscaledTime;
			if (unscaledTime >= nextSnapAt)
			{
				nextSnapAt = unscaledTime + 12f;
				CaptureTimestamped("auto");
			}
			if (unscaledTime >= nextPruneAt)
			{
				nextPruneAt = unscaledTime + 120f;
				PruneOldScreenshots(60);
			}
		}
	}

	public void CaptureTimestamped(string tag)
	{
		try
		{
			string text = Path.Combine(Paths.ConfigPath, "CUCompanion", "shots");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string arg = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
			string path = $"{tag}_{arg}.png";
			ScreenCapture.CaptureScreenshot(Path.Combine(text, path));
		}
		catch (Exception ex)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[AutoScreenCapture] Screenshot failed: " + ex.Message));
			}
		}
	}

	public static void PruneOldScreenshots(int retainCount)
	{
		try
		{
			string path = Path.Combine(Paths.ConfigPath, "CUCompanion", "shots");
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "*.png");
			if (files.Length <= retainCount)
			{
				return;
			}
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			int num = files.Length - retainCount;
			for (int i = 0; i < num; i++)
			{
				try
				{
					File.Delete(files[i]);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}
}
