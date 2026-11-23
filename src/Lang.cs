namespace CUCompanion;

public static class Lang
{
	public static LangId[] All => LocalizationManager.All;

	public static LangId Current
	{
		get
		{
			return LocalizationManager.Current;
		}
		set
		{
			LocalizationManager.Current = value;
		}
	}

	public static string Code(LangId id)
	{
		return LocalizationManager.Code(id);
	}

	public static LangId Parse(string code)
	{
		return LocalizationManager.Parse(code);
	}

	public static string NativeName(LangId id)
	{
		return LocalizationManager.NativeName(id);
	}

	public static string T(string key)
	{
		return LocalizationManager.T(key);
	}

	public static void DefaultPersona(int slot, out string callPlayer, out string[] interactions, out string personality, out string appearance, out string relationship, out string traits)
	{
		LocalizationManager.DefaultPersona(slot, out callPlayer, out interactions, out personality, out appearance, out relationship, out traits);
	}
}
