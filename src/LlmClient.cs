using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CUCompanion;

public static class LlmClient
{
	private static HttpClient httpClient;

	public static Action<int, int> OnUsage;

	private static readonly char[] SentenceEnd = new char[10] { '。', '！', '？', '…', '”', '」', '）', '.', '!', '?' };

	private static readonly char[] CompleteEnd = new char[9] { '。', '！', '？', '”', '」', '）', '.', '!', '?' };

	private static HttpClient Http
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected O, but got Unknown
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Expected O, but got Unknown
			if (httpClient == null)
			{
				httpClient = new HttpClient((HttpMessageHandler)new HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = (HttpRequestMessage a, X509Certificate2 b, X509Chain c, SslPolicyErrors d) => true
				})
				{
					Timeout = TimeSpan.FromSeconds(120.0)
				};
			}
			return httpClient;
		}
	}

	public static bool HasKey(ApiSettings s)
	{
		if (s != null && !string.IsNullOrEmpty(s.apiKey))
		{
			return !string.IsNullOrEmpty(s.baseUrl);
		}
		return false;
	}

	public static async Task<string> ChatAsync(ApiSettings s, List<ChatMsg> messages, CancellationToken outer)
	{
		if (!HasKey(s))
		{
			return null;
		}
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
		cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, s.timeoutSec)));
		try
		{
			if (s.provider == "anthropic")
			{
				return await AnthropicAsync(s, messages, cts.Token);
			}
			return await OpenAiAsync(s, messages, cts.Token);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch (Exception ex2)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[LlmClient] Request error: " + ex2.Message));
			}
			return null;
		}
	}

	private static StringContent Json(object o)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		return new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
	}

	private static void ApplyExtraHeaders(HttpRequestMessage req, ApiSettings s)
	{
		if (string.IsNullOrEmpty(s.extraHeaders))
		{
			return;
		}
		string[] array = s.extraHeaders.Split(';');
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num > 0)
			{
				string text2 = text.Substring(0, num).Trim();
				string text3 = text.Substring(num + 1).Trim();
				if (text2.Length > 0)
				{
					((HttpHeaders)req.Headers).TryAddWithoutValidation(text2, text3);
				}
			}
		}
	}

	private static async Task<string> OpenAiAsync(ApiSettings s, List<ChatMsg> messages, CancellationToken ct)
	{
		JArray val = new JArray();
		foreach (ChatMsg message in messages)
		{
			val.Add((JToken)new JObject
			{
				["role"] = (JToken)message.role,
				["content"] = (JToken)message.content
			});
		}
		JObject o = new JObject
		{
			["model"] = (JToken)s.model,
			["messages"] = (JToken)val,
			["temperature"] = (JToken)s.temperature,
			["max_tokens"] = (JToken)s.maxTokens,
			["stream"] = (JToken)false
		};
		HttpRequestMessage val2 = new HttpRequestMessage(HttpMethod.Post, s.baseUrl);
		((HttpHeaders)val2.Headers).TryAddWithoutValidation("Authorization", "Bearer " + s.apiKey);
		ApplyExtraHeaders(val2, s);
		val2.Content = (HttpContent)Json(o);
		HttpResponseMessage resp = await ((HttpMessageInvoker)Http).SendAsync(val2, ct);
		string text = await resp.Content.ReadAsStringAsync();
		if (!resp.IsSuccessStatusCode)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[LlmClient] HTTP " + (int)resp.StatusCode + " " + Trim(text, 400)));
			}
			return null;
		}
		try
		{
			JObject val3 = JObject.Parse(text);
			try
			{
				JToken val4 = val3["usage"];
				if (val4 != null && OnUsage != null)
				{
					int arg = ((val4[(object)"prompt_tokens"] != null) ? Extensions.Value<int>((IEnumerable<JToken>)val4[(object)"prompt_tokens"]) : 0);
					int arg2 = ((val4[(object)"completion_tokens"] != null) ? Extensions.Value<int>((IEnumerable<JToken>)val4[(object)"completion_tokens"]) : 0);
					OnUsage(arg, arg2);
				}
			}
			catch
			{
			}
			JToken val5 = ((val3["choices"] != null) ? val3["choices"][(object)0][(object)"message"][(object)"content"] : null);
			if (val5 != null)
			{
				string text2 = Clean(((object)val5).ToString());
				if (string.IsNullOrEmpty(text2))
				{
					JToken val6 = val3["choices"][(object)0][(object)"message"][(object)"reasoning_content"];
					if (val6 != null && !string.IsNullOrEmpty(((object)val6).ToString()))
					{
						ManualLogSource log2 = CompanionPlugin.Log;
						if (log2 != null)
						{
							log2.LogWarning((object)"[LlmClient] Empty content while reasoning_content is present (try increasing maxTokens or using a non-thinking model).");
						}
					}
				}
				return text2;
			}
			JToken val7 = val3["error"];
			if (val7 != null)
			{
				ManualLogSource log3 = CompanionPlugin.Log;
				if (log3 != null)
				{
					log3.LogWarning((object)("[LlmClient] API error: " + Trim(((object)val7).ToString(), 300)));
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log4 = CompanionPlugin.Log;
			if (log4 != null)
			{
				log4.LogWarning((object)("[LlmClient] Parse exception: " + ex.Message));
			}
		}
		return null;
	}

	private static async Task<string> AnthropicAsync(ApiSettings s, List<ChatMsg> messages, CancellationToken ct)
	{
		string text = "";
		JArray val = new JArray();
		foreach (ChatMsg message in messages)
		{
			if (message.role == "system")
			{
				text = text + message.content + "\n";
				continue;
			}
			val.Add((JToken)new JObject
			{
				["role"] = (JToken)message.role,
				["content"] = (JToken)message.content
			});
		}
		JObject o = new JObject
		{
			["model"] = (JToken)s.model,
			["max_tokens"] = (JToken)s.maxTokens,
			["temperature"] = (JToken)s.temperature,
			["system"] = (JToken)text.Trim(),
			["messages"] = (JToken)val
		};
		HttpRequestMessage val2 = new HttpRequestMessage(HttpMethod.Post, s.baseUrl);
		((HttpHeaders)val2.Headers).TryAddWithoutValidation("x-api-key", s.apiKey);
		((HttpHeaders)val2.Headers).TryAddWithoutValidation("anthropic-version", "2023-06-01");
		ApplyExtraHeaders(val2, s);
		val2.Content = (HttpContent)Json(o);
		HttpResponseMessage resp = await ((HttpMessageInvoker)Http).SendAsync(val2, ct);
		string text2 = await resp.Content.ReadAsStringAsync();
		if (!resp.IsSuccessStatusCode)
		{
			ManualLogSource log = CompanionPlugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("[LlmClient] HTTP " + (int)resp.StatusCode + " " + Trim(text2, 400)));
			}
			return null;
		}
		try
		{
			JToken val3 = JObject.Parse(text2)["content"];
			JArray val4 = (JArray)((val3 is JArray) ? val3 : null);
			if (val4 != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (JToken item in val4)
				{
					if ((string)item[(object)"type"] == "text")
					{
						stringBuilder.Append(((object)item[(object)"text"]).ToString());
					}
				}
				if (stringBuilder.Length > 0)
				{
					return Clean(stringBuilder.ToString());
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = CompanionPlugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("[LlmClient] Parse exception: " + ex.Message));
			}
		}
		return null;
	}

	private static string Trim(string s, int n)
	{
		if (s == null)
		{
			return "";
		}
		s = s.Replace("\n", " ");
		if (s.Length > n)
		{
			return s.Substring(0, n);
		}
		return s;
	}

	private static string StripThink(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		while (true)
		{
			int num = s.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
			if (num < 0)
			{
				break;
			}
			int num2 = s.IndexOf("</think>", num + 7, StringComparison.OrdinalIgnoreCase);
			if (num2 >= 0)
			{
				s = s.Remove(num, num2 + 8 - num);
				continue;
			}
			s = s.Substring(0, num);
			break;
		}
		return s;
	}

	public static string CleanRaw(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		s = StripThink(s);
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		s = s.Trim();
		s = s.Replace("“", "\"").Replace("”", "\"");
		if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length > 2)
		{
			s = s.Substring(1, s.Length - 2);
		}
		s = s.Replace("**", "").Replace("*", "");
		if (s.StartsWith("角色:") || s.StartsWith("角色："))
		{
			s = s.Substring(3);
		}
		else if (s.StartsWith("Character:", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(10);
		}
		s = s.Trim();
		if (s.Length != 0)
		{
			return s;
		}
		return null;
	}

	private static int LineLimit()
	{
		try
		{
			ModConfig cfg = CompanionPlugin.Cfg;
			if (cfg != null && cfg.lineMaxChars > 0)
			{
				return Math.Max(40, Math.Min(1000, cfg.lineMaxChars));
			}
		}
		catch
		{
		}
		return 160;
	}

	public static string Clean(string s)
	{
		string text = CleanRaw(s);
		if (text == null)
		{
			return null;
		}
		int num = LineLimit();
		if (text.Length > num)
		{
			text = CutAtSentence(text, num);
		}
		if (text.Length != 0)
		{
			return text;
		}
		return null;
	}

	public static string DropUnfinished(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string[] array = text.Replace("\r", "").Split('\n');
		int num = -1;
		for (int num2 = array.Length - 1; num2 >= 0; num2--)
		{
			if (array[num2].Trim().Length > 0)
			{
				num = num2;
				break;
			}
		}
		if (num < 0)
		{
			return text;
		}
		string text2 = array[num].Trim();
		bool flag = Count(text2, '（') != Count(text2, '）') || Count(text2, '(') != Count(text2, ')');
		char c = text2[text2.Length - 1];
		bool flag2 = Array.IndexOf(CompleteEnd, c) >= 0 || c == ')' || c == '）';
		if (!flag && flag2)
		{
			return text.TrimEnd();
		}
		if (num == 0)
		{
			if (flag)
			{
				int num3 = text2.LastIndexOf('（');
				int num4 = text2.LastIndexOf('）');
				if (num3 > num4 && num3 > 0)
				{
					string text3 = text2.Substring(0, num3).TrimEnd();
					if (text3.Length >= 4)
					{
						return text3;
					}
				}
				int num5 = text2.LastIndexOf('(');
				int num6 = text2.LastIndexOf(')');
				if (num5 > num6 && num5 > 0)
				{
					string text4 = text2.Substring(0, num5).TrimEnd();
					if (text4.Length >= 4)
					{
						return text4;
					}
				}
			}
			return CutAtSentence(text2, Math.Max(1, text2.Length - 1));
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < num; i++)
		{
			if (array[i].Trim().Length > 0)
			{
				stringBuilder.AppendLine(array[i].Trim());
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static int Count(string s, char c)
	{
		int num = 0;
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] == c)
			{
				num++;
			}
		}
		return num;
	}

	public static string CutAtSentence(string s, int maxChars)
	{
		if (string.IsNullOrEmpty(s) || maxChars <= 0)
		{
			return s;
		}
		if (s.Length <= maxChars)
		{
			return s;
		}
		int num = maxChars;
		while (num > maxChars / 2 && num > 0 && Array.IndexOf(SentenceEnd, s[num - 1]) < 0)
		{
			num--;
		}
		if (num <= maxChars / 2)
		{
			num = maxChars;
		}
		return s.Substring(0, num).TrimEnd();
	}

	public static string FitBudget(string text, int maxChars)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (maxChars <= 0)
		{
			maxChars = 500;
		}
		string[] array = text.Replace("\r", "").Split('\n');
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text2 = array2[i].Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			int num2 = text2.Length + 1;
			if (num + num2 <= maxChars)
			{
				stringBuilder.AppendLine(text2);
				num += num2;
				continue;
			}
			int num3 = maxChars - num;
			if (num3 >= 12)
			{
				stringBuilder.AppendLine(CutAtSentence(text2, num3));
			}
			break;
		}
		string text3 = stringBuilder.ToString().TrimEnd();
		if (text3.Length != 0)
		{
			return text3;
		}
		return CutAtSentence(text.Trim(), maxChars);
	}

	public static string StripParens(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		int num = 0;
		foreach (char c in s)
		{
			switch (c)
			{
			case '(':
			case '（':
				num++;
				break;
			case ')':
			case '）':
				if (num > 0)
				{
					num--;
				}
				break;
			default:
				if (num == 0)
				{
					stringBuilder.Append(c);
				}
				break;
			}
		}
		string text = stringBuilder.ToString();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		text = text.Trim();
		while (text.Length > 0 && (text[0] == '，' || text[0] == ',' || text[0] == '、' || text[0] == '；' || text[0] == ';'))
		{
			text = text.Substring(1).TrimStart();
		}
		return text;
	}
}
