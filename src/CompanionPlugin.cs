using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

[BepInPlugin("com.yg.casualties.companion", "CU Companion 陪伴", "2.1.0")]
public class CompanionPlugin : BaseUnityPlugin
{
	public const string Guid = "com.yg.casualties.companion";

	/// <summary>界面上显示的版本号，改完能一眼看出跑的是哪一版。</summary>
	public const string VersionTag = "v2.1";

	public static CompanionPlugin Instance;

	public static ManualLogSource Log;

	public static ModConfig Cfg;

	public static PersonaStore Store;

	public static AppSettings App;

	private void Awake()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Instance = this;
		Log = Logger;
		Application.runInBackground = true;
		App = StorageManager.Load<AppSettings>(StorageManager.AppPath) ?? new AppSettings();
		LocalizationManager.Current = LocalizationManager.Parse(App.lang);
		Reload();
		try
		{
			new Harmony("com.yg.casualties.companion").PatchAll(typeof(CompanionPlugin).Assembly);
			Log.LogInfo((object)"[CompanionPlugin] Harmony runtime patches injected successfully.");
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("[CompanionPlugin] Harmony patch exception: " + ex.Message));
		}
		GameObject val = new GameObject("CUCompanionHost");
		Object.DontDestroyOnLoad((Object)val);
		((Object)val).hideFlags = (HideFlags)61;
		val.AddComponent<CompanionHost>();
		val.AddComponent<LegacyCompanionUi>();
		val.AddComponent<NativeHudController>();
		val.AddComponent<NativePanel>();
		val.AddComponent<SettingsEditorGui>();
		val.AddComponent<HealthMonitorGui>();
		val.AddComponent<IntimateController>();
		val.AddComponent<DanmakuBridge>();
		val.AddComponent<DualModeController>();
		val.AddComponent<CUCompanion.P2AI.P2BrainHub>();
		Log.LogInfo((object)("[CompanionPlugin] Initialized successfully. Config directory: " + StorageManager.ConfigDir));
	}

	public static void SwitchLanguage(LangId id)
	{
		try
		{
			SaveAll();
			ApiSettings apiSettings = ((Cfg.api == null) ? null : Cfg.api.Clone());
			App.lang = LocalizationManager.Code(id);
			StorageManager.Save(StorageManager.AppPath, App);
			LocalizationManager.Current = id;
			Reload();
			if (Cfg != null)
			{
				if (Cfg.prompt == null) Cfg.prompt = new PromptConfig();
				Cfg.prompt.languagePrompt = PromptConfig.GetDefaultLanguagePrompt(id);
			}
			Loc.RefreshAll();
			if (apiSettings != null && string.IsNullOrEmpty(Cfg.api.apiKey) && !string.IsNullOrEmpty(apiSettings.apiKey))
			{
				Cfg.api = apiSettings;
				SaveAll();
			}
			Log.LogInfo((object)("[CompanionPlugin] Language switched to " + LocalizationManager.NativeName(id) + " (" + LocalizationManager.Code(id) + ")"));
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("[CompanionPlugin] Language switch failed: " + ex.Message));
		}
	}

	public static void Reload()
	{
		Cfg = StorageManager.Load<ModConfig>(StorageManager.SettingsPath);
		if (Cfg == null && File.Exists(StorageManager.LegacySettingsPath))
		{
			Cfg = StorageManager.Load<ModConfig>(StorageManager.LegacySettingsPath);
			if (Cfg != null)
			{
				Log.LogInfo((object)("[CompanionPlugin] Migrated config from legacy language path: " + Path.GetFileName(StorageManager.LegacySettingsPath)));
			}
		}
		if (Cfg == null)
		{
			Cfg = new ModConfig();
		}
		if (Cfg.dualVersion < 3)
		{
			Cfg.dualVersion = 3;
			Log.LogInfo((object)"[CompanionPlugin] Dual mode config migrated to version 3.");
		}
		if (Cfg.api == null)
		{
			Cfg.api = new ApiSettings();
		}
		Cfg.api.maxTokens = Mathf.Clamp((Cfg.api.maxTokens <= 0) ? 300 : Cfg.api.maxTokens, 60, 4000);
		if (string.IsNullOrEmpty(Cfg.api.apiKey))
		{
			try
			{
				string path = Path.Combine(StorageManager.ConfigDir, "settings.json");
				if (File.Exists(path))
				{
					ModConfig modConfig = StorageManager.Load<ModConfig>(path);
					if (modConfig != null && modConfig.api != null && !string.IsNullOrEmpty(modConfig.api.apiKey))
					{
						Cfg.api = modConfig.api;
						Log.LogInfo((object)"[CompanionPlugin] Inherited API configuration from old settings.json");
					}
				}
			}
			catch
			{
			}
		}
		if (Cfg.reactions == null || Cfg.reactions.Count == 0)
		{
			Cfg.reactions = ModConfig.DefaultReactions();
		}
		if (Cfg.monitorParts == null)
		{
			Cfg.monitorParts = ModConfig.DefaultMonitorParts();
		}
		foreach (MonitorPart monitorPart in Cfg.monitorParts)
		{
			if (monitorPart != null && string.IsNullOrEmpty(monitorPart.id))
			{
				monitorPart.id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
			}
		}
		if (Cfg.monitorIntervalSec < 5f)
		{
			Cfg.monitorIntervalSec = 20f;
		}
		if (Cfg.prompt == null)
		{
			Cfg.prompt = new PromptConfig();
		}
		foreach (ReactionRule item in ModConfig.DefaultReactions())
		{
			bool flag = false;
			foreach (ReactionRule reaction in Cfg.reactions)
			{
				if (reaction != null && reaction.key == item.key)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Cfg.reactions.Add(item);
			}
		}
		foreach (ReactionRule reaction2 in Cfg.reactions)
		{
			if (reaction2 != null)
			{
				if (reaction2.key == "event" || reaction2.key == "injury" || reaction2.key == "farewell" || reaction2.key == "hurt")
				{
					reaction2.useBudget = false;
				}
			}
		}
		Store = PersonaStore.Load();
		AdoptGameNames();
		SaveAll();
	}

	public static void AdoptGameNames()
	{
		if (Store == null || Store.slots == null)
		{
			return;
		}
		string[] array = new string[4] { Loc.T("阿岚"), Loc.T("上等兵 K"), Loc.T("上等兵K"), Loc.T("灰") };
		for (int i = 0; i < Store.slots.Length; i++)
		{
			Persona persona = Store.slots[i];
			if (persona == null)
			{
				continue;
			}
			bool flag = string.IsNullOrEmpty(persona.name);
			for (int j = 0; j < array.Length; j++)
			{
				if (persona.name == array[j])
				{
					flag = true;
				}
			}
			if (!flag)
			{
				continue;
			}
			string text = null;
			try
			{
				text = Locale.GetOther("char" + i + "name");
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = NativeMenuButton.CleanCharName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					persona.name = text2;
				}
			}
		}
	}

	public static void SaveAll()
	{
		if (Cfg != null)
		{
			StorageManager.Save(StorageManager.SettingsPath, Cfg);
		}
		if (Store != null)
		{
			Store.Save();
		}
	}

	private void OnApplicationQuit()
	{
		SaveAll();
	}
}
