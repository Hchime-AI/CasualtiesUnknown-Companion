using System;
using System.Globalization;
using System.IO;
using BepInEx;
using UnityEngine;

namespace CUCompanion;

public static class DualAssets
{
	private const string Tag = "[DualAssets] ";

	private const string ProfileName = "player_profile.txt";

	private static string dir;

	public static string Dir
	{
		get
		{
			if (dir != null)
			{
				return dir;
			}
			string path;
			try
			{
				path = Paths.ConfigPath;
			}
			catch
			{
				path = Path.Combine(Environment.CurrentDirectory, "BepInEx", "config");
			}
			dir = Path.Combine(Path.Combine(path, "CUCompanion"), "player_assets");
			try
			{
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
			}
			catch
			{
			}
			return dir;
		}
	}

	private static void Log(string msg)
	{
		try
		{
			Debug.Log((object)("[DualAssets] " + msg));
		}
		catch
		{
		}
	}

	public static bool LoadProfile(out float speed, out float jump, out int skill)
	{
		speed = 0f;
		jump = 0f;
		skill = 0;
		try
		{
			string path = Path.Combine(Dir, "player_profile.txt");
			if (!File.Exists(path))
			{
				return false;
			}
			string[] array = File.ReadAllLines(path);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length != 2)
				{
					continue;
				}
				string text = array2[0].Trim();
				if (float.TryParse(array2[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					switch (text)
					{
					case "speed":
						speed = result;
						break;
					case "jump":
						jump = result;
						break;
					case "skill":
						skill = Mathf.RoundToInt(result);
						break;
					}
				}
			}
			return speed > 0f;
		}
		catch (Exception ex)
		{
			Log("Failed to load player profile: " + ex.Message);
			return false;
		}
	}

	public static void SaveProfile(float speed, float jump, int skill)
	{
		try
		{
			string text = Path.Combine(Dir, "player_profile.txt");
			string contents = "# CUCompanion: Base performance profile for P1Alloy" + Environment.NewLine + "speed=" + speed.ToString("R", CultureInfo.InvariantCulture) + Environment.NewLine + "jump=" + jump.ToString("R", CultureInfo.InvariantCulture) + Environment.NewLine + "skill=" + skill + Environment.NewLine;
			File.WriteAllText(text, contents);
			Log("P1Alloy profile written to: " + text);
		}
		catch (Exception ex)
		{
			Log("Failed to save player profile: " + ex.Message);
		}
	}
}
