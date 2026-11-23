namespace CUCompanion;

public static class Storage
{
	public static string Dir => StorageManager.Dir;

	public static string SettingsPath => StorageManager.SettingsPath;

	public static string LegacySettingsPath => StorageManager.LegacySettingsPath;

	public static string PersonaPath => StorageManager.PersonaPath;

	public static string AppPath => StorageManager.AppPath;

	public static string HistoryDir => StorageManager.HistoryDir;

	public static string HistoryPath => StorageManager.HistoryPath;

	public static string ArchiveDir => StorageManager.ArchiveDir;

	public static T Load<T>(string path) where T : class
	{
		return StorageManager.Load<T>(path);
	}

	public static void Save(string path, object obj)
	{
		StorageManager.Save(path, obj);
	}
}
