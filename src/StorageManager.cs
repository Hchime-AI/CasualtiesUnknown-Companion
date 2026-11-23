using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace CUCompanion;

public static class StorageManager
{
	public static string Dir
	{
		get
		{
			string text = Path.Combine(Paths.ConfigPath, "CUCompanion");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}
	}

	public static string ConfigDir => Dir;

	public static string SettingsPath => Path.Combine(Dir, "settings.json");

	public static string LegacySettingsPath => Path.Combine(Dir, "settings." + LocalizationManager.Code(LocalizationManager.Current) + ".json");

	public static string PersonaPath => Path.Combine(Dir, "personas." + LocalizationManager.Code(LocalizationManager.Current) + ".json");

	public static string AppPath => Path.Combine(Dir, "app.json");

	public static string HistoryDir
	{
		get
		{
			string text = Path.Combine(Dir, "history");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}
	}

	public static string HistoryPath => Path.Combine(HistoryDir, "current.json");

	public static string ArchiveDir
	{
		get
		{
			string text = Path.Combine(HistoryDir, "archive");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}
	}

	public static T Load<T>(string path) where T : class
	{
		T val = LoadRaw<T>(path);
		if (val != null)
		{
			return val;
		}
		if (!File.Exists(path))
		{
			return null;
		}
		string text = path + ".bak";
		T val2 = LoadRaw<T>(text);
		if (val2 != null)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log == null)
			{
				return val2;
			}
			log.LogWarning((object)("[StorageManager] Primary config corrupted; restored from backup: " + text));
		}
		return val2;
	}

	private static T LoadRaw<T>(string path) where T : class
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string text = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(text);
		}
		catch (Exception ex)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)$"[StorageManager] Failed to read {path}: {ex.Message}");
			}
			return null;
		}
	}

	public static void Save(string path, object obj)
	{
		string text = path + ".tmp";
		try
		{
			string contents = JsonConvert.SerializeObject(obj, (Formatting)1);
			File.WriteAllText(text, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			if (File.Exists(path))
			{
				try
				{
					File.Replace(text, path, path + ".bak", ignoreMetadataErrors: true);
					return;
				}
				catch
				{
					try
					{
						File.Copy(path, path + ".bak", overwrite: true);
					}
					catch
					{
					}
					File.Delete(path);
					File.Move(text, path);
					return;
				}
			}
			File.Move(text, path);
		}
		catch (Exception ex)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)$"[StorageManager] Save failed for {path}: {ex.Message}");
			}
			try
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}
}
