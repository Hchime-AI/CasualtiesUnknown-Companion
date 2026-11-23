using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CUCompanion;

public static class Llm
{
	public static Action<int, int> OnUsage
	{
		get
		{
			return LlmClient.OnUsage;
		}
		set
		{
			LlmClient.OnUsage = value;
		}
	}

	public static bool HasKey(ApiSettings s)
	{
		return LlmClient.HasKey(s);
	}

	public static Task<string> ChatAsync(ApiSettings s, List<ChatMsg> messages, CancellationToken outer)
	{
		return LlmClient.ChatAsync(s, messages, outer);
	}

	public static string Clean(string s)
	{
		return LlmClient.Clean(s);
	}

	public static string CleanRaw(string s)
	{
		return LlmClient.CleanRaw(s);
	}

	public static string DropUnfinished(string text)
	{
		return LlmClient.DropUnfinished(text);
	}

	public static string CutAtSentence(string s, int maxChars)
	{
		return LlmClient.CutAtSentence(s, maxChars);
	}

	public static string FitBudget(string text, int maxChars)
	{
		return LlmClient.FitBudget(text, maxChars);
	}

	public static string StripParens(string s)
	{
		return LlmClient.StripParens(s);
	}
}
